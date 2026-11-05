using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using _bootstrap_scaffold.Localization;

namespace Argus.Authentication.Tests;

[Collection(TestAuthenticationServiceCollection.Name)]
public sealed class LocalizationTests
{
    private static readonly Regex TranslationCall = new(
        "(?<![A-Za-z0-9_])T\\(\\s*\"((?:[^\"\\\\]|\\\\.)*)\"",
        RegexOptions.CultureInvariant);

    [Fact]
    public void Polish_is_the_default_language_and_returns_the_source_text()
    {
        Assert.Equal(UiLanguage.Polish, Lang.Current);
        Assert.Equal("Pulpit", Lang.T("Pulpit"));
    }

    [Fact]
    public void English_scope_translates_and_restores_the_previous_language()
    {
        using (Lang.Use(UiLanguage.English))
        {
            Assert.Equal("Dashboard", Lang.T("Pulpit"));
            Assert.Equal("en", Lang.HtmlLang);
        }

        Assert.Equal("Pulpit", Lang.T("Pulpit"));
    }

    [Fact]
    public void Each_area_maps_a_polish_key_to_a_single_non_empty_english_text()
    {
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (area, entries) in EnglishCatalog.Areas)
        {
            foreach (var (pl, en) in entries)
            {
                Assert.False(string.IsNullOrWhiteSpace(en), $"{area}: empty English text for '{pl}'.");
                if (seen.TryGetValue(pl, out var existing))
                {
                    Assert.True(existing == en, $"{area}: '{pl}' has conflicting translations '{existing}' and '{en}'.");
                }

                seen[pl] = en;
            }
        }
    }

    [Fact]
    public void Every_translated_literal_in_the_application_has_an_english_entry()
    {
        var missing = new List<string>();
        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file);
            Assert.False(
                Regex.IsMatch(text, "(?<![A-Za-z0-9_])T\\(\\s*\\$\""),
                $"{file} passes an interpolated string to T(); use a format key with arguments instead.");
            foreach (Match match in TranslationCall.Matches(text))
            {
                var key = Regex.Unescape(match.Groups[1].Value);
                if (!Lang.EnglishEntries.ContainsKey(key))
                {
                    missing.Add($"{Path.GetFileName(file)}: {key}");
                }
            }
        }

        Assert.True(missing.Count == 0, "Missing English entries:\n" + string.Join("\n", missing.Distinct()));
    }

    [Fact]
    public void English_copy_stays_descriptive_without_advice_or_forecast_language()
    {
        var banned = new[] { "buy", "sell", "recommend", "forecast", "profit", "guarantee", "best ", "risky", "you should" };
        foreach (var en in Lang.EnglishEntries.Values)
        {
            var lower = en.ToLowerInvariant();
            foreach (var word in banned)
            {
                Assert.False(lower.Contains(word), $"English text contains '{word}': {en}");
            }
        }
    }

    [Fact]
    public async Task Language_and_theme_cookies_switch_the_rendered_shell()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var polish = await client.GetStringAsync("/analysis");
        Assert.Contains("<html lang=\"pl\" data-theme=\"dark\">", polish);
        Assert.Matches(@"(?s)</svg>\s*Pulpit</a>", polish);

        var token = Regex.Match(polish, "name=\"__RequestVerificationToken\"[^>]+value=\"([^\"]+)\"").Groups[1].Value;
        using var language = await client.PostAsync("/preferences/Language", Form(token, ("language", "en"), ("returnUrl", "/analysis")));
        using var theme = await client.PostAsync("/preferences/Theme", Form(token, ("theme", "light"), ("returnUrl", "https://evil.example/")));

        Assert.Equal(HttpStatusCode.Redirect, language.StatusCode);
        Assert.Equal("/analysis", language.Headers.Location?.OriginalString);
        Assert.Equal("/", theme.Headers.Location?.OriginalString);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/analysis");
        request.Headers.Add("Cookie", $"{Lang.CookieName}=en; {Theme.CookieName}=light");
        using var response = await client.SendAsync(request);
        var english = await response.Content.ReadAsStringAsync();

        Assert.Contains("<html lang=\"en\" data-theme=\"light\">", english);
        Assert.Matches(@"(?s)</svg>\s*Dashboard</a>", english);
        Assert.Contains(SetCookie(language), value => value.StartsWith($"{Lang.CookieName}=en", StringComparison.Ordinal));
        Assert.Contains(SetCookie(theme), value => value.StartsWith($"{Theme.CookieName}=light", StringComparison.Ordinal));
    }

    private static FormUrlEncodedContent Form(string token, params (string Key, string Value)[] values) =>
        new([new KeyValuePair<string, string>("__RequestVerificationToken", token),
            .. values.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value))]);

    private static IEnumerable<string> SetCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values) ? values : [];

    private static IEnumerable<string> SourceFiles()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, ".bootstrap-scaffold.csproj")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Repository root not found.");
        }

        return Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) || path.EndsWith(".cshtml", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}Localization{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}.", StringComparison.Ordinal));
    }
}
