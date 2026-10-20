using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Analysis;

public interface IAnalysisService
{
    Task<AnalysisResponse> AnalyzeAsync(
        MarketDataResponse marketDataResponse,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default);

    AnalysisResponse Analyze(MarketDataResponse marketDataResponse, DateTimeOffset asOf);
}
