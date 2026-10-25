namespace _bootstrap_scaffold.Watchlist;

public static class WatchlistConstants
{
    public const int MaximumEntries = 50;
}

public sealed record WatchlistEntry(
    string AssetId,
    DateTimeOffset SavedAtUtc);

public enum WatchlistSaveStatus
{
    Saved,
    Duplicate,
    Unavailable,
    CapacityExceeded,
    StorageFailure
}

public sealed record WatchlistSaveItemResult(
    string AssetId,
    WatchlistSaveStatus Status);

public sealed record WatchlistSaveResult(
    IReadOnlyList<WatchlistSaveItemResult> Items)
{
    public bool Succeeded =>
        Items.Count > 0 &&
        Items.All(item =>
            item.Status is WatchlistSaveStatus.Saved or WatchlistSaveStatus.Duplicate);

    public bool HasStorageFailure =>
        Items.Any(item => item.Status == WatchlistSaveStatus.StorageFailure);
}

public enum WatchlistListStatus
{
    Succeeded,
    StorageFailure
}

public sealed record WatchlistListResult(
    WatchlistListStatus Status,
    IReadOnlyList<WatchlistEntry> Entries)
{
    public bool Succeeded => Status == WatchlistListStatus.Succeeded;
}

public enum WatchlistRemoveStatus
{
    Removed,
    NotFound,
    Unavailable,
    StorageFailure
}

public sealed record WatchlistRemoveResult(
    string AssetId,
    WatchlistRemoveStatus Status)
{
    public bool Succeeded => Status == WatchlistRemoveStatus.Removed;
}

public static class WatchlistPolicy
{
    public static IReadOnlyList<string> NormalizeAssetIds(IEnumerable<string>? assetIds) =>
        (assetIds ?? Array.Empty<string>())
            .Select(WatchlistIdentity.TryNormalizeAssetId)
            .Where(assetId => assetId is not null)
            .Select(assetId => assetId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static IReadOnlyList<WatchlistSaveItemResult> ClassifySave(
        IEnumerable<string> existingAssetIds,
        IEnumerable<string> requestedAssetIds,
        int maximumEntries)
    {
        var existing = existingAssetIds
            .Select(WatchlistIdentity.TryNormalizeAssetId)
            .Where(assetId => assetId is not null)
            .Select(assetId => assetId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requested = NormalizeAssetIds(requestedAssetIds);
        var newCount = requested.Count(assetId => !existing.Contains(assetId));
        var capacityExceeded = existing.Count + newCount > maximumEntries;

        return requested
            .Select(assetId => new WatchlistSaveItemResult(
                assetId,
                existing.Contains(assetId)
                    ? WatchlistSaveStatus.Duplicate
                    : capacityExceeded
                        ? WatchlistSaveStatus.CapacityExceeded
                        : WatchlistSaveStatus.Saved))
            .ToArray();
    }

    public static IReadOnlyList<WatchlistEntry> OrderEntries(
        IEnumerable<WatchlistEntry> entries) =>
        entries
            .OrderByDescending(entry => entry.SavedAtUtc)
            .ThenBy(entry => entry.AssetId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
