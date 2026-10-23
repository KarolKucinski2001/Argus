---
project: "Argus"
version: 2
status: draft
created: 2026-09-30
updated: 2026-10-23
prd_version: 1
main_goal: low-complexity
top_blocker: decisions
milestone_id: dashboard-and-watchlist
milestone_seq: 2
milestone_status: open
---

# Roadmap: Argus

> Derived from `context/foundation/prd.md` (v1, next tranche) + auto-researched and user-confirmed codebase baseline.
> Edit-in-place; archive when superseded.
> Slices below are listed in dependency order. The "At a glance" table is the index.

## Milestone

**M-2: Dashboard and watchlist** — Status: open

- **Intent:** Let a signed-in user keep a personal watchlist of assets and monitor it from a Dashboard tab that reuses the explained analysis from M-1, inside a tabbed workspace (Dashboard / Analysis / Watchlist).
- **Source materials:** `context/foundation/prd.md` (v1)
- **Done when:** every S-NN below is `done`.
- **Scope anchors:** FR-017 (primary), reusing FR-004, FR-006, FR-009, FR-010, FR-011, FR-014 for the Dashboard view.

## Vision recap

Argus helps beginners or prospective first-time investors understand how selected assets may change without assembling information from many pages, charts, and comparisons. It combines selected market data, transparent scoring factors, and grounded AI summaries while remaining an educational analysis tool rather than personalized investment advice.

## North star

**S-04: User can save assets to a personal watchlist** — the smallest end-to-end flow that turns a one-off analysis into something the user comes back to, and the per-user storage it introduces is what the Dashboard builds on.

> Here, "north star" means the smallest end-to-end slice whose successful delivery proves this milestone works; it is placed as early as its prerequisites allow.

## At a glance

| ID | Change ID | Outcome (user can …) | Prerequisites | PRD refs | Status |
| --- | --- | --- | --- | --- | --- |
| S-04 | save-assets-to-watchlist | save assets to a personal watchlist, review and remove them, and start an analysis from it | M-1 analysis workflow (closed) | FR-017, FR-002, FR-003 | ready |
| S-05 | watchlist-monitoring-dashboard | land on a Dashboard tab that shows watched assets with current price, 24h/7d/30d changes, compact trend, and top explained insights, with tab navigation to Analysis and Watchlist | S-04 | FR-017, FR-004, FR-006, FR-009, FR-010, FR-011, FR-014 | proposed |

## Baseline

What's already in place in the codebase as of `2026-10-23` (auto-researched + user-confirmed). Slices below assume these are present and do not re-scaffold them.

- **Frontend:** present — Razor Pages under `Pages/` with the token-backed analysis workspace (`Pages/Analysis/`, `wwwroot/css/site.css`).
- **Backend / API:** present — `Program.cs` plus analysis services in `Analysis/` and market data in `MarketData/`.
- **Data:** partial — market data is cached in memory only (`MarketData/InMemoryMarketDataCache.cs`); there is no persistent per-user storage.
- **Market data provider:** partial — only development and unavailable providers exist; the production source is still undecided.
- **Auth:** present — Entra ID OpenID Connect with cookie sessions in `Program.cs`.
- **Deploy / infra:** present — `.github/workflows/deploy.yml` deploys to Azure App Service.
- **Email / background jobs:** absent.
- **Observability:** absent beyond default ASP.NET Core logging.

## Foundations

None. Under the simplicity goal, per-user storage is introduced inside S-04, the first slice that needs it, rather than as a separate up-front layer.

## Slices

### S-04: Save assets to a watchlist

- **Outcome:** user can save assets to a personal watchlist, review and remove them, and start an analysis from the watchlist.
- **Change ID:** save-assets-to-watchlist
- **PRD refs:** FR-017, FR-002, FR-003
- **Prerequisites:** M-1 analysis workflow (closed)
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:**
  - Which persistent store holds per-user watchlists on Azure App Service? — Owner: team. Block: no (decided in `/10x-plan`).
- **Risk:** First persistent per-user data in Argus; it is sequenced first so the storage choice is proven on the smallest flow before the Dashboard depends on it.
- **Status:** ready

### S-05: Watchlist monitoring dashboard

- **Outcome:** user can land on a Dashboard tab that shows watched assets with current price, 24h/7d/30d changes, compact trend, and top explained insights, and move between Dashboard, Analysis, and Watchlist tabs.
- **Change ID:** watchlist-monitoring-dashboard
- **PRD refs:** FR-017, FR-004, FR-006, FR-009, FR-010, FR-011, FR-014
- **Prerequisites:** S-04
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:**
  - Should the Dashboard also show a fixed market overview (indices, BTC, ETH) beyond the user's own watchlist, given the "no full-market analysis" non-goal? — Owner: user. Block: no.
- **Risk:** Monitoring must stay descriptive and educational; "monitoring" must not drift into real-time alerts, which the PRD lists as a non-goal.
- **Status:** proposed

## Backlog Handoff

| Roadmap ID | Change ID | Suggested issue title | GitHub issue | Ready for `/10x-plan` | Notes |
| --- | --- | --- | --- | --- | --- |
| S-04 | save-assets-to-watchlist | Let users save assets to a personal watchlist | — | yes | Run `/10x-plan save-assets-to-watchlist`. |
| S-05 | watchlist-monitoring-dashboard | Add a Dashboard tab that monitors the watchlist | — | no | Requires S-04. |

## Open Roadmap Questions

1. **Which approved market-data source should be activated for production display?** — Owner: team. Block: real-provider activation only; S-04 and S-05 can proceed on the development provider. GitHub decision issue: [#1](https://github.com/KarolKucinski2001/Argus/issues/1).
2. **Should AI Copilot be included?** — Owner: user. Block: roadmap-wide only if included. GitHub decision issue: [#2](https://github.com/KarolKucinski2001/Argus/issues/2).
3. **Should Argus add public strategy profiles (institutional 13F funds, company insiders, congressional disclosures, known public portfolios), a simulated model-portfolio builder, and portfolio analysis?** — Owner: user. Block: M-3. Requires PRD v2 via `/10x-shape` → `/10x-prd`, rewording the "does not manage portfolios or allocate funds" non-goal into an explicit simulation-only, educational boundary, and choosing a public-filings data source.
4. **Should search trends get their own Trends tab (PRD secondary success criterion) or only feed the score?** — Owner: user. Block: future FR-019 work.

## Parked

- **Daily email report (FR-018)** — Parked: needs an email service, scheduling, and a production data source; revisit after M-2.
- **Search-trend scoring (FR-019)** — Parked: needs a trends data source decision; see Open Roadmap Question 4.
- **News-sentiment scoring (FR-020)** — Parked: needs a news source and sentiment method; adds an uncertain evidence source.
- **Strategy profiles, model portfolio builder, portfolio analysis** — Parked pending PRD v2 (Open Roadmap Question 3).
- **AI Copilot conversational questions** — Parked: no retained functional requirement (Open Roadmap Question 2).
- **Personalized advice, automated trading, portfolio management, full-market analysis, real-time alerts, and guaranteed predictions** — Parked by the PRD Non-Goals.

## Milestone History

- **M-1: First explained market analysis** (`first-explained-market-analysis`) — closed 2026-10-23. An authenticated beginner selects assets and gets a ranked, explained analysis with market data, factors, a compact trend view, a grounded AI summary, and the educational boundary.

## Done
- **S-02: user can select one or more supported assets and start a market analysis.** — Archived 2026-10-16 → `context/archive/2026-10-14-select-and-start-analysis/`. Lesson: —.
- **F-01: (foundation) the application has the minimum authentication and protected-route contract required for the MVP workflow.** — Archived 2026-10-02 → `context/archive/2026-10-01-minimal-auth-foundation/`. Lesson: —.
- **S-01: user can create an account, log in, and see a short explanation that Argus provides educational analysis rather than investment advice.** — Archived 2026-10-06 → `context/archive/2026-10-05-account-access-flow/`. Lesson: —.
- **F-02: (foundation) the analysis path has a verified source and evidence contract for current and historical data without committing to a provider-specific implementation.** — Archived 2026-10-07 → `context/archive/2026-10-06-market-data-access-contract/`. Lesson: —.
