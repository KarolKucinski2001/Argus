# Select and Start Analysis — Plan Brief

> Full plan: `context/changes/select-and-start-analysis/plan.md`

## What & Why

This change adds Argus's first protected product workflow after account
access: an authenticated user selects supported assets and starts an analysis.
It proves the selection and data-access handoff without prematurely building
the ranking and explanation experience reserved for S-03.

## Starting Point

Argus already has a protected `POST /api/market-data` route with allowlist,
history, cache, budget, and provider-error contracts. It does not yet have an
analysis page, form, shared service for browser/API callers, or runtime fake
provider; the account page is the current authenticated entry point.

## Desired End State

The user follows `/account` → `/analysis`, selects one or more configured
assets, and submits a server-rendered form. Development/Test shows a
deterministic and explicitly illustrative started state, while production
continues to show the existing unavailable/unapproved boundary until a real
provider is approved.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Slice boundary | Selection, start, and bounded status; no ranking | Keeps S-02 separate from the S-03 north-star result | Roadmap / Plan |
| Page architecture | Razor POST handler using a shared market-data service | Fits the existing server-rendered app and avoids self-HTTP or duplicated validation | Plan |
| Local data | Deterministic fake provider in Development/Test only | Enables an end-to-end local flow without weakening production approval | Plan |
| Asset source | `MarketDataOptions.AllowedAssets` | Prevents a second catalog from diverging from API validation | Plan |
| Selection controls | Server-rendered checkboxes with server validation | Works without JavaScript and matches the small configured catalog | Plan |
| History | Fixed 12-month daily range ending at UTC today | Uses the existing market-data contract without adding date UX | PRD / Research |
| Errors | Inline bounded messages with selections preserved | Helps beginners recover without losing work or exposing provider details | Plan |
| Route | Protected `/analysis` Razor Page | Establishes a stable future handoff for S-03 | Plan |

## Scope

**In scope:**

- Shared market-data service used by API and Razor Page.
- Deterministic Development/Test fake provider with an environment guard.
- Protected `/analysis` checkbox page and `/account` navigation link.
- Fixed 12-month daily request, selection validation, success/unavailable/error
  states, and contract tests.
- Documentation of the fake-provider and production-approval boundary.

**Out of scope:**

- Real provider activation, credentials, licensing, or public-display approval.
- Ranking, score, factors, chart, AI summary, persistence, or watchlists.
- User-selected date ranges, background analysis jobs, or frontend framework.

## Architecture / Approach

The Razor Page and existing API will share a market-data application service.
The service retains the current provider-neutral contracts and error semantics;
only the adapters differ: the API maps failures to ProblemDetails, while the
PageModel maps them to bounded inline messages. DI selects the deterministic
fake only for Development/Test and leaves the unavailable provider in other
environments.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Shared contract and safe fake | Reusable service and guarded local provider | Accidentally weakening production provider approval |
| 2. Protected selection page | `/analysis`, checkbox submission, inline states, account link | Duplicating or drifting from market-data validation |
| 3. Coverage and boundary | Tests, docs, and S-03 handoff | Missing an auth/error/viewport state |

**Prerequisites:** S-01 account access and F-02 market-data contract are
complete; no real provider approval is required.

**Estimated effort:** ~2–3 sessions across 3 phases, including manual UI
verification.

## Open Risks & Assumptions

- The existing top-level market-data route can be extracted without changing
  its public response and error behavior.
- Development/Test fake data is clearly labeled and cannot be resolved by
  non-development hosts.
- The configured allowlist remains small enough for checkbox selection; a
  search/catalog component is not needed for S-02.
- The real provider decision remains open and does not block this local/test
  contract slice.

## Success Criteria (Summary)

- An authenticated user can select 1–5 configured assets and start analysis
  through `/analysis`.
- Invalid selections and provider failures are bounded, understandable, and
  preserve the user's selection without leaking raw details.
- Development/Test can demonstrate the flow with illustrative data while
  production remains explicitly unavailable/unapproved until provider review.
