namespace _bootstrap_scaffold.Analysis;

public sealed record SummaryPerformer(string Name, decimal Change);

public sealed record AnalysisSummaryStripView(
    int AnalyzedCount,
    SummaryPerformer? Best,
    SummaryPerformer? Worst,
    decimal? AverageScore,
    SummaryPerformer? StrongestTrend = null,
    DateOnly? DataAsOf = null);

public static class AnalysisSummaryStripPresenter
{
    private const decimal StrongestTrendThreshold = 1m;

    public static string AssetCountLabel(int count)
    {
        var lastTwo = Math.Abs(count) % 100;
        var last = lastTwo % 10;
        var noun = count == 1 ? "aktywo"
            : last is >= 2 and <= 4 && lastTwo is < 12 or > 14 ? "aktywa"
            : "aktywów";

        return $"{count} {noun}";
    }

    public static AnalysisSummaryStripView Build(IReadOnlyList<AssetAnalysis> results)
    {
        var healthy = results.Where(result => result.Error is null).ToArray();
        var candidates = healthy
            .Where(result => result.Performance.Change7Days is { IsAvailable: true, Percentage: not null })
            .Select(result => new SummaryPerformer(
                AssetIdentityCatalog.For(result.Asset.Id).Name,
                result.Performance.Change7Days.Percentage!.Value))
            .ToArray();

        SummaryPerformer? best = null;
        SummaryPerformer? worst = null;
        foreach (var candidate in candidates)
        {
            if (best is null || candidate.Change > best.Change)
            {
                best = candidate;
            }

            if (worst is null || candidate.Change <= worst.Change)
            {
                worst = candidate;
            }
        }

        var scores = results
            .Where(result => result.Score.HasValue)
            .Select(result => result.Score!.Value)
            .ToArray();

        var strongestTrend = healthy
            .Where(result => result.Performance.Change30Days is { IsAvailable: true, Percentage: > StrongestTrendThreshold })
            .Select(result => new SummaryPerformer(
                AssetIdentityCatalog.For(result.Asset.Id).Name,
                result.Performance.Change30Days.Percentage!.Value))
            .Aggregate((SummaryPerformer?)null, (top, candidate) =>
                top is null || candidate.Change > top.Change ? candidate : top);

        return new AnalysisSummaryStripView(
            results.Count,
            best,
            candidates.Length < 2 ? null : worst,
            scores.Length == 0
                ? null
                : Math.Round(scores.Average(), 1, MidpointRounding.AwayFromZero),
            strongestTrend,
            healthy.Length == 0
                ? null
                : DateOnly.FromDateTime(healthy.Max(result => result.Freshness.ObservedAt).UtcDateTime));
    }
}
