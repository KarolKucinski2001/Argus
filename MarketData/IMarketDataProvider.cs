namespace _bootstrap_scaffold.MarketData;

public interface IMarketDataProvider
{
    Task<IReadOnlyList<MarketDataResult>> GetAsync(
        MarketDataRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class MarketDataProviderException(
    MarketDataErrorCategory category,
    string message,
    bool isRetryable,
    int? upstreamStatusCode = null) : Exception(message)
{
    public MarketDataErrorCategory Category { get; } = category;
    public bool IsRetryable { get; } = isRetryable;
    public int? UpstreamStatusCode { get; } = upstreamStatusCode;
}
