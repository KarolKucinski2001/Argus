using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Analysis;

public sealed class PerformanceCalculator : IPerformanceCalculator
{
    public AssetPerformance Calculate(MarketDataResult result, DateTimeOffset asOf)
    {
        var chartPoints = result.HistoricalPoints
            .OrderBy(point => point.Timestamp)
            .Select(point => new AnalysisChartPoint(point.Timestamp, point.Close))
            .ToArray();

        return new AssetPerformance(
            CalculatePeriod("24h", result, asOf.AddHours(-24)),
            CalculatePeriod("7d", result, asOf.AddDays(-7)),
            CalculatePeriod("30d", result, asOf.AddDays(-30)),
            chartPoints);
    }

    private static PerformanceMetric CalculatePeriod(
        string period,
        MarketDataResult result,
        DateTimeOffset boundary)
    {
        var historicalPoint = result.HistoricalPoints
            .Where(point => point.Timestamp <= boundary)
            .OrderByDescending(point => point.Timestamp)
            .FirstOrDefault();

        if (result.CurrentQuote is null ||
            historicalPoint is null ||
            historicalPoint.Close <= 0m)
        {
            return new PerformanceMetric(period, null, false);
        }

        var percentage = (result.CurrentQuote.Value - historicalPoint.Close) / historicalPoint.Close * 100m;
        return new PerformanceMetric(period, percentage, true);
    }
}
