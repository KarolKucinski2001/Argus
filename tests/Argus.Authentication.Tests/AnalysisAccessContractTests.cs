using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Argus.Authentication.Tests;

public sealed class AnalysisAccessContractTests
{
    [Fact]
    public async Task Unauthenticated_analysis_page_uses_the_explicit_provider_challenge()
    {
        await using var factory = new UnauthenticatedAnalysisFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/analysis");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("test.invalid", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Authenticated_analysis_page_renders_the_configured_assets_and_limit()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/analysis");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AAPL", html);
        Assert.Contains("MSFT", html);
        Assert.Contains("BTC-USD", html);
        Assert.Contains("ETH-USD", html);
        Assert.Contains("maksymalnie 5", html);
    }

    [Fact]
    public async Task Valid_analysis_submission_renders_an_illustrative_started_state()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync(
            "/analysis",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("SelectedAssetIds", "AAPL"),
                new KeyValuePair<string, string>("SelectedAssetIds", "BTC-USD")
            ]));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Analiza rozpoczęta", html);
        Assert.Contains("ilustracyjnych danych", html);
        Assert.Contains("AAPL", html);
        Assert.Contains("BTC-USD", html);
    }

    [Fact]
    public async Task Invalid_analysis_submission_renders_bounded_inline_validation()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync(
            "/analysis",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("SelectedAssetIds", "AAPL"),
                new KeyValuePair<string, string>("SelectedAssetIds", "aapl")
            ]));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("tylko raz", html);
        Assert.DoesNotContain("illustrative-development", html);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/analysis");
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]+value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, "The analysis page did not render an antiforgery token.");
        return match.Groups[1].Value;
    }
}

internal sealed class UnauthenticatedAnalysisFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        TestEntraConfiguration.Apply(builder);
        builder.ConfigureServices(services =>
        {
            TestAuthenticationService.Reset();
            services.Replace(ServiceDescriptor.Singleton<IAuthenticationService, TestAuthenticationService>());
        });
    }
}
