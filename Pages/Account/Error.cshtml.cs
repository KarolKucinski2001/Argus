using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _bootstrap_scaffold.Pages.Account;

[AllowAnonymous]
public sealed class ErrorModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    public string Message { get; private set; } = "We could not complete account access. Please try again.";

    public void OnGet()
    {
        Message = Code switch
        {
            "access-denied" => "Access was denied. You can try again or return to Argus.",
            "cancelled" => "Account access was cancelled. You can try again or return to Argus.",
            _ => "We could not complete account access. Please try again."
        };
    }
}
