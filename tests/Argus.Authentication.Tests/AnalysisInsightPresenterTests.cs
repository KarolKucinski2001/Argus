using Xunit;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace Argus.Authentication.Tests;

public sealed class AnalysisInsightPresenterTests
{
    [Theory]
    [InlineData("AAPL", "Apple Inc.", "A")]
    [InlineData("MSFT", "Microsoft", "M")]
    [InlineData("BTC-USD", "Bitcoin", "₿")]
    [InlineData("eth-usd", "Ethereum", "Ξ")]
    public void Known_tickers_map_to_name_and_monogram(string id, string name, string monogram)
    {
        var identity = AssetIdentityCatalog.For(id);

        Assert.Equal(name, identity.Name);
        Assert.Equal(monogram, identity.Monogram);
    }

    [Fact]
    public void Unknown_ticker_falls_back_to_ticker_and_first_letter()
    {
        var identity = AssetIdentityCatalog.For("xyz");

        Assert.Equal("xyz", identity.Id);
        Assert.Equal("xyz", identity.Name);
        Assert.Equal("X", identity.Monogram);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(20, 100)]
    [InlineData(-25, 0)]
    [InlineData(30, 100)]
    [InlineData(8.2, 71)]
    [InlineData(3, 58)]
    public void Normalize_matches_the_scorer_scale(decimal percentage, decimal expected)
    {
        Assert.Equal(expected, AnalysisInsightPresenter.Normalize(percentage));
    }

    [Fact]
    public void Insight_names_strongest_and_weakest_available_factor()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", -1.1m, 3m, 8.2m, 10m, 20m, 15m));

        Assert.Equal(
            "Najsilniejszy sygnał: Trend 30d (+8.2%); najsłabszy: Momentum 24h (-1.1%).",
            view.Insight);
    }

    [Fact]
    public void Insight_with_one_factor_names_only_that_factor()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, null, 5m, 10m, 11m));

        Assert.Equal("Trend 30d: +5%.", view.Insight);
    }

    [Fact]
    public void Insight_without_factors_says_there_is_not_enough_data()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, null, null));

        Assert.Equal("Za mało danych rynkowych, aby wskazać sygnał.", view.Insight);
    }

    [Fact]
    public void Factor_bars_follow_scorer_order_and_mark_unavailable_factors()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, 3m, 8.2m, 10m, 11m));

        Assert.Equal(new[] { "momentum-24h", "momentum-7d", "trend-30d" }, view.FactorBars.Select(bar => bar.Key));
        Assert.Equal(new[] { "Momentum 24h", "Momentum 7d", "Trend 30d" }, view.FactorBars.Select(bar => bar.Label));
        Assert.False(view.FactorBars[0].IsAvailable);
        Assert.Null(view.FactorBars[0].Value);
        Assert.Equal(58m, view.FactorBars[1].Value);
        Assert.Equal(71m, view.FactorBars[2].Value);
    }

    [Theory]
    [InlineData(3, InsightTone.Positive)]
    [InlineData(-2, InsightTone.Negative)]
    [InlineData(0, InsightTone.Neutral)]
    public void Tone_follows_the_seven_day_change(decimal sevenDay, InsightTone expected)
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, sevenDay, 1m, 10m, 11m));

        Assert.Equal(expected, view.Tone);
        Assert.Equal(sevenDay, view.SevenDayChange);
    }

    [Fact]
    public void Missing_seven_day_change_is_neutral()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, null, 1m, 10m, 11m));

        Assert.Equal(InsightTone.Neutral, view.Tone);
        Assert.Null(view.SevenDayChange);
    }

    [Fact]
    public void Sparkline_scales_closes_into_a_fixed_view_box()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, 1m, 1m, 10m, 20m, 15m));

        Assert.NotNull(view.Sparkline);
        Assert.Equal("0 0 100 32", view.Sparkline.ViewBox);
        Assert.Equal("0,30 50,2 100,16", view.Sparkline.LinePoints);
        Assert.Equal("M0,32 L0,30 L50,2 L100,16 L100,32 Z", view.Sparkline.AreaPath);
    }

    [Fact]
    public void Flat_sparkline_is_drawn_in_the_middle()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, 1m, 1m, 5m, 5m));

        Assert.Equal("0,16 100,16", view.Sparkline!.LinePoints);
    }

    [Fact]
    public void Fewer_than_two_points_produce_no_sparkline()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, 1m, 1m, 5m));

        Assert.Null(view.Sparkline);
    }

    private static AssetAnalysis Analysis(
        string id,
        decimal? change24Hours,
        decimal? change7Days,
        decimal? change30Days,
        params decimal[] closes)
    {
        var now = new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);
        var points = closes
            .Select((close, index) => new AnalysisChartPoint(now.AddDays(index - closes.Length), close))
            .ToArray();
        var performance = new AssetPerformance(
            Metric("24h", change24Hours),
            Metric("7d", change7Days),
            Metric("30d", change30Days),
            points);
        AnalysisFactor[] factors =
        [
            Factor("momentum-24h", change24Hours),
            Factor("momentum-7d", change7Days),
            Factor("trend-30d", change30Days)
        ];

        return new AssetAnalysis(
            new AssetReference(id, id, MarketAssetClass.Equity),
            60m,
            performance,
            factors,
            points,
            null,
            null,
            MarketDataQuality.Complete,
            new MarketDataFreshness(now, now, null, false),
            false,
            [],
            null);
    }

    private static PerformanceMetric Metric(string period, decimal? percentage) =>
        new(period, percentage, percentage.HasValue);

    private static AnalysisFactor Factor(string key, decimal? percentage) =>
        new(key, key, percentage, percentage.HasValue ? 0.33m : 0m, percentage.HasValue ? 10m : null, percentage.HasValue, "explanation");
}
