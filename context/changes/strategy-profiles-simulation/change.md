---
change_id: strategy-profiles-simulation
title: Strategy profiles and simulated model portfolio
status: impl_reviewed
created: 2026-10-27
updated: 2026-11-02
---

## Notes

Roadmap S-07, PRD v2 (`context/foundation/prd-v2.md`) FR-001 to FR-004. Curated
static public-portfolio profiles under `/strategies/profiles`, profile detail
with descriptive explained factors, copy into a private simulated portfolio
(per-user Azure Table store, in-memory fake in tests), and list/delete under
`/strategies/portfolios`. FR-005 is delivered as a composition-based explained
analysis at `/strategies/portfolios/{id}/analysis` (concentration via effective
number of positions, sector/class shares, coverage of Argus-supported assets).
Profile data
is approximate example data and must be reviewed before release (PRD v2 Open
Questions 1 and 4).
