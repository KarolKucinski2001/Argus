using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using _bootstrap_scaffold.Strategies;
using _bootstrap_scaffold.Watchlist;

namespace Argus.Authentication.Tests;

public sealed class StrategyProfilePresenterTests
{
    [Fact]
    public void Curated_profiles_are_small_complete_and_weights_sum_to_100()
    {
        Assert.Equal(3, CuratedProfileCatalog.Profiles.Count);
        foreach (var profile in CuratedProfileCatalog.Profiles)
        {
            Assert.InRange(profile.Holdings.Count, 5, 8);
            Assert.Equal(100m, profile.Holdings.Sum(holding => holding.Weight));
            Assert.False(string.IsNullOrWhiteSpace(profile.SourceLabel));
        }
    }

    [Fact]
    public void Detail_orders_holdings_and_groups_shares_by_sector_and_class()
    {
        var detail = StrategyProfilePresenter.BuildDetail(CuratedProfileCatalog.Find("all-weather-style")!);

        Assert.Equal("TLT", detail.Holdings[0].AssetId);
        Assert.Equal("40%", detail.Holdings[0].WeightText);
        Assert.Equal("7,5%", detail.Holdings[^1].WeightText);
        Assert.Equal("Obligacje", detail.AssetClassShares[0].Label);
        Assert.Equal("55%", detail.AssetClassShares[0].ShareText);
        Assert.Equal("Dane na dzień: 2025-06-30", detail.AsOfText);
    }

    [Fact]
    public void Factors_describe_composition_without_advice_or_forecast_language()
    {
        var banned = new[] { "kup", "sprzedaj", "polecam", "zalecamy", "warto", "prognoz", "zysk", "gwarant", "najlepsz" };
        foreach (var profile in CuratedProfileCatalog.Profiles)
        {
            var detail = StrategyProfilePresenter.BuildDetail(profile);
            Assert.NotEmpty(detail.Factors);
            var text = string.Join(" ", detail.Factors).ToLowerInvariant() +
                       " " + profile.Description.ToLowerInvariant() +
                       " " + CuratedProfileCatalog.Disclaimer.ToLowerInvariant();
            foreach (var word in banned)
            {
                Assert.DoesNotContain(word, text);
            }
        }
    }

    [Fact]
    public void Copy_snapshots_profile_holdings_and_keeps_as_of_date()
    {
        var profile = CuratedProfileCatalog.Find("berkshire-style")!;
        var copy = StrategyProfilePresenter.CreateCopy(profile, "a".PadRight(32, 'b'), DateTimeOffset.UnixEpoch);

        Assert.Equal(profile.Holdings.Count, copy.Holdings.Count);
        Assert.Equal(profile.AsOf, copy.AsOf);
        Assert.StartsWith("Symulacja:", copy.Name);
        Assert.Equal("1 pozycja", StrategyProfilePresenter.HoldingsCount(1));
        Assert.Equal("5 pozycji", StrategyProfilePresenter.HoldingsCount(5));
    }

    [Fact]
    public void Unverified_profiles_are_explicitly_labelled_as_illustrations()
    {
        var sAndP = CuratedProfileCatalog.Find("sp500-top-holdings")!;
        var allWeather = CuratedProfileCatalog.Find("all-weather-style")!;

        Assert.Contains("Ilustracyjny", sAndP.Name);
        Assert.Contains("bez zatwierdzonego źródła", sAndP.SourceLabel);
        Assert.Contains("stylu All Weather", allWeather.Name);
        Assert.Contains("nieoficjalna ilustracja", allWeather.SourceLabel);
    }
}

public sealed class StrategyPortfolioContractTests
{
    private static HttpClient NewClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    [Theory]
    [InlineData("/strategies/profiles")]
    [InlineData("/strategies/profiles/berkshire-style")]
    [InlineData("/strategies/portfolios")]
    public async Task Strategy_pages_require_authentication(string path)
    {
        await using var factory = new UnauthenticatedWatchlistFactory();
        using var client = NewClient(factory);

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("test.invalid/authorize", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Profiles_list_and_detail_show_disclaimer_and_as_of_date()
    {
        await using var factory = new StrategyTestFactory();
        using var client = NewClient(factory);

        var list = WebUtility.HtmlDecode(await client.GetStringAsync("/strategies/profiles"));
        var detail = WebUtility.HtmlDecode(await client.GetStringAsync("/strategies/profiles/berkshire-style"));

        foreach (var html in new[] { list, detail })
        {
            Assert.Contains("symulacja edukacyjna", html);
            Assert.Contains("Dane na dzień: 2025-06-30", html);
            Assert.Contains(CuratedProfileCatalog.ProvenanceNote, html);
            Assert.Contains(CuratedProfileCatalog.ApproximateExplanation, html);
        }

        Assert.Equal(3, Regex.Matches(list, "data-strategy-profile=").Count);
        Assert.Contains("AAPL", detail);
        Assert.Contains("Czynniki opisujące skład", detail);
    }

    [Fact]
    public async Task Unknown_profile_returns_not_found()
    {
        await using var factory = new StrategyTestFactory();
        using var client = NewClient(factory);

        using var response = await client.GetAsync("/strategies/profiles/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Copy_requires_antiforgery_token()
    {
        await using var factory = new StrategyTestFactory();
        using var client = NewClient(factory);

        using var response = await client.PostAsync(
            "/strategies/profiles/berkshire-style?handler=Copy",
            new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Portfolios.All);
    }

    [Fact]
    public async Task Copy_creates_private_portfolio_that_can_be_listed_and_deleted()
    {
        await using var factory = new StrategyTestFactory();
        using var client = NewClient(factory);

        var token = await GetTokenAsync(client, "/strategies/profiles/berkshire-style");
        using var copy = await client.PostAsync(
            "/strategies/profiles/berkshire-style?handler=Copy",
            Form(token));
        Assert.Equal(HttpStatusCode.Redirect, copy.StatusCode);
        Assert.Equal("/strategies/portfolios", copy.Headers.Location?.OriginalString);

        var stored = Assert.Single(factory.Portfolios.All);
        Assert.Equal("test-subject", stored.Subject);
        Assert.Empty((await factory.Portfolios.ListAsync("someone-else")).Portfolios);

        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/strategies/portfolios"));
        Assert.Contains("Symulacja", html);
        Assert.Contains("Dane na dzień: 2025-06-30", html);
        Assert.Contains("symulacja edukacyjna", html);

        var deleteToken = await GetTokenAsync(client, "/strategies/portfolios");
        using var delete = await client.PostAsync(
            $"/strategies/portfolios?handler=Delete&id={stored.Portfolio.Id}",
            Form(deleteToken));
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Empty(factory.Portfolios.All);
    }

    [Fact]
    public async Task Cannot_delete_another_users_portfolio()
    {
        await using var factory = new StrategyTestFactory();
        var other = StrategyProfilePresenter.CreateCopy(
            CuratedProfileCatalog.Profiles[0],
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow);
        await factory.Portfolios.CreateAsync("someone-else", other);
        using var client = NewClient(factory);

        var token = await GetTokenAsync(client, "/strategies/profiles/berkshire-style");
        using var delete = await client.PostAsync(
            $"/strategies/portfolios?handler=Delete&id={other.Id}",
            Form(token));

        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Single(factory.Portfolios.All);
    }

    [Fact]
    public async Task Copy_reports_capacity_limit()
    {
        await using var factory = new StrategyTestFactory();
        for (var index = 0; index < SimulatedPortfolioConstants.MaximumPortfolios; index++)
        {
            await factory.Portfolios.CreateAsync(
                "test-subject",
                StrategyProfilePresenter.CreateCopy(
                    CuratedProfileCatalog.Profiles[0],
                    Guid.NewGuid().ToString("N"),
                    DateTimeOffset.UtcNow));
        }

        using var client = NewClient(factory);
        var token = await GetTokenAsync(client, "/strategies/profiles/berkshire-style");
        using var response = await client.PostAsync(
            "/strategies/profiles/berkshire-style?handler=Copy",
            Form(token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("limit", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        Assert.Equal(SimulatedPortfolioConstants.MaximumPortfolios, factory.Portfolios.All.Count);
    }

    private static FormUrlEncodedContent Form(string token) => new(
    [
        new KeyValuePair<string, string>("__RequestVerificationToken", token)
    ]);

    private static async Task<string> GetTokenAsync(HttpClient client, string path)
    {
        var html = WebUtility.HtmlDecode(await client.GetStringAsync(path));
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]+value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant);
        Assert.True(match.Success, "The page did not render an antiforgery token.");
        return match.Groups[1].Value;
    }
}

internal sealed class InMemorySimulatedPortfolioStore : ISimulatedPortfolioStore
{
    private readonly List<(string Subject, SimulatedPortfolio Portfolio)> items = [];

    public IReadOnlyList<(string Subject, SimulatedPortfolio Portfolio)> All => items.ToArray();

    public Task<SimulatedPortfolioListResult> ListAsync(
        string subjectId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new SimulatedPortfolioListResult(
            SimulatedPortfolioStatus.Succeeded,
            SimulatedPortfolioConstants.Order(
                items.Where(item => item.Subject == subjectId).Select(item => item.Portfolio))));

    public Task<SimulatedPortfolioStatus> CreateAsync(
        string subjectId,
        SimulatedPortfolio portfolio,
        CancellationToken cancellationToken = default)
    {
        if (items.Count(item => item.Subject == subjectId) >= SimulatedPortfolioConstants.MaximumPortfolios)
        {
            return Task.FromResult(SimulatedPortfolioStatus.CapacityExceeded);
        }

        items.Add((subjectId, portfolio));
        return Task.FromResult(SimulatedPortfolioStatus.Succeeded);
    }

    public Task<SimulatedPortfolioStatus> DeleteAsync(
        string subjectId,
        string portfolioId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(items.RemoveAll(item => item.Subject == subjectId && item.Portfolio.Id == portfolioId) > 0
            ? SimulatedPortfolioStatus.Succeeded
            : SimulatedPortfolioStatus.NotFound);
}

internal sealed class StrategyTestFactory : WebApplicationFactory<Program>
{
    public InMemorySimulatedPortfolioStore Portfolios { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        TestEntraConfiguration.Apply(builder);
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
            services.PostConfigure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(
                options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultForbidScheme = "Test";
                    options.DefaultSignInScheme = "Test";
                    options.DefaultSignOutScheme = "Test";
                });
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy =
                    new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Test")
                        .RequireAuthenticatedUser()
                        .Build();
            });
            services.RemoveAll<IWatchlistStore>();
            services.AddSingleton<IWatchlistStore, InMemoryWatchlistStore>();
            services.RemoveAll<ISimulatedPortfolioStore>();
            services.AddSingleton<ISimulatedPortfolioStore>(Portfolios);
        });
    }
}
