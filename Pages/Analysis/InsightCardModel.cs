using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Pages.Analysis;

public sealed record InsightCardModel(
    AssetAnalysis Result,
    int Index,
    MarketQuote? Quote,
    string? ErrorMessage,
    string SummaryStatusMessage)
{
    public int? Rank => Result.Score.HasValue ? Index + 1 : null;
}
