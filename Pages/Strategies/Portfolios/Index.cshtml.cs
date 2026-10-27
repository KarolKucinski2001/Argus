using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using _bootstrap_scaffold.Strategies;

namespace _bootstrap_scaffold.Pages.Strategies.Portfolios;

[AllowAnonymous]
public sealed class IndexModel(ISimulatedPortfolioStore store) : PageModel
{
    public IReadOnlyList<SimulatedPortfolioRow> Portfolios { get; private set; } = [];
    public string? Message { get; private set; }
    public bool IsError { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!IsAuthenticated())
        {
            return ChallengeToList();
        }

        Message = TempData["PortfolioMessage"] as string;
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (!IsAuthenticated())
        {
            return ChallengeToList();
        }

        var status = await store.DeleteAsync(GetSubjectId(), id, cancellationToken);
        switch (status)
        {
            case SimulatedPortfolioStatus.Succeeded:
                Message = "Portfel symulowany usunięto.";
                break;
            case SimulatedPortfolioStatus.NotFound:
                Message = "Ten portfel symulowany nie istnieje już na Twojej liście.";
                break;
            default:
                Message = "Nie udało się usunąć portfela. Spróbuj ponownie później.";
                IsError = true;
                break;
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await store.ListAsync(GetSubjectId(), cancellationToken);
        if (!result.Succeeded)
        {
            Portfolios = [];
            Message ??= "Nie udało się pobrać portfeli symulowanych.";
            IsError = true;
            return;
        }

        Portfolios = result.Portfolios.Select(StrategyProfilePresenter.BuildPortfolioRow).ToArray();
    }

    private bool IsAuthenticated() => User.Identity?.IsAuthenticated ?? false;

    private ChallengeResult ChallengeToList() => Challenge(
        new AuthenticationProperties { RedirectUri = "/strategies/portfolios" },
        OpenIdConnectDefaults.AuthenticationScheme);

    private string GetSubjectId() =>
        User.FindFirstValue("sub") ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        throw new InvalidOperationException("Authenticated subject unavailable.");
}
