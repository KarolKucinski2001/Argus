using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;
using Xunit;

namespace Argus.Authentication.Tests;

public sealed class AnalysisContractTests
{
    private static readonly DateTimeOffset AsOf = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Calculate_uses_latest_history_point_at_or_before_each_period_boundary()
    {
        var calculator = new PerformanceCalculator();
        var result = CreateResult(
            120m,
            Point(AsOf.AddHours(-24).AddHours(-2), 90m),
            Point(AsOf.AddHours(-24), 100m),
            Point(AsOf.AddHours(-24).AddMinutes(1), 110m),
            Point(AsOf.AddDays(-7).AddHours(-1), 80m),
            Point(AsOf.AddDays(-7), 96m),
            Point(AsOf.AddDays(-7).AddMinutes(1), 110m),
            Point(AsOf.AddDays(-30).AddHours(-1), 70m),
            Point(AsOf.AddDays(-30), 80m),
            Point(AsOf.AddDays(-30).AddMinutes(1), 110m));

        var performance = calculator.Calculate(result, AsOf);

        Assert.Equal(20m, performance.Change24Hours.Percentage);
        Assert.True(performance.Change24Hours.IsAvailable);
        Assert.Equal(25m, performance.Change7Days.Percentage);
        Assert.True(performance.Change7Days.IsAvailable);
        Assert.Equal(50m, performance.Change30Days.Percentage);
        Assert.True(performance.Change30Days.IsAvailable);
    }

    [Fact]
    public void Calculate_returns_percentage_change_from_historical_close_to_current_quote()
    {
        var calculator = new PerformanceCalculator();
        var result = CreateResult(
            120m,
            Point(AsOf.AddDays(-30), 80m));

        var performance = calculator.Calculate(result, AsOf);

        Assert.Equal(50m, performance.Change24Hours.Percentage);
        Assert.Equal(50m, performance.Change7Days.Percentage);
        Assert.Equal(50m, performance.Change30Days.Percentage);
    }

    [Fact]
    public void Calculate_marks_period_unavailable_when_no_eligible_point_exists()
    {
        var calculator = new PerformanceCalculator();
        var result = CreateResult(
            120m,
            Point(AsOf.AddDays(-30).AddMinutes(1), 80m));

        var performance = calculator.Calculate(result, AsOf);

        Assert.Null(performance.Change30Days.Percentage);
        Assert.False(performance.Change30Days.IsAvailable);
    }

    [Fact]
    public void Calculate_projects_chart_points_in_timestamp_order()
    {
        var calculator = new PerformanceCalculator();
        var latest = Point(AsOf.AddDays(-1), 110m);
        var earliest = Point(AsOf.AddDays(-3), 90m);
        var middle = Point(AsOf.AddDays(-2), 100m);
        var result = CreateResult(120m, latest, earliest, middle);

        var performance = calculator.Calculate(result, AsOf);

        Assert.Collection(
            performance.ChartPoints,
            point =>
            {
                Assert.Equal(earliest.Timestamp, point.Timestamp);
                Assert.Equal(90m, point.Close);
            },
            point =>
            {
                Assert.Equal(middle.Timestamp, point.Timestamp);
                Assert.Equal(100m, point.Close);
            },
            point =>
            {
                Assert.Equal(latest.Timestamp, point.Timestamp);
                Assert.Equal(110m, point.Close);
            });
    }

    [Fact]
    public void Calculate_does_not_use_points_after_a_period_boundary()
    {
        var calculator = new PerformanceCalculator();
        var result = CreateResult(
            120m,
            Point(AsOf.AddDays(-7).AddMinutes(-1), 100m),
            Point(AsOf.AddDays(-7).AddMinutes(1), 60m));

        var performance = calculator.Calculate(result, AsOf);

        Assert.Equal(20m, performance.Change7Days.Percentage);
    }

    private static MarketDataResult CreateResult(decimal currentQuote, params HistoricalPricePoint[] points)
    {
        var history = new MarketDataHistoryRequest(
            DateOnly.FromDateTime(AsOf.AddDays(-30).Date),
            DateOnly.FromDateTime(AsOf.Date),
            MarketDataGranularity.Daily);
        var freshness = new MarketDataFreshness(AsOf, AsOf, AsOf.AddMinutes(5), false);

        return new MarketDataResult(
            new AssetReference("AAPL", "Apple", MarketAssetClass.Equity),
            history,
            new MarketQuote(currentQuote, "USD", AsOf, freshness, MarketDataQuality.Complete),
            points,
            new MarketDataSource("test", "Test provider", false, null),
            freshness,
            MarketDataQuality.Complete,
            []);
    }

    private static HistoricalPricePoint Point(DateTimeOffset timestamp, decimal close) =>
        new(timestamp, null, null, null, close, null, "USD", true);
}
