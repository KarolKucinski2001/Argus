namespace _bootstrap_scaffold.Analysis;

public sealed record AssetIdentity(string Id, string Name, string Monogram);

public static class AssetIdentityCatalog
{
    private static readonly IReadOnlyDictionary<string, AssetIdentity> Known =
        new Dictionary<string, AssetIdentity>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new("AAPL", "Apple Inc.", "A"),
            ["MSFT"] = new("MSFT", "Microsoft", "M"),
            ["BTC-USD"] = new("BTC-USD", "Bitcoin", "₿"),
            ["ETH-USD"] = new("ETH-USD", "Ethereum", "Ξ")
        };

    public static AssetIdentity For(string id) =>
        Known.TryGetValue(id, out var identity)
            ? identity
            : new AssetIdentity(
                id,
                id,
                string.IsNullOrEmpty(id) ? "?" : id[..1].ToUpperInvariant());
}
