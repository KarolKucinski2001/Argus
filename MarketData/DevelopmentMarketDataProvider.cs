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
            ["ETH-USD"] = new(3500m, -0.0004, 0.10, 21, 0.6, 53),
            ["NVDA"] = new(120m, 0.0009, 0.06, 40, 0.3, 61),
            ["GOOGL"] = new(170m, 0.0004, 0.03, 35, 1.8, 67),
            ["AMZN"] = new(185m, 0.0005, 0.035, 28, 2.9, 71),
            ["META"] = new(500m, 0.0007, 0.04, 33, 0.9, 73),
            ["TSLA"] = new(240m, -0.0002, 0.09, 26, 1.5, 79),
            ["JPM"] = new(200m, 0.0003, 0.02, 50, 2.1, 83),
            ["V"] = new(275m, 0.0002, 0.015, 45, 0.4, 89),
            ["KO"] = new(62m, 0.0001, 0.012, 55, 1.1, 97),
            ["SPY"] = new(540m, 0.0004, 0.015, 40, 0.7, 101),
            ["QQQ"] = new(470m, 0.0005, 0.02, 38, 1.3, 103),
            ["SOL-USD"] = new(150m, 0.0010, 0.12, 24, 2.0, 107),
            ["XRP-USD"] = new(0.55m, -0.0003, 0.11, 30, 0.2, 109),
            ["ADA-USD"] = new(0.45m, -0.0005, 0.10, 27, 2.6, 113)
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
