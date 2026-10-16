using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Pages.Analysis;

[AllowAnonymous]
public sealed class IndexModel(
    IOptions<MarketDataOptions> configuredOptions,
    IMarketDataService marketDataService) : PageModel
{
    private readonly MarketDataOptions options = configuredOptions.Value;

    [BindProperty]
    public string[] SelectedAssetIds { get; set; } = [];

    public IReadOnlyList<SelectableAsset> Assets { get; private set; } = [];
    public int MaximumSelectedAssets => options.MaximumSelectedAssets;
    public bool HasStarted { get; private set; }
    public string? StartedMessage { get; private set; }
    public string? ProviderMessage { get; private set; }

    public IActionResult OnGet()
    {
        if (!EnsureAuthenticated())
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = "/analysis" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        LoadAssets();
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
            return Page();
        }

        var responseError = result.Response!.Results
            .Select(item => item.Error)
            .FirstOrDefault(error => error is not null);
        if (responseError is not null)
        {
            ProviderMessage = MapProviderMessage(responseError.Category);
            return Page();
        }

        HasStarted = true;
        StartedMessage = result.Response.Results.Any(item =>
            item.Source?.Attribution?.Contains("illustrative", StringComparison.OrdinalIgnoreCase) == true)
            ? "Analiza została rozpoczęta na podstawie ilustracyjnych danych."
            : "Analiza została rozpoczęta.";
        return Page();
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

    private static string MapProviderMessage(MarketDataErrorCategory? category) =>
        category switch
        {
            MarketDataErrorCategory.ProviderNotApproved or
            MarketDataErrorCategory.NotConfigured =>
                "Analiza jest chwilowo niedostępna. Spróbuj ponownie później.",
            MarketDataErrorCategory.Timeout =>
                "Pobieranie danych trwało zbyt długo. Spróbuj ponownie.",
            MarketDataErrorCategory.QuotaExceeded =>
                "Osiągnięto limit zapytań. Spróbuj ponownie później.",
            _ =>
                "Nie udało się pobrać danych do analizy. Spróbuj ponownie później."
        };
}

public sealed record SelectableAsset(string Id, string AssetClass);
