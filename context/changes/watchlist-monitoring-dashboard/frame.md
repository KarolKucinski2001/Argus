# Frame Brief: Watchlist monitoring on the Dashboard

> Framing step before /10x-plan. Run autonomously (user away): the
> clarifying questions were answered by documented defaults, listed under
> "Narrowing Signals" so the user can overrule them.

## Reported Observation

Roadmap S-05 says the user should "land on a Dashboard tab that shows watched
assets with current price, 24h/7d/30d changes, compact trend, and top
explained insights, with tab navigation to Analysis and Watchlist".
After the previous change, `/dashboard` exists but its watchlist-related
blocks are placeholders.

## Initial Framing (preserved)

- **User's stated cause or approach**: S-05 is a new, separate slice that
  builds a Dashboard tab for monitoring the watchlist.
- **User's proposed direction**: Option A, "real watchlist/monitoring data",
  so Dashboard and list stop showing example data.
- **Pre-dispatch narrowing**: not asked (user unavailable); defaults below.

## Dimension Map

1. **Navigation / tab** — Dashboard tab with links to Analysis and Watchlist.
2. **Watched-assets data on the Dashboard** — price, 24h/7d/30d, sparkline for
   the user's own saved assets.  ← initial framing
3. **Top explained insights** — descriptive insight lines from analysis.
4. **Market overview scope** — fixed assets vs. watchlist only (roadmap unknown).
5. **"Monitoring" semantics** — refresh, alerts, history (PRD non-goal risk).

## Hypothesis Investigation

| Hypothesis | Evidence | Verdict |
| --- | --- | --- |
| 1. Navigation is missing | Sidebar links Dashboard/Analiza/Strategy Profiles/Obserwowane in `Pages/Shared/_Layout.cshtml`; `/dashboard` routed in `Pages/Dashboard/Index.cshtml.cs` | NONE (already done) |
| 2. Watched-asset data missing on Dashboard | `/watchlist` already shows price, 24h/7d/30d and sparkline per saved asset (`Pages/Watchlist/Index.cshtml.cs`: `ListAsync` line ~84, `BuildMovers` ~111, sync `Analyze` ~148). `/dashboard` loads only the fixed `AllowedAssets` set (`Pages/Dashboard/Index.cshtml.cs`, `LoadSnapshotsAsync`) and never reads the watchlist store | STRONG (real gap) |
| 3. Insights are placeholders | Dashboard "Najważniejsze wnioski" card is labelled "Przykładowe dane" (`Pages/Dashboard/Index.cshtml` ~line 74); real why-bullets exist in `AnalysisInsightPresenter` and sync `IAnalysisService.Analyze` returns them | STRONG (real gap) |
| 4. Market overview scope conflicts with the non-goal | Fixed 5-asset tile row already ships; PRD forbids full-market analysis, 5 fixed allowed assets do not violate it | WEAK |
| 5. Needs alerts/refresh | PRD lists real-time alerts as a non-goal; no background infrastructure exists (roadmap baseline) | NONE |

## Narrowing Signals

Defaults chosen autonomously (user may overrule):

- Keep the fixed market overview as a secondary row; put "Twoja lista
  obserwowanych" first on the Dashboard.
- Insights come from existing descriptive analysis output of the user's saved
  assets only; no new data source, no AI call on page load.
- No alerts, no auto-refresh, no new storage.
- Empty watchlist shows a CTA to /analysis, not placeholders.

## Cross-System Convention

Dashboards in comparable products summarise the user's list first and link to
detail pages; reuse of the list presenter avoids two divergent renderings.

## Reframed (or Confirmed) Problem Statement

> **The actual problem to plan around is**: `/dashboard` does not read the
> user's watchlist; the data and presenters needed already exist in
> `/watchlist` and the analysis service, so S-05 is a wiring and composition
> change, not a new feature.

Navigation and tabs are done. The work is to feed the Dashboard from the
watchlist store, reuse `WatchlistTablePresenter` and analysis why-bullets, and
replace placeholder cards for watchlist and insights with real or empty-state
content.

## Confidence

**HIGH** — code evidence on both sides (Dashboard ignores the store;
Watchlist already computes everything required).

## What Changes for /10x-plan

Plan a small change: Dashboard watchlist summary (real rows, trend, empty
state, error state), real insights from sync analysis of saved assets, keep
placeholders only for portfolio and strategy cards, update roadmap S-05 to
reflect reduced scope, extend tests and docs.

## References

- `Pages/Dashboard/Index.cshtml(.cs)`, `Dashboard/DashboardPresenter.cs`
- `Pages/Watchlist/Index.cshtml.cs`, `Watchlist/WatchlistTablePresenter.cs`
- `Analysis/IAnalysisService`, `Analysis/AnalysisInsightPresenter`
- `context/foundation/roadmap.md` S-05
