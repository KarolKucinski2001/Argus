using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Pages.Analysis;

[AllowAnonymous]
public sealed class IndexModel(
    IOptions<MarketDataOptions> configuredOptions,
    IMarketDataService marketDataService,
    IAnalysisService analysisService) : PageModel
{
    private readonly MarketDataOptions options = configuredOptions.Value;

    [BindProperty]
    public string[] SelectedAssetIds { get; set; } = [];

    public IReadOnlyList<SelectableAsset> Assets { get; private set; } = [];
    public int MaximumSelectedAssets => options.MaximumSelectedAssets;
    public bool HasStarted { get; private set; }
    public bool HasResults => AnalysisResults is not null;
    public bool HasPartialResults => AnalysisResults?.Results.Any(result => result.Error is not null) == true &&
                                     AnalysisResults.Results.Any(result => result.Error is null) == true;
    public string? StartedMessage { get; private set; }
    public string? ProviderMessage { get; private set; }
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
        SelectedAssetIds = options.AllowedAssets.Keys
            .Where(id => DefaultAssetIds.Contains(id, StringComparer.OrdinalIgnoreCase))
            .Take(options.MaximumSelectedAssets)
            .ToArray();
        if (SelectedAssetIds.Length > 0)
        {
            await RunAnalysisAsync(cancellationToken);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!EnsureAuthenticated())
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = "/analysis" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        LoadAssets();

        var validationError = ValidateSelection();
        if (validationError is not null)
        {
            ModelState.AddModelError(nameof(SelectedAssetIds), validationError);
            return Page();
        }

        await RunAnalysisAsync(cancellationToken);
        return Page();
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

    private string? ValidateSelection()
    {
        if (SelectedAssetIds.Length == 0)
        {
            return "Wybierz co najmniej jedno aktywo.";
        }

        if (SelectedAssetIds.Length > options.MaximumSelectedAssets)
        {
            return $"Wybierz maksymalnie {options.MaximumSelectedAssets} aktyw.";
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
