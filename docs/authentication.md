# Authentication and account access

Argus uses Microsoft Entra ID (workforce or External ID tenant) through OpenID Connect. The browser
account flow uses the provider for both sign-in and account creation, then
stores the authenticated session in a secure, HTTP-only cookie. Argus does not
persist local profiles, passwords, tokens, or provider credentials.

## Required settings

Configure these values through user secrets, environment variables, or Azure
App Service application settings. ASP.NET Core maps the double-underscore form
to nested configuration keys:

| Environment variable | Configuration key | Secret? | Purpose |
| --- | --- | --- | --- |
| `Entra__Authority` | `Entra:Authority` | No | OpenID Connect authority, including the tenant/policy path |
| `Entra__TenantId` | `Entra:TenantId` | No | Home tenant identifier of the app registration |
| `Entra__AllowAnyTenant` | `Entra:AllowAnyTenant` | No | Optional, default `false`; set `true` with the `common` authority (see *Who can sign in*) |
| `Entra__ClientId` | `Entra:ClientId` | No | Application (client) ID |
| `Entra__ClientSecret` | `Entra:ClientSecret` | Yes | Confidential-client secret |
| `Entra__CallbackPath` | `Entra:CallbackPath` | No | Relative callback path; use `/signin-oidc` |

Outside Development, all five values are required at startup. Keep secrets
and tenant-specific values out of tracked files. Never commit a client secret,
signing key, tenant credential, authorization code, access token, or refresh
token.

## Browser contract

The public root route `/` explains Argus's educational purpose and provides one
unified **Zaloguj się lub utwórz konto** action at `/account/signin`. The action
starts an explicit OpenID Connect challenge; it does not change the API's
default challenge behavior.

After a successful provider callback at `/signin-oidc`, the user is sent to
`/account`. The authenticated shell exposes `/analysis` as the primary
destination and `/account` as the account surface. The account page displays
available subject, display-name, and email claims and links to analysis. It
does not create or update a local profile. `/account` also explicitly starts
the provider challenge when no local session exists.

The account logout action at `/account/signout` signs out both the Argus cookie
and the Entra provider session, then returns to `/`. A local return path may be
provided for browser navigation, but external absolute URLs and protocol-relative
URLs are rejected and replaced with the safe default. Do not use return paths
from untrusted provider or client data without this local-path validation.

Provider cancellation, access denial, and remote callback failures are routed
to `/account/error` with a bounded, friendly message. Raw provider exceptions,
query-string details, authorization codes, tokens, and credentials are not
rendered.

## Who can sign in and what they can access

Authorization has no roles or policies: every page and endpoint only calls
`RequireAuthorization()`, so **every signed-in user can use every feature**.
Watchlists and simulated portfolios are scoped to the user's `sub` claim, so
users never see each other's data.

Who can sign in is decided by the Entra app registration, not by Argus code:

- **Single tenant (current setup)** — `Entra:Authority` is
  `https://login.microsoftonline.com/<tenant-id>/v2.0`. Only members of that
  tenant and invited guests can sign in.
- **Any Microsoft account** — in the Azure portal set the app registration's
  *Supported account types* to *Accounts in any organizational directory and
  personal Microsoft accounts*, then set
  `Entra:Authority` = `https://login.microsoftonline.com/common/v2.0` and
  `Entra:AllowAnyTenant` = `true`. `AllowAnyTenant` replaces the default
  issuer check (which cannot match `common`) with one that accepts only the
  v2.0 issuer of the tenant named in the token's `tid` claim.
- **Self-service sign-up with any email** — use an Entra External ID tenant with
  a sign-up user flow and point `Entra:Authority` at its `ciamlogin.com`
  authority; `AllowAnyTenant` stays `false`.
## Local development

Register a web application in a non-production Entra tenant and add
this exact redirect URI:

```text
https://localhost:7219/signin-oidc
```

Use the HTTPS launch profile. The HTTP profile is useful for diagnostics, but
the secure cookie and OpenID Connect callback require HTTPS.

### Preferred: user secrets

Store values in the per-user secret store, outside the repository:

```powershell
dotnet user-secrets set "Entra:Authority" "https://<tenant-or-policy-authority>"
dotnet user-secrets set "Entra:TenantId" "<tenant-id>"
dotnet user-secrets set "Entra:ClientId" "<client-id>"
dotnet user-secrets set "Entra:CallbackPath" "/signin-oidc"
dotnet user-secrets set "Entra:ClientSecret" "<secret>"
dotnet run --project .bootstrap-scaffold.csproj --launch-profile https
```

Use the secret **Value** shown at creation time, never the Secret ID. Replace
the placeholders locally; do not edit tracked configuration files.

If Windows blocks the generated `.bootstrap-scaffold.exe` with `Access is
denied`, start the compiled assembly through `dotnet` instead:

```powershell
dotnet build .bootstrap-scaffold.csproj
dotnet .\bin\Debug\net10.0\.bootstrap-scaffold.dll `
  --environment Development --urls "https://localhost:7219"
```

### Alternative: environment variables

```powershell
$env:Entra__Authority = "https://<tenant-or-policy-authority>"
$env:Entra__TenantId = "<tenant-id>"
$env:Entra__ClientId = "<client-id>"
$env:Entra__ClientSecret = "<secret>"
$env:Entra__CallbackPath = "/signin-oidc"
dotnet run --project .bootstrap-scaffold.csproj --launch-profile https
```

The callback URL is the public HTTPS origin plus `Entra:CallbackPath`.

### Local smoke test

1. Open `https://localhost:7219/`.
2. Choose **Sign in or create an account**.
3. Complete the provider flow and verify that `/account` displays the claims
   available from the provider.
4. Choose logout and confirm the browser returns to `/`.
5. Request `/api/me` after logout. It must return `401` with
   `application/problem+json` and no `Location` header.
6. Repeat the flow with provider cancellation or denial and confirm the
   friendly `/account/error` page.

## API contract

`GET /api/me` remains an API endpoint, not a browser redirect. Without an
authenticated session it returns:

- HTTP `401 Unauthorized`
- `Content-Type: application/problem+json`
- no `Location` header

The response title is `Authentication required`. Keep the cookie scheme as the
default API challenge scheme; browser pages must explicitly name the OpenID
Connect scheme.

## Azure App Service

In **Configuration → Application settings**, add the same five names:
`Entra__Authority`, `Entra__TenantId`, `Entra__ClientId`,
`Entra__ClientSecret`, and `Entra__CallbackPath`. Keep the secret restricted
according to the deployment policy, then save and restart the app. Do not put
the secret in workflow files or shared scripts. Watchlist storage settings are
documented in `docs/watchlist.md`.

For the default App Service hostname, register:

```text
https://<app-service-hostname>/signin-oidc
```

For a custom domain, register the exact public HTTPS origin plus
`/signin-oidc`. The existing deployment workflow builds and publishes
`.bootstrap-scaffold.csproj` and deploys the package; it does not provision
Entra settings or require a live tenant in CI.

## Deterministic contract tests

Run the offline authentication contract tests without production credentials
or network access:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore
```

The test host supplies deterministic claims and authentication services. It
also verifies production startup validation and the unchanged `/api/me`
ProblemDetails contract. No live-provider or browser-E2E dependency is needed.
