using System.Globalization;
using _bootstrap_scaffold.Localization;
using _bootstrap_scaffold.MarketData;

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

public enum TrendDirection
{
    Rising,
    Mixed,
    Falling
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
    string AreaPath,
    _bootstrap_scaffold.Watchlist.WatchlistTone Tone = _bootstrap_scaffold.Watchlist.WatchlistTone.Neutral);

public sealed record AssetInsightView(
    AssetIdentity Identity,
    InsightTone Tone,
    decimal? SevenDayChange,
    IReadOnlyList<FactorBarView> FactorBars,
    string Takeaway,
    SparklineView? Sparkline,
    ScoreBand? Band,
    TrendDirection? Trend,
    string? TrendLabel,
    string AssetClassLabel,
    string WhyHeading,
    IReadOnlyList<string> WhyBullets);

public static class AnalysisInsightPresenter
{
    private const decimal SparklineWidth = 100m;
    private const decimal SparklineHeight = 32m;
    private const decimal SparklinePadding = 2m;
    private const int MaximumWhyBullets = 3;

    private static readonly (string Key, string Label, string Period)[] FactorOrder =
    [
        ("momentum-24h", "Impet 24h", "24 godzin"),
        ("momentum-7d", "Impet 7 dni", "7 dni"),
        ("trend-30d", "Trend 30 dni", "30 dni")
    ];

    public static AssetInsightView Present(AssetAnalysis analysis, int? rank = null, decimal? averageScore = null)
    {
        var sevenDay = analysis.Performance.Change7Days is { IsAvailable: true, Percentage: { } percentage }
            ? percentage
            : (decimal?)null;
        var bars = FactorOrder
            .Select(entry => CreateBar(entry.Key, Lang.T(entry.Label), analysis.Factors))
            .ToArray();
        var strongest = StrongestFactorIndex(bars);
        var trend = TrendFor(bars.Select(bar => bar.Direction));
        var isRanked = rank.HasValue && analysis.Score.HasValue;

        return new AssetInsightView(
            AssetIdentityCatalog.For(analysis.Asset.Id),
            ToneFor(sevenDay),
            sevenDay,
            bars,
            BuildTakeaway(bars, strongest, analysis.Score, rank),
            BuildSparkline(analysis.ChartPoints),
            analysis.Score is { } score ? BandFor(score) : null,
            trend,
            trend is { } direction ? TrendLabel(direction) : null,
            AssetClassLabel(analysis.Asset.AssetClass),
            isRanked ? Lang.T("Dlaczego miejsce #{0}?", rank) : Lang.T("Co wiemy o tym aktywie"),
            BuildWhyBullets(bars, strongest, analysis.Factors, analysis.Score, averageScore));
    }

    public static TrendDirection? TrendFor(IEnumerable<FactorDirection> directions)
    {
        var available = directions.Where(direction => direction != FactorDirection.Unavailable).ToArray();
        if (available.Length == 0)
        {
            return null;
        }

        var rising = available.Count(direction => direction == FactorDirection.Rising);
        var falling = available.Count(direction => direction == FactorDirection.Falling);

        return rising * 2 > available.Length ? TrendDirection.Rising
            : falling * 2 > available.Length ? TrendDirection.Falling
            : TrendDirection.Mixed;
    }

    public static string TrendLabel(TrendDirection trend) =>
        trend switch
        {
            TrendDirection.Rising => Lang.T("Trend wzrostowy"),
            TrendDirection.Falling => Lang.T("Trend spadkowy"),
            _ => Lang.T("Trend mieszany")
        };

    public static string AssetClassLabel(MarketAssetClass assetClass) =>
        assetClass switch
        {
            MarketAssetClass.Crypto => Lang.T("Krypto"),
            // "Akcje" also means "Actions" (table column) elsewhere, so the stock label cannot be a shared key.
            _ => Lang.IsEnglish ? "Stocks" : "Akcje"
        };

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
            ScoreBand.Strong => Lang.T("Silny"),
            ScoreBand.Moderate => Lang.T("Umiarkowany"),
            _ => Lang.T("Słaby")
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
        Lang.T(FactorOrder.FirstOrDefault(entry => entry.Key == key).Label ?? "Czynnik");

    public static string FactorWord(FactorDirection direction) =>
        direction switch
        {
            FactorDirection.Rising => Lang.T("Rosnący"),
            FactorDirection.Falling => Lang.T("Malejący"),
            FactorDirection.Stable => Lang.T("Stabilny"),
            _ => Lang.T("n/d")
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

    private static IReadOnlyList<string> BuildWhyBullets(
        IReadOnlyList<FactorBarView> bars,
        int strongest,
        IReadOnlyList<AnalysisFactor> factors,
        decimal? score,
        decimal? averageScore)
    {
        var bullets = new List<string>(MaximumWhyBullets);
        if (score is { } value && averageScore is { } average)
        {
            var difference = Math.Round(value - average, 1, MidpointRounding.AwayFromZero);
            bullets.Add(difference switch
            {
                > 0m => Lang.T("Wynik {0} — o {1} pkt powyżej średniej analizy", Format(value), Format(difference)),
                < 0m => Lang.T("Wynik {0} — o {1} pkt poniżej średniej analizy", Format(value), Format(-difference)),
                _ => Lang.T("Wynik {0} — równy średniej analizy", Format(value))
            });
        }

        if (strongest < 0)
        {
            bullets.Add(Lang.T("Za mało danych rynkowych, aby uzasadnić pozycję."));
            return bullets;
        }

        var remaining = bars
            .Select((bar, index) => (Bar: bar, Index: index))
            .Where(entry => entry.Bar.IsAvailable && entry.Index != strongest)
            .OrderByDescending(entry => Math.Abs(
                factors.FirstOrDefault(factor => factor.Key == entry.Bar.Key)?.Contribution ?? 0m))
            .ThenBy(entry => entry.Index)
            .Select(entry => FactorPhrase(entry.Bar));

        bullets.AddRange(remaining.Take(MaximumWhyBullets - bullets.Count));
        if (bullets.Count == 0)
        {
            bullets.Add(Lang.T("Za mało danych rynkowych, aby uzasadnić pozycję."));
        }

        return bullets;
    }

    private static string FactorPhrase(FactorBarView bar)
    {
        var change = FormatSignedPercent(bar.Percentage!.Value);
        return bar.Direction switch
        {
            FactorDirection.Rising => Lang.T("{0} rośnie ({1})", bar.Label, change),
            FactorDirection.Falling => Lang.T("{0} spada ({1})", bar.Label, change),
            _ => Lang.T("{0} bez wyraźnej zmiany ({1})", bar.Label, change)
        };
    }

    private static int StrongestFactorIndex(IReadOnlyList<FactorBarView> bars)
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

        return strongest;
    }

    private static string BuildTakeaway(IReadOnlyList<FactorBarView> bars, int strongest, decimal? score, int? rank)
    {
        if (strongest < 0)
        {
            return Lang.T("Za mało danych rynkowych, aby opisać zmianę ceny.");
        }

        var percentage = bars[strongest].Percentage!.Value;
        var period = Lang.T(FactorOrder[strongest].Period);
        var movement = bars[strongest].Direction switch
        {
            FactorDirection.Rising => Lang.T("Cena wzrosła o {0}% w ciągu {1}", Format(Math.Abs(percentage)), period),
            FactorDirection.Falling => Lang.T("Cena spadła o {0}% w ciągu {1}", Format(Math.Abs(percentage)), period),
            _ => Lang.T("Cena pozostała stabilna w ciągu {0} ({1})", period, FormatSignedPercent(percentage))
        };

        return movement + RankSuffix(score, rank);
    }

    private static string RankSuffix(decimal? score, int? rank) =>
        (score, rank) switch
        {
            (null, _) => Lang.T(" — za mało danych, aby nadać miejsce w rankingu."),
            (_, 1) => Lang.T(" — najwyżej oceniane aktywo w tej analizie."),
            (_, { } place) => Lang.T(" — miejsce {0} w tej analizie.", place),
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
            $"M0,{baseline} L{string.Join(" L", coordinates)} L{Format(SparklineWidth)},{baseline} Z",
            closes[^1] > closes[0] ? _bootstrap_scaffold.Watchlist.WatchlistTone.Positive
                : closes[^1] < closes[0] ? _bootstrap_scaffold.Watchlist.WatchlistTone.Negative
                : _bootstrap_scaffold.Watchlist.WatchlistTone.Neutral);
    }

    private static string FormatSignedPercent(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:+0.##;-0.##;0}%");

    private static string Format(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
