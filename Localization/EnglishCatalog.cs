namespace _bootstrap_scaffold.Localization;

/// <summary>Merges the per-area English dictionaries; keys are the Polish source strings.</summary>
public static class EnglishCatalog
{
    public static IReadOnlyList<(string Area, (string Pl, string En)[] Entries)> Areas { get; } =
    [
        ("Shell", EnglishShell.Entries),
        ("Analysis", EnglishAnalysis.Entries),
        ("WatchlistDashboard", EnglishWatchlistDashboard.Entries),
        ("Strategies", EnglishStrategies.Entries)
    ];

    public static IReadOnlyDictionary<string, string> Build()
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (_, area) in Areas)
        {
            foreach (var (pl, en) in area)
            {
                entries.TryAdd(pl, en);
            }
        }

        return entries;
    }
}