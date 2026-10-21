namespace _bootstrap_scaffold.Analysis;

public enum AssetAccent
{
    Equity,
    Bitcoin,
    Ethereum,
    Neutral,
    Apple,
    Microsoft,
    Nvidia,
    Tesla
}

public sealed record AssetIdentity(string Id, string Name, string Monogram, AssetAccent Accent);

public static class AssetIdentityCatalog
{
    private static readonly IReadOnlyDictionary<string, AssetIdentity> Known =
        new Dictionary<string, AssetIdentity>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new("AAPL", "Apple", "A", AssetAccent.Apple),
            ["MSFT"] = new("MSFT", "Microsoft", "M", AssetAccent.Microsoft),
            ["NVDA"] = new("NVDA", "NVIDIA", "N", AssetAccent.Nvidia),
            ["TSLA"] = new("TSLA", "Tesla", "T", AssetAccent.Tesla),
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
            AssetAccent.Apple => "asset-accent--apple",
            AssetAccent.Microsoft => "asset-accent--microsoft",
            AssetAccent.Nvidia => "asset-accent--nvidia",
            AssetAccent.Tesla => "asset-accent--tesla",
            AssetAccent.Bitcoin => "asset-accent--bitcoin",
            AssetAccent.Ethereum => "asset-accent--ethereum",
            _ => "asset-accent--neutral"
        };
}
