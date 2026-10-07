using System.Collections.Concurrent;

namespace _bootstrap_scaffold.MarketData;

public sealed class InMemoryMarketDataCache : IMarketDataCache
{
    private readonly ConcurrentDictionary<MarketDataCacheKey, MarketDataCacheEntry> entries = new();

    public ValueTask<MarketDataCacheEntry?> GetAsync(
        MarketDataCacheKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        entries.TryGetValue(key, out var entry);
        return ValueTask.FromResult(entry);
    }

    public ValueTask SetAsync(
        MarketDataCacheEntry entry,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        entries[entry.Key] = entry;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(
        MarketDataCacheKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        entries.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }
}
