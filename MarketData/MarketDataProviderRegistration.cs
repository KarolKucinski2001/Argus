using _bootstrap_scaffold.MarketData.Live;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace _bootstrap_scaffold.MarketData;

public static class MarketDataProviderRegistration
{
    public static IServiceCollection AddMarketDataProvider(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var marketData = new MarketDataOptions();
        configuration.GetSection(MarketDataOptions.SectionName).Bind(marketData);

        // Both conditions must hold; MarketDataOptions validation also refuses a ProviderId without approval.
        var liveEnabled = marketData.PublicDisplayApproved &&
            string.Equals(marketData.ProviderId, LiveMarketDataOptions.ProviderId, StringComparison.Ordinal);

        if (liveEnabled)
        {
            return services.AddLiveMarketDataProviders(configuration, marketData.RequestTimeout);
        }

        return services.AddSingleton<IMarketDataProvider>(_ =>
            environment.IsDevelopment() || environment.IsEnvironment("Testing")
                ? new DevelopmentMarketDataProvider()
                : new UnavailableMarketDataProvider());
    }

    private static IServiceCollection AddLiveMarketDataProviders(
        this IServiceCollection services,
        IConfiguration configuration,
        TimeSpan requestTimeout)
    {
        services.AddOptions<LiveMarketDataOptions>()
            .Bind(configuration.GetSection(LiveMarketDataOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);

        AddClient(services, LiveEquityMarketDataProvider.FinnhubClientName, o => o.FinnhubBaseAddress, requestTimeout);
        AddClient(services, LiveEquityMarketDataProvider.TwelveDataClientName, o => o.TwelveDataBaseAddress, requestTimeout);
        AddClient(services, LiveCryptoMarketDataProvider.ClientName, o => o.CoinGeckoBaseAddress, requestTimeout);

        services.AddSingleton<LiveEquityMarketDataProvider>();
        services.AddSingleton<LiveCryptoMarketDataProvider>();
        return services.AddSingleton<IMarketDataProvider>(sp =>
            new RoutingMarketDataProvider(new Dictionary<MarketAssetClass, IMarketDataProvider>
            {
                [MarketAssetClass.Equity] = sp.GetRequiredService<LiveEquityMarketDataProvider>(),
                [MarketAssetClass.Crypto] = sp.GetRequiredService<LiveCryptoMarketDataProvider>()
            }));
    }

    private static void AddClient(
        IServiceCollection services,
        string name,
        Func<LiveMarketDataOptions, Uri> baseAddress,
        TimeSpan requestTimeout) =>
        services.AddHttpClient(name).ConfigureHttpClient((sp, client) =>
        {
            client.BaseAddress = baseAddress(sp.GetRequiredService<IOptions<LiveMarketDataOptions>>().Value);
            client.Timeout = requestTimeout;
        });
}
