using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using _bootstrap_scaffold.Account;

namespace _bootstrap_scaffold.Pages.Account;

[AllowAnonymous]
public sealed class IndexModel : PageModel
{
    public string? SubjectId { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Email { get; private set; }
    public string Initials { get; private set; } = AccountProfilePresenter.FallbackInitials;

    public IActionResult OnGet()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return Challenge(
                new AuthenticationProperties { RedirectUri = "/account" },
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        SubjectId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        DisplayName = User.FindFirstValue("name") ?? User.Identity?.Name;
        Email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        Initials = AccountProfilePresenter.Initials(DisplayName, Email);
        return Page();
    }
}
