using Xunit;
using _bootstrap_scaffold.Watchlist;

namespace Argus.Authentication.Tests;

public sealed class WatchlistTablePresenterTests
{
    private static readonly DateTimeOffset Saved = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static WatchlistRowView Row(string id, decimal? change24h, bool available = true) =>
        WatchlistTablePresenter.BuildRow(
            id,
            id,
            "Equity",
            Saved,
            available,
            new WatchlistMarketSnapshot(10m, "USD", change24h, null, null, null));

    [Theory]
    [InlineData(2.36, "+2,36%")]
    [InlineData(-0.841, "-0,84%")]
    [InlineData(0, "0,00%")]
    [InlineData(-0.001, "0,00%")]
    [InlineData(12.456, "+12,46%")]
    public void SignedPercent_uses_polish_format_with_sign(double value, string expected) =>
        Assert.Equal(expected, WatchlistTablePresenter.SignedPercent((decimal)value));

    [Fact]
    public void Change_maps_tone_from_rounded_value()
    {
        Assert.Equal(WatchlistTone.Positive, WatchlistTablePresenter.Change(1.5m).Tone);
        Assert.Equal(WatchlistTone.Negative, WatchlistTablePresenter.Change(-1.5m).Tone);
        Assert.Equal(WatchlistTone.Neutral, WatchlistTablePresenter.Change(0.001m).Tone);
        var missing = WatchlistTablePresenter.Change(null);
        Assert.Equal(WatchlistTone.Unavailable, missing.Tone);
        Assert.Equal("brak danych", missing.Text);
    }

    [Fact]
    public void PriceText_formats_amount_with_currency_or_reports_missing_data()
    {
        Assert.Equal("178,42 USD", WatchlistTablePresenter.PriceText(178.42m, "USD"));
        Assert.Equal("brak danych", WatchlistTablePresenter.PriceText(null, "USD"));
    }

    [Fact]
    public void BuildRow_without_snapshot_or_unavailable_shows_placeholders()
    {
        var noData = WatchlistTablePresenter.BuildRow("AAPL", "Apple", "Equity", Saved, true, null);
        var unavailable = WatchlistTablePresenter.BuildRow(
            "GONE",
            "GONE",
            "Niedostępne",
            Saved,
            false,
            new WatchlistMarketSnapshot(10m, "USD", 1m, 1m, 1m, null));

        foreach (var row in new[] { noData, unavailable })
        {
            Assert.Equal("brak danych", row.PriceText);
            Assert.Equal("brak danych", row.Change24Hours.Text);
            Assert.Equal("brak danych", row.Change7Days.Text);
            Assert.Equal("brak danych", row.Change30Days.Text);
            Assert.Null(row.Sparkline);
        }
    }

    [Fact]
    public void BuildMovers_orders_by_change_caps_at_five_and_splits_by_sign()
    {
        var rows = new[]
        {
            Row("A", 1m), Row("B", 7m), Row("C", 3m), Row("D", 5m), Row("E", 2m),
            Row("F", 9m), Row("G", -4m), Row("H", -1m), Row("I", null), Row("J", 0m),
            Row("K", 50m, available: false)
        };

        var movers = WatchlistTablePresenter.BuildMovers(rows);

        Assert.Equal(["F", "B", "D", "C", "E"], movers.Gainers.Select(item => item.AssetId));
        Assert.All(movers.Gainers, item => Assert.Equal(WatchlistTone.Positive, item.Tone));
        Assert.Equal("+9,00%", movers.Gainers[0].ChangeText);
        Assert.Equal(["G", "H"], movers.Losers.Select(item => item.AssetId));
        Assert.All(movers.Losers, item => Assert.Equal(WatchlistTone.Negative, item.Tone));
    }

    [Fact]
    public void BuildMovers_is_empty_without_data()
    {
        var movers = WatchlistTablePresenter.BuildMovers([Row("A", null)]);

        Assert.Empty(movers.Gainers);
        Assert.Empty(movers.Losers);
    }
}
