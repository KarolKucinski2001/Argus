# Save assets to a personal watchlist — Plan Brief

> Full plan: `context/changes/save-assets-to-watchlist/plan.md`

## What & Why

Argus currently lets an authenticated user select assets and run an explained
analysis, but the selection disappears after the workflow. This plan adds a
personal watchlist so the user can save selected assets, return to them later,
remove them, and start the same analysis again without turning Argus into a
portfolio manager or advice product.

## Starting Point

The existing `/analysis` Razor Page validates configured asset IDs, limits
analysis to five assets, and renders ranking, factors, market data, summaries,
and the educational boundary. Authentication already provides a stable `sub`
claim, but the application has no durable database, user-owned data model, or
storage abstraction. The roadmap identifies this as S-04, the prerequisite
for the later monitoring Dashboard.

## Desired End State

An authenticated user can save the current analysis selection, open
`/watchlist`, see saved entries newest-first, remove entries immediately, and
select up to five available entries to reuse the existing analysis flow.
Saved data is isolated by subject claim and persists through process restarts.
If storage is unavailable, the UI reports the failure and keeps the user's
selection instead of showing a false success.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) |
| --- | --- | --- |
| Persistent store | Azure Table Storage | Fits the bounded key/value workload and Azure App Service target without introducing a relational model before the Dashboard needs one. |
| Entry identity | One row per `(subject ID, asset ID)` | Makes repeated saves idempotent and prevents duplicate watchlist rows. |
| Ordering | Newest saved entries first | Gives the user a stable, useful default while avoiding a manual ordering feature. |
| Save entry point | Save selected assets from `/analysis` plus `/watchlist` | Reuses the existing checkbox selection and keeps the feature discoverable in the current workflow. |
| Watchlist route | Dedicated `/watchlist` Razor Page | Separates review/remove behavior from the analysis workspace and gives S-05 a stable data surface. |
| Ownership key | `sub`, fallback `ClaimTypes.NameIdentifier` | Matches the existing authentication contract and is stable across display-name/email changes. |
| Capacity | 50 entries with explicit rejection beyond the cap | Bounds reads and storage while leaving enough room for a beginner's monitored set. |
| Removed configuration | Keep row, show unavailable, allow removal only | Avoids silent data loss while preventing analysis of assets no longer supported. |
| Analysis handoff | Select up to five available entries and reuse existing analysis | Preserves the existing validation and result contracts instead of duplicating business logic. |
| Local development | Configurable Azure endpoint/emulator | Keeps local setup deterministic without committing credentials or requiring production access. |
| Failure behavior | Explicit error with selection preserved | Prevents success-shaped UI when durable storage did not complete. |
| Verification | Store tests plus Razor contract tests and manual smoke checks | Covers persistence semantics, user isolation, route behavior, and the real browser flow. |

## Scope

**In scope:** durable subject-owned rows, save from analysis, `/watchlist`,
newest-first list, immediate remove, empty/error/unavailable states, analysis
handoff for up to five assets, Azure/local configuration, and automated tests.

**Out of scope:** Dashboard monitoring, alerts, email, portfolios, trading,
manual ordering, save history, soft delete/restore, full-market analysis, and
personalized advice.

## Architecture / Approach

`/analysis` and `/watchlist` depend on an application-owned
`IWatchlistStore`. The Azure Table implementation stores the subject as the
partition key and the normalized asset ID as the row key. Page handlers resolve
the authenticated subject, validate against the configured allowlist, and
translate store results into explicit Razor Page states. The watchlist posts
available selections into the existing analysis handler rather than creating a
second analysis service.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Storage contract | Azure Table store, entity keys, options, registration, and store tests | Mis-scoped keys could expose another user's data |
| 2. User flows | Save, `/watchlist`, remove, unavailable state, and analysis handoff | UI could report success or duplicate analysis behavior incorrectly |
| 3. Verification/docs | Full contract coverage and secret-safe local/Azure setup | Deployment may omit the storage setting or regress existing auth/analysis |

**Prerequisites:** M-1 analysis workflow is closed; Azure Table/Azurite
endpoint access must be available for manual smoke testing.

**Estimated effort:** ~3 implementation phases across 3-5 focused sessions.

## Open Risks & Assumptions

- Azure App Service configuration must provide the watchlist connection setting;
  the plan intentionally does not provision Azure resources.
- The production market-data provider remains undecided, so watchlist
  eligibility continues to follow the development/configured allowlist.
- The Azure Table adapter and page contract tests need a deterministic test
  seam so CI never depends on Entra or a live Azure account.

## Success Criteria (Summary)

- A user's saved assets persist, remain isolated from other subjects, and can
  be listed and removed through `/watchlist`.
- Repeated saves are idempotent, the 50-entry boundary is explicit, and
  unavailable assets are retained for removal but cannot be analyzed.
- Existing analysis behavior and educational-not-advice wording remain intact
  when started from the watchlist.

