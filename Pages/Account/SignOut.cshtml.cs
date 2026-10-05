using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _bootstrap_scaffold.Pages.Account;

[AllowAnonymous]
public sealed class SignOutModel : PageModel
{
    public IActionResult OnGet(string? returnUrl)
    {
        var destination = ReturnPath.LocalOrDefault(Url, returnUrl, "/");
        return SignOut(
            new AuthenticationProperties { RedirectUri = destination },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
