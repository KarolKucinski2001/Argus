using Microsoft.Extensions.Options;
using Xunit;
using _bootstrap_scaffold.MarketData;

namespace Argus.Authentication.Tests;

public sealed class MarketDataContractTests
{
    [Fact]
    public void Normalized_result_uses_application_contract_types()
    {
        var asset = new AssetReference("AAPL", "Apple", MarketAssetClass.Equity);
        var history = new MarketDataHistoryRequest(
            new DateOnly(2025, 10, 1),
            new DateOnly(2026, 10, 1),
            MarketDataGranularity.Daily);
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
            new MarketDataSource("test-provider", "Test Provider", true, TimeSpan.FromMinutes(15)),
            freshness,
            MarketDataQuality.Complete,
            []);

        Assert.Equal("AAPL", result.Asset.Id);
        Assert.Equal(MarketDataGranularity.Daily, result.RequestedHistory.Granularity);
        Assert.Equal("USD", result.CurrentQuote?.Currency);
        Assert.Equal("test-provider", result.Source?.ProviderId);
    }

    [Fact]
    public void Options_validation_rejects_invalid_operational_settings()
    {
        var invalidOptions = new[]
        {
            new MarketDataOptions { HistoryWindowMonths = 11, ProviderId = "test", PublicDisplayApproved = true },
            new MarketDataOptions { MaximumSelectedAssets = 0, ProviderId = "test", PublicDisplayApproved = true },
            new MarketDataOptions { RequestTimeout = TimeSpan.Zero, ProviderId = "test", PublicDisplayApproved = true },
            new MarketDataOptions { CacheDuration = TimeSpan.Zero, ProviderId = "test", PublicDisplayApproved = true },
            new MarketDataOptions { ApplicationRequestBudget = 0, ProviderId = "test", PublicDisplayApproved = true },
            new MarketDataOptions { ProviderId = "", PublicDisplayApproved = true },
            new MarketDataOptions { ProviderId = "test", PublicDisplayApproved = false }
        };

        foreach (var options in invalidOptions)
        {
            var result = options.Validate(Options.DefaultName, options);

            Assert.False(result.Succeeded);
        }
    }

    [Fact]
    public void Options_validation_accepts_a_compliant_configuration()
    {
        var options = new MarketDataOptions
        {
            ProviderId = "approved-provider",
            PublicDisplayApproved = true
        };

        var result = options.Validate(Options.DefaultName, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Options_validation_accepts_the_disabled_provider_default()
    {
        var options = new MarketDataOptions();

        var result = options.Validate(Options.DefaultName, options);

        Assert.True(result.Succeeded);
    }
}
