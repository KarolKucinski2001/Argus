using Microsoft.AspNetCore.Mvc;

namespace _bootstrap_scaffold.Pages.Account;

internal static class ReturnPath
{
    public static string LocalOrDefault(IUrlHelper url, string? candidate, string fallback)
    {
        return !string.IsNullOrWhiteSpace(candidate) && url.IsLocalUrl(candidate)
            ? candidate
            : fallback;
    }
}
