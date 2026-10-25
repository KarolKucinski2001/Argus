using System.ComponentModel.DataAnnotations;

namespace _bootstrap_scaffold.Watchlist;

public sealed class WatchlistOptions : IValidatableObject
{
    public const string SectionName = "Watchlist";

    public string? ConnectionString { get; set; }

    public string? Endpoint { get; set; }

    [Required]
    public string? TableName { get; set; }

    public int MaximumEntries { get; set; } = WatchlistConstants.MaximumEntries;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ConnectionString) &&
            string.IsNullOrWhiteSpace(Endpoint))
        {
            yield return new ValidationResult(
                "Watchlist:ConnectionString or Watchlist:Endpoint is required.",
                new[] { nameof(ConnectionString), nameof(Endpoint) });
        }

        if (string.IsNullOrWhiteSpace(TableName))
        {
            yield return new ValidationResult(
                "Watchlist:TableName is required.",
                new[] { nameof(TableName) });
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(
                     TableName,
                     "^[A-Za-z][A-Za-z0-9]{2,62}$"))
        {
            yield return new ValidationResult(
                "Watchlist:TableName must be a valid Azure Table name.",
                new[] { nameof(TableName) });
        }

        if (MaximumEntries != WatchlistConstants.MaximumEntries)
        {
            yield return new ValidationResult(
                $"Watchlist:MaximumEntries must be {WatchlistConstants.MaximumEntries}.",
                new[] { nameof(MaximumEntries) });
        }
    }
}
