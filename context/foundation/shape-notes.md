---
project: "Argus"
context_type: brownfield
product_type: web-app
target_scale:
  users: large
  qps: unknown
  data_volume: unknown
timeline_budget:
  delivery_weeks: 1
  hard_deadline: null
  after_hours_only: true
created: 2026-10-26
updated: 2026-10-26
checkpoint:
  current_phase: 8
  phases_completed: [1, 2, 3, 4, 5, 6, 7]
  gray_areas_resolved:
    - topic: "change category"
      decision: "major feature"
    - topic: "core of the change"
      decision: "strategy profiles + model portfolio + portfolio analysis, all as educational simulation"
    - topic: "access model"
      decision: "no change: Entra login, flat model"
    - topic: "first profile type"
      decision: "known public portfolios (curated, static, no filings pipeline)"
    - topic: "delivery time"
      decision: "1 week, scoped down; blast radius limited to new pages and data plus one nav tab"
  frs_drafted: 7
  quality_check_status: accepted
---

## Current System

Argus is an ASP.NET Core 10 Razor Pages web app with Entra ID login. Authenticated beginners select assets, get a ranked explained analysis (market data, factors, trend, grounded AI summary, educational boundary), save assets to a personal watchlist, and monitor them on a Dashboard. Market data comes from a development provider; the production provider is undecided (issue #1). Baseline PRD v1: `context/foundation/prd.md`; v1 shape notes archived in `context/foundation/archive/`.

## Vision & Problem Statement

Delta: add strategy profiles, a simulated model portfolio, and portfolio analysis as an educational simulation.

Pain / gap: beginners cannot see how real investors or funds allocate, nor test a portfolio idea risk-free; Argus only analyzes single assets today.

## User & Persona

Primary persona unchanged: a beginner or prospective first-time investor.

## Access Control

No changes planned — current model preserved. Entra login, flat model. Simulated portfolios are owned by the user who creates them.

## Success Criteria

### Primary

- User opens a known public portfolio profile, sees its holdings with explained factors, and copies it into a simulated model portfolio.

### Secondary

- Portfolio analysis with explained factors for the simulated portfolio.

### Guardrails

- Educational-not-advice boundary holds on every new view.
- Existing watchlist, analysis, and Dashboard behavior does not regress.

## Functional Requirements

- FR-001: User can browse a list of curated strategy profiles. Priority: must-have. Change: new
  > Socrates: No counter-argument; it stands as written.
- FR-002: User can open a profile and see its holdings with explained factors. Priority: must-have. Change: new
  > Socrates: Counter-argument considered: explained factors on someone else's holdings could read as a recommendation to copy. Resolution: kept; wording must stay descriptive.
- FR-003: User can copy a profile into a simulated model portfolio. Priority: must-have. Change: new
  > Socrates: Counter-argument considered: users may mistake the simulation for a real allocation or advice. Resolution: kept; simulation labeling is an NFR.
- FR-004: User can view and delete their own simulated portfolios. Priority: must-have. Change: new
  > Socrates: No counter-argument; it stands as written.
- FR-005: User can see an explained analysis of a simulated portfolio. Priority: nice-to-have. Change: new
  > Socrates: Counter-argument considered: portfolio-level scores may imply forecasts. Resolution: kept as nice-to-have; riskiest FR for the educational boundary.
- FR-006: User can keep using the existing watchlist, analysis, and Dashboard unchanged. Priority: must-have. Change: preserved
- FR-007: All new content stays educational with no advice or forecast language. Priority: must-have. Change: preserved

## User Stories

### US-01: User copies a public portfolio into a simulation

- **Given** a signed-in user on the strategy profiles list
- **When** they open a known public portfolio and choose to copy it
- **Then** they see its holdings with explained factors and get a simulated model portfolio labeled as a simulation

## Business Logic

Argus explains how a public strategy's holdings are composed and what a copied simulated portfolio looks like, using descriptive factors only, never advice or forecasts.

## Constraints & Preserved Behavior

- Existing watchlist storage, routes, and analysis presenters stay compatible.
- Profile data is static and versioned with an "as of" date; no data migration.
- Must not break: watchlist, analysis, Dashboard, educational-not-advice boundary.

## Non-Functional Requirements

- Every profile and portfolio view shows a visible educational/simulation disclaimer and the data's "as of" date.

## Non-Goals

- No real-money trading or allocation: simulation only.
- No live 13F / insider / congressional filings pipeline in v1: only curated known public portfolios.
- No advice or forecasts.
- No sharing of portfolios.

## Quality cross-check

All items present; no gaps. Open: whether the 1-week estimate holds for FR-005 (nice-to-have).
