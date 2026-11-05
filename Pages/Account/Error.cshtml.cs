using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using _bootstrap_scaffold.Localization;

namespace _bootstrap_scaffold.Pages.Account;

[AllowAnonymous]
public sealed class ErrorModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    public string Message { get; private set; } = Lang.T("Nie udało się uzyskać dostępu do konta. Spróbuj ponownie.");

    public void OnGet()
    {
        Message = Lang.T(Code switch
        {
            "access-denied" => "Odmówiono dostępu. Możesz spróbować ponownie lub wrócić do Argus.",
            "cancelled" => "Logowanie zostało anulowane. Możesz spróbować ponownie lub wrócić do Argus.",
            _ => "Nie udało się uzyskać dostępu do konta. Spróbuj ponownie."
        });
    }
}
