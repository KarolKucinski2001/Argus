---
project: "Argus"
version: 2
status: draft
created: 2026-10-26
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
---

## Current System Overview

Argus is an ASP.NET Core 10 Razor Pages web app with Entra ID login. Authenticated beginners select assets, get a ranked explained analysis (market data, factors, trend, grounded AI summary, educational boundary), save assets to a personal watchlist, and monitor them on a Dashboard. Market data comes from a development provider; the production provider is undecided (issue #1). Baseline PRD v1: `context/foundation/prd.md`; v1 shape notes are archived in `context/foundation/archive/`.

## Problem Statement & Motivation

Delta: add strategy profiles, a simulated model portfolio, and portfolio analysis as an educational simulation.

Pain / gap: beginners cannot see how real investors or funds allocate, nor test a portfolio idea risk-free; Argus only analyzes single assets today.

## User & Persona

Primary persona unchanged: a beginner or prospective first-time investor. Existing users see one new area of the product; their current experience of watchlist, analysis, and Dashboard does not change.

## Success Criteria

### Primary

- User opens a known public portfolio profile, sees its holdings with explained factors, and copies it into a simulated model portfolio.

### Secondary

- Portfolio analysis with explained factors for the simulated portfolio.

### Guardrails

- Educational-not-advice boundary holds on every new view.
- Existing watchlist, analysis, and Dashboard behavior does not regress.

## User Stories

### US-01: User copies a public portfolio into a simulation

- **Given** a signed-in user on the strategy profiles list
- **When** they open a known public portfolio and choose to copy it
- **Then** they see its holdings with explained factors and get a simulated model portfolio labeled as a simulation

#### Acceptance Criteria
- Before this change: the user can only analyze single assets and save them to a watchlist.
- Every profile and portfolio view shows the educational/simulation disclaimer and the data's "as of" date.

## Scope of Change

- [new] FR-001: User can browse a list of curated strategy profiles. Priority: must-have.
  > Socrates: No counter-argument; it stands as written.
- [new] FR-002: User can open a profile and see its holdings with explained factors. Priority: must-have.
  > Socrates: Counter-argument considered: explained factors on someone else's holdings could read as a recommendation to copy. Resolution: kept; wording must stay descriptive.
- [new] FR-003: User can copy a profile into a simulated model portfolio. Priority: must-have.
  > Socrates: Counter-argument considered: users may mistake the simulation for a real allocation or advice. Resolution: kept; simulation labeling is an NFR.
- [new] FR-004: User can view and delete their own simulated portfolios. Priority: must-have.
  > Socrates: No counter-argument; it stands as written.
- [new] FR-005: User can see an explained analysis of a simulated portfolio. Priority: nice-to-have.
  > Socrates: Counter-argument considered: portfolio-level scores may imply forecasts. Resolution: kept as nice-to-have; riskiest FR for the educational boundary.
- [preserved] FR-006: User can keep using the existing watchlist, analysis, and Dashboard unchanged. Priority: must-have.
- [preserved] FR-007: All new content stays educational with no advice or forecast language. Priority: must-have.

## Constraints & Compatibility

- Existing watchlist storage, routes, and analysis presenters stay compatible.
- Profile data is static and versioned with an "as of" date; existing stored data is not changed.
- Preserved behavior (must not break): watchlist, analysis, Dashboard, educational-not-advice boundary.

## Non-Functional Requirements

- Every profile and portfolio view shows a visible educational/simulation disclaimer and the data's "as of" date.

## Business Logic Changes

Argus explains how a public strategy's holdings are composed and what a copied simulated portfolio looks like, using descriptive factors only, never advice or forecasts.

## Access Control Changes

No access control changes — current model preserved. Login, flat model. Simulated portfolios are owned by the user who creates them.

## Non-Goals

- No real-money trading or allocation: simulation only.
- No live 13F / insider / congressional filings pipeline in v1: only curated known public portfolios.
- No advice or forecasts.
- No sharing of portfolios.

## Open Questions

1. **Which known public portfolios are included in the first curated list, and who maintains their holdings and "as of" dates?** — Owner: user. Block: yes for FR-001/FR-002.
2. **Is a one-week delivery realistic once FR-005 (portfolio analysis) is included?** — Owner: user. Block: no; FR-005 is nice-to-have and can be cut.
3. **Do holdings in profiles need real market data, or is the development data acceptable until a production source is chosen?** — Owner: user. Block: no; depends on roadmap S-06 and GitHub issue #1.
4. **May the curated public-portfolio holdings be displayed under their sources' terms of use?** — Owner: user. Block: yes before release.
