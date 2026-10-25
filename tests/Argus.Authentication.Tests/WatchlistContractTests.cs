using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using _bootstrap_scaffold.MarketData;
using _bootstrap_scaffold.Watchlist;

namespace Argus.Authentication.Tests;

public sealed class WatchlistContractTests
{
    [Fact]
    public async Task Watchlist_requires_authentication()
    {
        await using var factory = new UnauthenticatedWatchlistFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/watchlist");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("test.invalid/authorize", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Watchlist_renders_empty_state_for_authenticated_user()
    {
        await using var factory = new WatchlistTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/watchlist");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Lista obserwowanych jest pusta", html);
        Assert.Contains("Przejdź do analizy", html);
        Assert.Contains("href=\"/watchlist\">Obserwowane", html);
        Assert.Contains("href=\"/analysis\">Przejdź do analizy", html);
    }

    [Fact]
    public async Task Analysis_save_handler_persists_selected_asset()
    {
        await using var factory = new WatchlistTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync(
            "/analysis?handler=Save",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("SelectedAssetIds", "AAPL")
            ]));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/analysis", response.Headers.Location?.OriginalString);
        Assert.Contains("AAPL", factory.Store.Entries.Select(entry => entry.AssetId));

        using var navResponse = await client.GetAsync("/watchlist");
        var html = await navResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("href=\"/analysis?handler=", html);
        Assert.DoesNotContain("href=\"/watchlist?handler=", html);
    }

    [Fact]
    public async Task Watchlist_store_keeps_duplicates_capped_and_subject_scoped()
    {
        var store = new InMemoryWatchlistStore();

        var firstSave = await store.SaveAsync("subject-a", ["AAPL"]);
        var duplicateSave = await store.SaveAsync("subject-a", ["aapl"]);
        var otherSubject = await store.ListAsync("subject-b");

        Assert.Equal(WatchlistSaveStatus.Saved, firstSave.Items.Single().Status);
        Assert.Equal(WatchlistSaveStatus.Duplicate, duplicateSave.Items.Single().Status);
        Assert.Empty(otherSubject.Entries);

        await store.SaveAsync(
            "subject-a",
            Enumerable.Range(1, WatchlistConstants.MaximumEntries - 1)
                .Select(index => $"ASSET-{index}"));
        var overCapacity = await store.SaveAsync("subject-a", ["ASSET-51"]);

        Assert.Equal(
            WatchlistSaveStatus.CapacityExceeded,
            overCapacity.Items.Single().Status);
    }

    [Fact]
    public async Task Watchlist_renders_unavailable_asset_and_removes_it()
    {
        await using var factory = new WatchlistTestFactory();
        await factory.Store.SaveAsync("test-subject", ["DELISTED"]);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var getResponse = await client.GetAsync("/watchlist");
        var getHtml = await getResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Contains("Niedostępne", getHtml);
        Assert.Contains("DELISTED", getHtml);

        var token = await GetAntiforgeryTokenAsync(client, "/watchlist");
        using var removeResponse = await client.PostAsync(
            "/watchlist?handler=Remove",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("assetId", "DELISTED")
            ]));

        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        Assert.Empty((await factory.Store.ListAsync("test-subject")).Entries);
    }

    [Fact]
    public async Task Watchlist_renders_saved_asset_with_signed_change_from_market_data()
    {
        await using var factory = new WatchlistTestFactory(new StaticMarketDataService());
        await factory.Store.SaveAsync("test-subject", ["AAPL"]);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/watchlist");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AAPL", html);
        Assert.Matches("watchlist-tone--positive\">(\\+|&#x2B;)[0-9]+,[0-9]{2}%", html);
        Assert.Matches("name=\"SelectedAssetIds\"\\s+value=\"AAPL\"", html);
    }

    [Fact]
    public async Task Watchlist_still_renders_rows_when_market_data_fails()
    {
        await using var factory = new WatchlistTestFactory(new ThrowingMarketDataService());
        await factory.Store.SaveAsync("test-subject", ["AAPL"]);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/watchlist");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AAPL", html);
        Assert.Contains("brak danych", html);
    }

    [Fact]
    public async Task Testing_ui_state_fixture_renders_all_seven_states()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/watchlist/ui-states");
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

        Assert.Contains("watchlist-table__row--hover", html);
        Assert.Contains("watchlist-table__remove--focus-visible", html);
        Assert.Contains("watchlist-tab--placeholder", html);
        Assert.Contains("aria-disabled=\"true\"", html);
        Assert.Contains("status-card status-card--error\" role=\"alert\"", html);
        Assert.Contains("watchlist-empty", html);
        Assert.Contains("watchlist-skeleton", html);
        Assert.Contains("aria-busy=\"true\"", html);
    }

    [Fact]
    public async Task Ui_state_fixture_is_not_available_outside_testing()
    {
        await using var factory = new DevelopmentWatchlistFixtureFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/watchlist/ui-states");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(        HttpClient client,
        string path = "/analysis")
    {
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]+value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, "The analysis page did not render an antiforgery token.");
        return match.Groups[1].Value;
    }
}

internal sealed class DevelopmentWatchlistFixtureFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        TestEntraConfiguration.Apply(builder);
    }
}

internal sealed class UnauthenticatedWatchlistFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        TestEntraConfiguration.Apply(builder);
        builder.ConfigureServices(services =>
        {
            TestAuthenticationService.Reset();
            services.Replace(
                ServiceDescriptor.Singleton<IAuthenticationService, TestAuthenticationService>());
            services.RemoveAll<IWatchlistStore>();
            services.AddSingleton<IWatchlistStore, InMemoryWatchlistStore>();
        });
    }
}

internal sealed class WatchlistTestFactory(IMarketDataService? marketDataService = null)
    : WebApplicationFactory<Program>
{
    public InMemoryWatchlistStore Store { get; } = new();

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
            services.AddSingleton<IWatchlistStore>(Store);
            if (marketDataService is not null)
            {
                services.RemoveAll<IMarketDataService>();
                services.AddSingleton(marketDataService);
            }
        });
    }
}

internal sealed class InMemoryWatchlistStore : IWatchlistStore
{
    private readonly Dictionary<string, Dictionary<string, WatchlistEntry>> entries =
        new(StringComparer.Ordinal);

    public IReadOnlyCollection<WatchlistEntry> Entries =>
        entries.Values.SelectMany(values => values.Values).ToArray();

    public Task<WatchlistListResult> ListAsync(
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        var result = entries.TryGetValue(subjectId, out var subjectEntries)
            ? subjectEntries.Values.OrderByDescending(entry => entry.SavedAtUtc).ToArray()
            : Array.Empty<WatchlistEntry>();
        return Task.FromResult(new WatchlistListResult(
            WatchlistListStatus.Succeeded,
            result));
    }

    public Task<WatchlistSaveResult> SaveAsync(
        string subjectId,
        IEnumerable<string> assetIds,
        CancellationToken cancellationToken = default)
    {
        if (!entries.TryGetValue(subjectId, out var subjectEntries))
        {
            subjectEntries = new Dictionary<string, WatchlistEntry>(
                StringComparer.OrdinalIgnoreCase);
            entries[subjectId] = subjectEntries;
        }

        var normalized = WatchlistPolicy.NormalizeAssetIds(assetIds);
        var results = WatchlistPolicy.ClassifySave(
            subjectEntries.Keys,
            normalized,
            WatchlistConstants.MaximumEntries);
        foreach (var item in results.Where(item => item.Status == WatchlistSaveStatus.Saved))
        {
            subjectEntries[item.AssetId] = new WatchlistEntry(item.AssetId, DateTimeOffset.UtcNow);
        }

        return Task.FromResult(new WatchlistSaveResult(results));
    }

    public Task<WatchlistRemoveResult> RemoveAsync(
        string subjectId,
        string assetId,
        CancellationToken cancellationToken = default)
    {
        var normalized = WatchlistIdentity.TryNormalizeAssetId(assetId);
        if (normalized is null ||
            !entries.TryGetValue(subjectId, out var subjectEntries) ||
            !subjectEntries.Remove(normalized))
        {
            return Task.FromResult(new WatchlistRemoveResult(
                assetId,
                WatchlistRemoveStatus.NotFound));
        }

        return Task.FromResult(new WatchlistRemoveResult(
            normalized,
            WatchlistRemoveStatus.Removed));
    }
}

internal sealed class ThrowingMarketDataService : IMarketDataService
{
    public Task<MarketDataServiceResult> GetAsync(
        MarketDataRequest? request,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("market data unavailable");
}

internal sealed class StaticMarketDataService : IMarketDataService
{
    public Task<MarketDataServiceResult> GetAsync(
        MarketDataRequest? request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var results = request!.Assets.Select(asset => new MarketDataResult(
            asset,
            request.History,
            new MarketQuote(
                110m,
                "USD",
                now,
                new MarketDataFreshness(now, now, now.AddHours(1), false),
                MarketDataQuality.Complete),
            Enumerable.Range(1, 45)
                .Select(day => new HistoricalPricePoint(
                    now.AddDays(-day), null, null, null, 100m, null, "USD", false))
                .ToArray(),
            new MarketDataSource("test", null, false, null),
            new MarketDataFreshness(now, now, now.AddHours(1), false),
            MarketDataQuality.Complete,
            [])).ToArray();
        return Task.FromResult(MarketDataServiceResult.Success(new MarketDataResponse(results)));
    }
}