using Microsoft.Extensions.Options;
using Xunit;
using _bootstrap_scaffold.Strategies;
using _bootstrap_scaffold.Watchlist;

namespace Argus.Authentication.Tests;

// The Azure SDK wraps exhausted connection retries in AggregateException, not
// RequestFailedException; stores must still report StorageFailure.
public sealed class TableStoreUnreachableTests
{
    private static readonly WatchlistOptions UnreachableStorage = new()
    {
        ConnectionString =
            "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=dGVzdA==;" +
            "TableEndpoint=http://127.0.0.1:1/devstoreaccount1;",
        TableName = "ArgusWatchlist"
    };

    [Fact]
    public async Task Watchlist_store_reports_storage_failure_when_endpoint_refuses_connection()
    {
        var store = new AzureTableWatchlistStore(Options.Create(UnreachableStorage));

        var result = await store.ListAsync("subject");

        Assert.Equal(WatchlistListStatus.StorageFailure, result.Status);
    }

    [Fact]
    public async Task Portfolio_store_reports_storage_failure_when_endpoint_refuses_connection()
    {
        var store = new AzureTableSimulatedPortfolioStore(
            Options.Create(new SimulatedPortfolioOptions()),
            Options.Create(UnreachableStorage));

        var result = await store.ListAsync("subject");

        Assert.Equal(SimulatedPortfolioStatus.StorageFailure, result.Status);
    }
}
