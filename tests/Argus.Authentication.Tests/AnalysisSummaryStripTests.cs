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

    private static AssetAnalysis Result(string id, decimal? score, decimal? change7Days, bool failed = false)
    {
        var now = new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);
        var sevenDays = new PerformanceMetric("7d", change7Days, change7Days.HasValue);
        var missing = new PerformanceMetric("24h", null, false);

        return new AssetAnalysis(
            new AssetReference(id, id, MarketAssetClass.Equity),
            score,
            new AssetPerformance(missing, sevenDays, missing with { Period = "30d" }, []),
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
