using System.Globalization;

namespace _bootstrap_scaffold.Analysis;

public enum InsightTone
{
    Positive,
    Negative,
    Neutral
}

public enum ScoreBand
{
    Strong,
    Moderate,
    Weak
}

public enum FactorDirection
{
    Rising,
    Stable,
    Falling,
    Unavailable
}

public sealed record FactorBarView(
    string Key,
    string Label,
    decimal? Percentage,
    decimal? Value,
    bool IsAvailable,
    FactorDirection Direction,
    string Word);

public sealed record SparklineView(
    string ViewBox,
    string LinePoints,
    string AreaPath);

public sealed record AssetInsightView(
    AssetIdentity Identity,
    InsightTone Tone,
    decimal? SevenDayChange,
    IReadOnlyList<FactorBarView> FactorBars,
    string Takeaway,
    SparklineView? Sparkline,
    ScoreBand? Band);

public static class AnalysisInsightPresenter
{
    private const decimal SparklineWidth = 100m;
    private const decimal SparklineHeight = 32m;
    private const decimal SparklinePadding = 2m;

    private static readonly (string Key, string Label, string Period)[] FactorOrder =
    [
        ("momentum-24h", "Momentum 24h", "24 godzin"),
        ("momentum-7d", "Momentum 7d", "7 dni"),
        ("trend-30d", "Trend 30d", "30 dni")
    ];

    public static AssetInsightView Present(AssetAnalysis analysis, int? rank = null)
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
            BuildTakeaway(bars, analysis.Score, rank),
            BuildSparkline(analysis.ChartPoints),
            analysis.Score is { } score ? BandFor(score) : null);
    }

    // Same 0–100 scale as OpportunityScorer.Normalize, rounded for display.
    public static decimal Normalize(decimal percentage) =>
        Math.Round(
            Math.Clamp((percentage + 20m) / 40m * 100m, 0m, 100m),
            0,
            MidpointRounding.AwayFromZero);

    public static InsightTone ToneFor(decimal? change) =>
        change switch
        {
            > 0m => InsightTone.Positive,
            < 0m => InsightTone.Negative,
            _ => InsightTone.Neutral
        };

    public static ScoreBand BandFor(decimal score) =>
        score switch
        {
            >= 70m => ScoreBand.Strong,
            >= 40m => ScoreBand.Moderate,
            _ => ScoreBand.Weak
        };

    public static string BandLabel(ScoreBand band) =>
        band switch
        {
            ScoreBand.Strong => "Silny",
            ScoreBand.Moderate => "Umiarkowany",
            _ => "Słaby"
        };

    public static FactorDirection DirectionFor(decimal? percentage) =>
        percentage switch
        {
            null => FactorDirection.Unavailable,
            > 1m => FactorDirection.Rising,
            < -1m => FactorDirection.Falling,
            _ => FactorDirection.Stable
        };

    public static string FactorLabel(string key) =>
        FactorOrder.FirstOrDefault(entry => entry.Key == key).Label ?? "Czynnik";

    public static string FactorWord(FactorDirection direction) =>
        direction switch
        {
            FactorDirection.Rising => "Rosnący",
            FactorDirection.Falling => "Malejący",
            FactorDirection.Stable => "Stabilny",
            _ => "n/d"
        };

    private static FactorBarView CreateBar(string key, string label, IReadOnlyList<AnalysisFactor> factors)
    {
        var factor = factors.FirstOrDefault(item => item.Key == key);
        var isAvailable = factor is { IsAvailable: true, Percentage: not null };
        var direction = isAvailable ? DirectionFor(factor!.Percentage) : FactorDirection.Unavailable;

        return new FactorBarView(
            key,
            label,
            factor?.Percentage,
            isAvailable ? Normalize(factor!.Percentage!.Value) : null,
            isAvailable,
            direction,
            FactorWord(direction));
    }

    private static string BuildTakeaway(IReadOnlyList<FactorBarView> bars, decimal? score, int? rank)
    {
        var strongest = -1;
        for (var index = 0; index < bars.Count; index++)
        {
            if (!bars[index].IsAvailable)
            {
                continue;
            }

            if (strongest < 0 ||
                Math.Abs(bars[index].Percentage!.Value) > Math.Abs(bars[strongest].Percentage!.Value))
            {
                strongest = index;
            }
        }

        if (strongest < 0)
        {
            return "Za mało danych rynkowych, aby opisać zmianę ceny.";
        }

        var percentage = bars[strongest].Percentage!.Value;
        var period = FactorOrder[strongest].Period;
        var movement = bars[strongest].Direction switch
        {
            FactorDirection.Rising => $"Cena wzrosła o {Format(Math.Abs(percentage))}% w ciągu {period}",
            FactorDirection.Falling => $"Cena spadła o {Format(Math.Abs(percentage))}% w ciągu {period}",
            _ => $"Cena pozostała stabilna w ciągu {period} ({FormatSignedPercent(percentage)})"
        };

        return movement + RankSuffix(score, rank);
    }

    private static string RankSuffix(decimal? score, int? rank) =>
        (score, rank) switch
        {
            (null, _) => " — za mało danych, aby nadać miejsce w rankingu.",
            (_, 1) => " — najwyżej oceniane aktywo w tej analizie.",
            (_, { } place) => $" — miejsce {place} w tej analizie.",
            _ => "."
        };

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
