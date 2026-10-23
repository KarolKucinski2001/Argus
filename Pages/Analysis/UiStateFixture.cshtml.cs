using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Pages.Analysis;

public sealed record FixtureResultState(string Heading, string? Note, InsightCardModel Card);

[AllowAnonymous]
public sealed class UiStateFixtureModel(IWebHostEnvironment environment) : PageModel
{
    public IReadOnlyList<FixtureResultState> ResultStates { get; } = BuildResultStates();

    public bool ShowRankedGrid { get; private set; }

    public IReadOnlyList<AssetAnalysis> RankedResults { get; } = BuildRankedResults();

    public IReadOnlyList<InsightCardModel> RankedCards =>
        RankedResults.Select((analysis, index) => Card(analysis, index, RankedSummary.AverageScore)).ToArray();

    private AnalysisSummaryStripView? rankedSummary;

    public AnalysisSummaryStripView RankedSummary => rankedSummary ??= AnalysisSummaryStripPresenter.Build(RankedResults);

    public IReadOnlyList<AssetOptionModel> RankedOptions =>
        RankedResults
            .Select(analysis => new AssetOptionModel(analysis.Asset.Id, true, analysis.Score))
            .Concat(
            [
                new AssetOptionModel("NVDA", false, null),
                new AssetOptionModel("TSLA", false, null)
            ])
            .ToArray();

    public IActionResult OnGet(string? view)
    {
        if (!string.Equals(environment.EnvironmentName, "Testing", StringComparison.Ordinal))
        {
            return NotFound();
        }

        ShowRankedGrid = string.Equals(view, "ranked", StringComparison.OrdinalIgnoreCase);
        return Page();
    }

    private static IReadOnlyList<AssetAnalysis> BuildRankedResults()
    {
        var now = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        var current = new MarketDataFreshness(now, now, now.AddMinutes(5), false);
        var generated = AnalysisSummaryResult.Generated("Wynik oparty na wyświetlonych danych.");

        return
        [
            Sample("AAPL", 78m, 1.2m, 4.8m, 9.6m, [210m, 212m, 209m, 214m, 216m, 215.5m], MarketDataQuality.Complete, current, generated),
            Sample("MSFT", 66m, 0.4m, 1.6m, 3.4m, [412m, 415m, 414m, 418m, 419m, 420.25m], MarketDataQuality.Complete, current, generated),
            Sample("BTC-USD", 52m, -0.8m, -0.6m, 2.1m, [66000m, 67200m, 66800m, 67500m, 66900m, 67100m], MarketDataQuality.Complete, current, generated),
            Sample("ETH-USD", 31m, -1.5m, -3.9m, -6.2m, [3720m, 3690m, 3650m, 3610m, 3580m, 3540m], MarketDataQuality.Complete, current, generated)
        ];
    }

    private static IReadOnlyList<FixtureResultState> BuildResultStates()
    {
        var now = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        var current = new MarketDataFreshness(now, now, now.AddMinutes(5), false);
        var stale = new MarketDataFreshness(now.AddHours(-8), now, null, true);
        var generated = AnalysisSummaryResult.Generated("Wynik oparty na wyświetlonych danych.");

        return
        [
            new("Wynik gotowy", null, Card(
                Sample("AAPL", 82m, 1.2m, 4.8m, 9.6m, [210m, 212m, 209m, 214m, 216m, 215.5m], MarketDataQuality.Complete, current, generated),
                0)),
            new("Częściowy wynik", "Częściowe wyniki — część aktywów nie ma pełnych danych.", Card(
                Sample("MSFT", 58m, null, 2.1m, 3.4m, [418m, 421m, 419m, 420.25m], MarketDataQuality.Incomplete, current, generated),
                1)),
            new("Nieaktualne dane", null, Card(
                Sample("BTC-USD", 64m, -0.8m, 3.5m, 12.2m, [64000m, 66500m, 65200m, 68000m], MarketDataQuality.Complete, stale, generated),
                2)),
            new("Niewystarczające dane", null, Card(
                Sample("ETH-USD", null, null, null, -2.4m, [3500m], MarketDataQuality.Incomplete, current, AnalysisSummaryResult.InsufficientEvidence()),
                3)),
            new("Opóźnione dane", null, Card(
                Sample("NVDA", 71m, 0.6m, 3.9m, 8.1m, [118m, 121m, 119m, 124m, 126m], MarketDataQuality.Complete, current, generated, isDelayed: true),
                4)),
            new("Błąd", null, FailedCard())
        ];
    }

    private static InsightCardModel FailedCard()
    {
        var now = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        var error = new MarketDataError(MarketDataErrorCategory.Timeout, "Fixture timeout.", true);
        var analysis = Sample(
            "TSLA", null, null, null, null, [], MarketDataQuality.Incomplete,
            new MarketDataFreshness(now, now, null, false),
            AnalysisSummaryResult.Unavailable(),
            error: error);

        return new InsightCardModel(
            analysis,
            5,
            null,
            IndexModel.MapAssetErrorMessage(error.Category),
            IndexModel.MapSummaryStatusMessage(AnalysisSummaryStatus.Unavailable));
    }

    private static InsightCardModel Card(AssetAnalysis analysis, int index, decimal? averageScore = null) =>
        new(
            analysis,
            index,
            QuoteFor(analysis),
            null,
            IndexModel.MapSummaryStatusMessage(analysis.SummaryResult?.Status ?? AnalysisSummaryStatus.Unavailable),
            averageScore);

    private static MarketQuote? QuoteFor(AssetAnalysis analysis) =>
        analysis.ChartPoints.Count == 0
            ? null
            : new MarketQuote(
                analysis.ChartPoints[^1].Close,
                "USD",
                analysis.Freshness.ObservedAt,
                analysis.Freshness,
                analysis.Quality);

    private static AssetAnalysis Sample(
        string id,
        decimal? score,
        decimal? change24Hours,
        decimal? change7Days,
        decimal? change30Days,
        decimal[] closes,
        MarketDataQuality quality,
        MarketDataFreshness freshness,
        AnalysisSummaryResult summary,
        bool isDelayed = false,
        MarketDataError? error = null)
    {
        var points = closes
            .Select((close, index) => new AnalysisChartPoint(freshness.ObservedAt.AddDays(index - closes.Length), close))
            .ToArray();
        var performance = new AssetPerformance(
            Metric("24h", change24Hours),
            Metric("7d", change7Days),
            Metric("30d", change30Days),
            points);
        AnalysisFactor[] factors =
        [
            Factor("momentum-24h", "24-hour momentum", change24Hours),
            Factor("momentum-7d", "7-day momentum", change7Days),
            Factor("trend-30d", "30-day trend", change30Days)
        ];

        return new AssetAnalysis(
            new AssetReference(id, id, id.EndsWith("-USD", StringComparison.Ordinal) ? MarketAssetClass.Crypto : MarketAssetClass.Equity),
            score,
            performance,
            factors,
            points,
            summary.Text,
            summary,
            quality,
            freshness,
            isDelayed,
            [],
            error);
    }

    private static PerformanceMetric Metric(string period, decimal? percentage) =>
        new(period, percentage, percentage.HasValue);

    private static AnalysisFactor Factor(string key, string label, decimal? percentage) =>
        new(
            key,
            label,
            percentage,
            percentage.HasValue ? 0.33m : 0m,
            percentage.HasValue ? 20m : null,
            percentage.HasValue,
            $"{label} (fixture).");
}
