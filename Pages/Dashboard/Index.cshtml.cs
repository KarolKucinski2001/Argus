using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.Dashboard;
using _bootstrap_scaffold.MarketData;
using _bootstrap_scaffold.Watchlist;

namespace _bootstrap_scaffold.Pages.Dashboard;

[AllowAnonymous]
public sealed class IndexModel(
    IOptions<MarketDataOptions> configuredMarketData,
    IWatchlistStore watchlistStore,
    IMarketDataService marketDataService,
    IAnalysisService analysisService,
    ILogger<IndexModel> logger) : PageModel
{
    private readonly MarketDataOptions marketData = configuredMarketData.Value;

    public string Greeting { get; private set; } = DashboardPresenter.Greeting(null);
    public IReadOnlyList<DashboardMarketTile> Tiles { get; private set; } = [];
    public bool MarketUnavailable { get; private set; }
    public IReadOnlyList<WatchlistRowView> WatchlistRows { get; private set; } = [];
    public IReadOnlyList<DashboardInsightView> Insights { get; private set; } = [];
    public bool WatchlistFailed { get; private set; }
    public bool WatchlistEmpty { get; private set; }
    public bool WatchlistHasMore { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = "/dashboard" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        var displayName = User.FindFirstValue("name") ?? User.Identity?.Name;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        Greeting = DashboardPresenter.Greeting(DashboardPresenter.FirstName(displayName, email));

        var entries = await ListWatchlistAsync(cancellationToken);
        var availableWatchlistIds = entries
            .Select(entry => entry.AssetId)
            .Where(marketData.AllowedAssets.ContainsKey)
            .ToArray();

        var market = await LoadMarketAsync(availableWatchlistIds, cancellationToken);
        Tiles = DashboardPresenter.BuildTiles(marketData.AllowedAssets.Keys, market.Snapshots);
        MarketUnavailable = DashboardPresenter.IsMarketUnavailable(Tiles);

        WatchlistEmpty = !WatchlistFailed && entries.Count == 0;
        WatchlistHasMore = DashboardPresenter.HasMoreRows(entries.Count);
        WatchlistRows = DashboardPresenter.BuildWatchlistRows(
            entries, marketData.AllowedAssets.Keys, market.Snapshots);
        Insights = DashboardPresenter.BuildInsights(
            market.Insights.Where(insight => availableWatchlistIds.Contains(
                insight.Identity.Id, StringComparer.OrdinalIgnoreCase)));
        return Page();
    }

    private async Task<IReadOnlyList<WatchlistEntry>> ListWatchlistAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var subjectId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (subjectId is null)
            {
                WatchlistFailed = true;
                return [];
            }

            var result = await watchlistStore.ListAsync(subjectId, cancellationToken);
            if (!result.Succeeded)
            {
                WatchlistFailed = true;
                return [];
            }

            return WatchlistPolicy.OrderEntries(result.Entries);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Watchlist for the dashboard failed.");
            WatchlistFailed = true;
            return [];
        }
    }

    private async Task<(IReadOnlyDictionary<string, WatchlistMarketSnapshot> Snapshots, IReadOnlyList<AssetInsightView> Insights)> LoadMarketAsync(
        IEnumerable<string> watchlistAssetIds,
        CancellationToken cancellationToken)
    {
        var snapshots = new Dictionary<string, WatchlistMarketSnapshot>(StringComparer.OrdinalIgnoreCase);
        var insights = new List<AssetInsightView>();
        var assets = DashboardPresenter.RequestableAssetIds(marketData.AllowedAssets.Keys)
            .Concat(watchlistAssetIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(assetId => new AssetReference(assetId, assetId, marketData.AllowedAssets[assetId]))
            .ToArray();
        if (assets.Length == 0)
        {
            return (snapshots, insights);
        }

        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var request = new MarketDataRequest(
                assets,
                new MarketDataHistoryRequest(
                    today.AddMonths(-MarketDataOptions.RequiredHistoryMonths),
                    today,
                    MarketDataGranularity.Daily));
            var response = await marketDataService.GetAsync(request, cancellationToken);
            if (!response.Succeeded)
            {
                return (snapshots, insights);
            }

            var quotes = response.Response!.Results
                .Where(item => item.CurrentQuote is not null)
                .ToDictionary(item => item.Asset.Id, item => item.CurrentQuote!, StringComparer.OrdinalIgnoreCase);
            var analysis = analysisService.Analyze(response.Response, DateTimeOffset.UtcNow);
            foreach (var item in analysis.Results.Where(item => item.Error is null))
            {
                quotes.TryGetValue(item.Asset.Id, out var quote);
                var insight = AnalysisInsightPresenter.Present(item);
                insights.Add(insight);
                snapshots[item.Asset.Id] = new WatchlistMarketSnapshot(
                    quote?.Value,
                    quote?.Currency,
                    Available(item.Performance.Change24Hours),
                    Available(item.Performance.Change7Days),
                    Available(item.Performance.Change30Days),
                    insight.Sparkline);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Market data is decoration here; the rest of the dashboard must still render.
            logger.LogWarning(exception, "Market data for the dashboard failed.");
            snapshots.Clear();
            insights.Clear();
        }

        return (snapshots, insights);
    }

    private static decimal? Available(PerformanceMetric metric) =>
        metric.IsAvailable ? metric.Percentage : null;
}
