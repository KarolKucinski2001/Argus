using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using _bootstrap_scaffold.Localization;
using _bootstrap_scaffold.MarketData;
using _bootstrap_scaffold.Strategies;

namespace _bootstrap_scaffold.Pages.Strategies.Portfolios;

[AllowAnonymous]
public sealed class AnalysisModel(
    ISimulatedPortfolioStore store,
    IOptions<MarketDataOptions> marketDataOptions) : PageModel
{
    public SimulatedPortfolioAnalysisView? Analysis { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        if (!IsAuthenticated())
        {
            return ChallengeTo(id);
        }

        if (!SimulatedPortfolioConstants.IsValidId(id))
        {
            return NotFound();
        }

        var result = await store.ListAsync(GetSubjectId(), cancellationToken);
        if (!result.Succeeded)
        {
            ErrorMessage = Lang.T("Nie udało się pobrać portfela symulowanego. Spróbuj ponownie później.");
            return Page();
        }

        var portfolio = result.Portfolios.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal));
        if (portfolio is null)
        {
            return NotFound();
        }

        var options = marketDataOptions.Value;
        Analysis = SimulatedPortfolioAnalysisPresenter.Build(
            portfolio,
            options.AllowedAssets.Keys);
        return Page();
    }

    private bool IsAuthenticated() => User.Identity?.IsAuthenticated ?? false;

    private ChallengeResult ChallengeTo(string id) => Challenge(
        new AuthenticationProperties { RedirectUri = $"/strategies/portfolios/{Uri.EscapeDataString(id)}/analysis" },
        OpenIdConnectDefaults.AuthenticationScheme);

    private string GetSubjectId() =>
        User.FindFirstValue("sub") ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        throw new InvalidOperationException("Authenticated subject unavailable.");
}
