using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using _bootstrap_scaffold.Strategies;

namespace _bootstrap_scaffold.Pages.Strategies.Profiles;

[AllowAnonymous]
public sealed class DetailModel(ISimulatedPortfolioStore store) : PageModel
{
    public ProfileDetailView? Profile { get; private set; }
    public string? Message { get; private set; }

    public IActionResult OnGet(string id)
    {
        if (!IsAuthenticated())
        {
            return ChallengeTo(id);
        }

        return Load(id) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostCopyAsync(string id, CancellationToken cancellationToken)
    {
        if (!IsAuthenticated())
        {
            return ChallengeTo(id);
        }

        var curated = CuratedProfileCatalog.Find(id);
        if (curated is null)
        {
            return NotFound();
        }

        var copy = StrategyProfilePresenter.CreateCopy(
            curated,
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow);
        var status = await store.CreateAsync(GetSubjectId(), copy, cancellationToken);
        if (status == SimulatedPortfolioStatus.Succeeded)
        {
            TempData["PortfolioMessage"] = "Skopiowano profil do symulowanego portfela.";
            return RedirectToPage("/Strategies/Portfolios/Index");
        }

        Load(id);
        Message = status == SimulatedPortfolioStatus.CapacityExceeded
            ? $"Osiągnięto limit {SimulatedPortfolioConstants.MaximumPortfolios} portfeli symulowanych. Usuń jeden, aby dodać kolejny."
            : "Nie udało się zapisać portfela symulowanego. Spróbuj ponownie później.";
        return Page();
    }

    private bool Load(string id)
    {
        var curated = CuratedProfileCatalog.Find(id);
        Profile = curated is null ? null : StrategyProfilePresenter.BuildDetail(curated);
        return Profile is not null;
    }

    private bool IsAuthenticated() => User.Identity?.IsAuthenticated ?? false;

    private ChallengeResult ChallengeTo(string id) => Challenge(
        new AuthenticationProperties { RedirectUri = "/strategies/profiles/" + Uri.EscapeDataString(id) },
        OpenIdConnectDefaults.AuthenticationScheme);

    private string GetSubjectId() =>
        User.FindFirstValue("sub") ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        throw new InvalidOperationException("Authenticated subject unavailable.");
}
