using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;
using _bootstrap_scaffold.Watchlist;

namespace _bootstrap_scaffold.Pages.Watchlist;

[AllowAnonymous]
public sealed class IndexModel(
    IOptions<MarketDataOptions> configuredMarketData,
    IWatchlistStore watchlistStore) : PageModel
{
    private readonly MarketDataOptions marketData = configuredMarketData.Value;

    [BindProperty]
    public string[] SelectedAssetIds { get; set; } = [];

    public IReadOnlyList<WatchlistRow> Rows { get; private set; } = [];
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
            Message = "Nie udało się usunąć aktywa. Spróbuj ponownie później.";
            IsError = true;
        }
        else if (result.Status == WatchlistRemoveStatus.NotFound)
        {
            Message = "Aktywo nie znajduje się już na liście obserwowanych.";
        }
        else if (result.Status == WatchlistRemoveStatus.Unavailable)
        {
            Message = "To aktywo nie jest już dostępne, ale można je usunąć z listy.";
        }
        else
        {
            Message = "Aktywo usunięto z listy obserwowanych.";
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
            Message ??= "Nie udało się pobrać listy obserwowanych aktywów.";
            IsError = true;
            return;
        }

        Rows = WatchlistPolicy.OrderEntries(result.Entries).Select(entry =>
        {
            var isAvailable = marketData.AllowedAssets.TryGetValue(entry.AssetId, out var assetClass);
            var identity = AssetIdentityCatalog.For(entry.AssetId);
            return new WatchlistRow(
                entry.AssetId,
                identity.Name,
                isAvailable ? assetClass.ToString() : "Niedostępne",
                entry.SavedAtUtc,
                isAvailable);
        }).ToArray();
    }

    private bool EnsureAuthenticated() => User.Identity?.IsAuthenticated ?? false;

    private string GetSubjectId() =>
        User.FindFirstValue("sub") ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        throw new InvalidOperationException("Authenticated subject unavailable.");
}

public sealed record WatchlistRow(
    string AssetId,
    string Name,
    string AssetClass,
    DateTimeOffset SavedAtUtc,
    bool IsAvailable);
