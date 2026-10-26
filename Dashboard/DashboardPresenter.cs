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

public sealed record DashboardInsightView(string AssetId, string Name, string Text, string? TrendLabel);

public static class DashboardPresenter
{
    public const string UnavailableMessage =
        "Dane rynkowe są obecnie niedostępne. Pozostałe sekcje pulpitu działają normalnie.";

    public const int MaximumWatchlistRows = 5;
    public const int MaximumInsights = 3;
    public const string WatchlistFailedMessage =
        "Nie udało się pobrać listy obserwowanych aktywów. Pozostałe sekcje pulpitu działają normalnie.";

    private static readonly string[] MarketAssets = ["AAPL", "MSFT", "NVDA", "BTC-USD", "ETH-USD"];

    public static IReadOnlyList<string> MarketAssetIds => MarketAssets;

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

    public static bool HasMoreRows(int totalCount) => totalCount > MaximumWatchlistRows;

    public static IReadOnlyList<WatchlistRowView> BuildWatchlistRows(
        IEnumerable<WatchlistEntry> orderedEntries,
        IEnumerable<string> allowedAssetIds,
        IReadOnlyDictionary<string, WatchlistMarketSnapshot> snapshots)
    {
        var allowed = new HashSet<string>(allowedAssetIds, StringComparer.OrdinalIgnoreCase);
        return orderedEntries.Take(MaximumWatchlistRows).Select(entry =>
        {
            var isAvailable = allowed.Contains(entry.AssetId);
            snapshots.TryGetValue(entry.AssetId, out var snapshot);
            return WatchlistTablePresenter.BuildRow(
                entry.AssetId,
                AssetIdentityCatalog.For(entry.AssetId).Name,
                isAvailable ? "Dostępne" : "Niedostępne",
                entry.SavedAtUtc,
                isAvailable,
                snapshot);
        }).ToArray();
    }

    public static IReadOnlyList<DashboardInsightView> BuildInsights(IEnumerable<AssetInsightView> insights) =>
        insights
            .Where(insight => insight.SevenDayChange.HasValue)
            .OrderByDescending(insight => Math.Abs(insight.SevenDayChange!.Value))
            .ThenBy(insight => insight.Identity.Id, StringComparer.Ordinal)
            .Take(MaximumInsights)
            .Select(insight => new DashboardInsightView(
                insight.Identity.Id,
                insight.Identity.Name,
                insight.WhyBullets.FirstOrDefault() ?? insight.Takeaway,
                insight.TrendLabel))
            .ToArray();

    public static bool IsMarketUnavailable(IEnumerable<DashboardMarketTile> tiles) =>
        !tiles.Any(tile => tile.HasData);
}
