using Microsoft.Extensions.Options;

namespace _bootstrap_scaffold.MarketData;

public sealed class MarketDataOptions : IValidateOptions<MarketDataOptions>
{
    public const string SectionName = "MarketData";
    public const int RequiredHistoryMonths = 12;

    public int HistoryWindowMonths { get; set; } = RequiredHistoryMonths;
    public int MaximumSelectedAssets { get; set; } = 5;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    public int ApplicationRequestBudget { get; set; } = 100;
    public string ProviderId { get; set; } = string.Empty;
    public bool PublicDisplayApproved { get; set; }
    public string? ProviderApiKeyConfigurationKey { get; set; }
    public Dictionary<string, MarketAssetClass> AllowedAssets { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AAPL"] = MarketAssetClass.Equity,
        ["MSFT"] = MarketAssetClass.Equity,
        ["BTC-USD"] = MarketAssetClass.Crypto,
        ["ETH-USD"] = MarketAssetClass.Crypto
    };

    public ValidateOptionsResult Validate(string? name, MarketDataOptions options)
    {
        var errors = new List<string>();

        if (options.HistoryWindowMonths != RequiredHistoryMonths)
        {
            errors.Add($"{nameof(HistoryWindowMonths)} must be exactly {RequiredHistoryMonths}.");
        }

        if (options.MaximumSelectedAssets <= 0)
        {
            errors.Add($"{nameof(MaximumSelectedAssets)} must be greater than zero.");
        }

        if (options.RequestTimeout <= TimeSpan.Zero)
        {
            errors.Add($"{nameof(RequestTimeout)} must be greater than zero.");
        }

        if (options.CacheDuration <= TimeSpan.Zero)
        {
            errors.Add($"{nameof(CacheDuration)} must be greater than zero.");
        }

        if (options.ApplicationRequestBudget <= 0)
        {
            errors.Add($"{nameof(ApplicationRequestBudget)} must be greater than zero.");
        }

        if (options.AllowedAssets is null || options.AllowedAssets.Count == 0)
        {
            errors.Add($"{nameof(AllowedAssets)} must contain at least one asset.");
        }

        if (options.PublicDisplayApproved && string.IsNullOrWhiteSpace(options.ProviderId))
        {
            errors.Add($"{nameof(ProviderId)} is required when public display is approved.");
        }

        if (!options.PublicDisplayApproved && !string.IsNullOrWhiteSpace(options.ProviderId))
        {
            errors.Add($"{nameof(ProviderId)} cannot be configured before public display is approved.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
