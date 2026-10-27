using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using _bootstrap_scaffold.Strategies;

namespace _bootstrap_scaffold.Pages.Strategies.Profiles;

[AllowAnonymous]
public sealed class IndexModel : PageModel
{
    public IReadOnlyList<ProfileCardView> Cards { get; private set; } = [];

    public IActionResult OnGet()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = "/strategies/profiles" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        Cards = CuratedProfileCatalog.Profiles.Select(StrategyProfilePresenter.BuildCard).ToArray();
        return Page();
    }
}
