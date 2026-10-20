namespace _bootstrap_scaffold.Analysis;

public interface IAnalysisSummaryGenerator
{
    Task<AnalysisSummaryResult> GenerateAsync(
        AnalysisSummaryRequest request,
        CancellationToken cancellationToken);
}
