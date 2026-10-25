namespace _bootstrap_scaffold.Watchlist;

public interface IWatchlistStore
{
    Task<WatchlistListResult> ListAsync(
        string subjectId,
        CancellationToken cancellationToken = default);

    Task<WatchlistSaveResult> SaveAsync(
        string subjectId,
        IEnumerable<string> assetIds,
        CancellationToken cancellationToken = default);

    Task<WatchlistRemoveResult> RemoveAsync(
        string subjectId,
        string assetId,
        CancellationToken cancellationToken = default);
}
