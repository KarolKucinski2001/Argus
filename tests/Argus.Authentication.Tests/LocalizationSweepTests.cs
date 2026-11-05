using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Argus.Authentication.Tests;

[Collection(TestAuthenticationServiceCollection.Name)]
public sealed class LocalizationSweepTests
{
    private static readonly Regex PolishLetters = new(
        "[ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]|\\b(?:brak danych|Analiza|Pulpit|Obserwowane|Strategie|Aktywa|aktyw[ao]?|Wyloguj|Zapisz|Konto|Portfel|Wyniki|dni|Przejdz|Zobacz|Otworz|Ranking aktywow)\\b",
        RegexOptions.Compiled);

    [Theory]
    [InlineData("/")]
    [InlineData("/account")]
    [InlineData("/dashboard")]
    [InlineData("/watchlist")]
    [InlineData("/watchlist/ui-states")]
    [InlineData("/analysis")]
    [InlineData("/analysis/ui-state-fixture")]
    [InlineData("/analysis/ui-state-fixture?view=ranked")]
    [InlineData("/strategies")]
    [InlineData("/strategies/ui-states")]
    [InlineData("/strategies/profiles")]
    [InlineData("/strategies/profiles/berkshire-style")]
    [InlineData("/strategies/portfolios")]
    public async Task English_pages_contain_no_polish_text(string path)
    {
        await using var factory = new WatchlistTestFactory(new StaticMarketDataService());
        await factory.Store.SaveAsync("test-subject", ["AAPL", "BTC-USD"]);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add("Cookie", "argus-lang=en");

        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        html = Regex.Replace(html, "(?s)<(script|style)\\b.*?</\\1>", " ");
        var text = WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>|\\s+", " "));
        var attributes = Regex.Matches(html, "(?:aria-label|title|placeholder|alt)=\"([^\"]*)\"")
            .Select(match => WebUtility.HtmlDecode(match.Groups[1].Value));

        var leaks = Regex.Split(text, "\\s{2,}")
            .Concat(attributes)
            .Select(fragment => fragment.Trim())
            .Where(fragment => PolishLetters.IsMatch(fragment))
            // The switch back to Polish is labelled in its target language (lang="pl").
            .Where(fragment => fragment != "Zmień język na polski")
            .Distinct()
            .ToArray();

        Assert.True(leaks.Length == 0, $"Polish text on {path}: {string.Join(" | ", leaks)}");
    }
}
