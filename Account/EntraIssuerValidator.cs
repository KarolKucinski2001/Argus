using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace _bootstrap_scaffold.Account;

// With the multi-tenant "common" authority the discovery issuer contains a "{tenantid}"
// placeholder, so the default issuer check rejects every token. Accept only the
// Microsoft identity platform v2.0 issuer of the tenant named in the token's own "tid".
public static class EntraIssuerValidator
{
    private const string IssuerPrefix = "https://login.microsoftonline.com/";
    private const string IssuerSuffix = "/v2.0";

    public static string ValidateAnyTenant(string issuer, SecurityToken token)
    {
        var tenantId = token is JsonWebToken jwt && jwt.TryGetPayloadValue<string>("tid", out var tid)
            ? tid
            : null;

        if (Guid.TryParse(tenantId, out _) &&
            string.Equals(issuer, IssuerPrefix + tenantId + IssuerSuffix, StringComparison.Ordinal))
        {
            return issuer;
        }

        throw new SecurityTokenInvalidIssuerException("The token issuer does not match its tenant.");
    }
}
