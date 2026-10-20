namespace _bootstrap_scaffold.Analysis;

public static class AnalysisSummaryResultValidator
{
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
            normalized.Any(char.IsControl))
        {
            return null;
        }

        return normalized;
    }
}
