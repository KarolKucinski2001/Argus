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
    IMarketDataService marketDataService,
    IAnalysisService analysisService,
    ILogger<IndexModel> logger) : PageModel
{
    private readonly MarketDataOptions marketData = configuredMarketData.Value;

    public string Greeting { get; private set; } = DashboardPresenter.Greeting(null);
    public IReadOnlyList<DashboardMarketTile> Tiles { get; private set; } = [];
    public bool MarketUnavailable { get; private set; }

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

        var snapshots = await LoadSnapshotsAsync(cancellationToken);
        Tiles = DashboardPresenter.BuildTiles(marketData.AllowedAssets.Keys, snapshots);
        MarketUnavailable = DashboardPresenter.IsMarketUnavailable(Tiles);
        return Page();
    }

    private async Task<IReadOnlyDictionary<string, WatchlistMarketSnapshot>> LoadSnapshotsAsync(
        CancellationToken cancellationToken)
    {
        var snapshots = new Dictionary<string, WatchlistMarketSnapshot>(StringComparer.OrdinalIgnoreCase);
        var assets = DashboardPresenter.RequestableAssetIds(marketData.AllowedAssets.Keys)
            .Select(assetId => new AssetReference(assetId, assetId, marketData.AllowedAssets[assetId]))
            .ToArray();
        if (assets.Length == 0)
        {
            return snapshots;
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
                return snapshots;
            }

            var quotes = response.Response!.Results
                .Where(item => item.CurrentQuote is not null)
                .ToDictionary(item => item.Asset.Id, item => item.CurrentQuote!, StringComparer.OrdinalIgnoreCase);
            var analysis = analysisService.Analyze(response.Response, DateTimeOffset.UtcNow);
            foreach (var item in analysis.Results.Where(item => item.Error is null))
            {
                quotes.TryGetValue(item.Asset.Id, out var quote);
                snapshots[item.Asset.Id] = new WatchlistMarketSnapshot(
                    quote?.Value,
                    quote?.Currency,
                    Available(item.Performance.Change24Hours),
                    Available(item.Performance.Change7Days),
                    Available(item.Performance.Change30Days),
                    AnalysisInsightPresenter.Present(item).Sparkline);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Market overview is decoration here; the rest of the dashboard must still render.
            logger.LogWarning(exception, "Market data for the dashboard overview failed.");
            snapshots.Clear();
        }

        return snapshots;
    }

    private static decimal? Available(PerformanceMetric metric) =>
        metric.IsAvailable ? metric.Percentage : null;
}
