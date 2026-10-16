namespace _bootstrap_scaffold.MarketData;

public sealed record MarketDataServiceError(
    int StatusCode,
    string Title,
    string Detail,
    MarketDataErrorCategory Category,
    bool IsRetryable);

public sealed record MarketDataServiceResult(
    MarketDataResponse? Response,
    MarketDataServiceError? Error)
{
    public bool Succeeded => Response is not null && Error is null;

    public static MarketDataServiceResult Success(MarketDataResponse response) =>
        new(response, null);

    public static MarketDataServiceResult Failure(MarketDataServiceError error) =>
        new(null, error);
}

public interface IMarketDataService
{
    Task<MarketDataServiceResult> GetAsync(
        MarketDataRequest? request,
        CancellationToken cancellationToken = default);
}
