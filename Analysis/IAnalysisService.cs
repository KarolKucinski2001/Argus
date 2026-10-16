using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Analysis;

public interface IAnalysisService
{
    AnalysisResponse Analyze(MarketDataResponse marketDataResponse, DateTimeOffset asOf);
}
