using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.Watchlist;

namespace _bootstrap_scaffold.Dashboard;

public sealed record DashboardMarketTile(
    string AssetId,
    string Name,
    string PriceText,
    WatchlistChangeCell Change,
    SparklineView? Sparkline,
    bool HasData);

public sealed record DashboardInsightView(string Title, string Text, string Age, string Monogram);

public static class DashboardPresenter
{
    public const string UnavailableMessage =
        "Dane rynkowe są obecnie niedostępne. Pozostałe sekcje pulpitu działają normalnie.";

    private static readonly string[] MarketAssets = ["AAPL", "MSFT", "NVDA", "BTC-USD", "ETH-USD"];

    public static IReadOnlyList<string> MarketAssetIds => MarketAssets;

    public static IReadOnlyList<DashboardInsightView> SampleInsights { get; } =
    [
        new(
            "Zmiany w sektorze technologicznym",
            "Przykładowy opis: spółki technologiczne w obserwowanym zbiorze zmieniały się w podobnym kierunku.",
            "2 godz. temu",
            "T"),
        new(
            "Aktywność osób z wewnątrz spółek",
            "Przykładowy opis: zgłoszenia transakcji zarządów dotyczą kilku spółek w ostatnim miesiącu.",
            "5 godz. temu",
            "I"),
        new(
            "Ujawnienia członków Kongresu",
            "Przykładowy opis: opublikowano nowe zgłoszenia dotyczące spółek z sektora technologicznego.",
            "1 dzień temu",
            "K")
    ];

    public static string? FirstName(string? displayName, string? email)
    {
        var fromName = displayName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!string.IsNullOrEmpty(fromName))
        {
            return fromName;
        }

        var segment = email?.Split('@')[0]
            .Split(['.', '_', '-', '+'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return string.IsNullOrEmpty(segment)
            ? null
            : char.ToUpperInvariant(segment[0]) + segment[1..];
    }

    public static string Greeting(string? firstName) =>
        string.IsNullOrWhiteSpace(firstName) ? "Dzień dobry!" : $"Dzień dobry, {firstName}!";

    public static IReadOnlyList<string> RequestableAssetIds(IEnumerable<string> allowedAssetIds)
    {
        var allowed = new HashSet<string>(allowedAssetIds, StringComparer.OrdinalIgnoreCase);
        return MarketAssets.Where(allowed.Contains).ToArray();
    }

    public static IReadOnlyList<DashboardMarketTile> BuildTiles(
        IEnumerable<string> allowedAssetIds,
        IReadOnlyDictionary<string, WatchlistMarketSnapshot> snapshots)
    {
        var allowed = new HashSet<string>(allowedAssetIds, StringComparer.OrdinalIgnoreCase);
        return MarketAssets.Select(assetId =>
        {
            var identity = AssetIdentityCatalog.For(assetId);
            WatchlistMarketSnapshot? snapshot = null;
            if (allowed.Contains(assetId))
            {
                snapshots.TryGetValue(assetId, out snapshot);
            }

            return new DashboardMarketTile(
                assetId,
                identity.Name,
                WatchlistTablePresenter.PriceText(snapshot?.LastPrice, snapshot?.Currency),
                WatchlistTablePresenter.Change(snapshot?.Change24Hours),
                snapshot?.Sparkline,
                snapshot?.LastPrice is not null);
        }).ToArray();
    }

    public static bool IsMarketUnavailable(IEnumerable<DashboardMarketTile> tiles) =>
        !tiles.Any(tile => tile.HasData);
}
