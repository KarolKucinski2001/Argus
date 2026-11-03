namespace _bootstrap_scaffold.Strategies;

public sealed record CuratedHolding(
    string AssetId,
    string Name,
    decimal Weight,
    string Sector,
    string AssetClass);

public sealed record CuratedProfile(
    string Id,
    string Name,
    string Description,
    string Monogram,
    int AccentIndex,
    DateOnly AsOf,
    string SourceLabel,
    IReadOnlyList<CuratedHolding> Holdings);

/// <summary>
/// Small static set of well-known public portfolios. Weights are rounded, approximate and
/// normalized to the listed positions; they illustrate composition and are not exact filings data.
/// </summary>
public static class CuratedProfileCatalog
{
    public const string ApproximateBadge = "Przykładowe, przybliżone dane";

    public const string Disclaimer =
        "To symulacja edukacyjna. Argus nie prowadzi prawdziwych portfeli, nie składa zleceń i nie udziela porad inwestycyjnych.";

    public const string DataNote =
        "Składy i wagi są przybliżone, zaokrąglone i przeliczone na wyświetlane pozycje. Nie zastępują oficjalnych zgłoszeń.";

    public const string ProvenanceNote =
        "Profile publiczne to edukacyjne przykłady składu portfeli opisanych w publicznych źródłach, takich jak zgłoszenia SEC 13F lub publiczne opisy strategii. Argus nie jest powiązany z ich autorami ani zarządzającymi.";

    public const string ApproximateExplanation =
        "„Przykładowe, przybliżone dane” oznacza, że wagi są zaokrąglone i przeliczone tylko na pokazane pozycje. Służą do nauki i porównań, a nie do odtworzenia oficjalnego zgłoszenia.";

    public static IReadOnlyList<CuratedProfile> Profiles { get; } =
    [
        new(
            "berkshire-style",
            "Portfel w stylu Berkshire Hathaway",
            "Skoncentrowany portfel akcji amerykańskich spółek, opisywany w publicznych zgłoszeniach 13F.",
            "BH",
            1,
            new DateOnly(2025, 6, 30),
            "Publiczne zgłoszenia SEC 13F (przybliżone)",
            [
                new("AAPL", "Apple Inc.", 40m, "Technologia", "Akcje"),
                new("AXP", "American Express", 17m, "Finanse", "Akcje"),
                new("BAC", "Bank of America", 14m, "Finanse", "Akcje"),
                new("KO", "Coca-Cola", 10m, "Dobra konsumpcyjne", "Akcje"),
                new("CVX", "Chevron", 9m, "Energia", "Akcje"),
                new("OXY", "Occidental Petroleum", 5m, "Energia", "Akcje"),
                new("KHC", "Kraft Heinz", 5m, "Dobra konsumpcyjne", "Akcje")
            ]),
        new(
            "sp500-top-holdings",
            "Ilustracyjny skrót S&P 500",
            "Ilustracyjny, przybliżony wybór dużych spółek z szerokiego indeksu amerykańskiego.",
            "SP",
            2,
            new DateOnly(2025, 6, 30),
            "Ilustracyjny przykład bez zatwierdzonego źródła publicznego",
            [
                new("NVDA", "NVIDIA Corp.", 21m, "Technologia", "Akcje"),
                new("MSFT", "Microsoft Corp.", 19m, "Technologia", "Akcje"),
                new("AAPL", "Apple Inc.", 17m, "Technologia", "Akcje"),
                new("AMZN", "Amazon.com Inc.", 11m, "Dobra konsumpcyjne", "Akcje"),
                new("GOOGL", "Alphabet Inc.", 10m, "Komunikacja", "Akcje"),
                new("META", "Meta Platforms", 9m, "Komunikacja", "Akcje"),
                new("AVGO", "Broadcom Inc.", 8m, "Technologia", "Akcje"),
                new("TSLA", "Tesla Inc.", 5m, "Dobra konsumpcyjne", "Akcje")
            ]),
        new(
            "all-weather-style",
            "Portfel w stylu All Weather (przykład)",
            "Edukacyjna, przybliżona ilustracja podziału między akcje, obligacje, złoto i surowce.",
            "AW",
            3,
            new DateOnly(2025, 6, 30),
            "Publicznie opisywany styl; nieoficjalna ilustracja z ETF-ami",
            [
                new("VTI", "Vanguard Total Stock Market ETF", 30m, "Akcje amerykańskie", "Akcje"),
                new("TLT", "iShares 20+ Year Treasury Bond ETF", 40m, "Obligacje długoterminowe", "Obligacje"),
                new("IEF", "iShares 7-10 Year Treasury Bond ETF", 15m, "Obligacje średnioterminowe", "Obligacje"),
                new("GLD", "SPDR Gold Shares", 7.5m, "Złoto", "Surowce"),
                new("DBC", "Invesco DB Commodity Index Tracking Fund", 7.5m, "Surowce szerokie", "Surowce")
            ])
    ];

    public static CuratedProfile? Find(string? id) =>
        Profiles.FirstOrDefault(profile =>
            string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase));
}
