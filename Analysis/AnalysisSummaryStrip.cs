namespace _bootstrap_scaffold.Analysis;

public sealed record SummaryPerformer(string Name, decimal Change);

public sealed record AnalysisSummaryStripView(
    int AnalyzedCount,
    SummaryPerformer? Best,
    SummaryPerformer? Worst,
    decimal? AverageScore);

public static class AnalysisSummaryStripPresenter
{
    public static AnalysisSummaryStripView Build(IReadOnlyList<AssetAnalysis> results)
    {
        var candidates = results
            .Where(result => result.Error is null &&
                result.Performance.Change7Days is { IsAvailable: true, Percentage: not null })
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

        return new AnalysisSummaryStripView(
            results.Count,
            best,
            candidates.Length < 2 ? null : worst,
            scores.Length == 0
                ? null
                : Math.Round(scores.Average(), 1, MidpointRounding.AwayFromZero));
    }
}
