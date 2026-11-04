using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.Dashboard;
using _bootstrap_scaffold.Strategies;
using _bootstrap_scaffold.Watchlist;

namespace Argus.Authentication.Tests;

public sealed class DashboardAndStrategiesContractTests
{
    private static readonly Regex AdviceWording = new(
        @"\b(kup|sprzedaj|buy|sell)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static WebApplicationFactoryClientOptions ClientOptions => new()
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    };

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/strategies")]
    public async Task Pages_require_authentication(string path)
    {
        await using var factory = new UnauthenticatedWatchlistFactory();
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("test.invalid/authorize", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Dashboard_renders_greeting_sidebar_and_placeholder_badges()
    {
        await using var factory = new WatchlistTestFactory(new StaticMarketDataService());
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/dashboard");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Dzień dobry, Test!", html);
        Assert.Matches(@"(?s)href=""/dashboard""><svg.*?</svg>\s*Pulpit", html);
        Assert.Matches(@"(?s)href=""/strategies""><svg.*?</svg>\s*Profile strategii", html);
        Assert.Matches("class=\"app-nav__link app-nav__link--active\" aria-current=\"page\" href=\"/dashboard\"", html);
        Assert.DoesNotMatch(@"(?s)href=""/analysis""><svg.*?</svg>\s*Pulpit", html);
        Assert.Contains("Przykładowe dane", html);
        Assert.Contains("Przegląd rynku", html);
        Assert.Contains("data-market-tile=\"AAPL\"", html);
        Assert.Contains("watchlist-sparkline", html);
        Assert.Contains("Duże fundusze", html);
        Assert.DoesNotContain(DashboardPresenter.UnavailableMessage, html);
        Assert.DoesNotMatch(AdviceWording, html);

        var insights = Regex.Match(html, "(?s)<section[^>]*aria-labelledby=\"dashboard-insights-title\".*?</section>").Value;
        Assert.Contains("Najważniejsze wnioski", insights);
        Assert.DoesNotContain("Przykładowe dane", insights);
        Assert.True(
            html.IndexOf("dashboard-watchlist-title", StringComparison.Ordinal) <
            html.IndexOf("dashboard-market-title", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dashboard_renders_watchlist_rows_and_insights_for_saved_assets()
    {
        await using var factory = new WatchlistTestFactory(new StaticMarketDataService());
        await factory.Store.SaveAsync("test-subject", ["AAPL", "MSFT"]);
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/dashboard");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Twoja lista obserwowanych", html);
        Assert.Contains("data-dashboard-row=\"AAPL\"", html);
        Assert.Contains("data-dashboard-row=\"MSFT\"", html);
        Assert.Contains("data-dashboard-insight=\"AAPL\"", html);
        Assert.Contains("data-dashboard-insight=\"MSFT\"", html);
        Assert.Contains("+10,00%", html);
        Assert.Contains("href=\"/watchlist\"", html);
        Assert.DoesNotContain("Lista obserwowanych jest pusta", html);
        Assert.DoesNotMatch(AdviceWording, html);
        Assert.DoesNotContain(">Analiza</th>", html);
        Assert.DoesNotContain(">Analiza</a>", html);
        Assert.DoesNotContain("Przejdź do analizy", html);
        Assert.Contains("Zobacz wszystkie", html);
        Assert.Contains(
            $"title=\"{WatchlistTablePresenter.TrendHint}\">{WatchlistTablePresenter.TrendHeader}</th>",
            html);
    }

    [Fact]
    public async Task Dashboard_shows_empty_state_with_analysis_link()
    {
        await using var factory = new WatchlistTestFactory(new StaticMarketDataService());
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/dashboard");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Lista obserwowanych jest pusta", html);
        Assert.Contains("href=\"/analysis\">Przejdź do analizy", html);
        Assert.DoesNotContain("data-dashboard-row=", html);
        Assert.DoesNotContain("data-dashboard-insight=", html);
    }

    [Fact]
    public async Task Dashboard_degrades_when_watchlist_store_fails()
    {
        await using var factory = new WatchlistTestFactory(
            new StaticMarketDataService(),
            new FailingWatchlistStore());
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/dashboard");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(DashboardPresenter.WatchlistFailedMessage, html);
        Assert.DoesNotContain("Lista obserwowanych jest pusta", html);
        Assert.Contains("data-market-tile=\"AAPL\"", html);
        Assert.Contains("Poznaj profile strategii", html);
    }

    [Fact]
    public async Task Dashboard_shows_unavailable_saved_asset_without_requesting_it()
    {
        var market = new RecordingMarketDataService();
        await using var factory = new WatchlistTestFactory(market);
        await factory.Store.SaveAsync("test-subject", ["DELISTED", "AAPL"]);
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/dashboard");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-dashboard-row=\"DELISTED\"", html);
        Assert.Contains("Niedostępne", html);
        Assert.DoesNotContain("data-dashboard-insight=\"DELISTED\"", html);
        Assert.Equal(1, market.Calls);
        Assert.DoesNotContain("DELISTED", market.RequestedIds);
        Assert.Contains("AAPL", market.RequestedIds);
    }

    [Fact]
    public async Task Dashboard_degrades_when_market_data_fails()
    {
        await using var factory = new WatchlistTestFactory(new ThrowingMarketDataService());
        await factory.Store.SaveAsync("test-subject", ["AAPL"]);
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/dashboard");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(DashboardPresenter.UnavailableMessage, html);
        Assert.Contains("data-dashboard-row=\"AAPL\"", html);
        Assert.Contains("brak danych", html);
        Assert.DoesNotContain("data-dashboard-insight=", html);
        Assert.Contains("Poznaj profile strategii", html);
    }

    [Fact]
    public async Task Strategies_renders_profiles_samples_and_no_advice_wording()
    {
        await using var factory = new WatchlistTestFactory();
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/strategies");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        foreach (var profile in StrategyProfileCatalog.Profiles)
        {
            Assert.Contains(profile.Name, html);
        }

        Assert.Contains("Przykładowe dane", html);
        Assert.Contains("Zapisz alokację", html);
        Assert.Matches("<button[^>]*disabled[^>]*>Zapisz alokację", html);
        Assert.Matches("class=\"app-nav__link app-nav__link--active\" aria-current=\"page\" href=\"/strategies\"", html);
        Assert.DoesNotMatch(AdviceWording, html);
    }

    [Fact]
    public async Task Strategies_ui_state_fixture_renders_all_seven_states()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/strategies/ui-states");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        foreach (var label in new[]
        {
            "Domyślny", "Najechanie", "Fokus klawiatury", "Wyłączony",
            "Błąd", "Brak danych", "Ładowanie"
        })
        {
            Assert.Contains($">{label}</h2>", html);
        }

        Assert.Contains("strategy-card--hover", html);
        Assert.Contains("strategy-card__link--focus-visible", html);
        Assert.Contains("disabled", html);
        Assert.Contains("status-card status-card--error\" role=\"alert\"", html);
        Assert.Contains("strategy-empty", html);
        Assert.Contains("watchlist-skeleton", html);
        Assert.Contains("aria-busy=\"true\"", html);
    }

    [Fact]
    public async Task Strategies_ui_state_fixture_is_not_available_outside_testing()
    {
        await using var factory = new DevelopmentWatchlistFixtureFactory();
        using var client = factory.CreateClient(ClientOptions);

        using var response = await client.GetAsync("/strategies/ui-states");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Test User", null, "Test")]
    [InlineData("  Anna  Maria Nowak ", null, "Anna")]
    [InlineData(null, "jan.kowalski@example.test", "Jan")]
    [InlineData("", "x@example.test", "X")]
    [InlineData(null, null, null)]
    [InlineData("   ", "", null)]
    public void FirstName_prefers_display_name_then_email(string? name, string? email, string? expected)
    {
        Assert.Equal(expected, DashboardPresenter.FirstName(name, email));
    }

    [Fact]
    public void Greeting_includes_first_name_or_falls_back()
    {
        Assert.Equal("Dzień dobry, Jan!", DashboardPresenter.Greeting("Jan"));
        Assert.Equal("Dzień dobry!", DashboardPresenter.Greeting(null));
        Assert.Equal("Dzień dobry!", DashboardPresenter.Greeting(" "));
    }

    [Fact]
    public void BuildTiles_formats_market_snapshots_and_marks_missing_data()
    {
        var snapshot = new WatchlistMarketSnapshot(5248.34m, "USD", 1.24m, null, null, null);
        var tiles = DashboardPresenter.BuildTiles(
            ["AAPL", "BTC-USD"],
            new Dictionary<string, WatchlistMarketSnapshot>(StringComparer.OrdinalIgnoreCase)
            {
                ["AAPL"] = snapshot,
                ["NVDA"] = snapshot
            });

        Assert.Equal(DashboardPresenter.MarketAssetIds, tiles.Select(tile => tile.AssetId));
        var apple = tiles.Single(tile => tile.AssetId == "AAPL");
        Assert.True(apple.HasData);
        Assert.Equal("+1,24%", apple.Change.Text);
        Assert.Equal(WatchlistTone.Positive, apple.Change.Tone);
        Assert.Contains("USD", apple.PriceText);

        var crypto = tiles.Single(tile => tile.AssetId == "BTC-USD");
        Assert.False(crypto.HasData);
        Assert.Equal(WatchlistTablePresenter.MissingDataText, crypto.PriceText);

        var notAllowed = tiles.Single(tile => tile.AssetId == "NVDA");
        Assert.False(notAllowed.HasData);
        Assert.False(DashboardPresenter.IsMarketUnavailable(tiles));
    }

    [Fact]
    public void BuildTiles_without_snapshots_is_unavailable()
    {
        var tiles = DashboardPresenter.BuildTiles(
            DashboardPresenter.MarketAssetIds,
            new Dictionary<string, WatchlistMarketSnapshot>());

        Assert.Equal(DashboardPresenter.MarketAssetIds.Count, tiles.Count);
        Assert.True(DashboardPresenter.IsMarketUnavailable(tiles));
    }

    [Fact]
    public void RequestableAssetIds_are_limited_to_allowed_assets()
    {
        var ids = DashboardPresenter.RequestableAssetIds(["aapl", "ETH-USD", "OTHER"]);

        Assert.Equal(["AAPL", "ETH-USD"], ids);
    }

    [Fact]
    public void Market_asset_set_is_small_and_has_identities()
    {
        Assert.InRange(DashboardPresenter.MarketAssetIds.Count, 4, 6);
        Assert.All(
            DashboardPresenter.MarketAssetIds,
            id => Assert.NotEqual(AssetAccent.Neutral, AssetIdentityCatalog.For(id).Accent));
    }

    [Fact]
    public void Sample_allocation_weights_sum_to_one_hundred_on_supported_steps()
    {
        Assert.Equal(StrategyProfileCatalog.AllocationTotal, StrategyProfileCatalog.Allocation.Sum(slice => slice.Weight));
        Assert.All(
            StrategyProfileCatalog.Allocation,
            slice => Assert.True(slice.Weight is > 0 and <= 100 && slice.Weight % StrategyProfileCatalog.WeightStep == 0));
        Assert.Contains("20%", StrategyProfileCatalog.DonutLabel());
    }

    [Fact]
    public void Profiles_have_unique_ids_and_two_labelled_neutral_examples()
    {
        var profiles = StrategyProfileCatalog.Profiles;

        Assert.Equal(profiles.Count, profiles.Select(profile => profile.Id).Distinct().Count());
        var examples = profiles.Where(profile => profile.IsExample).ToArray();
        Assert.Equal(2, examples.Length);
        Assert.All(examples, profile => Assert.StartsWith("Profil publiczny: Przykład", profile.Name));
        Assert.DoesNotContain(StrategyProfileCatalog.DashboardProfiles, profile => profile.Id == "broad-market-benchmark");
        Assert.All(profiles, profile => Assert.InRange(profile.AccentIndex, 1, 4));
    }

    [Fact]
    public void Catalog_and_dashboard_copy_has_no_advice_wording()
    {
        var insights = DashboardPresenter.BuildInsights(
        [
            Insight("AAPL", 4m, ["Cena rosła w ostatnim tygodniu."], "Opis danych."),
            Insight("MSFT", -3m, [], "Cena spadała w ostatnim tygodniu.")
        ]);
        var texts = StrategyProfileCatalog.Profiles.SelectMany(profile => new[] { profile.Name, profile.Description })
            .Concat(StrategyProfileCatalog.OverviewMetrics.Select(metric => metric.Label))
            .Concat(StrategyProfileCatalog.Holdings.Select(holding => holding.Source))
            .Concat(insights.SelectMany(insight => new[] { insight.Name, insight.Text }))
            .Append(DashboardPresenter.UnavailableMessage)
            .Append(DashboardPresenter.WatchlistFailedMessage);

        Assert.All(texts, text => Assert.DoesNotMatch(AdviceWording, text));
    }

    private static AssetInsightView Insight(
        string id,
        decimal? sevenDayChange,
        string[] bullets,
        string takeaway = "Opis.",
        string? trendLabel = "Trend wzrostowy") =>
        new(
            new AssetIdentity(id, $"{id} Name", id[..1], AssetAccent.Neutral),
            InsightTone.Neutral,
            sevenDayChange,
            [],
            takeaway,
            null,
            null,
            null,
            trendLabel,
            "Akcje",
            "Dlaczego",
            bullets);

    [Fact]
    public void BuildInsights_limits_orders_by_absolute_change_and_breaks_ties_by_id()
    {
        var insights = DashboardPresenter.BuildInsights(
        [
            Insight("ZZZ", 5m, ["z"]),
            Insight("AAA", -5m, ["a"]),
            Insight("MID", 2m, ["m"]),
            Insight("BIG", -9m, ["b"]),
            Insight("LOW", 0.1m, ["l"])
        ]);

        Assert.Equal(["BIG", "AAA", "ZZZ"], insights.Select(insight => insight.AssetId));
    }

    [Fact]
    public void BuildInsights_skips_missing_data_and_falls_back_to_takeaway()
    {
        var insights = DashboardPresenter.BuildInsights(
        [
            Insight("NONE", null, ["x"]),
            Insight("AAA", 1m, ["pierwszy", "drugi"]),
            Insight("BBB", 0.5m, [], "Opis zastępczy.")
        ]);

        Assert.Equal(["AAA", "BBB"], insights.Select(insight => insight.AssetId));
        Assert.Equal("pierwszy", insights[0].Text);
        Assert.Equal("Opis zastępczy.", insights[1].Text);
        Assert.Equal("Trend wzrostowy", insights[0].TrendLabel);
        Assert.Empty(DashboardPresenter.BuildInsights([]));
    }

    [Fact]
    public void BuildWatchlistRows_limits_rows_flags_unavailable_and_missing_snapshots()
    {
        var now = DateTimeOffset.UtcNow;
        var entries = new[] { "AAPL", "MSFT", "GONE", "NVDA", "BTC-USD", "ETH-USD" }
            .Select((id, index) => new WatchlistEntry(id, now.AddMinutes(-index)))
            .ToArray();
        var snapshots = new Dictionary<string, WatchlistMarketSnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new(10m, "USD", 1m, 2m, 3m, null),
            ["GONE"] = new(10m, "USD", 1m, 2m, 3m, null)
        };

        var rows = DashboardPresenter.BuildWatchlistRows(
            entries,
            ["AAPL", "MSFT", "NVDA", "BTC-USD", "ETH-USD"],
            snapshots);

        Assert.Equal(DashboardPresenter.MaximumWatchlistRows, rows.Count);
        Assert.True(DashboardPresenter.HasMoreRows(entries.Length));
        Assert.False(DashboardPresenter.HasMoreRows(DashboardPresenter.MaximumWatchlistRows));
        Assert.Equal("+1,00%", rows.Single(row => row.AssetId == "AAPL").Change24Hours.Text);
        Assert.Equal(
            WatchlistTablePresenter.MissingDataText,
            rows.Single(row => row.AssetId == "MSFT").PriceText);
        var gone = rows.Single(row => row.AssetId == "GONE");
        Assert.False(gone.IsAvailable);
        Assert.Equal(WatchlistTablePresenter.MissingDataText, gone.PriceText);
    }

    [Fact]
    public void Holdings_have_valid_completeness()
    {
        Assert.NotEmpty(StrategyProfileCatalog.Holdings);
        Assert.All(StrategyProfileCatalog.Holdings, holding => Assert.InRange(holding.Completeness, 0, 100));
    }
}
