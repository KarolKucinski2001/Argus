namespace _bootstrap_scaffold.MarketData;

public sealed record MarketDataCacheKey(
    string AssetId,
    MarketAssetClass AssetClass,
    DateOnly Start,
    DateOnly End,
    MarketDataGranularity Granularity);

public sealed record MarketDataCacheEntry(
    MarketDataCacheKey Key,
    MarketDataResult Result,
    DateTimeOffset CachedAt,
    DateTimeOffset ExpiresAt)
{
    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;
}

public interface IMarketDataCache
{
    ValueTask<MarketDataCacheEntry?> GetAsync(
        MarketDataCacheKey key,
        CancellationToken cancellationToken = default);

    ValueTask SetAsync(
        MarketDataCacheEntry entry,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(
        MarketDataCacheKey key,
        CancellationToken cancellationToken = default);
}
