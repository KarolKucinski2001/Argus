# Account Access Flow — Plan Brief

> Full plan: `context/changes/account-access-flow/plan.md`

## What & Why

This plan adds Argus's first user-facing entry and account experience. Visitors will see what Argus does and that it is educational analysis rather than investment advice, then use one provider-backed action to sign in or create an account.

The change turns the existing authentication foundation into a usable browser flow without introducing local passwords, profile persistence, or analysis functionality.

## Starting Point

`Program.cs` already configures Microsoft Entra External ID, a secure cookie, authorization middleware, and protected `GET /api/me`. There is no root page, interactive challenge route, logout route, Razor Pages surface, or account UI yet.

The API's deliberate behavior is important: unauthenticated `/api/me` calls return 401 ProblemDetails rather than redirecting. The new browser pages must build around that contract, not change it.

## Desired End State

An anonymous visitor can open `/`, read the Argus purpose and disclaimer, and choose “sign in or create an account.” After the provider flow, the user reaches a protected `/account` page showing available identity claims and a logout action.

Local return paths are validated, logout ends both local and provider sessions, and provider cancellation or failure leads to a friendly error page without raw diagnostics. Automated tests remain offline and deterministic.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) |
| --- | --- | --- |
| UI surface | Razor Pages in the existing web project | It adds a conventional, maintainable server-rendered UI without a separate frontend or new client toolchain. |
| Account entry | One unified sign-in-or-create-account action | The current OIDC configuration exposes one authority and callback, so the product should not pretend to have separate local flows. |
| Logout | Clear local cookie and sign out at the provider | Users need a complete account exit rather than a browser-local partial logout. |
| Return paths | Local paths only, validated before use | This prevents open redirects while preserving useful post-login navigation. |
| Post-login destination | Protected `/account` home | Analysis is not in this slice, so the account page provides a stable authenticated handoff. |
| OIDC failures | Friendly local error page | Users get actionable feedback without exposing provider internals or secrets. |
| User persistence | Claims and cookie only | The PRD and roadmap do not require local profiles; adding a database would expand the slice. |
| Testing | Offline contract/integration tests | Existing tests already use deterministic handlers and CI must not require a live Entra tenant. |

## Scope

**In scope:**

- Public Razor Pages landing page, layout, purpose statement, and educational disclaimer.
- Explicit Entra OIDC sign-in/create-account challenge.
- Protected account page rendering available claims.
- Provider-backed logout and safe local return paths.
- Friendly OIDC cancellation/denial/error handling.
- Offline tests, authentication documentation, and focused repository guidance updates.

**Out of scope:**

- Local credentials, password reset, profile persistence, roles, or admin access.
- Market data, asset selection, ranking, charts, AI summaries, watchlists, and alerts.
- External return URLs, API redirects, browser E2E infrastructure, and live-provider CI.

## Architecture / Approach

Razor Pages are registered beside the current minimal API routes. Browser page handlers explicitly use the OIDC scheme, while `/api/me` retains the cookie challenge override that returns 401. Claims are rendered directly from the authenticated principal; no local user store is introduced. Tests use the existing `WebApplicationFactory` pattern with deterministic schemes and configuration.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Public entry and account flow | Razor Pages, explicit account lifecycle, logout, and safe error handling | Accidentally changing the API's 401 behavior or accepting unsafe redirects |
| 2. Contract and handoff | Offline regression tests plus local/Azure documentation | Documentation or tests implying a live provider is required in CI |

**Prerequisites:** F-01 (`minimal-auth-foundation`) is complete; Entra application settings and HTTPS callback registration are required only for manual browser verification.

**Estimated effort:** ~2 implementation sessions across 2 phases, with one manual Entra smoke-test checkpoint.

## Open Risks & Assumptions

- The configured Entra authority must support the unified sign-in/create-account experience; this plan does not add provider-specific policy selection.
- Provider-specific callback failures may vary, so automated tests assert the safe local error contract rather than a live provider round trip.
- The existing bootstrap project name and namespace remain unchanged during this slice.

## Success Criteria (Summary)

- A visitor can understand Argus's purpose and educational boundary, start account access, and reach a protected account page.
- Logout and provider failures are safe and understandable, with no open redirect or sensitive diagnostic leakage.
- Existing API authentication behavior remains intact and the full contract is proven without network access or production secrets.
