# Minimum Authenticated Access — Plan Brief

> Full plan: `context/changes/minimal-auth-foundation/plan.md`

## What & Why

Argus needs a minimal authentication foundation before it can deliver the protected analysis workflow. This plan integrates Microsoft Entra External ID with a secure cookie session and exposes a small current-user contract, while deliberately leaving account registration and login UX to the dependent `account-access-flow` slice.

## Starting Point

The repository is a .NET 10 minimal API scaffold with OpenAPI, HTTPS redirection, and the starter `/weatherforecast` route. It has no authentication packages, identity configuration, user persistence, test project, or protected endpoint; deployment already targets Azure App Service.

## Desired End State

The API has an explicit Entra External ID configuration contract, standard authentication and authorization middleware, and a named protected current-user route. Unauthenticated API calls receive HTTP 401 ProblemDetails, authenticated test principals are exposed through a stable response, and no credentials are stored in source control.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) |
| --- | --- | --- |
| Change identity | Rename folder to `minimal-auth-foundation` | Matches the stable Change ID already used by the roadmap and enables status synchronization. |
| Foundation boundary | Auth mechanism and protected route only | Keeps F-01 minimal and leaves registration/login UX to S-01. |
| Provider | Microsoft Entra External ID | Fits the Azure App Service deployment target and supplies the external identity boundary without adding user persistence to the scaffold. |
| Session | Secure cookie session | Matches a browser web app and keeps the future UI from managing bearer tokens directly. |
| API failure contract | HTTP 401 ProblemDetails | Gives API callers an explicit machine-readable failure instead of an implicit anonymous success or redirect. |
| Protected contract | Dedicated current-user route | Provides a stable integration point for S-01 without prematurely creating an analysis endpoint. |
| Secret handling | Environment variables and App Service settings | Keeps tenant credentials out of committed JSON and supports local/deployed configuration parity. |

## Scope

**In scope:**

- OpenID Connect and secure cookie registration for Entra External ID.
- Authentication/authorization middleware and a protected current-user route.
- Deterministic integration tests with a local test authentication scheme.
- Local/Azure configuration handoff and accurate repository guidance.

**Out of scope:**

- Registration, login, logout screens or endpoints.
- User persistence, roles, permissions, database/ORM, and domain analysis.
- Changes to the starter `/weatherforecast` route.
- Live-provider credentials in CI or tests.

## Architecture / Approach

The existing top-level ASP.NET Core startup registers the OIDC challenge and cookie session, enables authorization, and maps one protected API route. Tests replace the external provider with a deterministic scheme, so the HTTP contract is verified offline. Azure App Service supplies production settings through environment configuration; no secrets enter the repository.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Configure Entra authentication and protected access | Provider settings, middleware, secure session, and current-user route | Missing or invalid provider configuration must fail explicitly. |
| 2. Add deterministic verification and operational handoff | Offline contract tests and local/Azure setup documentation | Tests must remain independent of a live Entra tenant. |

**Prerequisites:** A non-production Entra External ID application registration and callback URLs are needed for manual smoke testing; implementation tests do not require provider access.
**Estimated effort:** ~2–3 implementation sessions across 2 phases for a solo developer, plus provider setup/smoke-test time.

## Open Risks & Assumptions

- Entra External ID tenant and application-registration details are external prerequisites and may require manual portal configuration.
- The current scaffold has no test project, so test-project conventions will be established as part of Phase 2.
- The existing `.github/workflows/deploy.yml` builds and deploys but does not run tests; CI changes are limited to tests that do not need external credentials.

## Success Criteria (Summary)

- An unauthenticated request to the current-user route receives 401 ProblemDetails, while an authenticated principal receives the stable user response.
- Local and Azure configuration can supply Entra settings without committed secrets.
- S-01 has a documented protected-access contract and F-01 contains no registration/login UX.
