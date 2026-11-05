using System.Globalization;
using _bootstrap_scaffold.Localization;
using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Analysis;

public sealed class AnalysisExplanationBuilder : IAnalysisExplanationBuilder
{
    public string Build(
        MarketDataResult marketData,
        AssetPerformance performance,
        ScoreAssessment assessment)
    {
        var availableFactors = assessment.Factors
            .Where(factor => factor.IsAvailable && factor.Percentage.HasValue)
            .ToArray();

        var evidence = availableFactors.Length < 2 || !assessment.HasSufficientEvidence
            ? Lang.T("Niewystarczające dane: dostępnych jest mniej niż wymagane dwa czynniki wydajności do obliczenia wyniku.")
            : Lang.T("Wynik {0}/100 na podstawie {1} czynników wydajności.", FormatDecimal(assessment.Score.GetValueOrDefault()), availableFactors.Length);

        var factorText = availableFactors.Length == 0
            ? Lang.T("brak dostępnych metryk wydajności")
            : string.Join(
                "; ",
                availableFactors.Select(factor =>
                    $"{PolishFactorLabel(factor.Key)} {FormatSignedPercentage(factor.Percentage.GetValueOrDefault())}"));

        var freshnessText = marketData.Freshness.IsStale
            ? Lang.T("świeżość danych nieaktualna")
            : Lang.T("świeżość danych aktualna");

        return Lang.T("{0} Metryki: {1}. Jakość danych: {2}; {3}.", evidence, factorText, PolishQualityLabel(marketData.Quality), freshnessText);
    }

    private static string PolishFactorLabel(string key) =>
        key switch
        {
            "momentum-24h" => Lang.T("impet 24 godziny"),
            "momentum-7d" => Lang.T("impet 7 dni"),
            "trend-30d" => Lang.T("trend 30 dni"),
            _ => Lang.T("czynnik")
        };

    private static string PolishQualityLabel(MarketDataQuality quality) =>
        quality switch
        {
            MarketDataQuality.Incomplete => Lang.T("niepełna"),
            MarketDataQuality.Stale => Lang.T("nieaktualna"),
            MarketDataQuality.Unavailable => Lang.T("niedostępna"),
            _ => Lang.T("kompletna")
        };

    private static string FormatSignedPercentage(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:+0.##;-0.##;0}%");

    private static string FormatDecimal(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
