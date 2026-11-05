using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.Localization;

namespace _bootstrap_scaffold.Watchlist;

public enum WatchlistTone
{
    Positive,
    Negative,
    Neutral,
    Unavailable
}

public sealed record WatchlistMarketSnapshot(
    decimal? LastPrice,
    string? Currency,
    decimal? Change24Hours,
    decimal? Change7Days,
    decimal? Change30Days,
    SparklineView? Sparkline);

public sealed record WatchlistChangeCell(string Text, WatchlistTone Tone, decimal? Value);

public sealed record WatchlistRowView(
    string AssetId,
    string Name,
    string AssetClass,
    DateTimeOffset SavedAtUtc,
    bool IsAvailable,
    string PriceText,
    WatchlistChangeCell Change24Hours,
    WatchlistChangeCell Change7Days,
    WatchlistChangeCell Change30Days,
    SparklineView? Sparkline);

public sealed record WatchlistMoverView(
    string AssetId,
    string Name,
    string ChangeText,
    WatchlistTone Tone);

public sealed record WatchlistMoversView(
    IReadOnlyList<WatchlistMoverView> Gainers,
    IReadOnlyList<WatchlistMoverView> Losers);

public static class WatchlistTablePresenter
{
    public const string MissingDataText = "brak danych";
    public const string TrendHeader = "Trend 12 mies.";
    public const string TrendHint =
        "Dzienne ceny zamknięcia z ostatnich 12 miesięcy. Zielony: cena wyższa niż 12 miesięcy temu, czerwony: niższa.";
    public const int MaximumMovers = 5;

    public static WatchlistRowView BuildRow(
        string assetId,
        string name,
        string assetClass,
        DateTimeOffset savedAtUtc,
        bool isAvailable,
        WatchlistMarketSnapshot? snapshot)
    {
        var data = isAvailable ? snapshot : null;
        return new WatchlistRowView(
            assetId,
            name,
            assetClass,
            savedAtUtc,
            isAvailable,
            PriceText(data?.LastPrice, data?.Currency),
            Change(data?.Change24Hours),
            Change(data?.Change7Days),
            Change(data?.Change30Days),
            data?.Sparkline);
    }

    public static string PriceText(decimal? price, string? currency)
    {
        if (price is not { } value)
        {
            return Lang.T(MissingDataText);
        }

        var amount = value.ToString("N2", Lang.Culture);
        return string.IsNullOrWhiteSpace(currency) ? amount : $"{amount} {currency}";
    }

    public static string SignedPercent(decimal value)
    {
        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return rounded.ToString("+0.00;-0.00;0.00", Lang.Culture) + "%";
    }

    public static WatchlistChangeCell Change(decimal? value)
    {
        if (value is not { } change)
        {
            return new WatchlistChangeCell(Lang.T(MissingDataText), WatchlistTone.Unavailable, null);
        }

        var rounded = Math.Round(change, 2, MidpointRounding.AwayFromZero);
        var tone = rounded > 0m ? WatchlistTone.Positive
            : rounded < 0m ? WatchlistTone.Negative
            : WatchlistTone.Neutral;
        return new WatchlistChangeCell(SignedPercent(change), tone, change);
    }

    public static WatchlistMoversView BuildMovers(IEnumerable<WatchlistRowView> rows)
    {
        var withChange = rows
            .Where(row => row.IsAvailable && row.Change24Hours.Value.HasValue)
            .ToArray();

        return new WatchlistMoversView(
            Take(withChange.Where(row => row.Change24Hours.Tone == WatchlistTone.Positive)
                .OrderByDescending(row => row.Change24Hours.Value)),
            Take(withChange.Where(row => row.Change24Hours.Tone == WatchlistTone.Negative)
                .OrderBy(row => row.Change24Hours.Value)));
    }

    private static IReadOnlyList<WatchlistMoverView> Take(IOrderedEnumerable<WatchlistRowView> ordered) =>
        ordered
            .ThenBy(row => row.AssetId, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumMovers)
            .Select(row => new WatchlistMoverView(
                row.AssetId,
                row.Name,
                row.Change24Hours.Text,
                row.Change24Hours.Tone))
            .ToArray();

    public static string ToneClass(WatchlistTone tone) =>
        tone switch
        {
            WatchlistTone.Positive => "watchlist-tone--positive",
            WatchlistTone.Negative => "watchlist-tone--negative",
            WatchlistTone.Neutral => "watchlist-tone--neutral",
            _ => "watchlist-tone--unavailable"
        };
}
