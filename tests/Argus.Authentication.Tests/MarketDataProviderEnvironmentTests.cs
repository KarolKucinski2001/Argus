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
