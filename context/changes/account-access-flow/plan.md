# Account Access Flow Implementation Plan

## Overview

Add the first user-facing Argus entry and account flow on top of the existing Microsoft Entra External ID foundation. The application will expose a Razor Pages landing page with the educational-purpose boundary, start one unified sign-in-or-registration challenge, provide a protected account home after authentication, and support provider-backed logout without adding local password or profile persistence.

## Current State Analysis

Argus is a .NET 10 minimal API scaffold. `Program.cs:7-119` already registers cookie authentication, OpenID Connect, authorization middleware, and the protected `GET /api/me` route, but it has no root route, interactive challenge route, logout route, or browser UI. The cookie redirect override intentionally returns `401 application/problem+json`, so browser pages must issue explicit OpenID Connect challenges rather than relying on the default challenge.

The project has no Razor Pages files or UI dependencies (`.bootstrap-scaffold.csproj:1-21`). The current authentication handoff documents the provider configuration and deliberately states that `/` is `404` because login UX belongs to this slice (`docs/authentication.md:1-5, 78-99`). Existing tests use `WebApplicationFactory<Program>` with deterministic authentication handlers and no network or production secrets (`tests/Argus.Authentication.Tests/AuthenticationContractTests.cs:1-166`).

## Desired End State

An anonymous visitor can open `/`, understand that Argus provides educational market analysis rather than investment advice, and choose one clearly labeled action to sign in or create an account through Entra External ID. The browser flow validates local return paths, reaches a protected `/account` page after a successful callback, and provides logout that clears the local cookie and ends the provider session.

OIDC cancellation, denial, and remote failures reach a friendly account-error page without exposing raw provider details. API behavior remains unchanged: unauthenticated `/api/me` requests still return 401 ProblemDetails without a redirect, and no local user database, roles, trading behavior, market analysis, or AI functionality is introduced.

### Key Discoveries:

- `Program.cs:22-68` uses cookie authentication as the default challenge scheme specifically to keep API challenges at 401; interactive pages must name the OpenID Connect scheme explicitly.
- `Program.cs:70-79` already has the required authentication/authorization middleware order.
- `Program.cs:81-102` provides the authenticated claims contract that the account page can display without adding profile persistence.
- `.bootstrap-scaffold.csproj:1-21` is a web SDK project with no separate UI package; Razor Pages can be added within the existing project.
- `docs/authentication.md:78-99` currently documents the absence of sign-in UX and must be updated when `/` becomes live.
- `context/foundation/roadmap.md:100-108` defines this slice as account creation, login, and Argus's educational-purpose explanation, with F-01 already complete.

## What We're NOT Doing

- Adding a local password flow, password reset, account database, profile table, or ASP.NET Core Identity persistence.
- Adding roles, admin access, tenant-specific authorization, or differentiated user permissions.
- Adding the market-analysis workflow, asset selection, market-data integration, scoring, charts, AI summaries, watchlists, or alerts.
- Allowing arbitrary external return URLs or redirecting API callers to the identity provider.
- Adding browser end-to-end infrastructure or requiring a live Entra tenant in automated tests.
- Removing or redesigning `/weatherforecast`; it remains starter code outside this slice.
- Committing Entra secrets, tenant credentials, signing keys, or environment-specific identifiers.

## Implementation Approach

Use Razor Pages in the existing ASP.NET Core application rather than introducing a separate frontend or embedding HTML in minimal-API delegates. Register Razor Pages and map them alongside the existing API routes. The public index page owns the purpose/disclaimer content and one unified account action. Account page handlers explicitly challenge the configured OpenID Connect scheme, carry only validated local return paths, render claims from the authenticated principal, and sign out both the cookie and OpenID Connect schemes. Remote OIDC failures are mapped to a safe local error page. Extend the existing offline test project with route-level contract coverage and update the authentication handoff documentation.

## Critical Implementation Details

The default authentication challenge must remain the cookie scheme because `/api/me` is required to return 401 ProblemDetails. Only browser page handlers may explicitly challenge `OpenIdConnectDefaults.AuthenticationScheme`; changing the global default would regress API clients. Return paths must be normalized and limited to local application paths before being stored in authentication properties or used after logout.

## Phase 1: Build the public entry and account flow

### Overview

Introduce the Razor Pages surface and wire the browser-only account lifecycle on top of the existing claims-based session.

### Changes Required:

#### 1. Razor Pages registration and page routing

**Files**: `.bootstrap-scaffold.csproj`, `Program.cs`, `Pages/Index.cshtml`, `Pages/Index.cshtml.cs`, `Pages/Shared/_Layout.cshtml`

**Intent**: Add the smallest maintainable server-rendered UI surface for the public entry page without creating a separate frontend project or changing the existing API pipeline.

**Contract**: Register Razor Pages and map them without moving or weakening the existing named API routes. `/` must render successfully and provide the Argus purpose statement, educational-not-advice disclaimer, and one unified sign-in-or-create-account action.

#### 2. Explicit interactive sign-in

**Files**: `Pages/Account/SignIn.cshtml`, `Pages/Account/SignIn.cshtml.cs`, `Program.cs`

**Intent**: Start the Entra External ID browser flow explicitly while preserving the API's non-redirecting authentication contract.

**Contract**: The sign-in handler challenges `OpenIdConnectDefaults.AuthenticationScheme`, accepts only a validated local return path, and defaults to `/account`. It must not implement local credential handling or expose provider secrets. The visible action and route semantics must represent both sign-in and account creation as one provider-backed entry.

#### 3. Protected account home

**Files**: `Pages/Account/Index.cshtml`, `Pages/Account/Index.cshtml.cs`

**Intent**: Give an authenticated user a stable post-login destination that confirms the session and provides a future extension point for market analysis.

**Contract**: The page requires an authenticated principal, displays available subject/display-name/email claims, links to logout, and does not persist or mutate a local profile. An unauthenticated browser request uses an explicit OIDC challenge rather than the API 401 response.

#### 4. Provider-backed logout

**Files**: `Pages/Account/SignOut.cshtml`, `Pages/Account/SignOut.cshtml.cs`, `Program.cs`

**Intent**: End both the local Argus session and the Entra provider session from a browser-safe account action.

**Contract**: Logout signs out the cookie and OpenID Connect schemes, returns to a validated local destination, and never accepts an arbitrary external redirect. Repeated logout and missing-session cases must resolve to a safe public page rather than an exception or an authenticated-looking response.

#### 5. Friendly OIDC failure handling

**Files**: `Pages/Account/Error.cshtml`, `Pages/Account/Error.cshtml.cs`, `Program.cs`

**Intent**: Convert cancellation, access denial, and remote callback failures into a useful local browser experience without leaking raw identity-provider diagnostics.

**Contract**: OIDC failure events route to the account-error page with a bounded, user-safe message. The page must not render exception details, tokens, authorization codes, client secrets, or arbitrary query content.

### Success Criteria:

#### Automated Verification:

- `dotnet build .bootstrap-scaffold.csproj --no-restore` succeeds with Razor Pages registered.
- The test host can request `/` and receive a successful HTML response containing the purpose statement and educational-not-advice boundary.
- Interactive sign-in tests prove the page uses an explicit OIDC challenge and preserves only validated local return paths.
- Account-page tests prove an authenticated deterministic principal can reach `/account` and an unauthenticated browser request does not receive the API ProblemDetails contract.
- Logout tests prove the local session is cleared and the post-logout destination remains local.
- Error-page tests prove provider failure handling does not expose raw exception or credential data.

#### Manual Verification:

- With valid Entra settings over the HTTPS launch profile, a visitor can open `/`, start the unified sign-in-or-create-account flow, complete authentication, and reach `/account`.
- The account page displays the claims available from the provider and presents a working logout action.
- Cancelling or denying the provider flow shows a friendly account-error page, while `/api/me` still returns 401 ProblemDetails when no session exists.

**Implementation Note**: Complete this phase only after the deterministic tests pass and a maintainer has manually confirmed the browser flow with a non-production Entra registration.

## Phase 2: Lock the contract and operational handoff

### Overview

Make the account flow reproducible and regression-resistant through focused offline tests, documentation, and repository guidance updates.

### Changes Required:

#### 1. Account-access contract tests

**Files**: `tests/Argus.Authentication.Tests/AccountAccessContractTests.cs` and supporting test-factory helpers under `tests/Argus.Authentication.Tests/`

**Intent**: Extend the existing deterministic test approach to cover browser routes without contacting Entra or requiring production credentials.

**Contract**: Tests cover public landing-page content, explicit sign-in challenge behavior, local return-path acceptance/rejection, authenticated account rendering, logout behavior, friendly OIDC failure routing, and preservation of `/api/me` 401 ProblemDetails. Use deterministic claims and test configuration; do not weaken production startup validation.

#### 2. Authentication documentation

**File**: `docs/authentication.md`

**Intent**: Replace the foundation-only handoff with the actual account-access contract so local and Azure operators can configure and smoke-test the interactive flow.

**Contract**: Document the public root, unified sign-in/create-account action, `/account` destination, logout behavior, safe callback/return-path expectations, friendly failure behavior, HTTPS callback requirement, and the unchanged API 401 contract. Keep all credentials in user secrets, environment variables, or App Service settings.

#### 3. Repository guidance and deployment alignment

**Files**: `AGENTS.md`, `.github/workflows/deploy.yml` only if required by the final test/project layout

**Intent**: Keep agent guidance accurate about the new Razor Pages and account-access test surfaces without duplicating framework documentation or adding a live-provider CI dependency.

**Contract**: Point contributors to the canonical authentication documentation and deterministic test command. Deployment continues to build and publish `.bootstrap-scaffold.csproj`; it does not provision Entra settings or require a live tenant.

### Success Criteria:

#### Automated Verification:

- `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore` passes without network access or production secrets.
- `dotnet build .bootstrap-scaffold.csproj --no-restore` and the authentication test project build pass together.
- Tests assert the existing `/api/me` 401/no-Location/application-problem+json contract remains unchanged.
- A repository search confirms no client secret, tenant credential, token, or authorization code was added to tracked files.

#### Manual Verification:

- A maintainer can follow `docs/authentication.md` to configure a non-production Entra registration and run the HTTPS browser flow.
- The documentation's local and Azure callback examples match the configured `/signin-oidc` callback and do not require editing tracked secrets.
- The roadmap slice is ready for `select-and-start-analysis` to link future analysis navigation from the authenticated account page.

**Implementation Note**: Manual verification records routes and configuration names only; never record secret values or provider tokens.

## Testing Strategy

### Unit Tests:

- Validate local return-path normalization at root, nested account paths, empty input, query strings, and external/absolute URLs.
- Validate safe rendering of claims when display name or email is absent.
- Validate friendly error-message mapping without exposing provider exception text.

### Integration Tests:

- Request `/` anonymously and assert the purpose/disclaimer contract.
- Request sign-in and assert an explicit OIDC challenge rather than a cookie-scheme 401.
- Request `/account` with deterministic claims and without claims.
- Exercise logout and assert the local session is not retained.
- Preserve the existing real-scheme `/api/me` unauthenticated test.

### Manual Testing Steps:

1. Configure the documented Entra settings and `https://localhost:7219/signin-oidc` redirect URI in a non-production registration.
2. Run the HTTPS profile, open `/`, and choose the unified sign-in-or-create-account action.
3. Complete the provider flow, verify `/account` displays available claims, then log out.
4. Repeat with cancellation or denial and verify the friendly error page.
5. Request `/api/me` after logout and verify 401 ProblemDetails with no `Location` header.

## Performance Considerations

The landing, account, and error pages are server-rendered and contain no database or market-data calls. OIDC remains the only remote interaction; keep page handlers free of provider calls outside the explicit challenge/sign-out lifecycle and keep rendered account data limited to existing claims.

## Migration Notes

No local users, profiles, or schema exist, so no data migration is required. The operational migration is limited to registering the deployed HTTPS callback URI and supplying the existing Entra settings through supported external configuration providers.

## References

- Roadmap slice: `context/foundation/roadmap.md:100-108`
- Product requirements: `context/foundation/prd.md:66-67,92-94,116-124`
- Existing authentication setup: `Program.cs:15-102`
- Authentication handoff: `docs/authentication.md:1-99`
- Existing deterministic tests: `tests/Argus.Authentication.Tests/AuthenticationContractTests.cs:1-166`
- Stack decision: `context/foundation/tech-stack.md`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `.github/skills/10x-plan/references/progress-format.md`.

### Phase 1: Build the public entry and account flow

#### Automated

- [x] 1.1 Build the API with Razor Pages registered — 55d9b94
- [x] 1.2 Verify the public landing page renders the purpose and educational boundary — 55d9b94
- [x] 1.3 Verify explicit OIDC challenge and validated local return paths — 55d9b94
- [x] 1.4 Verify authenticated account rendering and unauthenticated interactive behavior — 55d9b94
- [x] 1.5 Verify logout clears the local session and preserves a local destination — 55d9b94
- [x] 1.6 Verify OIDC failures reach a safe error page without sensitive details — 55d9b94

#### Manual

- [x] 1.7 Complete the HTTPS Entra browser flow from `/` to `/account` — 55d9b94
- [x] 1.8 Confirm logout and provider cancellation/denial behavior manually — 55d9b94

### Phase 2: Lock the contract and operational handoff

#### Automated

- [x] 2.1 Pass the deterministic account-access and existing authentication contract tests
- [x] 2.2 Pass the API and authentication test-project builds together
- [x] 2.3 Confirm no identity credentials or tokens are committed
- [x] 2.4 Confirm the existing `/api/me` 401/no-Location/application-problem+json contract remains unchanged

#### Manual

- [x] 2.5 Follow the updated local and Azure authentication handoff
- [x] 2.6 Confirm the documentation callback examples match the configured `/signin-oidc` callback
- [x] 2.7 Confirm the account page leaves a stable entry point for the next analysis slice
