namespace _bootstrap_scaffold.MarketData;

public interface IMarketDataProvider
{
    Task<IReadOnlyList<MarketDataResult>> GetAsync(
        MarketDataRequest request,
        CancellationToken cancellationToken = default);
}
