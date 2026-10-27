using System.Text.Json;
using Microsoft.Extensions.Options;

namespace _bootstrap_scaffold.MarketData.Live;

// Crypto adapter on the CoinGecko Demo API: /simple/price for the quote and
// /coins/{id}/market_chart/range for daily (00:00 UTC) history.
public sealed class LiveCryptoMarketDataProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<LiveMarketDataOptions> options,
    TimeProvider timeProvider) : IMarketDataProvider
{
    public const string ClientName = "market-data-coingecko";

    private static readonly MarketDataSource Source = new(
        "coingecko-demo",
        "Powered by CoinGecko API (coingecko.com).",
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
        if (string.IsNullOrWhiteSpace(settings.CoinGeckoApiKey))
        {
            throw new MarketDataProviderException(
                MarketDataErrorCategory.NotConfigured, "The crypto provider key is not configured.", false);
        }

        if (!settings.CoinGeckoIds.TryGetValue(asset.Id, out var coinId) || string.IsNullOrWhiteSpace(coinId))
        {
            throw new MarketDataProviderException(
                MarketDataErrorCategory.UnsupportedAsset, "The provider does not support this asset.", false);
        }

        var points = await GetHistoryAsync(Uri.EscapeDataString(coinId), history, settings.CoinGeckoApiKey, cancellationToken);

        var warnings = new List<MarketDataWarning>();
        MarketQuote? quote = null;
        try
        {
            quote = await GetQuoteAsync(Uri.EscapeDataString(coinId), settings.CoinGeckoApiKey, cancellationToken);
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

    private async Task<MarketQuote> GetQuoteAsync(string coinId, string apiKey, CancellationToken cancellationToken)
    {
        using var document = await LiveMarketDataHttp.GetJsonAsync(
            httpClientFactory.CreateClient(ClientName),
            $"simple/price?ids={coinId}&vs_currencies=usd&include_last_updated_at=true",
            "x-cg-demo-api-key",
            apiKey,
            cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty(Uri.UnescapeDataString(coinId), out var entry) ||
            entry.ValueKind != JsonValueKind.Object)
        {
            throw new MarketDataProviderException(
                MarketDataErrorCategory.UnsupportedAsset, "The provider has no quote for this asset.", false);
        }

        var price = LiveMarketDataHttp.ReadDecimal(entry, "usd");
        var updated = LiveMarketDataHttp.ReadDecimal(entry, "last_updated_at");
        var now = timeProvider.GetUtcNow();
        return LiveMarketDataHttp.CreateQuote(price, DateTimeOffset.FromUnixTimeSeconds((long)updated), now);
    }

    private async Task<IReadOnlyList<HistoricalPricePoint>> GetHistoryAsync(
        string coinId,
        MarketDataHistoryRequest history,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var from = new DateTimeOffset(history.Start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
        var to = new DateTimeOffset(history.End.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
        using var document = await LiveMarketDataHttp.GetJsonAsync(
            httpClientFactory.CreateClient(ClientName),
            $"coins/{coinId}/market_chart/range?vs_currency=usd&from={from}&to={to}",
            "x-cg-demo-api-key",
            apiKey,
            cancellationToken);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("prices", out var prices) ||
            prices.ValueKind != JsonValueKind.Array)
        {
            throw LiveMarketDataHttp.Invalid();
        }

        var volumes = new Dictionary<long, decimal>();
        if (root.TryGetProperty("total_volumes", out var volumeArray) && volumeArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var pair in volumeArray.EnumerateArray())
            {
                if (pair.GetArrayLength() == 2 &&
                    LiveMarketDataHttp.TryReadDecimal(pair[0], out var ms) &&
                    LiveMarketDataHttp.TryReadDecimal(pair[1], out var volume))
                {
                    volumes[(long)ms] = volume;
                }
            }
        }

        var points = new List<HistoricalPricePoint>();
        foreach (var pair in prices.EnumerateArray())
        {
            if (pair.ValueKind != JsonValueKind.Array ||
                pair.GetArrayLength() != 2 ||
                !LiveMarketDataHttp.TryReadDecimal(pair[0], out var ms) ||
                !LiveMarketDataHttp.TryReadDecimal(pair[1], out var close))
            {
                throw LiveMarketDataHttp.Invalid();
            }

            // CoinGecko exposes only a close per day, so open/high/low stay null.
            points.Add(new HistoricalPricePoint(
                DateTimeOffset.FromUnixTimeMilliseconds((long)ms),
                null,
                null,
                null,
                close,
                volumes.TryGetValue((long)ms, out var dayVolume) ? dayVolume : null,
                "USD",
                false));
        }

        return points;
    }
}
