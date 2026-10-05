using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _bootstrap_scaffold.Pages.Account;

[AllowAnonymous]
public sealed class SignInModel : PageModel
{
    public IActionResult OnGet(string? returnUrl)
    {
        var destination = ReturnPath.LocalOrDefault(Url, returnUrl, "/account");
        return Challenge(
            new AuthenticationProperties { RedirectUri = destination },
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
