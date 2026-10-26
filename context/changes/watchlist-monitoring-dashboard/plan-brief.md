# Watchlist monitoring on the Dashboard — Plan Brief

> Full plan: `context/changes/watchlist-monitoring-dashboard/plan.md`
> Frame brief: `context/changes/watchlist-monitoring-dashboard/frame.md`

## What & Why

`/dashboard` does not read the user's watchlist; the data and presenters already
exist in `/watchlist` and the analysis service, so S-05 is a wiring and
composition change, not a new feature.

## Starting Point

`/dashboard` shows a fixed five-asset market overview and placeholder cards
(portfolio, insights, strategies). `/watchlist` already computes price, 24h/7d/30d
and sparkline for saved assets.

## Desired End State

The Dashboard leads with the user's real watched assets and descriptive insights
from their analysis, with empty and failure states, and keeps the market overview
and the badged placeholders for portfolio and strategies.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Scope | Compose existing data, no new storage | Everything needed already exists | Frame |
| Insights | Top 3 by absolute 7d change, id tie-break, first why-bullet | Makes "top" well-defined and descriptive | Plan (default) |
| Rows | Max 5, link to `/watchlist` for the rest | Keeps the dashboard compact | Plan (default) |
| Market overview | Kept below the watchlist | Already shipped, harmless | Frame |
| Alerts/refresh/AI | Excluded | PRD non-goal; keeps load cheap | Frame |

## Scope

**In scope:** presenter, page model, view, tests, AGENTS.md and roadmap status.

**Out of scope:** alerts, auto-refresh, AI summaries, changes to `/watchlist` or `/analysis`, real strategy data.

## Architecture / Approach

Page model lists the user's entries, makes one market-data call for the union of
watched and tile assets, runs sync analysis, and hands results to pure presenters
unit-tested under `Dashboard/`; Razor only binds.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Data composition | Presenter + page-model wiring | Diverging from `/watchlist` logic |
| 2. View | Real cards, empty/error states | Layout regressions, literals in views |
| 3. Docs | AGENTS.md, roadmap | Stale guidance |

**Prerequisites:** S-04 watchlist (done).
**Estimated effort:** ~1 session, 3 small phases.

## Open Risks & Assumptions

- Defaults above were chosen while the user was away; they may overrule them.
- Production market-data provider is still undecided (issue #1); development provider is used.

## Success Criteria (Summary)

- Saved assets appear on `/dashboard` with real data; empty and failure states are friendly.
- All tests green, no literals in views, copy stays descriptive.
