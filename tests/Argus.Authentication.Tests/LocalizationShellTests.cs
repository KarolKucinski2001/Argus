using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using _bootstrap_scaffold.Localization;

namespace Argus.Authentication.Tests;

[Collection(TestAuthenticationServiceCollection.Name)]
public sealed class LocalizationShellTests
{
    [Fact]
    public async Task Landing_page_renders_english_copy_when_language_cookie_is_english()
    {
        var html = await GetEnglishHtml("/");

        Assert.Contains("Understand the market before you choose assets.", html);
        Assert.Contains("does not offer personalized investment advice", html);
        Assert.Contains("Illustrative data", html);
        Assert.DoesNotContain("Zrozum rynek", html);
    }

    [Fact]
    public async Task Account_page_renders_english_copy_and_sample_figures()
    {
        var html = await GetEnglishHtml("/account");

        Assert.Contains("Profile information", html);
        Assert.Contains("Sample data", html);
        Assert.Contains("+18.4%", html);
        Assert.Contains("Results are educational and do not constitute investment advice.", html);
        Assert.DoesNotContain("Przykładowe dane", html);
    }

    [Fact]
    public async Task Account_error_translates_the_friendly_message()
    {
        var html = await GetEnglishHtml("/account/error?code=access-denied");

        Assert.Contains("Access was denied. You can try again or return to Argus.", html);
        Assert.Contains("Back to Argus", html);
    }

    private static async Task<string> GetEnglishHtml(string path)
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", $"{Lang.CookieName}=en");

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
    }
}
