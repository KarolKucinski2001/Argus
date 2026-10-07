using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using _bootstrap_scaffold.MarketData;

namespace Argus.Authentication.Tests;

public sealed class MarketDataContractTests
{
    [Fact]
    public async Task Market_data_requires_authentication()
    {
        await using var factory = new RealSchemeFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.PostAsJsonAsync("/api/market-data", CreateRequest("AAPL"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Single_asset_request_returns_normalized_data_and_attribution()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateResult("AAPL"));
        await using var factory = new MarketDataTestFactory(provider);

        var response = await PostAsync(factory, CreateRequest("AAPL"));
        var data = await ReadResponseAsync(response);
        var result = Assert.Single(data.Results);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("AAPL", result.Asset.Id);
        Assert.Equal(215.50m, result.CurrentQuote?.Value);
        Assert.Equal("USD", result.CurrentQuote?.Currency);
        Assert.Equal("fake-provider", result.Source?.ProviderId);
        Assert.Equal("Fake provider attribution", result.Source?.Attribution);
        Assert.Equal(MarketDataQuality.Complete, result.Quality);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task Multi_asset_request_preserves_each_normalized_result()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateResult("AAPL"), CreateResult("BTC-USD", MarketAssetClass.Crypto));
        await using var factory = new MarketDataTestFactory(provider);

        var response = await PostAsync(factory, CreateRequest("AAPL", "BTC-USD"));
        var data = await ReadResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["AAPL", "BTC-USD"], data.Results.Select(result => result.Asset.Id));
        Assert.All(data.Results, result => Assert.Equal(MarketDataGranularity.Daily, result.RequestedHistory.Granularity));
    }

    [Fact]
    public async Task Mismatched_provider_result_is_rejected_and_not_cached()
    {
        var result = CreateResult("AAPL") with
        {
            RequestedHistory = CreateHistory() with
            {
                Start = CreateHistory().Start.AddDays(1)
            }
        };
        var provider = new FakeMarketDataProvider();
        provider.SetResults(result);
        await using var factory = new MarketDataTestFactory(provider);

        var first = Assert.Single((await ReadResponseAsync(await PostAsync(factory, CreateRequest("AAPL")))).Results);
        var second = Assert.Single((await ReadResponseAsync(await PostAsync(factory, CreateRequest("AAPL")))).Results);

        Assert.Equal(MarketDataErrorCategory.InvalidProviderResponse, first.Error?.Category);
        Assert.Equal(MarketDataErrorCategory.InvalidProviderResponse, second.Error?.Category);
        Assert.Equal(2, provider.CallCount);
    }

    [Fact]
    public async Task Partial_provider_evidence_keeps_usable_result_and_marks_missing_asset()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateResult("AAPL"));
        await using var factory = new MarketDataTestFactory(provider);

        var response = await PostAsync(factory, CreateRequest("AAPL", "MSFT"));
        var data = await ReadResponseAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MarketDataQuality.Complete, data.Results[0].Quality);
        Assert.Equal(MarketDataErrorCategory.UpstreamUnavailable, data.Results[1].Error?.Category);
        Assert.Equal(MarketDataQuality.Unavailable, data.Results[1].Quality);
    }

    [Fact]
    public async Task Fresh_cache_hit_avoids_a_second_provider_call()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateResult("AAPL"));
        await using var factory = new MarketDataTestFactory(provider);

        Assert.Equal(HttpStatusCode.OK, (await PostAsync(factory, CreateRequest("AAPL"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(factory, CreateRequest("AAPL"))).StatusCode);

        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public async Task Asset_id_casing_is_canonicalized_before_cache_lookup()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateResult("AAPL"));
        await using var factory = new MarketDataTestFactory(provider);

        Assert.Equal(HttpStatusCode.OK, (await PostAsync(factory, CreateRequest("aapl"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostAsync(factory, CreateRequest("AAPL"))).StatusCode);

        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public async Task Expired_cache_exposes_stale_evidence_when_refresh_fails()
    {
        var provider = new FakeMarketDataProvider
        {
            Results = [CreateResult(
                "AAPL",
                quality: MarketDataQuality.Unavailable,
                error: new MarketDataError(
                    MarketDataErrorCategory.UpstreamUnavailable,
                    "fake outage",
                    true))]
        };
        var cache = new SeededMarketDataCache(
            new MarketDataCacheEntry(
                CreateKey("AAPL"),
                CreateResult("AAPL"),
                DateTimeOffset.UtcNow.AddMinutes(-10),
                DateTimeOffset.UtcNow.AddMinutes(-1)));
        await using var factory = new MarketDataTestFactory(provider, cache);

        var response = await PostAsync(factory, CreateRequest("AAPL"));
        var result = Assert.Single((await ReadResponseAsync(response)).Results);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MarketDataErrorCategory.UpstreamUnavailable, result.Error?.Category);
        Assert.Contains(result.Warnings, warning => warning.Category == MarketDataWarningCategory.StaleData);
        Assert.Contains(result.Warnings, warning => warning.Category == MarketDataWarningCategory.UpstreamUnavailable);
        Assert.True(result.Freshness.IsStale);
    }

    [Fact]
    public async Task Expired_cache_is_preserved_when_refresh_returns_no_result()
    {
        var cached = CreateResult("AAPL");
        var cache = new SeededMarketDataCache(
            new MarketDataCacheEntry(
                new MarketDataCacheKey(
                    "AAPL",
                    MarketAssetClass.Equity,
                    CreateHistory().Start,
                    CreateHistory().End,
                    MarketDataGranularity.Daily),
                cached,
                DateTimeOffset.UtcNow.AddMinutes(-10),
                DateTimeOffset.UtcNow.AddMinutes(-1)));
        await using var factory = new MarketDataTestFactory(new FakeMarketDataProvider(), cache);

        var result = Assert.Single((await ReadResponseAsync(await PostAsync(factory, CreateRequest("AAPL")))).Results);

        Assert.Equal(cached.CurrentQuote?.Value, result.CurrentQuote?.Value);
        Assert.NotEmpty(result.HistoricalPoints);
        Assert.Equal(MarketDataErrorCategory.UpstreamUnavailable, result.Error?.Category);
        Assert.Contains(result.Warnings, warning => warning.Category == MarketDataWarningCategory.UpstreamUnavailable);
        Assert.True(result.Freshness.IsStale);
    }

    [Fact]
    public async Task Invalid_asset_returns_explicit_error_category()
    {
        await using var factory = new MarketDataTestFactory(new FakeMarketDataProvider());

        using var response = await PostAsync(factory, CreateRequest("NOT-ALLOWED"));
        var category = await ReadProblemCategoryAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("UnsupportedAsset", category);
    }

    [Fact]
    public async Task Over_limit_request_returns_invalid_request_problem()
    {
        await using var factory = new MarketDataTestFactory(
            new FakeMarketDataProvider(),
            configureOptions: options => options.MaximumSelectedAssets = 1);

        using var response = await PostAsync(factory, CreateRequest("AAPL", "MSFT"));
        var category = await ReadProblemCategoryAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidRequest", category);
    }

    [Fact]
    public async Task Invalid_history_range_returns_invalid_request_problem()
    {
        await using var factory = new MarketDataTestFactory(new FakeMarketDataProvider());
        var request = CreateRequest("AAPL") with
        {
            History = new MarketDataHistoryRequest(
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-13)),
                DateOnly.FromDateTime(DateTime.UtcNow),
                MarketDataGranularity.Daily)
        };

        using var response = await PostAsync(factory, request);
        var category = await ReadProblemCategoryAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InvalidRequest", category);
    }

    [Fact]
    public async Task Quota_exhaustion_returns_per_asset_error()
    {
        var provider = new FakeMarketDataProvider
        {
            Results = [CreateResult(
                "AAPL",
                quality: MarketDataQuality.Unavailable,
                error: new MarketDataError(
                    MarketDataErrorCategory.QuotaExceeded,
                    "fake quota exhausted",
                    true))]
        };
        await using var factory = new MarketDataTestFactory(provider);

        var result = Assert.Single((await ReadResponseAsync(await PostAsync(factory, CreateRequest("AAPL")))).Results);

        Assert.Equal(MarketDataErrorCategory.QuotaExceeded, result.Error?.Category);
        Assert.True(result.Error?.IsRetryable);
    }

    [Fact]
    public async Task Timeout_returns_explicit_retryable_error()
    {
        var provider = new FakeMarketDataProvider { DelayUntilCancelled = true };
        await using var factory = new MarketDataTestFactory(
            provider,
            configureOptions: options => options.RequestTimeout = TimeSpan.FromMilliseconds(20));

        var result = Assert.Single((await ReadResponseAsync(await PostAsync(factory, CreateRequest("AAPL")))).Results);

        Assert.Equal(MarketDataErrorCategory.Timeout, result.Error?.Category);
        Assert.True(result.Error?.IsRetryable);
    }

    [Fact]
    public async Task Upstream_outage_returns_explicit_retryable_error()
    {
        var provider = new FakeMarketDataProvider { ThrowUpstreamUnavailable = true };
        await using var factory = new MarketDataTestFactory(provider);

        var result = Assert.Single((await ReadResponseAsync(await PostAsync(factory, CreateRequest("AAPL")))).Results);

        Assert.Equal(MarketDataErrorCategory.UpstreamUnavailable, result.Error?.Category);
        Assert.True(result.Error?.IsRetryable);
    }

    [Fact]
    public async Task Provider_exception_preserves_stable_error_category()
    {
        var provider = new FakeMarketDataProvider
        {
            ExceptionToThrow = new MarketDataProviderException(
                MarketDataErrorCategory.AuthenticationFailed,
                "fake authentication failure",
                false,
                401)
        };
        await using var factory = new MarketDataTestFactory(provider);

        var result = Assert.Single((await ReadResponseAsync(await PostAsync(factory, CreateRequest("AAPL")))).Results);

        Assert.Equal(MarketDataErrorCategory.AuthenticationFailed, result.Error?.Category);
        Assert.False(result.Error?.IsRetryable);
        Assert.Equal(401, result.Error?.UpstreamStatusCode);
    }

    [Fact]
    public async Task Application_request_budget_returns_too_many_requests_after_limit()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateResult("AAPL"));
        await using var factory = new MarketDataTestFactory(
            provider,
            configureOptions: options => options.ApplicationRequestBudget = 1);

        Assert.Equal(HttpStatusCode.OK, (await PostAsync(factory, CreateRequest("AAPL"))).StatusCode);
        using var response = await PostAsync(factory, CreateRequest("MSFT"));
        var category = await ReadProblemCategoryAsync(response);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("QuotaExceeded", category);
        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public async Task Provider_not_approved_returns_unavailable_problem_without_calling_provider()
    {
        var provider = new FakeMarketDataProvider();
        await using var factory = new MarketDataTestFactory(provider, configureOptions: options =>
        {
            options.ProviderId = string.Empty;
            options.PublicDisplayApproved = false;
        });

        using var response = await PostAsync(factory, CreateRequest("AAPL"));
        var category = await ReadProblemCategoryAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("ProviderNotApproved", category);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task Approved_configuration_with_unavailable_provider_returns_not_configured_result()
    {
        await using var factory = new MarketDataTestFactory(
            new FakeMarketDataProvider(),
            useDefaultProvider: true);

        using var response = await PostAsync(factory, CreateRequest("AAPL"));
        var category = await ReadProblemCategoryAsync(response);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("NotConfigured", category);
    }

    [Fact]
    public async Task Normalized_contract_does_not_require_provider_payload_fields()
    {
        var asset = new AssetReference("AAPL", "Apple", MarketAssetClass.Equity);
        var history = CreateHistory();
        var freshness = new MarketDataFreshness(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(5),
            false);

        var result = new MarketDataResult(
            asset,
            history,
            new MarketQuote(215.50m, "USD", freshness.ObservedAt, freshness, MarketDataQuality.Complete),
            [],
            new MarketDataSource("fake-provider", "Fake provider attribution", true, TimeSpan.FromMinutes(15)),
            freshness,
            MarketDataQuality.Complete,
            []);

        Assert.Equal("AAPL", result.Asset.Id);
        Assert.Equal(MarketDataGranularity.Daily, result.RequestedHistory.Granularity);
        Assert.Equal("fake-provider", result.Source?.ProviderId);
    }

    private static MarketDataRequest CreateRequest(params string[] assetIds) =>
        new(
            assetIds.Select(id => new AssetReference(
                id,
                id == "BTC-USD" ? "Bitcoin" : id,
                id.EndsWith("-USD", StringComparison.Ordinal) ? MarketAssetClass.Crypto : MarketAssetClass.Equity)).ToArray(),
            CreateHistory());

    private static MarketDataHistoryRequest CreateHistory() =>
        new(DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-12)), DateOnly.FromDateTime(DateTime.UtcNow), MarketDataGranularity.Daily);

    private static MarketDataResult CreateResult(
        string id,
        MarketAssetClass assetClass = MarketAssetClass.Equity,
        MarketDataQuality quality = MarketDataQuality.Complete,
        MarketDataError? error = null)
    {
        var now = DateTimeOffset.UtcNow;
        var history = CreateHistory();
        var freshness = new MarketDataFreshness(now.AddMinutes(-1), now, now.AddMinutes(5), false);
        return new MarketDataResult(
            new AssetReference(id, id, assetClass),
            history,
            error is null ? new MarketQuote(215.50m, "USD", now, freshness, quality) : null,
            error is null ? [new HistoricalPricePoint(now.AddDays(-1), null, null, null, 214.25m, null, "USD", true)] : [],
            error is null ? new MarketDataSource("fake-provider", "Fake provider attribution", true, TimeSpan.FromMinutes(15)) : null,
            freshness,
            quality,
            [],
            error);
    }

    private static MarketDataCacheKey CreateKey(string id) =>
        new(id, MarketAssetClass.Equity, CreateHistory().Start, CreateHistory().End, MarketDataGranularity.Daily);

    private static async Task<HttpResponseMessage> PostAsync(
        MarketDataTestFactory factory,
        MarketDataRequest request)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        return await client.PostAsJsonAsync("/api/market-data", request);
    }

    private static async Task<MarketDataResponse> ReadResponseAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MarketDataResponse>())!;
    }

    private static async Task<string?> ReadProblemCategoryAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("errorCategory", out var category)
            ? category.GetString()
            : null;
    }
}

internal sealed class FakeMarketDataProvider : IMarketDataProvider
{
    public IReadOnlyList<MarketDataResult> Results { get; set; } = [];
    public bool DelayUntilCancelled { get; init; }
    public bool ThrowUpstreamUnavailable { get; init; }
    public MarketDataProviderException? ExceptionToThrow { get; init; }
    public int CallCount { get; private set; }

    public async Task<IReadOnlyList<MarketDataResult>> GetAsync(
        MarketDataRequest request,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        if (DelayUntilCancelled)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        if (ThrowUpstreamUnavailable)
        {
            throw new HttpRequestException("fake outage");
        }

        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Results.Where(result => request.Assets.Any(asset =>
            string.Equals(asset.Id, result.Asset.Id, StringComparison.OrdinalIgnoreCase))).ToArray();
    }

    public void SetResults(params MarketDataResult[] results) => Results = results;
}

internal sealed class SeededMarketDataCache(MarketDataCacheEntry entry) : IMarketDataCache
{
    private MarketDataCacheEntry? current = entry;

    public ValueTask<MarketDataCacheEntry?> GetAsync(
        MarketDataCacheKey key,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(current?.Key == key ? current : null);

    public ValueTask SetAsync(MarketDataCacheEntry cacheEntry, CancellationToken cancellationToken = default)
    {
        current = cacheEntry;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(MarketDataCacheKey key, CancellationToken cancellationToken = default)
    {
        if (current?.Key == key)
        {
            current = null;
        }

        return ValueTask.CompletedTask;
    }
}

internal sealed class MarketDataTestFactory : WebApplicationFactory<Program>
{
    private readonly IMarketDataProvider provider;
    private readonly IMarketDataCache? cache;
    private readonly Action<MarketDataOptions>? configureOptions;
    private readonly bool useDefaultProvider;

    public MarketDataTestFactory(
        IMarketDataProvider provider,
        IMarketDataCache? cache = null,
        Action<MarketDataOptions>? configureOptions = null,
        bool useDefaultProvider = false)
    {
        this.provider = provider;
        this.cache = cache;
        this.configureOptions = configureOptions;
        this.useDefaultProvider = useDefaultProvider;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        TestEntraConfiguration.Apply(builder);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MarketData:ProviderId"] = "fake-provider",
                ["MarketData:PublicDisplayApproved"] = "true",
                ["MarketData:RequestTimeout"] = "00:00:10",
                ["MarketData:CacheDuration"] = "00:05:00"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultForbidScheme = "Test";
                options.DefaultSignInScheme = "Test";
                options.DefaultSignOutScheme = "Test";
            });
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder("Test")
                    .RequireAuthenticatedUser()
                    .Build();
            });

            if (!useDefaultProvider)
            {
                services.RemoveAll<IMarketDataProvider>();
                services.AddSingleton(provider);
            }
            if (cache is not null)
            {
                services.RemoveAll<IMarketDataCache>();
                services.AddSingleton(cache);
            }

            if (configureOptions is not null)
            {
                services.PostConfigure<MarketDataOptions>(configureOptions);
            }
        });
    }
}
