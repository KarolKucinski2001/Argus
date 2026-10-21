using System.Globalization;

namespace _bootstrap_scaffold.Analysis;

public enum InsightTone
{
    Positive,
    Negative,
    Neutral
}

public sealed record FactorBarView(
    string Key,
    string Label,
    decimal? Percentage,
    decimal? Value,
    bool IsAvailable);

public sealed record SparklineView(
    string ViewBox,
    string LinePoints,
    string AreaPath);

public sealed record AssetInsightView(
    AssetIdentity Identity,
    InsightTone Tone,
    decimal? SevenDayChange,
    IReadOnlyList<FactorBarView> FactorBars,
    string Insight,
    SparklineView? Sparkline);

public static class AnalysisInsightPresenter
{
    private const decimal SparklineWidth = 100m;
    private const decimal SparklineHeight = 32m;
    private const decimal SparklinePadding = 2m;

    private static readonly (string Key, string Label)[] FactorOrder =
    [
        ("momentum-24h", "Momentum 24h"),
        ("momentum-7d", "Momentum 7d"),
        ("trend-30d", "Trend 30d")
    ];

    public static AssetInsightView Present(AssetAnalysis analysis)
    {
        var sevenDay = analysis.Performance.Change7Days is { IsAvailable: true, Percentage: { } percentage }
            ? percentage
            : (decimal?)null;
        var bars = FactorOrder
            .Select(entry => CreateBar(entry.Key, entry.Label, analysis.Factors))
            .ToArray();

        return new AssetInsightView(
            AssetIdentityCatalog.For(analysis.Asset.Id),
            ToneFor(sevenDay),
            sevenDay,
            bars,
            BuildInsight(bars),
            BuildSparkline(analysis.ChartPoints));
    }

    // Same 0–100 scale as OpportunityScorer.Normalize, rounded for display.
    public static decimal Normalize(decimal percentage) =>
        Math.Round(
            Math.Clamp((percentage + 20m) / 40m * 100m, 0m, 100m),
            0,
            MidpointRounding.AwayFromZero);

    private static FactorBarView CreateBar(string key, string label, IReadOnlyList<AnalysisFactor> factors)
    {
        var factor = factors.FirstOrDefault(item => item.Key == key);
        var isAvailable = factor is { IsAvailable: true, Percentage: not null };

        return new FactorBarView(
            key,
            label,
            factor?.Percentage,
            isAvailable ? Normalize(factor!.Percentage!.Value) : null,
            isAvailable);
    }

    private static InsightTone ToneFor(decimal? sevenDay) =>
        sevenDay switch
        {
            > 0m => InsightTone.Positive,
            < 0m => InsightTone.Negative,
            _ => InsightTone.Neutral
        };

    private static string BuildInsight(IReadOnlyList<FactorBarView> bars)
    {
        var available = bars
            .Where(bar => bar.IsAvailable)
            .OrderByDescending(bar => bar.Percentage!.Value)
            .ToArray();

        return available.Length switch
        {
            0 => "Za mało danych rynkowych, aby wskazać sygnał.",
            1 => $"{available[0].Label}: {FormatSignedPercent(available[0].Percentage!.Value)}.",
            _ => $"Najsilniejszy sygnał: {available[0].Label} ({FormatSignedPercent(available[0].Percentage!.Value)}); " +
                 $"najsłabszy: {available[^1].Label} ({FormatSignedPercent(available[^1].Percentage!.Value)})."
        };
    }

    private static SparklineView? BuildSparkline(IReadOnlyList<AnalysisChartPoint> chartPoints)
    {
        var closes = chartPoints
            .OrderBy(point => point.Timestamp)
            .Select(point => point.Close)
            .ToArray();
        if (closes.Length < 2)
        {
            return null;
        }

        var minimum = closes.Min();
        var range = closes.Max() - minimum;
        var plotHeight = SparklineHeight - 2m * SparklinePadding;
        var coordinates = closes
            .Select((close, index) =>
            {
                var x = index * SparklineWidth / (closes.Length - 1);
                var normalized = range == 0m ? 0.5m : (close - minimum) / range;
                var y = SparklinePadding + (1m - normalized) * plotHeight;
                return $"{Format(x)},{Format(y)}";
            })
            .ToArray();
        var baseline = Format(SparklineHeight);

        return new SparklineView(
            $"0 0 {Format(SparklineWidth)} {baseline}",
            string.Join(" ", coordinates),
            $"M0,{baseline} L{string.Join(" L", coordinates)} L{Format(SparklineWidth)},{baseline} Z");
    }

    private static string FormatSignedPercent(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:+0.##;-0.##;0}%");

    private static string Format(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
