using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.Localization;
using _bootstrap_scaffold.MarketData;
using _bootstrap_scaffold.Watchlist;

namespace _bootstrap_scaffold.Pages.Watchlist;

[AllowAnonymous]
public sealed class IndexModel(
    IOptions<MarketDataOptions> configuredMarketData,
    IWatchlistStore watchlistStore,
    IMarketDataService marketDataService,
    IAnalysisService analysisService,
    ILogger<IndexModel> logger) : PageModel
{
    private readonly MarketDataOptions marketData = configuredMarketData.Value;

    [BindProperty]
    public string[] SelectedAssetIds { get; set; } = [];

    public IReadOnlyList<WatchlistRowView> Rows { get; private set; } = [];
    public WatchlistMoversView Movers { get; private set; } = new([], []);
    public string? Message { get; private set; }
    public bool IsError { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!EnsureAuthenticated())
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = "/watchlist" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostRemoveAsync(
        string assetId,
        CancellationToken cancellationToken)
    {
        if (!EnsureAuthenticated())
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = "/watchlist" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        var result = await watchlistStore.RemoveAsync(
            GetSubjectId(),
            assetId,
            cancellationToken);
        if (result.Status == WatchlistRemoveStatus.StorageFailure)
        {
            Message = Lang.T("Nie udało się usunąć aktywa. Spróbuj ponownie później.");
            IsError = true;
        }
        else if (result.Status == WatchlistRemoveStatus.NotFound)
        {
            Message = Lang.T("Aktywo nie znajduje się już na liście obserwowanych.");
        }
        else if (result.Status == WatchlistRemoveStatus.Unavailable)
        {
            Message = Lang.T("To aktywo nie jest już dostępne, ale można je usunąć z listy.");
        }
        else
        {
            Message = Lang.T("Aktywo usunięto z listy obserwowanych.");
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await watchlistStore.ListAsync(GetSubjectId(), cancellationToken);
        if (!result.Succeeded)
        {
            Rows = [];
            Message ??= Lang.T("Nie udało się pobrać listy obserwowanych aktywów.");
            IsError = true;
            return;
        }

        var entries = WatchlistPolicy.OrderEntries(result.Entries).ToArray();
        var snapshots = await LoadSnapshotsAsync(
            entries.Where(entry => marketData.AllowedAssets.ContainsKey(entry.AssetId)),
            cancellationToken);

        Rows = entries.Select(entry =>
        {
            var isAvailable = marketData.AllowedAssets.TryGetValue(entry.AssetId, out var assetClass);
            var identity = AssetIdentityCatalog.For(entry.AssetId);
            snapshots.TryGetValue(entry.AssetId, out var snapshot);
            return WatchlistTablePresenter.BuildRow(
                entry.AssetId,
                identity.Name,
                isAvailable ? assetClass.ToString() : "Niedostępne",
                entry.SavedAtUtc,
                isAvailable,
                snapshot);
        }).ToArray();
        Movers = WatchlistTablePresenter.BuildMovers(Rows);
    }

    private async Task<IReadOnlyDictionary<string, WatchlistMarketSnapshot>> LoadSnapshotsAsync(
        IEnumerable<WatchlistEntry> availableEntries,
        CancellationToken cancellationToken)
    {
        var snapshots = new Dictionary<string, WatchlistMarketSnapshot>(StringComparer.OrdinalIgnoreCase);
        var assets = availableEntries
            .Select(entry => new AssetReference(
                entry.AssetId,
                entry.AssetId,
                marketData.AllowedAssets[entry.AssetId]))
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
            var analysis = analysisService.Analyze(
                response.Response,
                DateTimeOffset.UtcNow);
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
            // Market data is decoration here; the saved list must still render.
            logger.LogWarning(exception, "Market data for the watchlist table failed.");
            snapshots.Clear();
        }

        return snapshots;
    }

    private static decimal? Available(PerformanceMetric metric) =>
        metric.IsAvailable ? metric.Percentage : null;
    private bool EnsureAuthenticated() => User.Identity?.IsAuthenticated ?? false;

    private string GetSubjectId() =>
        User.FindFirstValue("sub") ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        throw new InvalidOperationException("Authenticated subject unavailable.");
}
