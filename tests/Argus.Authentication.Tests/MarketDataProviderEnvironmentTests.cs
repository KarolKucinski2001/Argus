using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using _bootstrap_scaffold.MarketData;

namespace Argus.Authentication.Tests;

public sealed class MarketDataProviderEnvironmentTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void Development_and_testing_environments_resolve_the_illustrative_provider(
        string environment)
    {
        using var factory = new ProviderEnvironmentFactory(environment);

        var provider = factory.Services.GetRequiredService<IMarketDataProvider>();

        Assert.IsType<DevelopmentMarketDataProvider>(provider);
    }

    [Fact]
    public void Production_environment_does_not_resolve_the_illustrative_provider()
    {
        using var factory = new ProviderEnvironmentFactory("Production");

        var provider = factory.Services.GetRequiredService<IMarketDataProvider>();

        Assert.IsType<UnavailableMarketDataProvider>(provider);
    }

    [Fact]
    public async Task Illustrative_provider_returns_only_requested_assets_and_history()
    {
        var history = new MarketDataHistoryRequest(
            new DateOnly(2025, 1, 1),
            new DateOnly(2026, 1, 1),
            MarketDataGranularity.Daily);
        var request = new MarketDataRequest(
            [new AssetReference("AAPL", "AAPL", MarketAssetClass.Equity)],
            history);

        var results = await new DevelopmentMarketDataProvider().GetAsync(request);
        var result = Assert.Single(results);

        Assert.Equal(request.Assets, results.Select(item => item.Asset));
        Assert.Equal(history, result.RequestedHistory);
        Assert.Equal("illustrative-development", result.Source?.ProviderId);
        Assert.Contains("Illustrative", result.Source?.Attribution);
        Assert.Equal(MarketDataQuality.Complete, result.Quality);
    }

    [Fact]
    public async Task Illustrative_provider_gives_each_known_asset_a_distinct_price_shape()
    {
        var results = await new DevelopmentMarketDataProvider().GetAsync(FourAssetRequest());

        var shapes = results
            .Select(result =>
            {
                var closes = result.HistoricalPoints.Select(point => point.Close).ToArray();
                var minimum = closes.Min();
                var range = closes.Max() - minimum;
                return closes.Select(close => Math.Round((close - minimum) / range, 2)).ToArray();
            })
            .ToArray();

        Assert.Equal(4, shapes.Length);
        for (var left = 0; left < shapes.Length; left++)
        {
            for (var right = left + 1; right < shapes.Length; right++)
            {
                var differingPoints = shapes[left].Zip(shapes[right]).Count(pair => pair.First != pair.Second);
                Assert.True(
                    differingPoints > shapes[left].Length * 0.3,
                    $"Assets {left} and {right} share {shapes[left].Length - differingPoints} of {shapes[left].Length} normalized points.");
            }
        }
    }

    [Fact]
    public async Task Illustrative_provider_is_deterministic_and_quotes_the_last_close()
    {
        var provider = new DevelopmentMarketDataProvider();

        var first = await provider.GetAsync(FourAssetRequest());
        var second = await provider.GetAsync(FourAssetRequest());

        for (var index = 0; index < first.Count; index++)
        {
            var firstCloses = first[index].HistoricalPoints.Select(point => point.Close);
            Assert.Equal(firstCloses, second[index].HistoricalPoints.Select(point => point.Close));
            Assert.All(first[index].HistoricalPoints, point => Assert.True(point.Close > 0m));
            Assert.Equal(first[index].HistoricalPoints[^1].Close, first[index].CurrentQuote?.Value);
        }
    }

    private static MarketDataRequest FourAssetRequest() =>
        new(
            [
                new AssetReference("AAPL", "AAPL", MarketAssetClass.Equity),
                new AssetReference("MSFT", "MSFT", MarketAssetClass.Equity),
                new AssetReference("BTC-USD", "BTC-USD", MarketAssetClass.Crypto),
                new AssetReference("ETH-USD", "ETH-USD", MarketAssetClass.Crypto)
            ],
            new MarketDataHistoryRequest(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 3, 31),
                MarketDataGranularity.Daily));
}

internal sealed class ProviderEnvironmentFactory(string environment)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        TestEntraConfiguration.Apply(builder);
    }
}
