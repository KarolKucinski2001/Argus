using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Analysis;

public interface IAnalysisExplanationBuilder
{
    string Build(
        MarketDataResult marketData,
        AssetPerformance performance,
        ScoreAssessment assessment);
}
