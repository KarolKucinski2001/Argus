using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Options;
using _bootstrap_scaffold.Watchlist;

namespace _bootstrap_scaffold.Strategies;

public sealed record SimulatedPortfolio(
    string Id,
    string ProfileId,
    string ProfileName,
    string Name,
    DateOnly AsOf,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<SimulatedHolding> Holdings);

public enum SimulatedPortfolioStatus
{
    Succeeded,
    NotFound,
    CapacityExceeded,
    StorageFailure
}

public sealed record SimulatedPortfolioListResult(
    SimulatedPortfolioStatus Status,
    IReadOnlyList<SimulatedPortfolio> Portfolios)
{
    public bool Succeeded => Status == SimulatedPortfolioStatus.Succeeded;
}

public static class SimulatedPortfolioConstants
{
    public const int MaximumPortfolios = 10;

    public static bool IsValidId(string? id) =>
        !string.IsNullOrEmpty(id) && id.Length == 32 && id.All(Uri.IsHexDigit);

    public static IReadOnlyList<SimulatedPortfolio> Order(IEnumerable<SimulatedPortfolio> portfolios) =>
        portfolios
            .OrderByDescending(portfolio => portfolio.CreatedAtUtc)
            .ThenBy(portfolio => portfolio.Id, StringComparer.Ordinal)
            .ToArray();
}

public sealed class SimulatedPortfolioOptions
{
    public const string SectionName = "SimulatedPortfolios";

    [Required]
    [RegularExpression("^[A-Za-z][A-Za-z0-9]{2,62}$")]
    public string? TableName { get; set; } = "ArgusSimulatedPortfolios";
}

public interface ISimulatedPortfolioStore
{
    Task<SimulatedPortfolioListResult> ListAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<SimulatedPortfolioStatus> CreateAsync(
        string subjectId,
        SimulatedPortfolio portfolio,
        CancellationToken cancellationToken = default);

    Task<SimulatedPortfolioStatus> DeleteAsync(
        string subjectId,
        string portfolioId,
        CancellationToken cancellationToken = default);
}

// Reuses the watchlist storage account settings; only the table differs.
public sealed class AzureTableSimulatedPortfolioStore : ISimulatedPortfolioStore
{
    private readonly TableClient? _tableClient;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private bool _tableInitialized;

    public AzureTableSimulatedPortfolioStore(
        IOptions<SimulatedPortfolioOptions> options,
        IOptions<WatchlistOptions> storage)
    {
        var tableName = options.Value.TableName;
        var account = storage.Value;
        try
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(account.ConnectionString))
            {
                _tableClient = new TableServiceClient(account.ConnectionString).GetTableClient(tableName);
            }
            else if (Uri.TryCreate(account.Endpoint, UriKind.Absolute, out var endpoint))
            {
                _tableClient = new TableServiceClient(endpoint, new DefaultAzureCredential())
                    .GetTableClient(tableName);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or UriFormatException)
        {
            _tableClient = null;
        }
    }

    public async Task<SimulatedPortfolioListResult> ListAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        var partitionKey = NormalizeSubjectId(subjectId);
        if (_tableClient is null)
        {
            return new SimulatedPortfolioListResult(SimulatedPortfolioStatus.StorageFailure, []);
        }

        try
        {
            await EnsureTableAsync(cancellationToken);
            var portfolios = new List<SimulatedPortfolio>();
            foreach (var entity in await ReadPartitionAsync(partitionKey, cancellationToken))
            {
                if (TryRead(entity) is { } portfolio)
                {
                    portfolios.Add(portfolio);
                }
            }

            return new SimulatedPortfolioListResult(
                SimulatedPortfolioStatus.Succeeded,
                SimulatedPortfolioConstants.Order(portfolios));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RequestFailedException)
        {
            return new SimulatedPortfolioListResult(SimulatedPortfolioStatus.StorageFailure, []);
        }
    }

    public async Task<SimulatedPortfolioStatus> CreateAsync(
        string subjectId,
        SimulatedPortfolio portfolio,
        CancellationToken cancellationToken = default)
    {
        var partitionKey = NormalizeSubjectId(subjectId);
        if (_tableClient is null || !SimulatedPortfolioConstants.IsValidId(portfolio.Id))
        {
            return SimulatedPortfolioStatus.StorageFailure;
        }

        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            await EnsureTableAsync(cancellationToken);
            var existing = await ReadPartitionAsync(partitionKey, cancellationToken);
            if (existing.Count >= SimulatedPortfolioConstants.MaximumPortfolios)
            {
                return SimulatedPortfolioStatus.CapacityExceeded;
            }

            var entity = new TableEntity(partitionKey, portfolio.Id)
            {
                ["ProfileId"] = portfolio.ProfileId,
                ["ProfileName"] = portfolio.ProfileName,
                ["Name"] = portfolio.Name,
                ["AsOf"] = portfolio.AsOf.ToString("yyyy-MM-dd"),
                ["CreatedAtUtc"] = portfolio.CreatedAtUtc,
                ["HoldingsJson"] = JsonSerializer.Serialize(portfolio.Holdings)
            };
            await _tableClient.AddEntityAsync(entity, cancellationToken);
            return SimulatedPortfolioStatus.Succeeded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RequestFailedException)
        {
            return SimulatedPortfolioStatus.StorageFailure;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<SimulatedPortfolioStatus> DeleteAsync(
        string subjectId,
        string portfolioId,
        CancellationToken cancellationToken = default)
    {
        var partitionKey = NormalizeSubjectId(subjectId);
        if (!SimulatedPortfolioConstants.IsValidId(portfolioId))
        {
            return SimulatedPortfolioStatus.NotFound;
        }

        if (_tableClient is null)
        {
            return SimulatedPortfolioStatus.StorageFailure;
        }

        try
        {
            await EnsureTableAsync(cancellationToken);
            await _tableClient.DeleteEntityAsync(partitionKey, portfolioId, ETag.All, cancellationToken);
            return SimulatedPortfolioStatus.Succeeded;
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return SimulatedPortfolioStatus.NotFound;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RequestFailedException)
        {
            return SimulatedPortfolioStatus.StorageFailure;
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
                           filter: $"PartitionKey eq '{partitionKey.Replace("'", "''")}'",
                           cancellationToken: cancellationToken))
        {
            entities.Add(entity);
        }

        return entities;
    }

    private static SimulatedPortfolio? TryRead(TableEntity entity)
    {
        try
        {
            var holdings = JsonSerializer.Deserialize<SimulatedHolding[]>(
                entity.GetString("HoldingsJson") ?? string.Empty);
            if (holdings is null ||
                !DateOnly.TryParse(entity.GetString("AsOf"), out var asOf) ||
                entity.GetDateTimeOffset("CreatedAtUtc") is not { } createdAt)
            {
                return null;
            }

            return new SimulatedPortfolio(
                entity.RowKey,
                entity.GetString("ProfileId") ?? string.Empty,
                entity.GetString("ProfileName") ?? string.Empty,
                entity.GetString("Name") ?? string.Empty,
                asOf,
                createdAt,
                holdings);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string NormalizeSubjectId(string subjectId)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            throw new ArgumentException("A subject identifier is required.", nameof(subjectId));
        }

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(subjectId.Trim()))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
