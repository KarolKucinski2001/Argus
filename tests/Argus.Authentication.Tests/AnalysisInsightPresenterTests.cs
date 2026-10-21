using Xunit;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace Argus.Authentication.Tests;

public sealed class AnalysisInsightPresenterTests
{
    [Theory]
    [InlineData("AAPL", "Apple", "A", AssetAccent.Equity)]
    [InlineData("MSFT", "Microsoft", "M", AssetAccent.Equity)]
    [InlineData("NVDA", "NVIDIA", "N", AssetAccent.Equity)]
    [InlineData("TSLA", "Tesla", "T", AssetAccent.Equity)]
    [InlineData("BTC-USD", "Bitcoin", "₿", AssetAccent.Bitcoin)]
    [InlineData("eth-usd", "Ethereum", "Ξ", AssetAccent.Ethereum)]
    public void Known_tickers_map_to_short_name_monogram_and_accent(
        string id, string name, string monogram, AssetAccent accent)
    {
        var identity = AssetIdentityCatalog.For(id);

        Assert.Equal(name, identity.Name);
        Assert.Equal(monogram, identity.Monogram);
        Assert.Equal(accent, identity.Accent);
    }

    [Theory]
    [InlineData("momentum-24h", "Momentum 24h")]
    [InlineData("momentum-7d", "Momentum 7d")]
    [InlineData("trend-30d", "Trend 30d")]
    [InlineData("other", "Czynnik")]
    public void Factor_labels_are_polish_and_fall_back_for_unknown_keys(string key, string label) =>
        Assert.Equal(label, AnalysisInsightPresenter.FactorLabel(key));

    [Fact]
    public void Freshness_is_rendered_in_polish()
    {
        Assert.Equal("Nieaktualne", _bootstrap_scaffold.Pages.Analysis.AnalysisFormatting.Freshness(true));
        Assert.Equal("Aktualne", _bootstrap_scaffold.Pages.Analysis.AnalysisFormatting.Freshness(false));
    }

    [Fact]
    public void Unknown_ticker_falls_back_to_ticker_first_letter_and_neutral_accent()
    {
        var identity = AssetIdentityCatalog.For("xyz");

        Assert.Equal("xyz", identity.Id);
        Assert.Equal("xyz", identity.Name);
        Assert.Equal("X", identity.Monogram);
        Assert.Equal(AssetAccent.Neutral, identity.Accent);
    }

    [Theory]
    [InlineData(AssetAccent.Equity, "asset-accent--equity")]
    [InlineData(AssetAccent.Bitcoin, "asset-accent--bitcoin")]
    [InlineData(AssetAccent.Ethereum, "asset-accent--ethereum")]
    [InlineData(AssetAccent.Neutral, "asset-accent--neutral")]
    public void Accent_maps_to_a_css_class(AssetAccent accent, string expected)
    {
        Assert.Equal(expected, AssetIdentityCatalog.AccentClass(accent));
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

    [Theory]
    [InlineData(100, ScoreBand.Strong)]
    [InlineData(70, ScoreBand.Strong)]
    [InlineData(69.99, ScoreBand.Moderate)]
    [InlineData(40, ScoreBand.Moderate)]
    [InlineData(39.99, ScoreBand.Weak)]
    [InlineData(0, ScoreBand.Weak)]
    public void Score_band_uses_70_and_40_thresholds(decimal score, ScoreBand expected)
    {
        Assert.Equal(expected, AnalysisInsightPresenter.BandFor(score));
    }

    [Theory]
    [InlineData(ScoreBand.Strong, "Silny")]
    [InlineData(ScoreBand.Moderate, "Umiarkowany")]
    [InlineData(ScoreBand.Weak, "Słaby")]
    public void Score_band_has_a_polish_label(ScoreBand band, string expected)
    {
        Assert.Equal(expected, AnalysisInsightPresenter.BandLabel(band));
    }

    [Theory]
    [InlineData(1.01, FactorDirection.Rising)]
    [InlineData(1, FactorDirection.Stable)]
    [InlineData(0, FactorDirection.Stable)]
    [InlineData(-1, FactorDirection.Stable)]
    [InlineData(-1.01, FactorDirection.Falling)]
    public void Factor_direction_uses_one_percent_thresholds(decimal percentage, FactorDirection expected)
    {
        Assert.Equal(expected, AnalysisInsightPresenter.DirectionFor(percentage));
    }

    [Fact]
    public void Missing_factor_percentage_is_unavailable()
    {
        Assert.Equal(FactorDirection.Unavailable, AnalysisInsightPresenter.DirectionFor(null));
    }

    [Theory]
    [InlineData(FactorDirection.Rising, "Rosnący")]
    [InlineData(FactorDirection.Stable, "Stabilny")]
    [InlineData(FactorDirection.Falling, "Malejący")]
    [InlineData(FactorDirection.Unavailable, "n/d")]
    public void Factor_direction_has_a_polish_word(FactorDirection direction, string expected)
    {
        Assert.Equal(expected, AnalysisInsightPresenter.FactorWord(direction));
    }

    [Fact]
    public void Factor_bars_carry_direction_and_word()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", -1.1m, 0.5m, 8.2m, 10m, 20m, 15m));

        Assert.Equal(
            new[] { FactorDirection.Falling, FactorDirection.Stable, FactorDirection.Rising },
            view.FactorBars.Select(bar => bar.Direction));
        Assert.Equal(new[] { "Malejący", "Stabilny", "Rosnący" }, view.FactorBars.Select(bar => bar.Word));
    }

    [Fact]
    public void Takeaway_for_the_top_ranked_asset_names_the_strongest_rise()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", -1.1m, 3m, 8.2m, 10m, 20m, 15m), 1);

        Assert.Equal("Cena wzrosła o 8.2% w ciągu 30 dni — najwyżej oceniane aktywo w tej analizie.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_for_a_lower_rank_names_the_strongest_fall_and_the_place()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", -1.1m, -3m, -8.2m, 10m, 20m, 15m), 3);

        Assert.Equal(InsightTone.Negative, view.Tone);
        Assert.Equal("Cena spadła o 8.2% w ciągu 30 dni — miejsce 3 w tej analizie.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_for_small_moves_says_the_price_was_stable()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 0.4m, -0.2m, 0.9m, 10m, 11m), 2);

        Assert.Equal("Cena pozostała stabilna w ciągu 30 dni (+0.9%) — miejsce 2 w tej analizie.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_ties_resolve_to_the_earlier_factor()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 5m, -5m, 5m, 10m, 11m), 1);

        Assert.Equal("Cena wzrosła o 5% w ciągu 24 godzin — najwyżej oceniane aktywo w tej analizie.", view.Takeaway);
    }

    [Fact]
    public void Available_factor_without_percentage_is_an_unavailable_bar_excluded_from_takeaway()
    {
        var analysis = Analysis("AAPL", -9m, 3m, 8.2m, 10m, 20m, 15m);
        var factors = analysis.Factors.ToArray();
        factors[0] = factors[0] with { IsAvailable = true, Percentage = null };

        var view = AnalysisInsightPresenter.Present(analysis with { Factors = factors }, 2);

        Assert.False(view.FactorBars[0].IsAvailable);
        Assert.Null(view.FactorBars[0].Value);
        Assert.Equal(FactorDirection.Unavailable, view.FactorBars[0].Direction);
        Assert.Equal("n/d", view.FactorBars[0].Word);
        Assert.Equal("Cena wzrosła o 8.2% w ciągu 30 dni — miejsce 2 w tej analizie.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_without_a_rank_ends_with_a_full_stop()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, null, 5m, 10m, 11m));

        Assert.Equal("Cena wzrosła o 5% w ciągu 30 dni.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_for_an_unscored_asset_says_it_cannot_be_ranked()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, null, -2.4m, 10m) with { Score = null }, 1);

        Assert.Null(view.Band);
        Assert.Equal("Cena spadła o 2.4% w ciągu 30 dni — za mało danych, aby nadać miejsce w rankingu.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_without_factors_says_there_is_not_enough_data()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, null, null), 1);

        Assert.Equal("Za mało danych rynkowych, aby opisać zmianę ceny.", view.Takeaway);
    }

    [Fact]
    public void Scored_analysis_gets_a_band()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, 1m, 1m, 10m, 11m));

        Assert.Equal(ScoreBand.Moderate, view.Band);
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
