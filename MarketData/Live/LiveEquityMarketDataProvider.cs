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
        var points = await GetHistoryAsync(symbol, history, settings.TwelveDataApiKey, cancellationToken);

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

    private async Task<IReadOnlyList<HistoricalPricePoint>> GetHistoryAsync(
        string symbol,
        MarketDataHistoryRequest history,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var uri = $"time_series?symbol={symbol}&interval=1day&order=ASC&timezone=UTC" +
                  $"&start_date={history.Start:yyyy-MM-dd}&end_date={history.End:yyyy-MM-dd}";
        using var document = await LiveMarketDataHttp.GetJsonAsync(
            httpClientFactory.CreateClient(TwelveDataClientName),
            uri,
            "Authorization",
            $"apikey {apiKey}",
            cancellationToken);
        var root = document.RootElement;

        // Twelve Data reports most failures inside a 200 response body.
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("status", out var status) &&
            string.Equals(status.GetString(), "error", StringComparison.OrdinalIgnoreCase))
        {
            var code = root.TryGetProperty("code", out var codeElement) && codeElement.TryGetInt32(out var parsed)
                ? parsed
                : 500;
            throw LiveMarketDataHttp.FromStatus(code);
        }

        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("values", out var values) ||
            values.ValueKind != JsonValueKind.Array)
        {
            throw LiveMarketDataHttp.Invalid();
        }

        var points = new List<HistoricalPricePoint>();
        foreach (var item in values.EnumerateArray())
        {
            if (!item.TryGetProperty("datetime", out var dateElement) ||
                !DateOnly.TryParseExact(
                    dateElement.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                throw LiveMarketDataHttp.Invalid();
            }

            points.Add(new HistoricalPricePoint(
                new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                LiveMarketDataHttp.ReadOptionalDecimal(item, "open"),
                LiveMarketDataHttp.ReadOptionalDecimal(item, "high"),
                LiveMarketDataHttp.ReadOptionalDecimal(item, "low"),
                LiveMarketDataHttp.ReadDecimal(item, "close"),
                LiveMarketDataHttp.ReadOptionalDecimal(item, "volume"),
                "USD",
                true));
        }

        return points;
    }
}
