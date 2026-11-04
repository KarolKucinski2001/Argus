namespace _bootstrap_scaffold.MarketData.Live;

public sealed class LiveMarketDataOptions
{
    public const string SectionName = "MarketData:Live";

    // ProviderId that selects the free-tier live adapters; it is only honored when PublicDisplayApproved is true.
    public const string ProviderId = "argus-live-free";

    public string? FinnhubApiKey { get; set; }
    public string? TwelveDataApiKey { get; set; }
    public string? CoinGeckoApiKey { get; set; }

    public Uri FinnhubBaseAddress { get; set; } = new("https://finnhub.io/api/v1/");
    public Uri TwelveDataBaseAddress { get; set; } = new("https://api.twelvedata.com/");
    public Uri CoinGeckoBaseAddress { get; set; } = new("https://api.coingecko.com/api/v3/");

    public Dictionary<string, string> CoinGeckoIds { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BTC-USD"] = "bitcoin",
        ["ETH-USD"] = "ethereum",
        ["SOL-USD"] = "solana",
        ["XRP-USD"] = "ripple",
        ["ADA-USD"] = "cardano"
    };
}
