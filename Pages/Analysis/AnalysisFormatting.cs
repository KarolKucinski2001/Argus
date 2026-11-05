using System.Globalization;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.Localization;
using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Pages.Analysis;

public static class AnalysisFormatting
{
    public static string Metric(PerformanceMetric metric) =>
        metric.IsAvailable && metric.Percentage.HasValue
            ? SignedPercent(metric.Percentage.Value)
            : Lang.T("niedostępne");

    public static string NullablePercent(decimal? value) =>
        value.HasValue ? SignedPercent(value.Value) : Lang.T("niedostępne");

    public static string SignedPercent(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:+0.##;-0.##;0}%");

    public static string NullableDecimal(decimal? value) =>
        value.HasValue ? Decimal(value.Value) : Lang.T("niedostępne");

    public static string Weight(decimal value) =>
        $"{(Math.Clamp(value, 0m, 1m) * 100m).ToString("0.##", CultureInfo.InvariantCulture)}%";

    public static string Quality(MarketDataQuality quality) =>
        quality switch
        {
            MarketDataQuality.Incomplete => Lang.T("Dane niepełne"),
            MarketDataQuality.Stale => Lang.T("Dane nieaktualne"),
            MarketDataQuality.Unavailable => Lang.T("Dane niedostępne"),
            _ => Lang.T("Dane kompletne")
        };

    public static string Freshness(bool isStale) =>
        isStale ? Lang.T("Nieaktualne") : Lang.T("Aktualne");

    public static string Warning(MarketDataWarningCategory category) =>
        category switch
        {
            MarketDataWarningCategory.MissingCurrentQuote => Lang.T("Brak bieżącej ceny"),
            MarketDataWarningCategory.MissingHistory => Lang.T("Brak historii"),
            MarketDataWarningCategory.IncompleteHistory => Lang.T("Historia niepełna"),
            MarketDataWarningCategory.StaleData => Lang.T("Dane nieaktualne"),
            MarketDataWarningCategory.DelayedData => Lang.T("Dane opóźnione"),
            MarketDataWarningCategory.ProviderQuota => Lang.T("Ograniczona dostępność danych"),
            MarketDataWarningCategory.UpstreamUnavailable => Lang.T("Źródło danych niedostępne"),
            _ => Lang.T("Ograniczona jakość danych")
        };

    public static string Decimal(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
