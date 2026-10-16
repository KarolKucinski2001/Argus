namespace _bootstrap_scaffold.Analysis;

public interface IOpportunityScorer
{
    ScoreAssessment Assess(AssetPerformance performance);
}
