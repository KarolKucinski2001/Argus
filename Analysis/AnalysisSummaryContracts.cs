using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Analysis;

public enum AnalysisSummaryStatus
{
    Generated,
    InsufficientEvidence,
    Unavailable
}

public sealed record AnalysisSummaryRequest(
    AssetReference Asset,
    decimal? Score,
    AssetPerformance Performance,
    IReadOnlyList<AnalysisFactor> Factors,
    MarketDataQuality Quality,
    MarketDataFreshness Freshness,
    bool IsDelayed,
    IReadOnlyList<MarketDataWarningCategory> WarningCategories);

public sealed record AnalysisSummaryResult
{
    public const int MaximumSummaryLength = 800;

    private AnalysisSummaryResult(AnalysisSummaryStatus Status, string? Text)
    {
        this.Status = Status;
        this.Text = Text;
    }

    public AnalysisSummaryStatus Status { get; }

    public string? Text { get; }

    public static AnalysisSummaryResult Generated(string text) =>
        !string.IsNullOrWhiteSpace(text) && text.Trim().Length <= MaximumSummaryLength
            ? new(AnalysisSummaryStatus.Generated, text.Trim())
            : Unavailable();

    public static AnalysisSummaryResult InsufficientEvidence() =>
        new(AnalysisSummaryStatus.InsufficientEvidence, null);

    public static AnalysisSummaryResult Unavailable() =>
        new(AnalysisSummaryStatus.Unavailable, null);
}
