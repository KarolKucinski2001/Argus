using _bootstrap_scaffold.Account;
using Xunit;

namespace Argus.Authentication.Tests;

public sealed class AccountProfilePresenterTests
{
    [Theory]
    [InlineData("Test User", null, "TU")]
    [InlineData("  jan   maria kowalski ", "x@example.test", "JM")]
    [InlineData("madonna", null, "M")]
    [InlineData(null, "test@example.test", "T")]
    [InlineData("", "anna.nowak@example.test", "AN")]
    [InlineData("   ", "  ", "?")]
    [InlineData(null, null, "?")]
    [InlineData("", "@example.test", "?")]
    public void Initials_derive_from_name_then_email_then_fallback(
        string? displayName, string? email, string expected)
    {
        Assert.Equal(expected, AccountProfilePresenter.Initials(displayName, email));
    }
}
