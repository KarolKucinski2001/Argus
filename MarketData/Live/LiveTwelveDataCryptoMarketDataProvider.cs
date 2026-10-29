using System.Text.Json;
using Microsoft.Extensions.Options;

namespace _bootstrap_scaffold.MarketData.Live;

// Crypto adapter on Twelve Data (free Basic plan): /time_series for daily history and /price for the
// current price. Used when no CoinGecko key is configured; asset ids such as BTC-USD map to BTC/USD.
public sealed class LiveTwelveDataCryptoMarketDataProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<LiveMarketDataOptions> options,
    TimeProvider timeProvider) : IMarketDataProvider
{
    private static readonly MarketDataSource Source = new(
        "twelvedata-crypto",
        "Crypto prices: Twelve Data (twelvedata.com).",
        true,
        TimeSpan.FromMinutes(1));

    public Task<IReadOnlyList<MarketDataResult>> GetAsync(
        MarketDataRequest request,
        CancellationToken cancellationToken = default) =>
        LiveMarketDataHttp.RunAsync(request, timeProvider, (asset, ct) => FetchAsync(asset, request.History, ct), cancellationToken);

    private async Task<MarketDataResult> FetchAsync(
        AssetReference asset,
        MarketDataHistoryRequest history,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.TwelveDataApiKey))
        {
            throw new MarketDataProviderException(
                MarketDataErrorCategory.NotConfigured, "The crypto provider key is not configured.", false);
        }

        if (!settings.CoinGeckoIds.ContainsKey(asset.Id))
        {
            throw new MarketDataProviderException(
                MarketDataErrorCategory.UnsupportedAsset, "The provider does not support this asset.", false);
        }

        var symbol = Uri.EscapeDataString(asset.Id.Replace('-', '/'));
        var client = httpClientFactory.CreateClient(LiveEquityMarketDataProvider.TwelveDataClientName);
        var points = await TwelveDataHistory.GetAsync(client, symbol, history, settings.TwelveDataApiKey, true, cancellationToken);

        var warnings = new List<MarketDataWarning>();
        MarketQuote? quote = null;
        try
        {
            quote = await GetQuoteAsync(client, symbol, settings.TwelveDataApiKey, cancellationToken);
        }
        catch (MarketDataProviderException exception) when (exception.Category != MarketDataErrorCategory.NotConfigured)
        {
            if (exception.Category == MarketDataErrorCategory.QuotaExceeded)
            {
                warnings.Add(new(MarketDataWarningCategory.ProviderQuota, "The quote request limit was reached.", true));
            }
        }

        return LiveMarketDataHttp.Assemble(asset, history, quote, points, Source, timeProvider.GetUtcNow(), warnings);
    }

    private async Task<MarketQuote> GetQuoteAsync(
        HttpClient client,
        string symbol,
        string apiKey,
        CancellationToken cancellationToken)
    {
        using var document = await LiveMarketDataHttp.GetJsonAsync(
            client,
            $"price?symbol={symbol}",
            "Authorization",
            $"apikey {apiKey}",
            cancellationToken);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("status", out var status) &&
            string.Equals(status.GetString(), "error", StringComparison.OrdinalIgnoreCase))
        {
            var code = root.TryGetProperty("code", out var codeElement) && codeElement.TryGetInt32(out var parsed)
                ? parsed
                : 500;
            throw LiveMarketDataHttp.FromStatus(code);
        }

        var price = LiveMarketDataHttp.ReadDecimal(root, "price");
        var now = timeProvider.GetUtcNow();
        return LiveMarketDataHttp.CreateQuote(price, now, now);
    }
}
