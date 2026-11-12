---
project: "Argus"
version: 3
status: draft
created: 2026-09-30
updated: 2026-11-12
prd_version: —
main_goal: speed
top_blocker: external
milestone_id: reviewer-ready-release
milestone_seq: 3
milestone_status: open
---

# Roadmap: Argus

> Derived from the user's milestone description (anchors below) + auto-researched and user-confirmed codebase baseline.
> Edit-in-place; the next milestone continues this file. Archive only when an independent roadmap replaces it.
> Slices below are listed in dependency order. The "At a glance" table is the index.

## Milestone

**M-3: Reviewer-ready release** — Status: open

- **Intent:** Make the delivered product easy and safe to evaluate: anyone with a Microsoft account can sign in, critical browser flows are proven automatically on every push, a reviewer can orient and run the project from the README, and recurring lessons are captured.
- **Source materials:** user description (anchors below), recorded verbatim: "README for reviewers, Playwright E2E in CI, lessons.md" plus the earlier decision "anyone with a Microsoft account (work/school or personal) can sign in".
- **Done when:** every S-NN below is `done`.
- **Scope anchors:**
  - MS-01: A README written for reviewers — what the product does, live link, how to sign in, how to run and test it locally, and where the product and process documents live.
  - MS-02: Browser-level (E2E) tests of the critical user flows, run automatically in CI on every push.
  - MS-03: `context/foundation/lessons.md` captures the recurring rules and pitfalls learned while building the product.
  - MS-04: Anyone with a Microsoft account — work/school or personal — can sign in to the deployed app.

## Vision recap

Argus helps beginners or prospective first-time investors understand how selected assets may change without assembling information from many pages, charts, and comparisons. It combines selected market data, transparent scoring factors, and grounded AI summaries while remaining an educational analysis tool rather than personalized investment advice.

## North star

**S-08: Critical browser flows are verified automatically in CI** — the riskiest item in this milestone (a real browser driving signed-in pages without a live identity provider), so it goes first under the speed goal: if it works, the rest is documentation and configuration.

> Here, "north star" means the smallest end-to-end slice whose successful delivery proves this milestone works; it is placed as early as its prerequisites allow.

## At a glance

| ID | Change ID | Outcome (user can …) | Prerequisites | PRD refs | Status |
| --- | --- | --- | --- | --- | --- |
| S-08 | e2e-critical-flows-ci | (maintainer) see critical browser flows — landing, analysis, watchlist, strategies, language and theme switch — pass automatically in CI before every deploy | M-2 closed | MS-02 | ready |
| S-09 | open-microsoft-sign-in | sign in to the deployed app with any Microsoft account, work/school or personal | Azure access to the app registration and App Service settings | MS-04 | ready |
| S-10 | reviewer-readme | (reviewer) open the README and, without other help, understand the product, reach the live app, sign in, and run and test it locally | S-08, S-09 | MS-01 | proposed |
| S-11 | lessons-capture | (maintainer) read the recurring rules and pitfalls from M-1–M-3 in one lessons file that reviews and plans consult | — | MS-03 | ready |

## Streams

Navigation aid — groups items that share a Prerequisites chain. Canonical ordering still lives in the dependency graph below; this table is the proposed reading order across parallel tracks.

| Stream | Theme | Chain | Note |
| --- | --- | --- | --- |
| A | Automated verification | `S-08` → `S-10` | Riskiest first under the speed goal; the README then documents the E2E command. |
| B | Access | `S-09` | Joins Stream A at `S-10`, which documents how reviewers sign in. |
| C | Knowledge capture | `S-11` | Standalone; can run alongside any other slice. |

## Baseline

What's already in place in the codebase as of `2026-11-12` (auto-researched + user-confirmed). Slices below assume these are present and do not re-scaffold them.

- **Frontend:** present — Razor Pages under `Pages/`, token-backed styles in `wwwroot/css/site.css`, Polish/English UI and dark/light theme.
- **Backend / API:** present — `Program.cs` with analysis, watchlist, dashboard and strategy services.
- **Data:** present — Azure Table Storage for watchlists and simulated portfolios (Azurite locally).
- **Market data provider:** present — live adapters plus the illustrative provider (`docs/market-data.md`).
- **Auth:** present — Entra ID OpenID Connect; any-tenant issuer validation already supported by configuration (`docs/authentication.md`).
- **Deploy / infra:** present — `.github/workflows/deploy.yml` builds, runs xUnit tests, and deploys to Azure App Service.
- **Tests:** partial — xUnit integration and unit tests only; no browser-level tests.
- **Docs:** partial — `README.md` exists but predates English UI, light theme, production link and sign-in policy; `lessons.md` has 2 entries.
- **Observability:** absent beyond default ASP.NET Core logging.

## Foundations

None. Under the speed goal, browser-test infrastructure is introduced inside S-08, the first slice that needs it.

## Slices

### S-08: Critical browser flows verified in CI

- **Outcome:** maintainer can see critical browser flows — landing, analysis, watchlist, strategies, language and theme switch — pass automatically in CI before every deploy.
- **Change ID:** e2e-critical-flows-ci
- **PRD refs:** MS-02
- **Prerequisites:** M-2 closed
- **Parallel with:** S-09, S-11
- **Blockers:** —
- **Unknowns:**
  - How do browser tests reach signed-in pages without a live identity provider (test-only environment vs. real sign-in)? — Owner: team. Block: no (decided in `/10x-plan`).
- **Risk:** Browser tests in CI can be slow or flaky; sequenced first because it is the only item with real technical uncertainty.
- **Status:** ready

### S-09: Sign-in with any Microsoft account

- **Outcome:** user can sign in to the deployed app with any Microsoft account, work/school or personal.
- **Change ID:** open-microsoft-sign-in
- **PRD refs:** MS-04
- **Prerequisites:** Azure access to the app registration and App Service settings
- **Parallel with:** S-08, S-11
- **Blockers:** Azure CLI session must be renewed by the owner; organisation policy may restrict changing the app registration's supported account types.
- **Unknowns:**
  - Does the tenant allow an app registration that accepts personal Microsoft accounts? — Owner: user. Block: no (fallback: any work/school account only).
- **Risk:** Mostly configuration outside the repository; a tenant policy refusal narrows the audience rather than blocking the milestone.
- **Status:** ready

### S-10: README for reviewers

- **Outcome:** reviewer can open the README and, without other help, understand the product, reach the live app, sign in, and run and test it locally.
- **Change ID:** reviewer-readme
- **PRD refs:** MS-01
- **Prerequisites:** S-08, S-09
- **Parallel with:** S-11
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Documents commands and sign-in behaviour, so it follows the slices that change them to avoid stale instructions.
- **Status:** proposed

### S-11: Lessons captured

- **Outcome:** maintainer can read the recurring rules and pitfalls from M-1–M-3 in one lessons file that reviews and plans consult.
- **Change ID:** lessons-capture
- **PRD refs:** MS-03
- **Prerequisites:** —
- **Parallel with:** S-08, S-09, S-10
- **Blockers:** —
- **Unknowns:** —
- **Risk:** Low; lessons must be specific and actionable, not a diary.
- **Status:** ready

## Backlog Handoff

| Roadmap ID | Change ID | Suggested issue title | Ready for `/10x-plan` | Notes |
| --- | --- | --- | --- | --- |
| S-08 | e2e-critical-flows-ci | Add browser E2E tests for critical flows to CI | yes | Run `/10x-plan e2e-critical-flows-ci` (or `/10x-e2e-setup`). |
| S-09 | open-microsoft-sign-in | Allow sign-in with any Microsoft account | yes | Needs renewed Azure CLI login. |
| S-10 | reviewer-readme | Rewrite README for reviewers | no | Requires S-08, S-09. |
| S-11 | lessons-capture | Capture recurring lessons in lessons.md | yes | Use `/10x-lesson`. |

## Open Roadmap Questions

1. **Should AI Copilot be included?** — Owner: user. Block: roadmap-wide only if included. GitHub decision issue: [#2](https://github.com/KarolKucinski2001/Argus/issues/2).
2. **Should search trends get their own Trends tab (PRD secondary success criterion) or only feed the score?** — Owner: user. Block: future FR-019 work.

## Parked

- **Daily email report (FR-018)** — Parked: needs an email service, scheduling, and a production data source.
- **Search-trend scoring (FR-019)** — Parked: needs a trends data source decision; see Open Roadmap Question 2.
- **News-sentiment scoring (FR-020)** — Parked: needs a news source and sentiment method.
- **AI Copilot conversational questions** — Parked: no retained functional requirement (Open Roadmap Question 1).
- **Public-display licensing of live market data** — Parked by owner decision; live data stays as implemented in S-06.
- **Observability beyond default logging** — Parked: not required for the reviewer-ready release under the speed goal.
- **Personalized advice, automated trading, portfolio management, full-market analysis, real-time alerts, and guaranteed predictions** — Parked by the PRD Non-Goals.

## Milestone History

- **M-1: First explained market analysis** (`first-explained-market-analysis`) — closed 2026-10-23. An authenticated beginner selects assets and gets a ranked, explained analysis with market data, factors, a compact trend view, a grounded AI summary, and the educational boundary.
- **M-2: Dashboard and watchlist** (`dashboard-and-watchlist`) — closed 2026-11-12. A signed-in user keeps a personal watchlist, monitors it on a Dashboard, sees live market data, and explores curated strategy profiles copied into private simulated portfolios.

## Done
- **S-07: user can browse curated public strategy profiles, open one to see its holdings with descriptive explained factors, copy it into a private simulated model portfolio, and list and delete their own portfolios; every view shows the educational/simulation disclaimer and the data "as of" date.** — Archived 2026-11-12 → `context/archive/2026-10-27-strategy-profiles-simulation/`. Lesson: —.
- **S-06: user sees real current and historical market data for the supported assets in Analysis, Watchlist, and Dashboard instead of development data.** — Archived 2026-11-12 → `context/archive/2026-10-27-production-market-data/`. Lesson: —.
- **S-04: user can save assets to a personal watchlist, review and remove them, and start an analysis from the watchlist.** — Archived 2026-10-26 → `context/archive/2026-10-23-save-assets-to-watchlist/`. Lesson: —.
- **S-05: user can land on a Dashboard tab that shows watched assets with current price, 24h/7d/30d changes, compact trend, and top explained insights, and move between Dashboard, Analysis, and Watchlist tabs.** — Archived 2026-10-26 → `context/archive/2026-10-26-watchlist-monitoring-dashboard/`. Lesson: —.
- **S-02: user can select one or more supported assets and start a market analysis.** — Archived 2026-10-16 → `context/archive/2026-10-14-select-and-start-analysis/`. Lesson: —.
- **F-01: (foundation) the application has the minimum authentication and protected-route contract required for the MVP workflow.** — Archived 2026-10-02 → `context/archive/2026-10-01-minimal-auth-foundation/`. Lesson: —.
- **S-01: user can create an account, log in, and see a short explanation that Argus provides educational analysis rather than investment advice.** — Archived 2026-10-06 → `context/archive/2026-10-05-account-access-flow/`. Lesson: —.
- **F-02: (foundation) the analysis path has a verified source and evidence contract for current and historical data without committing to a provider-specific implementation.** — Archived 2026-10-07 → `context/archive/2026-10-06-market-data-access-contract/`. Lesson: —.
