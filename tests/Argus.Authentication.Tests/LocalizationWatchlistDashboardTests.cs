using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using _bootstrap_scaffold.Dashboard;
using _bootstrap_scaffold.Localization;
using _bootstrap_scaffold.Watchlist;

namespace Argus.Authentication.Tests;

[Collection(TestAuthenticationServiceCollection.Name)]
public sealed class LocalizationWatchlistDashboardTests
{
    private static readonly WebApplicationFactoryClientOptions ClientOptions = new()
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    };

    [Fact]
    public void Watchlist_presenter_formats_english_text_and_numbers()
    {
        using (Lang.Use(UiLanguage.English))
        {
            Assert.Equal("no data", WatchlistTablePresenter.PriceText(null, "USD"));
            Assert.Equal("1,234.50 USD", WatchlistTablePresenter.PriceText(1234.5m, "USD"));
            Assert.Equal("+1.50%", WatchlistTablePresenter.Change(1.5m).Text);
            Assert.Equal("no data", WatchlistTablePresenter.Change(null).Text);
        }

        Assert.Equal("1 234,50 USD", WatchlistTablePresenter.PriceText(1234.5m, "USD").Replace('\u00A0', ' ').Replace('\u202F', ' '));
    }

    [Fact]
    public void Dashboard_greeting_is_translated()
    {
        using (Lang.Use(UiLanguage.English))
        {
            Assert.Equal("Hello, Jan!", DashboardPresenter.Greeting("Jan"));
            Assert.Equal("Hello!", DashboardPresenter.Greeting(null));
        }
    }

    [Fact]
    public async Task Watchlist_page_renders_in_english()
    {
        await using var factory = new WatchlistTestFactory();
        await factory.Store.SaveAsync("test-subject", ["DELISTED"]);
        using var client = factory.CreateClient(ClientOptions);
        client.DefaultRequestHeaders.Add("Cookie", "argus-lang=en");

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/watchlist"));

        Assert.Contains("<h1 id=\"watchlist-title\">Watchlist</h1>", html);
        Assert.Contains("1 asset", html);
        Assert.Contains("Unavailable", html);
        Assert.Contains(">Biggest changes</h2>", html);
        Assert.Contains("title=\"Daily closing prices over the last 12 months.", html);
        Assert.Contains(">Tech giants</strong>", html);
        Assert.DoesNotContain("Lista obserwowanych", html);
        Assert.DoesNotContain("Niedostępne", html);
    }

    [Fact]
    public async Task Dashboard_page_renders_in_english()
    {
        await using var factory = new WatchlistTestFactory(new StaticMarketDataService());
        using var client = factory.CreateClient(ClientOptions);
        client.DefaultRequestHeaders.Add("Cookie", "argus-lang=en");

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/dashboard"));

        Assert.Contains("Hello, Test!", html);
        Assert.Contains("Market overview", html);
        Assert.Contains("Key takeaways", html);
        Assert.Contains("Your virtual portfolio", html);
        Assert.Contains("Your watchlist is empty", html);
        Assert.DoesNotContain("Przegląd rynku", html);
        Assert.DoesNotContain("Najważniejsze wnioski", html);
    }
}
