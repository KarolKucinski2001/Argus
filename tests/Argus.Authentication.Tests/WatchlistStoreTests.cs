using _bootstrap_scaffold.Watchlist;
using Xunit;

namespace Argus.Authentication.Tests;

public sealed class WatchlistStoreTests
{
    [Fact]
    public void Identity_normalization_is_case_insensitive_and_row_key_safe()
    {
        Assert.Equal("BTC-USD", WatchlistIdentity.TryNormalizeAssetId(" btc-usd "));
        Assert.Equal("BTC-USD", WatchlistIdentity.NormalizeRowKey("btc-usd"));
        Assert.Null(WatchlistIdentity.TryNormalizeAssetId("asset/with/slash"));
    }

    [Fact]
    public void Save_policy_marks_duplicates_and_new_items()
    {
        var result = WatchlistPolicy.ClassifySave(
            ["AAPL"],
            ["aapl", "MSFT"],
            maximumEntries: 2);

        Assert.Collection(
            result,
            duplicate => Assert.Equal(WatchlistSaveStatus.Duplicate, duplicate.Status),
            saved => Assert.Equal(WatchlistSaveStatus.Saved, saved.Status));
    }

    [Fact]
    public void Save_policy_rejects_new_items_when_capacity_would_be_exceeded()
    {
        var result = WatchlistPolicy.ClassifySave(
            ["AAPL", "MSFT"],
            ["BTC-USD"],
            maximumEntries: 2);

        var item = Assert.Single(result);
        Assert.Equal(WatchlistSaveStatus.CapacityExceeded, item.Status);
    }

    [Fact]
    public void Entries_are_ordered_newest_first_with_deterministic_tie_breaking()
    {
        var savedAt = DateTimeOffset.UtcNow;
        var result = WatchlistPolicy.OrderEntries(
        [
            new WatchlistEntry("MSFT", savedAt),
            new WatchlistEntry("AAPL", savedAt),
            new WatchlistEntry("BTC-USD", savedAt.AddMinutes(-1))
        ]);

        Assert.Equal(["AAPL", "MSFT", "BTC-USD"], result.Select(entry => entry.AssetId));
    }

    [Fact]
    public void Save_result_exposes_storage_failure()
    {
        var result = new WatchlistSaveResult(
        [
            new WatchlistSaveItemResult("AAPL", WatchlistSaveStatus.StorageFailure)
        ]);

        Assert.True(result.HasStorageFailure);
        Assert.False(result.Succeeded);
    }
}
