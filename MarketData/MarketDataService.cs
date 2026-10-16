using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace _bootstrap_scaffold.MarketData;

public sealed class MarketDataService(
    IMarketDataProvider provider,
    IMarketDataCache cache,
    IMarketDataRequestBudget requestBudget,
    IOptions<MarketDataOptions> configuredOptions,
    IHostEnvironment environment) : IMarketDataService
{
    public async Task<MarketDataServiceResult> GetAsync(
        MarketDataRequest? request,
        CancellationToken cancellationToken = default)
    {
        var options = configuredOptions.Value;
        var validationError = ValidateRequest(request, options);
        if (validationError is not null)
        {
            return MarketDataServiceResult.Failure(validationError);
        }

        if (!(IsDevelopmentOrTesting(environment) && provider is DevelopmentMarketDataProvider) &&
            (!options.PublicDisplayApproved || string.IsNullOrWhiteSpace(options.ProviderId)))
        {
            return MarketDataServiceResult.Failure(new MarketDataServiceError(
                StatusCodes.Status503ServiceUnavailable,
                "Market data service unavailable",
                "Market data remains disabled until an approved provider and public-display terms are configured.",
                MarketDataErrorCategory.ProviderNotApproved,
                false));
        }

        if (provider is UnavailableMarketDataProvider)
        {
            return MarketDataServiceResult.Failure(new MarketDataServiceError(
                StatusCodes.Status503ServiceUnavailable,
                "Market data provider unavailable",
                "An approved provider is not registered for market-data access.",
                MarketDataErrorCategory.NotConfigured,
                false));
        }

        var validRequest = NormalizeRequest(request!, options);
        if (!requestBudget.TryAcquire(options.ApplicationRequestBudget, TimeSpan.FromMinutes(1)))
        {
            return MarketDataServiceResult.Failure(new MarketDataServiceError(
                StatusCodes.Status429TooManyRequests,
                "Market data request budget exceeded",
                "The application request budget has been exhausted for the current window.",
                MarketDataErrorCategory.QuotaExceeded,
                true));
        }

        var results = new List<MarketDataResult>();
        var uncachedAssets = new List<AssetReference>();
        var expiredEntries = new Dictionary<string, MarketDataResult>(StringComparer.OrdinalIgnoreCase);

        foreach (var asset in validRequest.Assets)
        {
            var key = CreateCacheKey(asset, validRequest.History);
            var cached = await cache.GetAsync(key, cancellationToken);
            if (cached is null)
            {
                uncachedAssets.Add(asset);
            }
            else if (!cached.IsExpired)
            {
                results.Add(cached.Result);
            }
            else
            {
                expiredEntries[asset.Id] = AddWarning(
                    cached.Result,
                    new MarketDataWarning(
                        MarketDataWarningCategory.StaleData,
                        "Cached evidence has expired and is being refreshed.",
                        true));
                uncachedAssets.Add(asset);
            }
        }

        if (uncachedAssets.Count > 0)
        {
            var providerResults = await GetProviderResultsAsync(
                validRequest,
                uncachedAssets,
                options,
                cancellationToken);

            foreach (var result in providerResults)
            {
                if (result.Error is null && !IsValidProviderResult(result, validRequest))
                {
                    results.Add(ErrorResult(
                        result.Asset,
                        validRequest.History,
                        new MarketDataError(
                            MarketDataErrorCategory.InvalidProviderResponse,
                            "The market data provider returned data that does not match the request.",
                            false)));
                    continue;
                }

                if (result.Error is null)
                {
                    await cache.SetAsync(
                        new MarketDataCacheEntry(
                            CreateCacheKey(result.Asset, validRequest.History),
                            result,
                            DateTimeOffset.UtcNow,
                            DateTimeOffset.UtcNow.Add(options.CacheDuration)),
                        cancellationToken);
                    results.Add(result);
                }
                else if (expiredEntries.TryGetValue(result.Asset.Id, out var expired))
                {
                    results.Add(AddWarning(
                        expired with { Error = result.Error },
                        new MarketDataWarning(
                            MarketDataWarningCategory.UpstreamUnavailable,
                            "Fresh evidence was unavailable; the expired cached evidence is shown.",
                            true)));
                }
                else
                {
                    results.Add(result);
                }
            }
        }

        var orderedResults = validRequest.Assets.Select(asset =>
            results.FirstOrDefault(result =>
                string.Equals(result.Asset.Id, asset.Id, StringComparison.OrdinalIgnoreCase))
            ?? (expiredEntries.TryGetValue(asset.Id, out var expired)
                ? AddWarning(
                    expired with
                    {
                        Error = new MarketDataError(
                            MarketDataErrorCategory.UpstreamUnavailable,
                            "The market data provider returned no result for this asset.",
                            true)
                    },
                    new MarketDataWarning(
                        MarketDataWarningCategory.UpstreamUnavailable,
                        "Fresh evidence was unavailable; the expired cached evidence is shown.",
                        true))
                : ErrorResult(
                    asset,
                    validRequest.History,
                    new MarketDataError(
                        MarketDataErrorCategory.UpstreamUnavailable,
                        "The market data provider returned no result for this asset.",
                        true)))).ToArray();

        return MarketDataServiceResult.Success(new MarketDataResponse(orderedResults));
    }

    private async Task<IReadOnlyList<MarketDataResult>> GetProviderResultsAsync(
        MarketDataRequest request,
        IReadOnlyList<AssetReference> uncachedAssets,
        MarketDataOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.RequestTimeout);
            return await provider.GetAsync(request with { Assets = uncachedAssets }, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return uncachedAssets.Select(asset => ErrorResult(
                asset,
                request.History,
                new MarketDataError(
                    MarketDataErrorCategory.Timeout,
                    "The market data provider timed out.",
                    true))).ToArray();
        }
        catch (MarketDataProviderException exception)
        {
            return uncachedAssets.Select(asset => ErrorResult(
                asset,
                request.History,
                new MarketDataError(
                    exception.Category,
                    exception.Message,
                    exception.IsRetryable,
                    exception.UpstreamStatusCode))).ToArray();
        }
        catch (Exception exception) when (exception is InvalidDataException or FormatException)
        {
            return uncachedAssets.Select(asset => ErrorResult(
                asset,
                request.History,
                new MarketDataError(
                    MarketDataErrorCategory.InvalidProviderResponse,
                    "The market data provider returned invalid data.",
                    false))).ToArray();
        }
        catch (HttpRequestException)
        {
            return uncachedAssets.Select(asset => ErrorResult(
                asset,
                request.History,
                new MarketDataError(
                    MarketDataErrorCategory.UpstreamUnavailable,
                    "The market data provider is unavailable.",
                    true))).ToArray();
        }
    }

    private static MarketDataServiceError? ValidateRequest(
        MarketDataRequest? request,
        MarketDataOptions options)
    {
        if (request is null || !request.IsValid)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Invalid market data request",
                "Assets and a valid historical range are required.",
                MarketDataErrorCategory.InvalidRequest);
        }

        if (request.Assets.Count > options.MaximumSelectedAssets)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Too many assets requested",
                $"A maximum of {options.MaximumSelectedAssets} assets may be requested.",
                MarketDataErrorCategory.InvalidRequest);
        }

        if (request.Assets.GroupBy(asset => asset.Id, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Duplicate asset requested",
                "Each asset may be requested only once.",
                MarketDataErrorCategory.InvalidRequest);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.History.End > today ||
            request.History.Start < request.History.End.AddMonths(-MarketDataOptions.RequiredHistoryMonths))
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "Invalid market data history range",
                $"The requested range must end today or earlier and span no more than {MarketDataOptions.RequiredHistoryMonths} months.",
                MarketDataErrorCategory.InvalidRequest);
        }

        var unsupported = request.Assets.FirstOrDefault(asset =>
            !options.AllowedAssets.TryGetValue(asset.Id, out var assetClass) ||
            assetClass != asset.AssetClass);
        return unsupported is null
            ? null
            : Failure(
                StatusCodes.Status400BadRequest,
                "Unsupported asset",
                $"Asset '{unsupported.Id}' is not in the configured market-data allowlist.",
                MarketDataErrorCategory.UnsupportedAsset);
    }

    private static MarketDataServiceError Failure(
        int statusCode,
        string title,
        string detail,
        MarketDataErrorCategory category,
        bool retryable = false) =>
        new(statusCode, title, detail, category, retryable);

    private static bool IsDevelopmentOrTesting(IHostEnvironment environment) =>
        environment.IsDevelopment() || environment.IsEnvironment("Testing");

    private static MarketDataRequest NormalizeRequest(
        MarketDataRequest request,
        MarketDataOptions options) =>
        request with
        {
            Assets = request.Assets.Select(asset =>
            {
                var configured = options.AllowedAssets.First(entry =>
                    string.Equals(entry.Key, asset.Id, StringComparison.OrdinalIgnoreCase));
                return asset with
                {
                    Id = configured.Key,
                    DisplayName = configured.Key,
                    AssetClass = configured.Value
                };
            }).ToArray()
        };

    private static MarketDataCacheKey CreateCacheKey(
        AssetReference asset,
        MarketDataHistoryRequest history) =>
        new(asset.Id, asset.AssetClass, history.Start, history.End, history.Granularity);

    private static bool IsValidProviderResult(
        MarketDataResult result,
        MarketDataRequest request)
    {
        var requestedAsset = request.Assets.FirstOrDefault(asset =>
            string.Equals(asset.Id, result.Asset.Id, StringComparison.OrdinalIgnoreCase));
        if (requestedAsset is null ||
            requestedAsset.AssetClass != result.Asset.AssetClass ||
            result.RequestedHistory != request.History)
        {
            return false;
        }

        var points = result.HistoricalPoints.OrderBy(point => point.Timestamp).ToArray();
        if (points.Any(point =>
                DateOnly.FromDateTime(point.Timestamp.UtcDateTime) < request.History.Start ||
                DateOnly.FromDateTime(point.Timestamp.UtcDateTime) > request.History.End))
        {
            return false;
        }

        return request.History.Granularity switch
        {
            MarketDataGranularity.Daily => true,
            MarketDataGranularity.Weekly => points.Zip(points.Skip(1))
                .All(pair => pair.Second.Timestamp - pair.First.Timestamp >= TimeSpan.FromDays(7)),
            MarketDataGranularity.Monthly => points.Zip(points.Skip(1))
                .All(pair => pair.Second.Timestamp.Month != pair.First.Timestamp.Month ||
                             pair.Second.Timestamp.Year != pair.First.Timestamp.Year),
            _ => false
        };
    }

    private static MarketDataResult ErrorResult(
        AssetReference asset,
        MarketDataHistoryRequest history,
        MarketDataError error)
    {
        var now = DateTimeOffset.UtcNow;
        return new MarketDataResult(
            asset,
            history,
            null,
            [],
            null,
            new MarketDataFreshness(now, now, null, true),
            MarketDataQuality.Unavailable,
            [],
            error);
    }

    private static MarketDataResult AddWarning(
        MarketDataResult result,
        MarketDataWarning warning) =>
        result with
        {
            Freshness = result.Freshness with { IsStale = true },
            Quality = result.Quality == MarketDataQuality.Complete
                ? MarketDataQuality.Stale
                : result.Quality,
            Warnings = result.Warnings.Append(warning).ToArray()
        };
}
