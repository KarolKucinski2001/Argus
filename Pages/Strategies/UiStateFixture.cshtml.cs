using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using _bootstrap_scaffold.Strategies;

namespace _bootstrap_scaffold.Pages.Strategies;

[AllowAnonymous]
public sealed class UiStateFixtureModel(IWebHostEnvironment environment) : PageModel
{
    public IReadOnlyList<StrategyProfileView> Profiles { get; } = StrategyProfileCatalog.Profiles;

    public IActionResult OnGet() =>
        string.Equals(environment.EnvironmentName, "Testing", StringComparison.Ordinal)
            ? Page()
            : NotFound();
}
