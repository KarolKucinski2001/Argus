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

    public IActionResult OnGet() =>
        string.Equals(environment.EnvironmentName, "Testing", StringComparison.Ordinal)
            ? Page()
            : NotFound();

    private static IReadOnlyList<FixtureResultState> BuildResultStates()
    {
        var now = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        var current = new MarketDataFreshness(now, now, now.AddMinutes(5), false);
        var stale = new MarketDataFreshness(now.AddHours(-8), now, null, true);
        var generated = AnalysisSummaryResult.Generated("Wynik oparty na wyświetlonych danych.");

        return
        [
            new("Completed", null, Card(
                Sample("AAPL", 82m, 1.2m, 4.8m, 9.6m, [210m, 212m, 209m, 214m, 216m, 215.5m], MarketDataQuality.Complete, current, generated),
                0)),
            new("Partial result", "Częściowe wyniki — część aktywów nie ma pełnych danych.", Card(
                Sample("MSFT", 58m, null, 2.1m, 3.4m, [418m, 421m, 419m, 420.25m], MarketDataQuality.Incomplete, current, generated),
                1)),
            new("Stale data", null, Card(
                Sample("BTC-USD", 64m, -0.8m, 3.5m, 12.2m, [64000m, 66500m, 65200m, 68000m], MarketDataQuality.Complete, stale, generated),
                2)),
            new("Insufficient evidence", null, Card(
                Sample("ETH-USD", null, null, null, -2.4m, [3500m], MarketDataQuality.Incomplete, current, AnalysisSummaryResult.InsufficientEvidence()),
                3))
        ];
    }

    private static InsightCardModel Card(AssetAnalysis analysis, int index) =>
        new(
            analysis,
            index,
            null,
            null,
            IndexModel.MapSummaryStatusMessage(analysis.SummaryResult?.Status ?? AnalysisSummaryStatus.Unavailable));

    private static AssetAnalysis Sample(
        string id,
        decimal? score,
        decimal? change24Hours,
        decimal? change7Days,
        decimal? change30Days,
        decimal[] closes,
        MarketDataQuality quality,
        MarketDataFreshness freshness,
        AnalysisSummaryResult summary)
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
            false,
            [],
            null);
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
