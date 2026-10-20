namespace _bootstrap_scaffold.Analysis;

public sealed class UnavailableAnalysisSummaryGenerator : IAnalysisSummaryGenerator
{
    public Task<AnalysisSummaryResult> GenerateAsync(
        AnalysisSummaryRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(AnalysisSummaryResult.Unavailable());
    }
}
