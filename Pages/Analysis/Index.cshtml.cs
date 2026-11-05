using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;
using _bootstrap_scaffold.Watchlist;

namespace _bootstrap_scaffold.Pages.Analysis;

[AllowAnonymous]
public sealed class IndexModel(
    IOptions<MarketDataOptions> configuredOptions,
    IMarketDataService marketDataService,
    IAnalysisService analysisService,
    IWatchlistStore watchlistStore,
    ILogger<IndexModel> logger) : PageModel
{
    private readonly MarketDataOptions options = configuredOptions.Value;

    [BindProperty]
    public string[] SelectedAssetIds { get; set; } = [];

    public IReadOnlyList<SelectableAsset> Assets { get; private set; } = [];
    public bool HasStarted { get; private set; }
    public bool HasResults => AnalysisResults is not null;
    public bool HasPartialResults => AnalysisResults?.Results.Any(result => result.Error is not null) == true &&
                                     AnalysisResults.Results.Any(result => result.Error is null) == true;
    public string? StartedMessage { get; private set; }
    public string? ProviderMessage { get; private set; }
    public string? WatchlistMessage { get; private set; }
    public bool WatchlistSaveSucceeded { get; private set; }
    public bool WatchlistSaveAttempted { get; private set; }
    public IReadOnlySet<string> SavedAssetIds { get; private set; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public AnalysisResponse? AnalysisResults { get; private set; }
    public IReadOnlyDictionary<string, MarketQuote> CurrentQuotes { get; private set; } =
        new Dictionary<string, MarketQuote>(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] DefaultAssetIds = ["AAPL", "MSFT", "BTC-USD", "ETH-USD"];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!EnsureAuthenticated())
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = "/analysis" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        LoadAssets();
        await LoadSavedAssetsAsync(cancellationToken);
        var defaults = DefaultAssetIds;
        if (TempData["WatchlistMessage"] is string savedMessage)
        {
            WatchlistMessage = savedMessage;
            WatchlistSaveAttempted = true;
            WatchlistSaveSucceeded = true;
            if (TempData["WatchlistSelection"] is string selection && selection.Length > 0)
            {
                defaults = selection.Split(',');
            }
        }

        SelectedAssetIds = options.AllowedAssets.Keys
            .Where(id => defaults.Contains(id, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        if (SelectedAssetIds.Length > 0)
        {
            try
            {
                await RunAnalysisAsync(cancellationToken);
                // Autorun is not a user action, so no "Analiza rozpoczęta" status is announced.
                HasStarted = false;
                StartedMessage = null;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The landing GET must always render the form, even if the automatic run fails.
                logger.LogError(exception, "Automatic analysis on GET /analysis failed.");
                AnalysisResults = null;
                CurrentQuotes = new Dictionary<string, MarketQuote>(StringComparer.OrdinalIgnoreCase);
                HasStarted = false;
                StartedMessage = null;
                ProviderMessage = MapProviderMessage(null);
            }
        }

        return Page();
    }

    public Task<IActionResult> OnPostAsync(CancellationToken cancellationToken) =>
        OnPostAnalyzeAsync(cancellationToken);

    public async Task<IActionResult> OnPostAnalyzeAsync(CancellationToken cancellationToken)
    {
        if (!EnsureAuthenticated())
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = "/analysis" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        LoadAssets();
        await LoadSavedAssetsAsync(cancellationToken);

        var validationError = ValidateSelection();
        if (validationError is not null)
        {
            ModelState.AddModelError(nameof(SelectedAssetIds), validationError);
            return Page();
        }

        await RunAnalysisAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        if (!EnsureAuthenticated())
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = "/analysis" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        LoadAssets();
        await LoadSavedAssetsAsync(cancellationToken);

        var validationError = ValidateSelection();
        if (validationError is not null)
        {
            ModelState.AddModelError(nameof(SelectedAssetIds), validationError);
            return Page();
        }

        WatchlistSaveAttempted = true;
        var result = await watchlistStore.SaveAsync(
            GetSubjectId(),
            SelectedAssetIds,
            cancellationToken);
        if (!result.Succeeded)
        {
            WatchlistMessage = result.HasStorageFailure
                ? "Nie udało się zapisać aktywów. Spróbuj ponownie później."
                : result.Items.Any(item => item.Status == WatchlistSaveStatus.CapacityExceeded)
                    ? $"Lista obserwowanych może zawierać maksymalnie {WatchlistConstants.MaximumEntries} aktywów."
                    : "Nie udało się zapisać wybranych aktywów.";
            return Page();
        }

        TempData["WatchlistMessage"] = "Wybrane aktywa zapisano na liście obserwowanych.";
        TempData["WatchlistSelection"] = string.Join(',', SelectedAssetIds);
        return RedirectToPage();
    }

    private async Task RunAnalysisAsync(CancellationToken cancellationToken)
    {
        var selectedAssets = SelectedAssetIds
            .Select(id => options.AllowedAssets.First(entry =>
                string.Equals(entry.Key, id, StringComparison.OrdinalIgnoreCase)))
            .Select(entry => new AssetReference(entry.Key, entry.Key, entry.Value))
            .ToArray();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new MarketDataRequest(
            selectedAssets,
            new MarketDataHistoryRequest(
                today.AddMonths(-MarketDataOptions.RequiredHistoryMonths),
                today,
                MarketDataGranularity.Daily));

        var result = await marketDataService.GetAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            ProviderMessage = MapProviderMessage(result.Error?.Category);
            return;
        }

        CurrentQuotes = result.Response!.Results
            .Where(item => item.CurrentQuote is not null)
            .ToDictionary(item => item.Asset.Id, item => item.CurrentQuote!, StringComparer.OrdinalIgnoreCase);
        AnalysisResults = await analysisService.AnalyzeAsync(
            result.Response,
            DateTimeOffset.UtcNow,
            cancellationToken);
        HasStarted = true;
        StartedMessage = AnalysisResults.Results.Any(item => item.Error is not null)
            ? "Częściowe wyniki są gotowe. Aktywa z brakującymi danymi pokazują bezpieczny stan."
            : result.Response.Results.Any(item =>
                item.Source?.Attribution?.Contains("illustrative", StringComparison.OrdinalIgnoreCase) == true)
                ? "Analiza została rozpoczęta na podstawie ilustracyjnych danych."
            : "Analiza została rozpoczęta.";
    }

    private void LoadAssets() =>
        Assets = options.AllowedAssets
            .Select(entry => new SelectableAsset(entry.Key, entry.Value.ToString()))
            .ToArray();

    private async Task LoadSavedAssetsAsync(CancellationToken cancellationToken)
    {
        var result = await watchlistStore.ListAsync(GetSubjectId(), cancellationToken);
        if (result.Succeeded)
        {
            SavedAssetIds = result.Entries
                .Select(entry => entry.AssetId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            WatchlistMessage = "Nie udało się pobrać listy obserwowanych aktywów.";
        }
    }

    private string GetSubjectId() =>
        User.FindFirstValue("sub") ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        throw new InvalidOperationException("Authenticated subject unavailable.");

    private string? ValidateSelection()
    {
        if (SelectedAssetIds.Length == 0)
        {
            return "Wybierz co najmniej jedno aktywo.";
        }

        if (SelectedAssetIds
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            return "Każde aktywo można wybrać tylko raz.";
        }

        if (SelectedAssetIds.Any(id => !options.AllowedAssets.Keys
            .Any(allowedId => string.Equals(allowedId, id, StringComparison.OrdinalIgnoreCase))))
        {
            return "Wybrano aktywo spoza dostępnej listy.";
        }

        return null;
    }

    private bool EnsureAuthenticated() =>
        User.Identity?.IsAuthenticated ?? false;

    public static string MapAssetErrorMessage(MarketDataErrorCategory? category) =>
        category switch
        {
            MarketDataErrorCategory.Timeout =>
                "Nie udało się przeanalizować tego aktywa. Pobieranie danych trwało zbyt długo. Spróbuj ponownie później.",
            MarketDataErrorCategory.QuotaExceeded =>
                "Nie udało się przeanalizować tego aktywa. Limit zapytań został osiągnięty. Spróbuj ponownie później.",
            MarketDataErrorCategory.UnsupportedAsset =>
                "Nie udało się przeanalizować tego aktywa. Aktywo nie jest teraz obsługiwane.",
            _ =>
                "Nie udało się przeanalizować tego aktywa. Spróbuj ponownie później."
        };

    public static string MapSummaryStatusMessage(AnalysisSummaryStatus status) =>
        status switch
        {
            AnalysisSummaryStatus.InsufficientEvidence =>
                "Niewystarczające dane do przygotowania podsumowania.",
            AnalysisSummaryStatus.Unavailable =>
                "Podsumowanie analizy jest obecnie niedostępne. Wynik, czynniki i dane pozostają dostępne.",
            _ =>
                "Podsumowanie analizy."
        };

    private static string MapProviderMessage(MarketDataErrorCategory? category) =>
        category switch
        {
            MarketDataErrorCategory.ProviderNotApproved or
            MarketDataErrorCategory.NotConfigured =>
                "Analiza jest obecnie niedostępna. Spróbuj ponownie później.",
            MarketDataErrorCategory.Timeout =>
                "Pobieranie danych trwało zbyt długo. Spróbuj ponownie.",
            MarketDataErrorCategory.QuotaExceeded =>
                "Osiągnięto limit zapytań. Spróbuj ponownie później.",
            _ =>
                "Nie udało się pobrać danych do analizy. Spróbuj ponownie później."
        };
}

public sealed record SelectableAsset(string Id, string AssetClass);
