namespace _bootstrap_scaffold.Analysis;

public sealed class AnalysisSummaryOptions
{
    public const string SectionName = "AnalysisSummary";

    public bool Enabled { get; set; }

    public bool ApprovedProvider { get; set; }

    public string? Provider { get; set; }

    public string? Endpoint { get; set; }

    public string? ApiKeyConfigurationKey { get; set; }

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public bool IsConfigured =>
        Enabled &&
        ApprovedProvider &&
        !string.IsNullOrWhiteSpace(Provider) &&
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(ApiKeyConfigurationKey);
}
