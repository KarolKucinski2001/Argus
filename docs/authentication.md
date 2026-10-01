# Authentication configuration

Argus uses Microsoft Entra External ID through OpenID Connect. The API keeps the
provider session in a secure, HTTP-only cookie and exposes the protected
`GET /api/me` route. Registration, login UX, and account persistence are
outside this foundation.

## Required settings

Configure these values through environment variables or an equivalent
configuration provider. ASP.NET Core maps the double underscore form to nested
configuration keys:

| Environment variable | Configuration key | Secret? | Purpose |
| --- | --- | --- | --- |
| `Entra__Authority` | `Entra:Authority` | No | OpenID Connect authority, including the tenant/policy path |
| `Entra__TenantId` | `Entra:TenantId` | No | External ID tenant identifier |
| `Entra__ClientId` | `Entra:ClientId` | No | Application (client) ID |
| `Entra__ClientSecret` | `Entra:ClientSecret` | Yes | Confidential-client secret |
| `Entra__CallbackPath` | `Entra:CallbackPath` | No | Relative callback path; use `/signin-oidc` |

In non-Development environments all five values are required. Never commit a
client secret, signing key, tenant credential, or a real environment-specific
identifier. Development configuration may contain placeholders, but a real
local provider flow still requires the environment variables above.

## Local development

Register a web application in a non-production Entra External ID tenant and
add this redirect URI:

```text
https://localhost:7219/signin-oidc
```

The HTTP launch profile is available for local diagnostics, but the secure
cookie and OpenID Connect callback should be tested with the HTTPS profile.
Set the values before running the app, for example in PowerShell:

```powershell
$env:Entra__Authority = "https://<tenant-or-policy-authority>"
$env:Entra__TenantId = "<tenant-id>"
$env:Entra__ClientId = "<client-id>"
$env:Entra__ClientSecret = "<secret>"
$env:Entra__CallbackPath = "/signin-oidc"
dotnet run --project .bootstrap-scaffold.csproj --launch-profile https
```

The callback URL is the public application origin plus
`Entra:CallbackPath`. Do not place the secret in `appsettings*.json`.

## Azure App Service

In the App Service portal, open **Configuration → Application settings** and
add the same five names (`Entra__Authority`, `Entra__TenantId`,
`Entra__ClientId`, `Entra__ClientSecret`, and `Entra__CallbackPath`). Mark the
secret setting for restricted access according to the deployment policy, then
save and restart the app. The deployed redirect URI must be:

```text
https://<app-service-hostname>/signin-oidc
```

Add that exact URI to the Entra application registration. For a custom domain,
use the custom HTTPS origin instead. The existing workflow in
`.github/workflows/deploy.yml` builds and publishes the app and deploys it to
App Service; it does not provision identity settings or require a live Entra
tenant during CI.

## Deterministic tests

Run the contract tests without network access or production credentials:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj
```

The test host replaces the external handler with a deterministic principal and
also verifies that missing required settings fail during production startup.
