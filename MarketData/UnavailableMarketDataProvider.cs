namespace _bootstrap_scaffold.MarketData;

public sealed class UnavailableMarketDataProvider : IMarketDataProvider
{
    public Task<IReadOnlyList<MarketDataResult>> GetAsync(
        MarketDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return Task.FromResult<IReadOnlyList<MarketDataResult>>(request.Assets.Select(asset =>
            new MarketDataResult(
                asset,
                request.History,
                null,
                [],
                null,
                new MarketDataFreshness(now, now, null, true),
                MarketDataQuality.Unavailable,
                [],
                new MarketDataError(
                    MarketDataErrorCategory.NotConfigured,
                    "No market data provider is configured.",
                    false))).ToArray());
    }
}
