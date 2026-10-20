using Microsoft.Extensions.Options;
using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Analysis;

public sealed class AnalysisService(
    IPerformanceCalculator performanceCalculator,
    IOpportunityScorer opportunityScorer,
    IAnalysisExplanationBuilder explanationBuilder,
    IAnalysisSummaryGenerator? summaryGenerator = null,
    IOptions<AnalysisSummaryOptions>? configuredSummaryOptions = null) : IAnalysisService
{
    private readonly IAnalysisSummaryGenerator summaryGenerator =
        summaryGenerator ?? new UnavailableAnalysisSummaryGenerator();
    private readonly TimeSpan summaryTimeout =
        configuredSummaryOptions?.Value.RequestTimeout > TimeSpan.Zero
            ? configuredSummaryOptions.Value.RequestTimeout
            : TimeSpan.FromSeconds(10);

    public AnalysisResponse Analyze(MarketDataResponse marketDataResponse, DateTimeOffset asOf)
    {
        var results = marketDataResponse.Results
            .Select((result, index) => new RankedAnalysis(CreateLegacyAnalysis(result, asOf), index))
            .OrderByDescending(result => result.Analysis.Score.HasValue)
            .ThenByDescending(result => result.Analysis.Score)
            .ThenBy(result => result.InputIndex)
            .Select(result => result.Analysis)
            .ToArray();

        return new AnalysisResponse(results);
    }

    public async Task<AnalysisResponse> AnalyzeAsync(
        MarketDataResponse marketDataResponse,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var analyses = new List<RankedAnalysis>(marketDataResponse.Results.Count);

        for (var index = 0; index < marketDataResponse.Results.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var analysis = await CreateAnalysisAsync(
                marketDataResponse.Results[index],
                asOf,
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            analyses.Add(new RankedAnalysis(analysis, index));
        }

        var results = analyses
            .OrderByDescending(result => result.Analysis.Score.HasValue)
            .ThenByDescending(result => result.Analysis.Score)
            .ThenBy(result => result.InputIndex)
            .Select(result => result.Analysis)
            .ToArray();

        return new AnalysisResponse(results);
    }

    private AssetAnalysis CreateLegacyAnalysis(MarketDataResult result, DateTimeOffset asOf)
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

    private async Task<AssetAnalysis> CreateAnalysisAsync(
        MarketDataResult result,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
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
        if (!assessment.HasSufficientEvidence)
        {
            return CreateAnalysisResult(
                result,
                performance,
                assessment,
                AnalysisSummaryResult.InsufficientEvidence());
        }

        var request = new AnalysisSummaryRequest(
            result.Asset,
            assessment.Score,
            performance,
            assessment.Factors,
            result.Quality,
            result.Freshness,
            result.Source?.IsDelayed == true,
            result.Warnings.Select(warning => warning.Category).Distinct().ToArray());

        AnalysisSummaryResult summary;
        try
        {
            using var summaryCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            summaryCancellation.CancelAfter(summaryTimeout);
            summary = await summaryGenerator.GenerateAsync(request, summaryCancellation.Token);
            summary = AnalysisSummaryResultValidator.Validate(summary);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            summary = AnalysisSummaryResult.Unavailable();
        }
        catch (Exception)
        {
            summary = AnalysisSummaryResult.Unavailable();
        }

        return CreateAnalysisResult(result, performance, assessment, summary);
    }

    private static AssetAnalysis CreateAnalysisResult(
        MarketDataResult result,
        AssetPerformance performance,
        ScoreAssessment assessment,
        AnalysisSummaryResult summary) =>
        new(
            result.Asset,
            assessment.Score,
            performance,
            assessment.Factors,
            performance.ChartPoints,
            summary.Text,
            summary,
            result.Quality,
            result.Freshness,
            result.Source?.IsDelayed == true,
            result.Warnings,
            null);

    private sealed record RankedAnalysis(AssetAnalysis Analysis, int InputIndex);
}
