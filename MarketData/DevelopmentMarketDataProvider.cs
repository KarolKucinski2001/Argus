namespace _bootstrap_scaffold.MarketData;

public sealed class DevelopmentMarketDataProvider : IMarketDataProvider
{
    public Task<IReadOnlyList<MarketDataResult>> GetAsync(
        MarketDataRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = request.Assets.Select(asset => CreateResult(asset, request.History)).ToArray();
        return Task.FromResult<IReadOnlyList<MarketDataResult>>(results);
    }

    private static MarketDataResult CreateResult(
        AssetReference asset,
        MarketDataHistoryRequest history)
    {
        var observedAt = new DateTimeOffset(history.End.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var retrievedAt = observedAt.AddMinutes(1);
        var freshness = new MarketDataFreshness(
            observedAt,
            retrievedAt,
            retrievedAt.AddMinutes(5),
            false);
        var baseValue = asset.Id switch
        {
            "AAPL" => 215.50m,
            "MSFT" => 420.25m,
            "BTC-USD" => 68000m,
            "ETH-USD" => 3500m,
            _ => 100m
        };
        var points = Enumerable.Range(0, history.End.DayNumber - history.Start.DayNumber + 1)
            .Select(offset =>
            {
                var date = history.Start.AddDays(offset);
                var timestamp = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                var close = baseValue + (offset % 7 - 3) * 0.25m;
                return new HistoricalPricePoint(
                    timestamp,
                    close - 1m,
                    close + 1m,
                    close - 2m,
                    close,
                    null,
                    "USD",
                    true);
            })
            .ToArray();

        return new MarketDataResult(
            asset,
            history,
            new MarketQuote(baseValue, "USD", observedAt, freshness, MarketDataQuality.Complete),
            points,
            new MarketDataSource(
                "illustrative-development",
                "Illustrative data for local development and testing only.",
                false,
                null),
            freshness,
            MarketDataQuality.Complete,
            []);
    }
}
