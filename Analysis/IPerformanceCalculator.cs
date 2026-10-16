using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Analysis;

public interface IPerformanceCalculator
{
    AssetPerformance Calculate(MarketDataResult result, DateTimeOffset asOf);
}
