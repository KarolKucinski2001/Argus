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
    Tesla,
    Crypto
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
            ["GOOGL"] = new("GOOGL", "Alphabet (Google)", "G", AssetAccent.Equity),
            ["AMZN"] = new("AMZN", "Amazon", "A", AssetAccent.Equity),
            ["META"] = new("META", "Meta Platforms", "M", AssetAccent.Equity),
            ["JPM"] = new("JPM", "JPMorgan Chase", "J", AssetAccent.Equity),
            ["V"] = new("V", "Visa", "V", AssetAccent.Equity),
            ["KO"] = new("KO", "Coca-Cola", "K", AssetAccent.Equity),
            ["SPY"] = new("SPY", "ETF na S&P 500 (SPY)", "S", AssetAccent.Equity),
            ["QQQ"] = new("QQQ", "ETF na Nasdaq-100 (QQQ)", "Q", AssetAccent.Equity),
            ["BTC-USD"] = new("BTC-USD", "Bitcoin", "₿", AssetAccent.Bitcoin),
            ["ETH-USD"] = new("ETH-USD", "Ethereum", "Ξ", AssetAccent.Ethereum),
            ["SOL-USD"] = new("SOL-USD", "Solana", "S", AssetAccent.Crypto),
            ["XRP-USD"] = new("XRP-USD", "XRP", "X", AssetAccent.Crypto),
            ["ADA-USD"] = new("ADA-USD", "Cardano", "A", AssetAccent.Crypto)
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
            AssetAccent.Crypto => "asset-accent--crypto",
            _ => "asset-accent--neutral"
        };
}
