namespace _bootstrap_scaffold.Analysis;

public sealed record PerformanceMetric(
    string Period,
    decimal? Percentage,
    bool IsAvailable);

public sealed record AssetPerformance(
    PerformanceMetric Change24Hours,
    PerformanceMetric Change7Days,
    PerformanceMetric Change30Days,
    IReadOnlyList<AnalysisChartPoint> ChartPoints);

public sealed record AnalysisChartPoint(
    DateTimeOffset Timestamp,
    decimal Close);

public sealed record AnalysisFactor(
    string Key,
    string Label,
    decimal? Percentage,
    decimal Weight,
    decimal? Contribution,
    bool IsAvailable,
    string Explanation);

public sealed record ScoreAssessment(
    decimal? Score,
    IReadOnlyList<AnalysisFactor> Factors,
    bool HasSufficientEvidence);
