using System.Globalization;
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
            ? "Niewystarczające dane: dostępnych jest mniej niż wymagane dwa czynniki wydajności do obliczenia wyniku."
            : $"Wynik {FormatDecimal(assessment.Score.GetValueOrDefault())}/100 na podstawie {availableFactors.Length} czynników wydajności.";

        var factorText = availableFactors.Length == 0
            ? "brak dostępnych metryk wydajności"
            : string.Join(
                "; ",
                availableFactors.Select(factor =>
                    $"{PolishFactorLabel(factor.Key)} {FormatSignedPercentage(factor.Percentage.GetValueOrDefault())}"));

        var freshnessText = marketData.Freshness.IsStale
            ? "świeżość danych nieaktualna"
            : "świeżość danych aktualna";

        return $"{evidence} Metryki: {factorText}. Jakość danych: {PolishQualityLabel(marketData.Quality)}; {freshnessText}.";
    }

    private static string PolishFactorLabel(string key) =>
        key switch
        {
            "momentum-24h" => "momentum 24 godziny",
            "momentum-7d" => "momentum 7 dni",
            "trend-30d" => "trend 30 dni",
            _ => "czynnik"
        };

    private static string PolishQualityLabel(MarketDataQuality quality) =>
        quality switch
        {
            MarketDataQuality.Incomplete => "niepełna",
            MarketDataQuality.Stale => "nieaktualna",
            MarketDataQuality.Unavailable => "niedostępna",
            _ => "kompletna"
        };

    private static string FormatSignedPercentage(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:+0.##;-0.##;0}%");

    private static string FormatDecimal(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
