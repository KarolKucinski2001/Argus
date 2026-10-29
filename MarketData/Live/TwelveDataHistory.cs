using System.Globalization;
using System.Text.Json;

namespace _bootstrap_scaffold.MarketData.Live;

internal static class TwelveDataHistory
{
    public static async Task<IReadOnlyList<HistoricalPricePoint>> GetAsync(
        HttpClient client,
        string escapedSymbol,
        MarketDataHistoryRequest history,
        string apiKey,
        bool hasOhlc,
        CancellationToken cancellationToken)
    {
        var uri = $"time_series?symbol={escapedSymbol}&interval=1day&order=ASC&timezone=UTC" +
                  $"&start_date={history.Start:yyyy-MM-dd}&end_date={history.End:yyyy-MM-dd}";
        using var document = await LiveMarketDataHttp.GetJsonAsync(
            client,
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
                hasOhlc));
        }

        return points;
    }
}
