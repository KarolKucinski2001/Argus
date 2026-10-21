using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _bootstrap_scaffold.Pages.Analysis;

[AllowAnonymous]
public sealed class UiStateFixtureModel(IWebHostEnvironment environment) : PageModel
{
    public IActionResult OnGet() =>
        string.Equals(environment.EnvironmentName, "Testing", StringComparison.Ordinal)
            ? Page()
            : NotFound();
}
