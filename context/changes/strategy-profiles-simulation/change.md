---
change_id: strategy-profiles-simulation
title: Strategy profiles and simulated model portfolio
status: implemented
created: 2026-10-27
updated: 2026-10-27
---

## Notes

Roadmap S-07, PRD v2 (`context/foundation/prd-v2.md`) FR-001 to FR-004. Curated
static public-portfolio profiles under `/strategies/profiles`, profile detail
with descriptive explained factors, copy into a private simulated portfolio
(per-user Azure Table store, in-memory fake in tests), and list/delete under
`/strategies/portfolios`. FR-005 (portfolio analysis) is deferred. Profile data
is approximate example data and must be reviewed before release (PRD v2 Open
Questions 1 and 4).
