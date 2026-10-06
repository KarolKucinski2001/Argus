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

        if (string.IsNullOrWhiteSpace(options.ProviderId))
        {
            errors.Add($"{nameof(ProviderId)} is required.");
        }

        if (!options.PublicDisplayApproved)
        {
            errors.Add($"{nameof(PublicDisplayApproved)} must be true before a provider can be enabled.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
