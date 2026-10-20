namespace _bootstrap_scaffold.Analysis;

public static class AnalysisSummaryResultValidator
{
    private static readonly string[] DisallowedTerms =
    [
        "buy",
        "sell",
        "best investment",
        "guaranteed",
        "top opportunity",
        "investment advice",
        "provider",
        "model",
        "prompt",
        "kup",
        "sprzedaj",
        "najlepsza inwestycja",
        "gwarant",
        "porada inwestycyjna"
    ];

    public static AnalysisSummaryResult Validate(AnalysisSummaryResult? result)
    {
        if (result is null || result.Status != AnalysisSummaryStatus.Generated)
        {
            return AnalysisSummaryResult.Unavailable();
        }

        var text = Normalize(result.Text);
        return text is null
            ? AnalysisSummaryResult.Unavailable()
            : AnalysisSummaryResult.Generated(text);
    }

    private static string? Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            text.Length > AnalysisSummaryResult.MaximumSummaryLength)
        {
            return null;
        }

        var normalized = string.Join(
            ' ',
            text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (normalized.Length == 0 ||
            normalized.Length > AnalysisSummaryResult.MaximumSummaryLength ||
            normalized.Any(char.IsControl) ||
            DisallowedTerms.Any(term =>
                normalized.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return normalized;
    }
}
