using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;
using _bootstrap_scaffold.MarketData;
using _bootstrap_scaffold.MarketData.Live;

namespace Argus.Authentication.Tests;

public sealed class LiveMarketDataProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 6, 12, 0, 0, TimeSpan.Zero);
    private static readonly MarketDataHistoryRequest History =
        new(new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 5), MarketDataGranularity.Daily);
    private static readonly AssetReference Aapl = new("AAPL", "AAPL", MarketAssetClass.Equity);
    private static readonly AssetReference Btc = new("BTC-USD", "BTC-USD", MarketAssetClass.Crypto);

    private const string QuoteJson = """{"c":215.5,"d":1,"dp":0.4,"h":216,"l":214,"o":215,"pc":214.5,"t":1767614400}""";
    private const string SeriesJson = """
        {"meta":{"symbol":"AAPL"},"values":[
          {"datetime":"2026-01-02","open":"210.0","high":"212.5","low":"209.0","close":"211.0","volume":"1000"},
          {"datetime":"2026-01-05","open":"211.0","high":"216.0","low":"210.5","close":"215.5","volume":"1200"},
          {"datetime":"2025-12-31","open":"1","high":"1","low":"1","close":"1","volume":"1"}],"status":"ok"}
        """;
    private const string CryptoJson = """
        {"prices":[[1767312000000,90000.5],[1767398400000,91000.25],[1767571200000,92000.0],[1767657600000,92500.0]],
         "total_volumes":[[1767312000000,500.0],[1767398400000,600.0],[1767571200000,700.0],[1767657600000,800.0]]}
        """;
    private const string CryptoQuoteJson = """{"bitcoin":{"usd":92100.5,"last_updated_at":1767614400}}""";

    [Fact]
    public async Task Equity_provider_maps_quote_history_and_attribution()
    {
        var provider = EquityProvider(out var calls, request =>
            request.RequestUri!.AbsolutePath.EndsWith("quote") ? Json(QuoteJson) : Json(SeriesJson));

        var result = Assert.Single(await provider.GetAsync(Request(Aapl)));

        Assert.Null(result.Error);
        Assert.Equal(MarketDataQuality.Complete, result.Quality);
        Assert.Equal(215.5m, result.CurrentQuote?.Value);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1767614400), result.CurrentQuote?.ObservedAt);
        Assert.Equal(2, result.HistoricalPoints.Count);
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), result.HistoricalPoints[0].Timestamp);
        Assert.Equal(211.0m, result.HistoricalPoints[0].Close);
        Assert.Equal(212.5m, result.HistoricalPoints[0].High);
        Assert.True(result.HistoricalPoints[0].IsAdjusted);
        Assert.True(result.Source?.IsDelayed);
        Assert.Contains("Finnhub", result.Source?.Attribution);
        Assert.Contains("Twelve Data", result.Source?.Attribution);
        Assert.Contains(result.Warnings, w => w.Category == MarketDataWarningCategory.DelayedData);
        Assert.All(calls, call => Assert.DoesNotContain("secret", call.RequestUri!.ToString()));
        Assert.Contains(calls, call => call.Headers.TryGetValues("X-Finnhub-Token", out var v) && v.Single() == "secret-finnhub");
        Assert.Contains(calls, call => call.Headers.TryGetValues("Authorization", out var v) && v.Single() == "apikey secret-twelve");
    }

    [Fact]
    public async Task Equity_provider_keeps_history_when_the_quote_hits_the_rate_limit()
    {
        var provider = EquityProvider(out _, request =>
            request.RequestUri!.AbsolutePath.EndsWith("quote")
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : Json(SeriesJson));

        var result = Assert.Single(await provider.GetAsync(Request(Aapl)));

        Assert.Null(result.Error);
        Assert.Null(result.CurrentQuote);
        Assert.Equal(MarketDataQuality.Incomplete, result.Quality);
        Assert.Contains(result.Warnings, w => w.Category == MarketDataWarningCategory.ProviderQuota);
        Assert.Contains(result.Warnings, w => w.Category == MarketDataWarningCategory.MissingCurrentQuote);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, MarketDataErrorCategory.AuthenticationFailed, false)]
    [InlineData(HttpStatusCode.Forbidden, MarketDataErrorCategory.AuthenticationFailed, false)]
    [InlineData(HttpStatusCode.TooManyRequests, MarketDataErrorCategory.QuotaExceeded, true)]
    [InlineData(HttpStatusCode.NotFound, MarketDataErrorCategory.UnsupportedAsset, false)]
    [InlineData(HttpStatusCode.InternalServerError, MarketDataErrorCategory.UpstreamUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, MarketDataErrorCategory.Timeout, true)]
    public async Task Http_status_codes_map_to_market_data_error_categories(
        HttpStatusCode status,
        MarketDataErrorCategory category,
        bool retryable)
    {
        var provider = CryptoProvider(out _, _ => new HttpResponseMessage(status));

        var result = Assert.Single(await provider.GetAsync(Request(Btc)));

        Assert.Equal(category, result.Error?.Category);
        Assert.Equal(retryable, result.Error?.IsRetryable);
        Assert.Equal((int)status, result.Error?.UpstreamStatusCode);
        Assert.Equal(MarketDataQuality.Unavailable, result.Quality);
    }

    [Fact]
    public async Task Twelve_data_body_errors_and_malformed_json_are_mapped()
    {
        var quota = EquityProvider(out _, _ =>
            Json("""{"code":429,"message":"limit","status":"error"}"""));
        var malformed = EquityProvider(out _, _ => Json("{not json"));
        var shapeless = EquityProvider(out _, _ => Json("""{"unexpected":true}"""));

        Assert.Equal(MarketDataErrorCategory.QuotaExceeded,
            Assert.Single(await quota.GetAsync(Request(Aapl))).Error?.Category);
        Assert.Equal(MarketDataErrorCategory.InvalidProviderResponse,
            Assert.Single(await malformed.GetAsync(Request(Aapl))).Error?.Category);
        Assert.Equal(MarketDataErrorCategory.InvalidProviderResponse,
            Assert.Single(await shapeless.GetAsync(Request(Aapl))).Error?.Category);
    }

    [Fact]
    public async Task Client_timeout_maps_to_timeout_but_caller_cancellation_propagates()
    {
        var timeout = CryptoProvider(out _, _ => throw new TaskCanceledException("client timeout"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var cancelled = CryptoProvider(out _, _ => throw new TaskCanceledException("cancelled"));

        var result = Assert.Single(await timeout.GetAsync(Request(Btc)));

        Assert.Equal(MarketDataErrorCategory.Timeout, result.Error?.Category);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled.GetAsync(Request(Btc), cts.Token));
    }

    [Fact]
    public async Task Crypto_provider_maps_daily_close_quote_and_attribution()
    {
        var provider = CryptoProvider(out var calls, request =>
            request.RequestUri!.AbsolutePath.EndsWith("simple/price") ? Json(CryptoQuoteJson) : Json(CryptoJson));

        var result = Assert.Single(await provider.GetAsync(Request(Btc)));

        Assert.Null(result.Error);
        Assert.Equal(92100.5m, result.CurrentQuote?.Value);
        // The 2026-01-06 point falls outside the requested window.
        Assert.Equal([90000.5m, 91000.25m, 92000.0m], result.HistoricalPoints.Select(p => p.Close));
        Assert.Equal(500.0m, result.HistoricalPoints[0].Volume);
        Assert.Null(result.HistoricalPoints[0].High);
        Assert.False(result.HistoricalPoints[0].IsAdjusted);
        Assert.Contains("CoinGecko", result.Source?.Attribution);
        Assert.Equal("coingecko-demo", result.Source?.ProviderId);
        Assert.All(calls, call => Assert.Equal("secret-gecko", call.Headers.GetValues("x-cg-demo-api-key").Single()));
        Assert.All(calls, call => Assert.DoesNotContain("secret", call.RequestUri!.Query));
    }

    [Fact]
    public async Task Unmapped_crypto_asset_is_unsupported_and_missing_keys_are_not_configured()
    {
        var provider = CryptoProvider(out _, _ => Json(CryptoJson));
        var noKeys = new LiveCryptoMarketDataProvider(
            new FakeFactory(_ => Json("{}"), []),
            Options.Create(new LiveMarketDataOptions()),
            new FixedTime());

        var unsupported = Assert.Single(await provider.GetAsync(
            Request(new AssetReference("DOGE-USD", "DOGE-USD", MarketAssetClass.Crypto))));
        var notConfigured = Assert.Single(await noKeys.GetAsync(Request(Btc)));

        Assert.Equal(MarketDataErrorCategory.UnsupportedAsset, unsupported.Error?.Category);
        Assert.Equal(MarketDataErrorCategory.NotConfigured, notConfigured.Error?.Category);
    }

    [Fact]
    public async Task Routing_provider_sends_each_asset_class_to_its_provider_in_request_order()
    {
        var equity = new StubProvider("equity");
        var crypto = new StubProvider("crypto");
        var router = new RoutingMarketDataProvider(new Dictionary<MarketAssetClass, IMarketDataProvider>
        {
            [MarketAssetClass.Equity] = equity,
            [MarketAssetClass.Crypto] = crypto
        });

        var results = await router.GetAsync(Request(Btc, Aapl));

        Assert.Equal(["BTC-USD", "AAPL"], results.Select(r => r.Asset.Id));
        Assert.Equal(["crypto", "equity"], results.Select(r => r.Source?.ProviderId));
        Assert.Equal(["BTC-USD"], crypto.Seen);
        Assert.Equal(["AAPL"], equity.Seen);
    }

    [Fact]
    public async Task Routing_provider_reports_not_configured_for_an_unrouted_asset_class()
    {
        var router = new RoutingMarketDataProvider(new Dictionary<MarketAssetClass, IMarketDataProvider>
        {
            [MarketAssetClass.Equity] = new StubProvider("equity")
        });

        var results = await router.GetAsync(Request(Aapl, Btc));

        Assert.Null(results[0].Error);
        Assert.Equal(MarketDataErrorCategory.NotConfigured, results[1].Error?.Category);
    }

    [Fact]
    public void Live_provider_id_is_refused_without_public_display_approval()
    {
        var options = new MarketDataOptions { ProviderId = LiveMarketDataOptions.ProviderId };

        var validation = options.Validate(null, options);

        Assert.True(validation.Failed);
    }

    [Theory]
    [InlineData("Development", typeof(DevelopmentMarketDataProvider))]
    [InlineData("Production", typeof(UnavailableMarketDataProvider))]
    public void Registration_keeps_the_non_live_provider_while_approval_is_missing(
        string environmentName,
        Type expected)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MarketData:ProviderId"] = LiveMarketDataOptions.ProviderId,
            ["MarketData:PublicDisplayApproved"] = "false",
            ["MarketData:Live:FinnhubApiKey"] = "configured"
        }).Build();
        var services = new ServiceCollection();

        services.AddMarketDataProvider(configuration, new TestEnvironment(environmentName));
        using var serviceProvider = services.BuildServiceProvider();

        Assert.IsType(expected, serviceProvider.GetRequiredService<IMarketDataProvider>());
    }

    private static MarketDataRequest Request(params AssetReference[] assets) => new(assets, History);

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static LiveEquityMarketDataProvider EquityProvider(
        out List<HttpRequestMessage> calls,
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        calls = [];
        var factory = new FakeFactory(respond, calls);
        return new LiveEquityMarketDataProvider(
            factory,
            Options.Create(new LiveMarketDataOptions
            {
                FinnhubApiKey = "secret-finnhub",
                TwelveDataApiKey = "secret-twelve"
            }),
            new FixedTime());
    }

    private static LiveCryptoMarketDataProvider CryptoProvider(
        out List<HttpRequestMessage> calls,
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        calls = [];
        return new LiveCryptoMarketDataProvider(
            new FakeFactory(respond, calls),
            Options.Create(new LiveMarketDataOptions { CoinGeckoApiKey = "secret-gecko" }),
            new FixedTime());
    }

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FakeFactory(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        List<HttpRequestMessage> calls) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(new FakeHandler(respond, calls)) { BaseAddress = new Uri("https://provider.test/api/") };
    }

    private sealed class FakeHandler(
        Func<HttpRequestMessage, HttpResponseMessage> respond,
        List<HttpRequestMessage> calls) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            calls.Add(request);
            return Task.FromResult(respond(request));
        }
    }

    private sealed class StubProvider(string id) : IMarketDataProvider
    {
        public List<string> Seen { get; } = [];

        public Task<IReadOnlyList<MarketDataResult>> GetAsync(
            MarketDataRequest request,
            CancellationToken cancellationToken = default)
        {
            Seen.AddRange(request.Assets.Select(a => a.Id));
            return Task.FromResult<IReadOnlyList<MarketDataResult>>(request.Assets.Select(asset =>
                new MarketDataResult(
                    asset,
                    request.History,
                    null,
                    [],
                    new MarketDataSource(id, null, false, null),
                    new MarketDataFreshness(Now, Now, null, false),
                    MarketDataQuality.Complete,
                    [])).ToArray());
        }
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Argus";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
