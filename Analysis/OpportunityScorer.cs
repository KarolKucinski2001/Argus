namespace _bootstrap_scaffold.Analysis;

public sealed class OpportunityScorer : IOpportunityScorer
{
    private const decimal TwentyFourHourWeight = 0.25m;
    private const decimal SevenDayWeight = 0.35m;
    private const decimal ThirtyDayWeight = 0.40m;
    private const int MinimumAvailableFactors = 2;

    public ScoreAssessment Assess(AssetPerformance performance)
    {
        var inputs = new[]
        {
            new FactorInput("momentum-24h", "Impet 24h", performance.Change24Hours, TwentyFourHourWeight),
            new FactorInput("momentum-7d", "Impet 7 dni", performance.Change7Days, SevenDayWeight),
            new FactorInput("trend-30d", "Trend 30 dni", performance.Change30Days, ThirtyDayWeight)
        };

        var availableWeight = inputs
            .Where(input => IsAvailable(input.Metric))
            .Sum(input => input.BaseWeight);

        var factors = inputs
            .Select(input => CreateFactor(input, availableWeight))
            .ToArray();

        var availableFactorCount = factors.Count(factor => factor.IsAvailable);
        var hasSufficientEvidence = availableFactorCount >= MinimumAvailableFactors;
        var score = hasSufficientEvidence
            ? Round(factors.Sum(factor => factor.Contribution ?? 0m))
            : (decimal?)null;

        return new ScoreAssessment(score, factors, hasSufficientEvidence);
    }

    private static AnalysisFactor CreateFactor(FactorInput input, decimal availableWeight)
    {
        if (!IsAvailable(input.Metric) || availableWeight == 0m)
        {
            return new AnalysisFactor(
                input.Key,
                input.Label,
                input.Metric.Percentage,
                0m,
                null,
                false,
                $"{input.Label}: brak wystarczającej historii danych, aby wyliczyć ten czynnik.");
        }

        var normalizedWeight = input.BaseWeight / availableWeight;
        var normalizedPerformance = Normalize(input.Metric.Percentage.GetValueOrDefault());
        var contribution = Round(normalizedPerformance * normalizedWeight);

        return new AnalysisFactor(
            input.Key,
            input.Label,
            input.Metric.Percentage,
            normalizedWeight,
            contribution,
            true,
            $"{input.Label}: wkład {contribution:0.##} pkt po normalizacji stałych wag.");
    }

    private static bool IsAvailable(PerformanceMetric metric) =>
        metric.IsAvailable && metric.Percentage.HasValue;

    private static decimal Normalize(decimal percentage) =>
        Math.Clamp((percentage + 20m) / 40m * 100m, 0m, 100m);

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record FactorInput(
        string Key,
        string Label,
        PerformanceMetric Metric,
        decimal BaseWeight);
}
