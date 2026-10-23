using Xunit;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace Argus.Authentication.Tests;

public sealed class AnalysisSummaryStripTests
{
    [Fact]
    public void Empty_results_have_zero_count_and_no_values()
    {
        var view = AnalysisSummaryStripPresenter.Build([]);

        Assert.Equal(0, view.AnalyzedCount);
        Assert.Null(view.Best);
        Assert.Null(view.Worst);
        Assert.Null(view.AverageScore);
    }

    [Fact]
    public void Ranked_results_report_best_worst_and_average()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 78m, 4.8m),
            Result("MSFT", 66m, 1.6m),
            Result("BTC-USD", 52m, -0.6m),
            Result("ETH-USD", 31m, -3.9m)
        ]);

        Assert.Equal(4, view.AnalyzedCount);
        Assert.Equal(new SummaryPerformer("Apple", 4.8m), view.Best);
        Assert.Equal(new SummaryPerformer("Ethereum", -3.9m), view.Worst);
        Assert.Equal(56.8m, view.AverageScore);
    }

    [Fact]
    public void Failed_and_unscored_results_count_but_do_not_feed_values()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 80m, 2m),
            Result("MSFT", 60m, -1m),
            Result("ETH-USD", null, null),
            Result("TSLA", null, 9m, failed: true)
        ]);

        Assert.Equal(4, view.AnalyzedCount);
        Assert.Equal(new SummaryPerformer("Apple", 2m), view.Best);
        Assert.Equal(new SummaryPerformer("Microsoft", -1m), view.Worst);
        Assert.Equal(70m, view.AverageScore);
    }

    [Fact]
    public void Single_seven_day_change_has_no_worst()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 70m, 3m),
            Result("MSFT", null, null)
        ]);

        Assert.Equal(new SummaryPerformer("Apple", 3m), view.Best);
        Assert.Null(view.Worst);
        Assert.Equal(70m, view.AverageScore);
    }

    [Fact]
    public void Ties_pick_the_first_best_and_the_last_worst()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 50m, 2m),
            Result("MSFT", 50m, 2m)
        ]);

        Assert.Equal("Apple", view.Best!.Name);
        Assert.Equal("Microsoft", view.Worst!.Name);
    }

    [Fact]
    public void Average_score_is_rounded_to_one_decimal()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 70m, 1m),
            Result("MSFT", 71m, 1m),
            Result("NVDA", 71m, 1m)
        ]);

        Assert.Equal(70.7m, view.AverageScore);
    }

    [Theory]
    [InlineData(0, "0 aktywów")]
    [InlineData(1, "1 aktywo")]
    [InlineData(2, "2 aktywa")]
    [InlineData(4, "4 aktywa")]
    [InlineData(5, "5 aktywów")]
    [InlineData(12, "12 aktywów")]
    [InlineData(14, "14 aktywów")]
    [InlineData(22, "22 aktywa")]
    [InlineData(25, "25 aktywów")]
    public void Asset_count_label_uses_polish_plurals(int count, string expected) =>
        Assert.Equal(expected, AnalysisSummaryStripPresenter.AssetCountLabel(count));

    [Fact]
    public void Strongest_trend_is_the_highest_thirty_day_rise_above_one_percent()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 60m, 1m, change30Days: 4.2m),
            Result("BTC-USD", 50m, 1m, change30Days: 9.5m),
            Result("ETH-USD", null, null, change30Days: 20m, failed: true)
        ]);

        Assert.Equal(new SummaryPerformer("Bitcoin", 9.5m), view.StrongestTrend);
    }

    [Fact]
    public void Thirty_day_moves_at_or_below_one_percent_have_no_strongest_trend()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 60m, 1m, change30Days: 1m),
            Result("MSFT", 50m, 1m, change30Days: -6m)
        ]);

        Assert.Null(view.StrongestTrend);
    }

    [Fact]
    public void Data_date_is_the_newest_observation_among_healthy_results()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 60m, 1m, observedAt: new DateTimeOffset(2026, 10, 20, 23, 30, 0, TimeSpan.FromHours(-4))),
            Result("MSFT", 50m, 1m, observedAt: new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero)),
            Result("TSLA", null, null, failed: true, observedAt: new DateTimeOffset(2026, 10, 25, 0, 0, 0, TimeSpan.Zero))
        ]);

        Assert.Equal(new DateOnly(2026, 10, 21), view.DataAsOf);
    }

    [Fact]
    public void Only_failed_results_have_no_data_date()
    {
        var view = AnalysisSummaryStripPresenter.Build([Result("AAPL", null, null, failed: true)]);

        Assert.Null(view.DataAsOf);
        Assert.Null(view.StrongestTrend);
    }

    private static AssetAnalysis Result(
        string id,
        decimal? score,
        decimal? change7Days,
        bool failed = false,
        decimal? change30Days = null,
        DateTimeOffset? observedAt = null)
    {
        var now = observedAt ?? new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);
        var sevenDays = new PerformanceMetric("7d", change7Days, change7Days.HasValue);
        var missing = new PerformanceMetric("24h", null, false);
        var thirtyDays = new PerformanceMetric("30d", change30Days, change30Days.HasValue);

        return new AssetAnalysis(
            new AssetReference(id, id, MarketAssetClass.Equity),
            score,
            new AssetPerformance(missing, sevenDays, thirtyDays, []),
            [],
            [],
            null,
            null,
            MarketDataQuality.Complete,
            new MarketDataFreshness(now, now, null, false),
            false,
            [],
            failed ? new MarketDataError(MarketDataErrorCategory.Timeout, "Test timeout.", true) : null);
    }
}
