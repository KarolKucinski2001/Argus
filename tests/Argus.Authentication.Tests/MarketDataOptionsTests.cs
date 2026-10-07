using Microsoft.Extensions.Options;
using Xunit;
using _bootstrap_scaffold.MarketData;

namespace Argus.Authentication.Tests;

public sealed class MarketDataOptionsTests
{
    [Theory]
    [InlineData(nameof(MarketDataOptions.HistoryWindowMonths))]
    [InlineData(nameof(MarketDataOptions.MaximumSelectedAssets))]
    [InlineData(nameof(MarketDataOptions.RequestTimeout))]
    [InlineData(nameof(MarketDataOptions.CacheDuration))]
    public void Invalid_positive_or_fixed_settings_are_rejected(string setting)
    {
        var options = new MarketDataOptions();
        switch (setting)
        {
            case nameof(MarketDataOptions.HistoryWindowMonths):
                options.HistoryWindowMonths = 6;
                break;
            case nameof(MarketDataOptions.MaximumSelectedAssets):
                options.MaximumSelectedAssets = 0;
                break;
            case nameof(MarketDataOptions.RequestTimeout):
                options.RequestTimeout = TimeSpan.Zero;
                break;
            case nameof(MarketDataOptions.CacheDuration):
                options.CacheDuration = TimeSpan.Zero;
                break;
        }

        var result = options.Validate(Options.DefaultName, options);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public void Invalid_request_budget_is_rejected()
    {
        var options = new MarketDataOptions { ApplicationRequestBudget = 0 };

        Assert.False(options.Validate(Options.DefaultName, options).Succeeded);
    }

    [Fact]
    public void Provider_configuration_requires_public_display_approval()
    {
        var options = new MarketDataOptions { ProviderId = "provider-id" };

        Assert.False(options.Validate(Options.DefaultName, options).Succeeded);
    }

    [Fact]
    public void Approved_public_display_requires_provider_configuration()
    {
        var options = new MarketDataOptions { PublicDisplayApproved = true };

        Assert.False(options.Validate(Options.DefaultName, options).Succeeded);
    }
}
