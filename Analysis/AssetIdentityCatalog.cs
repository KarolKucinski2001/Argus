namespace _bootstrap_scaffold.Analysis;

public enum AssetAccent
{
    Equity,
    Bitcoin,
    Ethereum,
    Neutral
}

public sealed record AssetIdentity(string Id, string Name, string Monogram, AssetAccent Accent);

public static class AssetIdentityCatalog
{
    private static readonly IReadOnlyDictionary<string, AssetIdentity> Known =
        new Dictionary<string, AssetIdentity>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new("AAPL", "Apple", "A", AssetAccent.Equity),
            ["MSFT"] = new("MSFT", "Microsoft", "M", AssetAccent.Equity),
            ["NVDA"] = new("NVDA", "NVIDIA", "N", AssetAccent.Equity),
            ["TSLA"] = new("TSLA", "Tesla", "T", AssetAccent.Equity),
            ["BTC-USD"] = new("BTC-USD", "Bitcoin", "₿", AssetAccent.Bitcoin),
            ["ETH-USD"] = new("ETH-USD", "Ethereum", "Ξ", AssetAccent.Ethereum)
        };

    public static AssetIdentity For(string id) =>
        Known.TryGetValue(id, out var identity)
            ? identity
            : new AssetIdentity(
                id,
                id,
                string.IsNullOrEmpty(id) ? "?" : id[..1].ToUpperInvariant(),
                AssetAccent.Neutral);

    public static string AccentClass(AssetAccent accent) =>
        accent switch
        {
            AssetAccent.Equity => "asset-accent--equity",
            AssetAccent.Bitcoin => "asset-accent--bitcoin",
            AssetAccent.Ethereum => "asset-accent--ethereum",
            _ => "asset-accent--neutral"
        };
}
