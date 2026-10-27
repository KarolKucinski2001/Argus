using System.Globalization;
using System.Text.Json;

namespace _bootstrap_scaffold.MarketData.Live;

internal static class LiveMarketDataHttp
{
    // The key travels in a header so it never appears in URLs, logs or exception messages.
    public static async Task<JsonDocument> GetJsonAsync(
        HttpClient client,
        string relativeUri,
        string headerName,
        string headerValue,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUri);
        request.Headers.TryAddWithoutValidation(headerName, headerValue);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MarketDataProviderException(
                MarketDataErrorCategory.Timeout, "The provider request timed out.", true);
        }
        catch (HttpRequestException)
        {
            throw new MarketDataProviderException(
                MarketDataErrorCategory.UpstreamUnavailable, "The provider is unreachable.", true);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw FromStatus((int)response.StatusCode);
            }

            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                throw Invalid();
            }
        }
    }

    public static MarketDataProviderException FromStatus(int status) => status switch
    {
        401 or 403 => new(MarketDataErrorCategory.AuthenticationFailed, "The provider rejected the credentials.", false, status),
        429 => new(MarketDataErrorCategory.QuotaExceeded, "The provider request limit was reached.", true, status),
        400 or 404 or 422 => new(MarketDataErrorCategory.UnsupportedAsset, "The provider does not support this asset.", false, status),
        408 or 504 => new(MarketDataErrorCategory.Timeout, "The provider request timed out.", true, status),
        _ => new(MarketDataErrorCategory.UpstreamUnavailable, "The provider is unavailable.", true, status)
    };

    public static MarketDataProviderException Invalid() =>
        new(MarketDataErrorCategory.InvalidProviderResponse, "The provider returned invalid data.", false);

    public static decimal ReadDecimal(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(name, out var value) ||
            !TryReadDecimal(value, out var result))
        {
            throw Invalid();
        }

        return result;
    }

    public static decimal? ReadOptionalDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && TryReadDecimal(value, out var result)
            ? result
            : null;

    public static bool TryReadDecimal(JsonElement value, out decimal result)
    {
        result = 0;
        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetDecimal(out result),
            JsonValueKind.String => decimal.TryParse(
                value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result),
            _ => false
        };
    }

    public static MarketDataResult Assemble(
        AssetReference asset,
        MarketDataHistoryRequest history,
        MarketQuote? quote,
        IEnumerable<HistoricalPricePoint> points,
        MarketDataSource source,
        DateTimeOffset now,
        List<MarketDataWarning> warnings)
    {
        var ordered = points
            .Where(point =>
            {
                var day = DateOnly.FromDateTime(point.Timestamp.UtcDateTime);
                return day >= history.Start && day <= history.End;
            })
            .GroupBy(point => DateOnly.FromDateTime(point.Timestamp.UtcDateTime))
            .Select(group => group.Last())
            .OrderBy(point => point.Timestamp)
            .ToList();
        ordered = Downsample(ordered, history.Granularity);

        if (quote is null)
        {
            warnings.Add(new(MarketDataWarningCategory.MissingCurrentQuote, "No current quote was returned.", true));
        }

        if (ordered.Count == 0)
        {
            warnings.Add(new(MarketDataWarningCategory.MissingHistory, "No historical prices were returned.", true));
        }

        if (source.IsDelayed)
        {
            warnings.Add(new(MarketDataWarningCategory.DelayedData, "Data from this provider may be delayed.", false));
        }

        var observedAt = quote?.ObservedAt ?? ordered.LastOrDefault()?.Timestamp ?? now;
        var freshness = new MarketDataFreshness(observedAt, now, now.AddMinutes(5), false);
        var quality = quote is not null && ordered.Count > 0
            ? MarketDataQuality.Complete
            : MarketDataQuality.Incomplete;

        return new MarketDataResult(
            asset, history, quote, ordered, source, freshness, quality, warnings);
    }

    public static MarketQuote CreateQuote(decimal value, DateTimeOffset observedAt, DateTimeOffset now) =>
        new(
            value,
            "USD",
            observedAt,
            new MarketDataFreshness(observedAt, now, now.AddMinutes(5), false),
            MarketDataQuality.Complete);

    public static MarketDataResult Failed(
        AssetReference asset,
        MarketDataHistoryRequest history,
        MarketDataProviderException exception,
        DateTimeOffset now) =>
        new(
            asset,
            history,
            null,
            [],
            null,
            new MarketDataFreshness(now, now, null, true),
            MarketDataQuality.Unavailable,
            [],
            new MarketDataError(
                exception.Category,
                exception.Message,
                exception.IsRetryable,
                exception.UpstreamStatusCode));

    public static async Task<IReadOnlyList<MarketDataResult>> RunAsync(
        MarketDataRequest request,
        TimeProvider timeProvider,
        Func<AssetReference, CancellationToken, Task<MarketDataResult>> fetch,
        CancellationToken cancellationToken)
    {
        var results = new List<MarketDataResult>();
        foreach (var asset in request.Assets)
        {
            try
            {
                results.Add(await fetch(asset, cancellationToken));
            }
            catch (MarketDataProviderException exception)
            {
                results.Add(Failed(asset, request.History, exception, timeProvider.GetUtcNow()));
            }
        }

        return results;
    }

    private static List<HistoricalPricePoint> Downsample(
        List<HistoricalPricePoint> points,
        MarketDataGranularity granularity)
    {
        if (granularity == MarketDataGranularity.Daily)
        {
            return points;
        }

        var kept = new List<HistoricalPricePoint>();
        foreach (var point in points)
        {
            if (kept.Count == 0 ||
                (granularity == MarketDataGranularity.Weekly &&
                 point.Timestamp - kept[^1].Timestamp >= TimeSpan.FromDays(7)) ||
                (granularity == MarketDataGranularity.Monthly &&
                 (point.Timestamp.Month != kept[^1].Timestamp.Month ||
                  point.Timestamp.Year != kept[^1].Timestamp.Year)))
            {
                kept.Add(point);
            }
        }

        return kept;
    }
}
