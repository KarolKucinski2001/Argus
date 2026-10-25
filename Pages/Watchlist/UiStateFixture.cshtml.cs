using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.Watchlist;

namespace _bootstrap_scaffold.Pages.Watchlist;

[AllowAnonymous]
public sealed class UiStateFixtureModel(IWebHostEnvironment environment) : PageModel
{
    private static readonly SparklineView SampleSparkline = new(
        "0 0 100 32",
        "0,24 20,18 40,22 60,12 80,14 100,6",
        "M0,24 L20,18 L40,22 L60,12 L80,14 L100,6 L100,32 L0,32 Z");

    public IReadOnlyList<WatchlistRowView> Rows { get; } =
    [
        Row("AAPL", "Apple", true, new(215.5m, "USD", 1.2m, 4.8m, 9.6m, SampleSparkline)),
        Row("ETH-USD", "Ethereum", true, new(3540m, "USD", -1.5m, -3.9m, -6.2m, SampleSparkline)),
        Row("NVDA", "NVIDIA", false, null)
    ];

    public IActionResult OnGet() =>
        string.Equals(environment.EnvironmentName, "Testing", StringComparison.Ordinal)
            ? Page()
            : NotFound();

    private static WatchlistRowView Row(
        string assetId,
        string name,
        bool isAvailable,
        WatchlistMarketSnapshot? snapshot) =>
        WatchlistTablePresenter.BuildRow(
            assetId,
            name,
            isAvailable ? "Equity" : "Niedostępne",
            new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero),
            isAvailable,
            snapshot);
}
