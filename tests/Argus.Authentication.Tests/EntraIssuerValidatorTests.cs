using Xunit;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using _bootstrap_scaffold.Account;

namespace Argus.Authentication.Tests;

public sealed class EntraIssuerValidatorTests
{
    private const string TenantId = "11111111-2222-3333-4444-555555555555";

    [Fact]
    public void Accepts_the_v2_issuer_of_the_tenant_in_the_token() =>
        Assert.Equal(
            $"https://login.microsoftonline.com/{TenantId}/v2.0",
            EntraIssuerValidator.ValidateAnyTenant(
                $"https://login.microsoftonline.com/{TenantId}/v2.0",
                Token(TenantId)));

    [Theory]
    [InlineData("https://login.microsoftonline.com/99999999-2222-3333-4444-555555555555/v2.0", TenantId)]
    [InlineData("https://evil.example/" + TenantId + "/v2.0", TenantId)]
    [InlineData("https://login.microsoftonline.com/not-a-guid/v2.0", "not-a-guid")]
    [InlineData("https://login.microsoftonline.com/" + TenantId + "/v2.0", null)]
    public void Rejects_issuers_that_do_not_match_the_token_tenant(string issuer, string? tid) =>
        Assert.Throws<SecurityTokenInvalidIssuerException>(() =>
            EntraIssuerValidator.ValidateAnyTenant(issuer, Token(tid)));

    private static JsonWebToken Token(string? tid)
    {
        var claims = new Dictionary<string, object> { ["sub"] = "user" };
        if (tid is not null)
        {
            claims["tid"] = tid;
        }

        var handler = new JsonWebTokenHandler();
        return new JsonWebToken(handler.CreateToken(new SecurityTokenDescriptor { Claims = claims }));
    }
}
