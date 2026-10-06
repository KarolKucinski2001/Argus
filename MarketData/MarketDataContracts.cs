namespace _bootstrap_scaffold.MarketData;

public enum MarketAssetClass
{
    Equity,
    Crypto
}

public enum MarketDataGranularity
{
    Daily,
    Weekly,
    Monthly
}

public enum MarketDataQuality
{
    Complete,
    Incomplete,
    Stale,
    Unavailable
}

public enum MarketDataWarningCategory
{
    MissingCurrentQuote,
    MissingHistory,
    IncompleteHistory,
    StaleData,
    DelayedData,
    ProviderQuota,
    UpstreamUnavailable
}

public enum MarketDataErrorCategory
{
    InvalidRequest,
    UnsupportedAsset,
    ProviderNotApproved,
    NotConfigured,
    Timeout,
    QuotaExceeded,
    AuthenticationFailed,
    UpstreamUnavailable,
    InvalidProviderResponse
}

public sealed record AssetReference(
    string Id,
    string DisplayName,
    MarketAssetClass AssetClass)
{
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Id) &&
        !string.IsNullOrWhiteSpace(DisplayName);
}

public sealed record MarketDataHistoryRequest(
    DateOnly Start,
    DateOnly End,
    MarketDataGranularity Granularity)
{
    public bool IsValid => Start <= End;
}

public sealed record MarketDataRequest(
    IReadOnlyList<AssetReference> Assets,
    MarketDataHistoryRequest History)
{
    public bool IsValid =>
        Assets.Count > 0 &&
        Assets.All(asset => asset.IsValid) &&
        History.IsValid;
}

public sealed record MarketDataFreshness(
    DateTimeOffset ObservedAt,
    DateTimeOffset RetrievedAt,
    DateTimeOffset? ExpiresAt,
    bool IsStale)
{
    public bool IsExpired =>
        ExpiresAt is not null && ExpiresAt <= DateTimeOffset.UtcNow;
}

public sealed record MarketDataSource(
    string ProviderId,
    string? Attribution,
    bool IsDelayed,
    TimeSpan? Delay);

public sealed record MarketQuote(
    decimal Value,
    string Currency,
    DateTimeOffset ObservedAt,
    MarketDataFreshness Freshness,
    MarketDataQuality Quality);

public sealed record HistoricalPricePoint(
    DateTimeOffset Timestamp,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal Close,
    decimal? Volume,
    string Currency,
    bool IsAdjusted);

public sealed record MarketDataWarning(
    MarketDataWarningCategory Category,
    string Message,
    bool IsRetryable);

public sealed record MarketDataError(
    MarketDataErrorCategory Category,
    string Message,
    bool IsRetryable,
    int? UpstreamStatusCode = null);

public sealed record MarketDataResult(
    AssetReference Asset,
    MarketDataHistoryRequest RequestedHistory,
    MarketQuote? CurrentQuote,
    IReadOnlyList<HistoricalPricePoint> HistoricalPoints,
    MarketDataSource? Source,
    MarketDataFreshness Freshness,
    MarketDataQuality Quality,
    IReadOnlyList<MarketDataWarning> Warnings,
    MarketDataError? Error = null);

public sealed record MarketDataResponse(
    IReadOnlyList<MarketDataResult> Results);
