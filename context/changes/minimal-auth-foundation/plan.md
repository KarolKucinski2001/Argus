# Minimum Authenticated Access Implementation Plan

## Overview

Establish the smallest production-configurable authentication foundation required by Argus's first protected workflow. The application will integrate with Microsoft Entra External ID through OpenID Connect and a secure cookie session, expose a dedicated current-user route, and leave registration and login UX to the dependent `account-access-flow` slice.

## Current State Analysis

Argus is an early .NET 10 minimal API scaffold. `Program.cs` registers only OpenAPI, HTTPS redirection, and the starter `/weatherforecast` route; the project references only `Microsoft.AspNetCore.OpenApi` and has no authentication, authorization, identity persistence, or test project. The roadmap defines F-01 as the minimum protected-route contract that unlocks S-01, while the PRD assigns account creation and login to FR-015 and FR-016.

The deployment workflow already builds and publishes `.bootstrap-scaffold.csproj` to Azure App Service, but it does not run tests. Existing configuration contains no identity-provider settings. Repository guidance treats `/weatherforecast` as starter code and requires environment-specific secrets to remain outside committed files.

## Desired End State

The application has a documented Microsoft Entra External ID configuration contract, registers OpenID Connect authentication with a secure cookie session, enables authorization middleware, and exposes a named current-user endpoint that requires authentication. Missing or invalid credentials receive an HTTP 401 ProblemDetails response rather than an implicit success or a redirect from the API contract.

Focused automated tests prove that the protected endpoint rejects unauthenticated requests and returns the authenticated principal when the test host supplies a valid principal. Configuration documentation explains the required local and Azure App Service settings without committing client secrets. Account creation, login screens/routes, user persistence, roles, and domain analysis remain outside this change.

### Key Discoveries:

- `Program.cs:1-41` is the complete startup and route surface; there is no existing auth seam to preserve.
- `.bootstrap-scaffold.csproj:3-12` has no auth or test dependencies and retains the bootstrap project/root namespace.
- `context/foundation/roadmap.md:72-82,143-145` defines F-01 as a minimal protected-route foundation and marks it ready.
- `context/foundation/prd.md:92-94,116-118` requires account access later and specifies one flat access scope without roles.
- `.github/workflows/deploy.yml:1-48` deploys to Azure App Service, providing the operational destination for environment configuration but no test gate.

## What We're NOT Doing

- Implementing account registration, login screens, logout UX, password handling, or user-profile persistence; these belong to `account-access-flow`.
- Adding application roles, permissions, tenant-specific authorization rules, or admin access.
- Protecting or redesigning `/weatherforecast`; it remains starter code until a later domain slice replaces it.
- Adding database/ORM infrastructure, market-data integration, frontend scaffolding, or analysis endpoints.
- Committing Entra client secrets, signing keys, tenant secrets, or environment-specific URLs.
- Adding an automatic redirect contract to API endpoints; the protected API route returns 401 ProblemDetails.

## Implementation Approach

Use ASP.NET Core's OpenID Connect handler with a secure cookie scheme and Microsoft Entra External ID settings supplied through configuration providers backed by environment variables and Azure App Service application settings. Keep the foundation in the existing top-level startup path, introduce a small current-user response contract, and isolate authentication behavior behind the standard middleware and endpoint authorization metadata. Add a focused test project using the ASP.NET Core test host so the protected-route contract can be checked without calling the external identity provider.

## Critical Implementation Details

The test host must replace the external authentication handler with a deterministic test scheme; tests must verify authorization behavior without network access or real Entra credentials. Production configuration must fail explicitly when required identity settings are absent rather than silently enabling an unauthenticated fallback.

## Phase 1: Configure Entra authentication and protected access

### Overview

Add the provider configuration contract and wire Microsoft Entra External ID authentication into the existing .NET startup. Establish secure cookie session behavior, authorization middleware, and a named current-user endpoint that is the stable prerequisite for S-01.

### Changes Required:

#### 1. Project and configuration contract

**Files**: `.bootstrap-scaffold.csproj`, `appsettings.json`, `appsettings.Development.json`, `Program.cs`

**Intent**: Add the supported ASP.NET Core OpenID Connect authentication dependency and register the Entra External ID settings required to validate the provider flow without storing secrets in source control.

**Contract**: Configuration must define the identity authority/tenant, client ID, client secret supplied by environment/App Service settings, callback path, and cookie security behavior. Missing required production settings must surface as an explicit startup/configuration error; development placeholders must not create a successful unauthenticated path.

#### 2. Authentication and authorization middleware

**File**: `Program.cs`

**Intent**: Make authentication and authorization part of the application request pipeline while preserving existing HTTPS redirection, Development-only OpenAPI exposure, and starter-route behavior.

**Contract**: Register one OpenID Connect challenge scheme with a secure cookie session, add authentication and authorization services, and invoke the corresponding middleware before protected endpoint execution. Use explicit route names consistent with the repository convention.

#### 3. Current-user protected route

**File**: `Program.cs` or a focused route/contract file if the implementation extracts one

**Intent**: Provide the minimum observable protected-route contract that proves the authenticated principal is available to the future account-access and analysis slices.

**Contract**: Add a named `GET` route under an API-oriented path for the current user. It requires authorization, returns a stable JSON shape containing the authenticated subject identifier and relevant display identity when present, and returns HTTP 401 ProblemDetails for an unauthenticated request. It must not rank assets, create accounts, or redirect API callers.

### Success Criteria:

#### Automated Verification:

- `dotnet restore .bootstrap-scaffold.csproj` completes with the selected authentication dependency.
- `dotnet build .bootstrap-scaffold.csproj --no-restore` succeeds with nullable analysis enabled.
- The application has no committed client secret, signing key, or environment-specific identity credential.

#### Manual Verification:

- With valid Entra External ID settings, the application can establish the configured secure cookie session and the current-user route exposes the authenticated principal.
- Without valid credentials, the current-user route returns 401 ProblemDetails and does not return a success-shaped anonymous response.
- Existing Development-only OpenAPI behavior and HTTPS redirection remain unchanged.

**Implementation Note**: After completing this phase and all automated verification passes, pause here for manual confirmation that the Entra configuration and protected-route smoke test succeeded before proceeding to Phase 2.

## Phase 2: Add deterministic verification and operational handoff

### Overview

Create a focused test project that verifies the protected-route contract without external network calls, and document the exact configuration handoff for local development and Azure App Service.

### Changes Required:

#### 1. Authentication contract tests

**Files**: New test project under `tests/`, project reference to `.bootstrap-scaffold.csproj`, and test source files following the repository's established layout once created

**Intent**: Establish regression coverage for the minimum authentication foundation so future slices can change the startup surface without weakening the protected-route contract.

**Contract**: The test host uses a deterministic authenticated test principal and covers at least: unauthenticated request returns 401 ProblemDetails; authenticated request returns the current-user response; malformed or missing required configuration fails explicitly in the configured validation path. Tests must not contact Entra or require production secrets.

#### 2. Configuration and deployment handoff

**Files**: `docs/authentication.md`, and, if needed, `.github/workflows/deploy.yml`

**Intent**: Make the Entra External ID setup reproducible for local development and Azure App Service while keeping secrets in environment configuration.

**Contract**: Document each required setting, its non-secret versus secret classification, the callback URL shape for local and deployed environments, and the Azure App Service application-setting handoff. Extend CI only if the new test project can run without external credentials; do not make deployment depend on a live Entra tenant.

#### 3. Repository guidance alignment

**Files**: `AGENTS.md` and any directly affected project documentation

**Intent**: Correct the repository guidance where it conflicts with the now-real authentication/test/deployment surfaces, without copying framework documentation into the agent guide.

**Contract**: Guidance must point to the canonical authentication configuration/test documentation, record the available test command, and accurately describe the existing GitHub Actions deployment workflow.

### Success Criteria:

#### Automated Verification:

- `dotnet test <test-project> --no-restore` passes with no network access or production secrets.
- The test suite proves both 401 unauthenticated rejection and authenticated current-user response.
- `dotnet build .bootstrap-scaffold.csproj --no-restore` and the test-project build pass together.
- A repository search confirms no client secret, signing key, or tenant credential is committed.

#### Manual Verification:

- A maintainer can follow the configuration handoff to run the app locally against a test Entra External ID registration.
- Azure App Service settings can be supplied through environment configuration without modifying committed JSON files.
- The protected route is ready for S-01 to build registration/login UX on top of it, while no account-access behavior has leaked into F-01.

**Implementation Note**: Manual confirmation should explicitly record the callback URL and tenant configuration used for the smoke test without recording secret values.

## Testing Strategy

### Unit Tests:

- Validate the current-user response mapping from an authenticated claims principal.
- Validate configuration binding/required-setting failures.

### Integration Tests:

- Exercise the HTTP pipeline with an unauthenticated request and assert 401 ProblemDetails.
- Exercise the same route with a deterministic authenticated test scheme and assert the stable current-user response.
- Ensure tests do not require a live identity provider or production configuration.

### Manual Testing Steps:

1. Configure a non-production Entra External ID application and local callback URL through environment variables.
2. Run the API and complete the provider flow to establish the secure cookie session.
3. Request the current-user route with and without the session and verify the documented responses.
4. Repeat the configuration handoff using Azure App Service application settings without placing secrets in the repository.

## Performance Considerations

Authentication is request middleware plus cookie validation; no database or market-data calls are introduced by this foundation. Keep the current-user response small and avoid remote provider calls on every request beyond the handler's normal validation behavior.

## Migration Notes

There are no existing users, database schema, or authenticated routes to migrate. The Entra application registration and its callback URLs are external operational prerequisites; removing the foundation requires removing the protected-route contract and dependent S-01 integration, not a data migration.

## References

- Roadmap foundation: `context/foundation/roadmap.md:72-82`
- Product access requirements: `context/foundation/prd.md:92-94,116-118`
- Current startup: `Program.cs:1-41`
- Current project dependencies: `.bootstrap-scaffold.csproj:3-12`
- Deployment workflow: `.github/workflows/deploy.yml:1-48`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Configure Entra authentication and protected access

#### Automated

- [x] 1.1 Restore the project with the selected authentication dependency
- [x] 1.2 Build the API with authentication and authorization services registered
- [x] 1.3 Confirm no identity secret or credential is committed

#### Manual

- [ ] 1.4 Verify the Entra-backed secure session and current-user route manually
- [x] 1.5 Verify unauthenticated current-user requests return 401 ProblemDetails
- [x] 1.6 Verify existing OpenAPI and HTTPS behavior remains unchanged

### Phase 2: Add deterministic verification and operational handoff

#### Automated

- [x] 2.1 Pass the authentication contract test project without network access or production secrets
- [x] 2.2 Pass API and test-project builds together
- [x] 2.3 Confirm the repository contains no committed identity credentials
- [x] 2.4 Confirm the test suite proves both unauthenticated rejection and authenticated current-user response

#### Manual

- [ ] 2.5 Follow the local Entra configuration handoff successfully
- [ ] 2.6 Supply Azure App Service settings without editing committed JSON files
- [ ] 2.7 Confirm S-01 can consume the protected-route contract without F-01 implementing account-access UX
