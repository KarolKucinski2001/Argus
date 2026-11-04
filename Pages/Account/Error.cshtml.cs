using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _bootstrap_scaffold.Pages.Account;

[AllowAnonymous]
public sealed class ErrorModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    public string Message { get; private set; } = "Nie udało się uzyskać dostępu do konta. Spróbuj ponownie.";

    public void OnGet()
    {
        Message = Code switch
        {
            "access-denied" => "Odmówiono dostępu. Możesz spróbować ponownie lub wrócić do Argus.",
            "cancelled" => "Logowanie zostało anulowane. Możesz spróbować ponownie lub wrócić do Argus.",
            _ => "Nie udało się uzyskać dostępu do konta. Spróbuj ponownie."
        };
    }
}
