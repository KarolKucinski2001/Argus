namespace _bootstrap_scaffold.MarketData;

public sealed class RoutingMarketDataProvider(
    IReadOnlyDictionary<MarketAssetClass, IMarketDataProvider> providers) : IMarketDataProvider
{
    public async Task<IReadOnlyList<MarketDataResult>> GetAsync(
        MarketDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var groups = request.Assets
            .GroupBy(asset => asset.AssetClass)
            .ToArray();

        var batches = await Task.WhenAll(groups.Select(async group =>
        {
            if (!providers.TryGetValue(group.Key, out var provider))
            {
                return NotConfigured(group, request.History);
            }

            return await provider.GetAsync(request with { Assets = group.ToArray() }, cancellationToken);
        }));

        var byAsset = batches
            .SelectMany(batch => batch)
            .GroupBy(result => result.Asset.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        // Results follow request order; assets a sub-provider omitted are left for the service to report.
        return request.Assets
            .Select(asset => byAsset.GetValueOrDefault(asset.Id))
            .OfType<MarketDataResult>()
            .ToArray();
    }

    private static IReadOnlyList<MarketDataResult> NotConfigured(
        IEnumerable<AssetReference> assets,
        MarketDataHistoryRequest history)
    {
        var now = DateTimeOffset.UtcNow;
        return assets.Select(asset => new MarketDataResult(
            asset,
            history,
            null,
            [],
            null,
            new MarketDataFreshness(now, now, null, true),
            MarketDataQuality.Unavailable,
            [],
            new MarketDataError(
                MarketDataErrorCategory.NotConfigured,
                "No market data provider is configured for this asset class.",
                false))).ToArray();
    }
}
