using _bootstrap_scaffold.Watchlist;

namespace _bootstrap_scaffold.Strategies;

public sealed record StrategyProfileView(
    string Id,
    string Name,
    string Description,
    string StockRange,
    string Monogram,
    int AccentIndex,
    bool IsExample);

public sealed record AllocationSliceView(string Label, int Weight, int DotIndex);

public sealed record StrategyMetricView(string Label, string Value, WatchlistTone Tone);

public sealed record StrategyHoldingView(
    string AssetId,
    string Name,
    string Profile,
    string Weight,
    string PositionValue,
    string LastTransaction,
    string Published,
    string Source,
    int Completeness);

public static class StrategyProfileCatalog
{
    public const string SampleBadge = "Przykładowe dane";
    public const int AllocationTotal = 100;
    public const int WeightStep = 10;

    public static IReadOnlyList<StrategyProfileView> Profiles { get; } =
    [
        new("large-funds", "Duże fundusze", "Fundusze hedgingowe i instytucjonalne", "50–200 spółek", "DF", 1, false),
        new("company-insiders", "Osoby z wewnątrz spółek", "Zarządy i rady nadzorcze (zgłoszenia SEC)", "10–50 spółek", "OW", 2, false),
        new("congress-disclosures", "Zgłoszenia Kongresu USA", "Członkowie Kongresu USA (zgłoszenia etyczne)", "10–100 spółek", "KG", 3, false),
        new("broad-market-benchmark", "Szeroki rynek (benchmark)", "S&P 500 lub skład całego indeksu", "500+ spółek", "SR", 4, false),
        new("public-profile-a", "Profil publiczny: Przykład A", "Przykładowy portfel publiczny (zgłoszenia SEC)", "5–20 spółek", "A", 1, true),
        new("public-profile-b", "Profil publiczny: Przykład B", "Przykładowy portfel publiczny (zgłoszenia SEC)", "5–20 spółek", "B", 3, true)
    ];

    public static IReadOnlyList<StrategyProfileView> DashboardProfiles { get; } =
        Profiles.Where(profile => profile.Id != "broad-market-benchmark").ToArray();

    public static IReadOnlyList<AllocationSliceView> Allocation { get; } =
    [
        new("Duże fundusze", 20, 1),
        new("Osoby z wewnątrz spółek", 10, 2),
        new("Szeroki rynek (benchmark)", 20, 3),
        new("Moja selekcja", 50, 4)
    ];

    public static IReadOnlyList<StrategyMetricView> OverviewMetrics { get; } =
    [
        new("Wartość całkowita (symulowana)", "100 000 USD", WatchlistTone.Neutral),
        new("Szacowana wartość bieżąca", "118 432 USD", WatchlistTone.Neutral),
        new("Zmiana całkowita", "+18,4%", WatchlistTone.Positive),
        new("Maksymalne obsunięcie", "-12,6%", WatchlistTone.Negative),
        new("Zmienność (rocznie)", "14,2%", WatchlistTone.Neutral)
    ];

    public static IReadOnlyList<StrategyHoldingView> Holdings { get; } =
    [
        new("AAPL", "Apple Inc.", "Moja selekcja", "12,5%", "12 500 USD", "2024-05-15", "2024-05-20", "SEC 13F", 72),
        new("MSFT", "Microsoft Corp.", "Moja selekcja", "10,8%", "10 800 USD", "2024-05-10", "2024-05-16", "SEC 13F", 68),
        new("NVDA", "NVIDIA Corp.", "Moja selekcja", "8,3%", "8 300 USD", "2024-05-08", "2024-05-14", "SEC 13F", 65),
        new("AMZN", "Amazon.com Inc.", "Moja selekcja", "7,2%", "7 200 USD", "2024-04-25", "2024-05-02", "SEC 4", 58),
        new("TSLA", "Tesla Inc.", "Moja selekcja", "6,9%", "6 900 USD", "2024-04-18", "2024-04-26", "SEC 4", 52)
    ];

    public static string AccentClass(int index) => $"strategy-accent--{index}";

    public static string WeightClass(int weight) => $"strategy-weight--{weight}";

    public static string DotClass(int index) => $"account-legend__dot--{index}";

    public static string DonutLabel() =>
        "Przykładowy podział: " + string.Join(", ", Allocation.Select(slice => $"{slice.Weight}%"));
}
