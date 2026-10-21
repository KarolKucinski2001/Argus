using System.Globalization;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Pages.Analysis;

public static class AnalysisFormatting
{
    public static string Metric(PerformanceMetric metric) =>
        metric.IsAvailable && metric.Percentage.HasValue
            ? SignedPercent(metric.Percentage.Value)
            : "niedostępne";

    public static string NullablePercent(decimal? value) =>
        value.HasValue ? SignedPercent(value.Value) : "niedostępne";

    public static string SignedPercent(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:+0.##;-0.##;0}%");

    public static string NullableDecimal(decimal? value) =>
        value.HasValue ? Decimal(value.Value) : "niedostępne";

    public static string Weight(decimal value) =>
        $"{(Math.Clamp(value, 0m, 1m) * 100m).ToString("0.##", CultureInfo.InvariantCulture)}%";

    public static string Quality(MarketDataQuality quality) =>
        quality switch
        {
            MarketDataQuality.Incomplete => "Dane niepełne",
            MarketDataQuality.Stale => "Dane nieaktualne",
            MarketDataQuality.Unavailable => "Dane niedostępne",
            _ => "Dane kompletne"
        };

    public static string Warning(MarketDataWarningCategory category) =>
        category switch
        {
            MarketDataWarningCategory.MissingCurrentQuote => "Brak bieżącej ceny",
            MarketDataWarningCategory.MissingHistory => "Brak historii",
            MarketDataWarningCategory.IncompleteHistory => "Historia niepełna",
            MarketDataWarningCategory.StaleData => "Dane nieaktualne",
            MarketDataWarningCategory.DelayedData => "Dane opóźnione",
            MarketDataWarningCategory.ProviderQuota => "Ograniczona dostępność danych",
            MarketDataWarningCategory.UpstreamUnavailable => "Źródło danych chwilowo niedostępne",
            _ => "Ograniczona jakość danych"
        };

    public static string Decimal(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
