using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace _bootstrap_scaffold.MarketData.Live;

// Equity adapter: Finnhub /quote for the current price (free tier) and Twelve Data
// /time_series for daily history, because Finnhub /stock/candle is premium-only.
public sealed class LiveEquityMarketDataProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<LiveMarketDataOptions> options,
    TimeProvider timeProvider) : IMarketDataProvider
{
    public const string FinnhubClientName = "market-data-finnhub";
    public const string TwelveDataClientName = "market-data-twelvedata";

    private static readonly MarketDataSource Source = new(
        "finnhub+twelvedata",
        "Quotes: Finnhub (finnhub.io). Daily history: Twelve Data (twelvedata.com).",
        true,
        null);

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
        if (string.IsNullOrWhiteSpace(settings.FinnhubApiKey) || string.IsNullOrWhiteSpace(settings.TwelveDataApiKey))
        {
            throw new MarketDataProviderException(
                MarketDataErrorCategory.NotConfigured, "Equity provider keys are not configured.", false);
        }

        var symbol = Uri.EscapeDataString(asset.Id);
        var points = await TwelveDataHistory.GetAsync(
            httpClientFactory.CreateClient(TwelveDataClientName), symbol, history, settings.TwelveDataApiKey, true, cancellationToken);

        var warnings = new List<MarketDataWarning>();
        MarketQuote? quote = null;
        try
        {
            quote = await GetQuoteAsync(symbol, settings.FinnhubApiKey, cancellationToken);
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

    private async Task<MarketQuote> GetQuoteAsync(string symbol, string apiKey, CancellationToken cancellationToken)
    {
        using var document = await LiveMarketDataHttp.GetJsonAsync(
            httpClientFactory.CreateClient(FinnhubClientName),
            $"quote?symbol={symbol}",
            "X-Finnhub-Token",
            apiKey,
            cancellationToken);
        var root = document.RootElement;
        var price = LiveMarketDataHttp.ReadDecimal(root, "c");
        var timestamp = LiveMarketDataHttp.ReadDecimal(root, "t");

        // Finnhub answers unknown symbols with 200 and all-zero fields.
        if (price <= 0 || timestamp <= 0)
        {
            throw new MarketDataProviderException(
                MarketDataErrorCategory.UnsupportedAsset, "The provider has no quote for this asset.", false);
        }

        var now = timeProvider.GetUtcNow();
        return LiveMarketDataHttp.CreateQuote(price, DateTimeOffset.FromUnixTimeSeconds((long)timestamp), now);
    }
}
