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
            ? "Insufficient evidence: fewer than two performance factors are available for a meaningful score."
            : $"Score {FormatDecimal(assessment.Score.GetValueOrDefault())}/100 from {availableFactors.Length} performance factors.";

        var factorText = availableFactors.Length == 0
            ? "No performance metrics are available."
            : string.Join(
                "; ",
                availableFactors.Select(factor =>
                    $"{factor.Label} {FormatSignedPercentage(factor.Percentage.GetValueOrDefault())}"));

        var freshnessText = marketData.Freshness.IsStale
            ? "freshness stale"
            : "freshness current";

        return $"{evidence} Metrics: {factorText}. Data quality {marketData.Quality}; {freshnessText}.";
    }

    private static string FormatSignedPercentage(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:+0.##;-0.##;0}%");

    private static string FormatDecimal(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
