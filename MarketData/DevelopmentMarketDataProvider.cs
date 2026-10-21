namespace _bootstrap_scaffold.MarketData;

public sealed class DevelopmentMarketDataProvider : IMarketDataProvider
{
    private static readonly SeriesShape DefaultShape = new(100m, 0.0002, 0.02, 30, 0.0, 7);

    // Illustrative, deterministic shapes so each development asset looks different.
    private static readonly IReadOnlyDictionary<string, SeriesShape> Shapes =
        new Dictionary<string, SeriesShape>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new(215.50m, 0.0006, 0.03, 45, 0.0, 11),
            ["MSFT"] = new(420.25m, 0.0003, 0.02, 30, 1.2, 23),
            ["BTC-USD"] = new(68000m, 0.0012, 0.08, 60, 2.4, 37),
            ["ETH-USD"] = new(3500m, -0.0004, 0.10, 21, 0.6, 53)
        };

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
        var shape = Shapes.GetValueOrDefault(asset.Id, DefaultShape);
        var dayCount = Math.Max(history.End.DayNumber - history.Start.DayNumber + 1, 0);
        var closes = GenerateCloses(shape, dayCount);
        var points = closes
            .Select((close, offset) =>
            {
                var date = history.Start.AddDays(offset);
                var timestamp = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                return new HistoricalPricePoint(
                    timestamp,
                    close,
                    Round(close * 1.01m),
                    Round(close * 0.99m),
                    close,
                    null,
                    "USD",
                    true);
            })
            .ToArray();
        var quoteValue = closes.Length > 0 ? closes[^1] : shape.BaseValue;

        return new MarketDataResult(
            asset,
            history,
            new MarketQuote(quoteValue, "USD", observedAt, freshness, MarketDataQuality.Complete),
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

    private static decimal[] GenerateCloses(SeriesShape shape, int count)
    {
        var random = new Random(shape.Seed);
        var closes = new decimal[count];
        for (var offset = 0; offset < count; offset++)
        {
            var wave = shape.Amplitude * Math.Sin(2 * Math.PI * offset / shape.PeriodDays + shape.Phase);
            var noise = (random.NextDouble() - 0.5) * shape.Amplitude * 0.4;
            var factor = 1 + shape.TrendPerDay * offset + wave + noise;
            closes[offset] = Round(shape.BaseValue * (decimal)factor);
        }

        return closes;
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record SeriesShape(
        decimal BaseValue,
        double TrendPerDay,
        double Amplitude,
        double PeriodDays,
        double Phase,
        int Seed);
}
