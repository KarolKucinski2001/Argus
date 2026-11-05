using _bootstrap_scaffold.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _bootstrap_scaffold.Pages.Preferences;

public sealed class IndexModel : PageModel
{
    public IActionResult OnGet() => Redirect("/");

    public IActionResult OnPostTheme(string? theme, string? returnUrl)
    {
        Response.Cookies.Append(Theme.CookieName, Theme.Normalize(theme), CookieOptions());
        return Back(returnUrl);
    }

    public IActionResult OnPostLanguage(string? language, string? returnUrl)
    {
        Response.Cookies.Append(Lang.CookieName, Lang.Code(Lang.Parse(language)), CookieOptions());
        return Back(returnUrl);
    }

    private IActionResult Back(string? returnUrl) =>
        LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");

    private CookieOptions CookieOptions() => new()
    {
        Expires = DateTimeOffset.UtcNow.AddYears(1),
        HttpOnly = true,
        Secure = Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        IsEssential = true
    };
}