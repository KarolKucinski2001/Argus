using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.Extensions.Options;

namespace _bootstrap_scaffold.Watchlist;

public sealed class AzureTableWatchlistStore : IWatchlistStore
{
    private readonly WatchlistOptions _options;
    private readonly TableClient? _tableClient;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private bool _tableInitialized;

    public AzureTableWatchlistStore(IOptions<WatchlistOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        try
        {
            ValidateOptions(_options);
            _tableClient = CreateTableClient(_options);
        }
        catch (OptionsValidationException)
        {
            _tableClient = null;
        }
    }

    public async Task<WatchlistListResult> ListAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        var partitionKey = NormalizeSubjectId(subjectId);

        if (_tableClient is null)
        {
            return new WatchlistListResult(
                WatchlistListStatus.StorageFailure,
                Array.Empty<WatchlistEntry>());
        }

        try
        {
            await EnsureTableAsync(cancellationToken);

            var entries = new List<WatchlistEntry>();
            await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
                               filter: $"PartitionKey eq '{EscapeOData(partitionKey)}'",
                               cancellationToken: cancellationToken))
            {
                if (!TryGetAssetId(entity, out var assetId) ||
                    !TryGetSavedAt(entity, out var savedAtUtc))
                {
                    continue;
                }

                entries.Add(new WatchlistEntry(assetId, savedAtUtc));
            }

            return new WatchlistListResult(
                WatchlistListStatus.Succeeded,
                WatchlistPolicy.OrderEntries(entries));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RequestFailedException)
        {
            return new WatchlistListResult(
                WatchlistListStatus.StorageFailure,
                Array.Empty<WatchlistEntry>());
        }
    }

    public async Task<WatchlistSaveResult> SaveAsync(
        string subjectId,
        IEnumerable<string> assetIds,
        CancellationToken cancellationToken = default)
    {
        var partitionKey = NormalizeSubjectId(subjectId);
        var requestedIds = assetIds?.ToArray() ?? Array.Empty<string>();
        var results = new List<WatchlistSaveItemResult>(requestedIds.Length);

        for (var index = 0; index < requestedIds.Length; index++)
        {
            if (WatchlistIdentity.TryNormalizeAssetId(requestedIds[index]) is null)
            {
                results.Add(new WatchlistSaveItemResult(
                    requestedIds[index],
                    WatchlistSaveStatus.Unavailable));
            }
        }

        var validIds = WatchlistPolicy.NormalizeAssetIds(requestedIds);

        if (validIds.Count == 0)
        {
            return new WatchlistSaveResult(results);
        }

        if (_tableClient is null)
        {
            results.AddRange(validIds.Select(assetId =>
                new WatchlistSaveItemResult(assetId, WatchlistSaveStatus.StorageFailure)));
            return new WatchlistSaveResult(results);
        }

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await EnsureTableAsync(cancellationToken);
            var existing = await ReadPartitionAsync(partitionKey, cancellationToken);
            var existingKeys = existing
                .Select(entity => entity.RowKey)
                .ToHashSet(StringComparer.Ordinal);
            var existingAssetIds = existing
                .Select(entity => entity.GetString("AssetId"))
                .Where(assetId => assetId is not null)
                .Select(assetId => assetId!);
            var classifications = WatchlistPolicy.ClassifySave(
                existingAssetIds,
                validIds,
                _options.MaximumEntries);

            if (classifications.Any(item =>
                    item.Status == WatchlistSaveStatus.CapacityExceeded))
            {
                results.AddRange(classifications);
                return new WatchlistSaveResult(results);
            }

            foreach (var classification in classifications)
            {
                var assetId = classification.AssetId;
                var rowKey = WatchlistIdentity.NormalizeRowKey(assetId);
                if (existingKeys.Contains(rowKey))
                {
                    results.Add(new WatchlistSaveItemResult(
                        assetId,
                        WatchlistSaveStatus.Duplicate));
                    continue;
                }

                var entity = new TableEntity(partitionKey, rowKey)
                {
                    ["AssetId"] = assetId,
                    ["SavedAtUtc"] = DateTimeOffset.UtcNow
                };

                try
                {
                    await _tableClient.AddEntityAsync(entity, cancellationToken);
                    results.Add(new WatchlistSaveItemResult(
                        assetId,
                        WatchlistSaveStatus.Saved));
                    existingKeys.Add(rowKey);
                }
                catch (RequestFailedException exception) when (exception.Status == 409)
                {
                    results.Add(new WatchlistSaveItemResult(
                        assetId,
                        WatchlistSaveStatus.Duplicate));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    results.Add(new WatchlistSaveItemResult(
                        assetId,
                        WatchlistSaveStatus.StorageFailure));
                }
            }

            return new WatchlistSaveResult(results);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RequestFailedException)
        {
            results.AddRange(validIds.Select(assetId =>
                new WatchlistSaveItemResult(assetId, WatchlistSaveStatus.StorageFailure)));
            return new WatchlistSaveResult(results);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<WatchlistRemoveResult> RemoveAsync(
        string subjectId,
        string assetId,
        CancellationToken cancellationToken = default)
    {
        var partitionKey = NormalizeSubjectId(subjectId);
        var normalizedAssetId = WatchlistIdentity.TryNormalizeAssetId(assetId);
        if (normalizedAssetId is null)
        {
            return new WatchlistRemoveResult(
                assetId,
                WatchlistRemoveStatus.Unavailable);
        }

        if (_tableClient is null)
        {
            return new WatchlistRemoveResult(
                normalizedAssetId,
                WatchlistRemoveStatus.StorageFailure);
        }

        try
        {
            await EnsureTableAsync(cancellationToken);
            await _tableClient.DeleteEntityAsync(
                partitionKey,
                WatchlistIdentity.NormalizeRowKey(normalizedAssetId),
                ETag.All,
                cancellationToken);
            return new WatchlistRemoveResult(
                normalizedAssetId,
                WatchlistRemoveStatus.Removed);
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return new WatchlistRemoveResult(
                normalizedAssetId,
                WatchlistRemoveStatus.NotFound);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RequestFailedException)
        {
            return new WatchlistRemoveResult(
                normalizedAssetId,
                WatchlistRemoveStatus.StorageFailure);
        }
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        if (_tableInitialized)
        {
            return;
        }

        await _tableClient!.CreateIfNotExistsAsync(cancellationToken);
        _tableInitialized = true;
    }

    private async Task<IReadOnlyList<TableEntity>> ReadPartitionAsync(
        string partitionKey,
        CancellationToken cancellationToken)
    {
        var entities = new List<TableEntity>();
        await foreach (var entity in _tableClient!.QueryAsync<TableEntity>(
                           filter: $"PartitionKey eq '{EscapeOData(partitionKey)}'",
                           cancellationToken: cancellationToken))
        {
            entities.Add(entity);
        }

        return entities;
    }

    private static TableClient CreateTableClient(WatchlistOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new TableServiceClient(options.ConnectionString).GetTableClient(options.TableName!);
        }

        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint))
        {
            throw new OptionsValidationException(
                WatchlistOptions.SectionName,
                typeof(WatchlistOptions),
                new[] { "Watchlist:Endpoint must be an absolute URI." });
        }

        return new TableServiceClient(endpoint, new DefaultAzureCredential())
            .GetTableClient(options.TableName!);
    }

    private static void ValidateOptions(WatchlistOptions options)
    {
        var validationResults = options.Validate(new ValidationContext(options)).ToArray();
        if (validationResults.Length > 0)
        {
            throw new OptionsValidationException(
                WatchlistOptions.SectionName,
                typeof(WatchlistOptions),
                validationResults.SelectMany(result => result.ErrorMessage is null
                    ? Array.Empty<string>()
                    : new[] { result.ErrorMessage }));
        }
    }

    private static string NormalizeSubjectId(string subjectId)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            throw new ArgumentException("A subject identifier is required.", nameof(subjectId));
        }

        var subjectBytes = Encoding.UTF8.GetBytes(subjectId.Trim());
        return Convert.ToBase64String(subjectBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string EscapeOData(string value) => value.Replace("'", "''");

    private static bool TryGetAssetId(TableEntity entity, out string assetId)
    {
        assetId = entity.GetString("AssetId") ?? string.Empty;
        return WatchlistIdentity.TryNormalizeAssetId(assetId) is not null;
    }

    private static bool TryGetSavedAt(TableEntity entity, out DateTimeOffset savedAtUtc)
    {
        savedAtUtc = entity.GetDateTimeOffset("SavedAtUtc") ?? DateTimeOffset.MinValue;
        return savedAtUtc != DateTimeOffset.MinValue;
    }
}

public static class WatchlistIdentity
{
    public static string? TryNormalizeAssetId(string? assetId)
    {
        if (string.IsNullOrWhiteSpace(assetId))
        {
            return null;
        }

        var normalized = assetId.Trim().ToUpperInvariant();
        return normalized.Length <= 128 &&
               normalized.All(character => char.IsLetterOrDigit(character) ||
                                           character is '-' or '_' or '.')
            ? normalized
            : null;
    }

    public static string NormalizeRowKey(string assetId) =>
        TryNormalizeAssetId(assetId) ??
        throw new ArgumentException("The asset identifier is invalid.", nameof(assetId));
}
