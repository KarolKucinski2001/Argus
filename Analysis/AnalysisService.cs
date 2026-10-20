using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Analysis;

public sealed class AnalysisService(
    IPerformanceCalculator performanceCalculator,
    IOpportunityScorer opportunityScorer,
    IAnalysisExplanationBuilder explanationBuilder) : IAnalysisService
{
    public AnalysisResponse Analyze(MarketDataResponse marketDataResponse, DateTimeOffset asOf)
    {
        var results = marketDataResponse.Results
            .Select((result, index) => new RankedAnalysis(CreateAnalysis(result, asOf), index))
            .OrderByDescending(result => result.Analysis.Score.HasValue)
            .ThenByDescending(result => result.Analysis.Score)
            .ThenBy(result => result.InputIndex)
            .Select(result => result.Analysis)
            .ToArray();

        return new AnalysisResponse(results);
    }

    private AssetAnalysis CreateAnalysis(MarketDataResult result, DateTimeOffset asOf)
    {
        var performance = performanceCalculator.Calculate(result, asOf);

        if (result.Error is not null)
        {
            return new AssetAnalysis(
                result.Asset,
                null,
                performance,
                [],
                performance.ChartPoints,
                null,
                null,
                result.Quality,
                result.Freshness,
                result.Source?.IsDelayed == true,
                result.Warnings,
                result.Error);
        }

        var assessment = opportunityScorer.Assess(performance);
        var summary = explanationBuilder.Build(result, performance, assessment);

        return new AssetAnalysis(
            result.Asset,
            assessment.Score,
            performance,
            assessment.Factors,
            performance.ChartPoints,
            summary,
            AnalysisSummaryResult.Generated(summary),
            result.Quality,
            result.Freshness,
            result.Source?.IsDelayed == true,
            result.Warnings,
            null);
    }

    private sealed record RankedAnalysis(AssetAnalysis Analysis, int InputIndex);
}
