using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _bootstrap_scaffold.Pages.Strategies;

[AllowAnonymous]
public sealed class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = "/strategies" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        return Page();
    }
}
