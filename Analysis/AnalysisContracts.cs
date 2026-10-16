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
