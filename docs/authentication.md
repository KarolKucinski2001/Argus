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

### Preferred: user secrets

Store the values once in the per-user secret store, outside the repository.
They persist across shells and reboots, and `dotnet user-secrets` cannot write
into tracked files:

```powershell
dotnet user-secrets set "Entra:Authority" "https://<tenant-or-policy-authority>"
dotnet user-secrets set "Entra:TenantId" "<tenant-id>"
dotnet user-secrets set "Entra:ClientId" "<client-id>"
dotnet user-secrets set "Entra:CallbackPath" "/signin-oidc"
dotnet user-secrets set "Entra:ClientSecret" "<secret>"
```

Use the secret **Value** shown once at creation time, never the Secret ID.

User secrets load only in the Development environment, which keeps local
credentials from reaching deployed environments. When launching the built
assembly directly, pass the environment explicitly — otherwise the host
defaults to Production, ignores user secrets, and fails startup validation:

```powershell
dotnet build .bootstrap-scaffold.csproj
dotnet .\bin\Debug\net10.0\.bootstrap-scaffold.dll --environment Development --urls "https://localhost:7219"
```

On machines where policy blocks running the generated `.exe` apphost,
launching the `.dll` through `dotnet` as shown above is the supported
workaround.

### Alternative: environment variables

Environment variables also work, but they live only in the shell that set
them:

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

### Expected local result

This foundation registers no sign-in endpoint, so a browser login cannot be
exercised yet; that belongs to the account-access slice. A correct local smoke
test is:

| Route | Expected |
| --- | --- |
| `/api/me` | `401` with `application/problem+json` and no `Location` header |
| `/openapi/v1.json` | `200` |
| `/` | `404` — no route is mapped at the root |

A `302` to the identity provider on `/api/me` is a defect: API callers must
receive `401` rather than a redirect.

## Azure App Service

In the App Service portal, open **Configuration → Application settings** and
add the same five names (`Entra__Authority`, `Entra__TenantId`,
`Entra__ClientId`, `Entra__ClientSecret`, and `Entra__CallbackPath`). Mark the
secret setting for restricted access according to the deployment policy, then
save and restart the app. The equivalent CLI form is:

```powershell
az webapp config appsettings set --name <app-name> --resource-group <rg> `
  --settings Entra__Authority="<authority>" Entra__TenantId="<tenant-id>" `
             Entra__ClientId="<client-id>" Entra__CallbackPath="/signin-oidc"
```

Set `Entra__ClientSecret` separately so the value stays out of shared scripts
and shell history. The deployed redirect URI must be:

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
