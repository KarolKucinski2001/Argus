# Watchlist monitoring on the Dashboard Implementation Plan

## Overview

Make `/dashboard` read the signed-in user's watchlist and show real, descriptive
monitoring data (price, 24h/7d/30d change, compact trend, top explained
insights), replacing the placeholder "Najważniejsze wnioski" card. It is a
composition change: all data and presenters already exist for `/watchlist` and
`/analysis`.

## Current State Analysis

Taken from `frame.md` (binding):

- `/dashboard` loads only the fixed market set (AAPL, MSFT, NVDA, BTC-USD,
  ETH-USD) and never reads `IWatchlistStore` (`Pages/Dashboard/Index.cshtml.cs`
  `LoadSnapshotsAsync`).
- `/watchlist` already derives price, 24h/7d/30d and sparkline per saved asset
  using one `IMarketDataService.GetAsync` call plus synchronous
  `IAnalysisService.Analyze` (`Pages/Watchlist/Index.cshtml.cs` `LoadAsync`,
  `LoadSnapshotsAsync`), rendered through `WatchlistTablePresenter`.
- The insights card shows `DashboardPresenter.SampleInsights` with the
  "Przykładowe dane" badge.
- Sidebar navigation and the Analysis/Watchlist links already exist.

## Desired End State

An authenticated user opening `/dashboard` sees, in order: greeting, welcome
card, **"Twoja lista obserwowanych"** (real rows with price, three changes,
sparkline, link to `/watchlist` and per-row link to `/analysis`), the fixed
market overview, **"Najważniejsze wnioski"** built from the user's saved assets
(descriptive why-bullet plus trend label, links to `/analysis`), the portfolio
and strategy placeholders (still badged), and the education disclaimer.
Empty watchlist shows a call to action to `/analysis`; storage or market-data
failure shows a friendly status and the rest of the page still renders.

### Key Discoveries:

- Row/snapshot building is currently inline in the Watchlist page model, so the
  Dashboard needs its own load method but must reuse `WatchlistTablePresenter`
  (`BuildRow`, `Change`, `PriceText`) and `AnalysisInsightPresenter.Present`
  (`WhyBullets`, `TrendLabel`, `Sparkline`).
- One market-data request can cover the union of watchlist assets and the fixed
  tiles, so the page makes a single provider call.
- Assets saved but no longer in `AllowedAssets` must be shown as unavailable and
  excluded from the request (same rule as `/watchlist`).

## What We're NOT Doing

- No real-time alerts, auto-refresh, background jobs or new storage (PRD non-goal).
- No AI summary generation on page load (synchronous `Analyze` only).
- No change to `/watchlist`, `/analysis` or the strategy/portfolio placeholders.
- No advice, forecast or buy/sell wording.
- No refactor of the Watchlist page model beyond what is needed to share logic.

## Implementation Approach

Add pure, unit-tested composition in `Dashboard/DashboardPresenter.cs`, wire the
page model to the watchlist store and a single market-data call, then update the
Razor view with token-backed classes already present in `site.css`.

Decisions made autonomously (documented, overrulable):

- "Top N" insights: at most 3 watched assets, ordered by absolute 7-day change
  descending, ties broken by asset id (ordinal); assets without analysis data are
  skipped.
- Watchlist card shows at most 5 rows ordered by `WatchlistPolicy.OrderEntries`,
  with a "Zobacz wszystkie" link when more exist.
- The fixed market overview stays, shown below the watchlist card.

## Phase 1: Dashboard data composition

### Overview

Pure presenter logic and page-model wiring; no markup change yet except what
keeps the build green.

### Changes Required:

#### 1. Presenter

**File**: `Dashboard/DashboardPresenter.cs`

**Intent**: Build the watchlist summary and insight lines from rows and analysis
output, and drop `SampleInsights`.

**Contract**: `BuildWatchlistRows(entries, allowedAssetIds, snapshots)` returns
up to 5 `WatchlistRowView`; `BuildInsights(IEnumerable<AssetInsightView>)`
returns up to 3 `DashboardInsightView(AssetId, Name, Text, TrendLabel)` using the
first why-bullet (fallback: takeaway) per the ordering rule above; `HasMoreRows`
flag. Wording remains descriptive.

#### 2. Page model

**File**: `Pages/Dashboard/Index.cshtml.cs`

**Intent**: Inject `IWatchlistStore`, list entries for the subject (`sub` or
`NameIdentifier`, as in `/watchlist`), request market data once for the union of
available watchlist assets and the fixed tile set, run sync `Analyze`, and expose
`WatchlistRows`, `Insights`, `WatchlistFailed`, `WatchlistEmpty`.

**Contract**: store failure → `WatchlistFailed=true`, no throw; market failure →
rows rendered with "brak danych" and existing market-unavailable status; page
stays `[AllowAnonymous]` with the manual OIDC challenge.

### Success Criteria:

#### Automated Verification:

- Solution builds: `dotnet build .bootstrap-scaffold.csproj --no-restore`
- Presenter unit tests (ordering, limits, tie-break, missing data) pass: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --configuration Release`

#### Manual Verification:

- Reviewing the diff shows no change to `/watchlist` or `/analysis` behavior

**Implementation Note**: After automated checks pass, continue to Phase 2 (user
is away; manual items are collected for the final report).

---

## Phase 2: Dashboard view

### Overview

Render the real watchlist card, real insights, empty and error states.

### Changes Required:

#### 1. Razor view

**File**: `Pages/Dashboard/Index.cshtml`

**Intent**: Add "Twoja lista obserwowanych" card (rows with `_AssetGlyph`, price,
three change cells, `_Sparkline`, links), replace the sample insight list with
real insights (no "Przykładowe dane" badge), add empty-state CTA to `/analysis`
and a failure status. Use full page names (`/Watchlist/Index`, `/Analysis/Index`).

**Contract**: only existing semantic classes and tokens; if a new primitive is
unavoidable add tokens/classes to `wwwroot/css/site.css` (CRLF, use `edit`). No
literal colors or arbitrary px/rem in the view. Rows carry `data-dashboard-row`,
insights `data-dashboard-insight` for tests.

### Success Criteria:

#### Automated Verification:

- Literal scan of `Pages/Dashboard/Index.cshtml` finds no colors or arbitrary sizes
- Contract tests for rows, empty state, storage failure, market failure and unavailable assets pass
- Whole auth test project passes in Release

#### Manual Verification:

- `/dashboard` at narrow and wide widths shows the watchlist card and insights without overlap or horizontal scroll
- Empty watchlist shows the call to action; saved assets show real prices

---

## Phase 3: Docs, roadmap and guardrails

### Overview

Keep guidance and roadmap accurate.

### Changes Required:

#### 1. Docs

**File**: `AGENTS.md`, `context/foundation/roadmap.md`

**Intent**: Update the `/dashboard` rule (real watchlist data, placeholders only
for portfolio and strategies) and set S-05 status per workflow.

**Contract**: AGENTS.md stays concise; roadmap changes only the S-05 status fields.

### Success Criteria:

#### Automated Verification:

- Wording test (no advice terms) covers new dashboard copy and passes
- Full auth test project passes in Release

#### Manual Verification:

- Final read-through of Dashboard copy confirms it is descriptive, not advice

---

## Testing Strategy

### Unit Tests:

- `BuildInsights`: limit 3, order by absolute 7d change, id tie-break, skips missing data
- `BuildWatchlistRows`: limit 5, unavailable assets flagged, missing snapshot → "brak danych"

### Integration Tests:

- `/dashboard` with saved assets renders rows and insights; empty store shows CTA; failing store and failing market data degrade gracefully; anonymous still gets 302

### Manual Testing Steps:

1. Save two assets on `/analysis`, open `/dashboard`, confirm rows match `/watchlist`.
2. Remove all assets, confirm the empty state.

## Performance Considerations

Single market-data call per page load, same cost as `/watchlist`; no AI call.

## Migration Notes

None; no stored data changes.

## References

- Frame: `context/changes/watchlist-monitoring-dashboard/frame.md`
- Pattern: `Pages/Watchlist/Index.cshtml.cs` (`LoadAsync`, `LoadSnapshotsAsync`)
- Presenters: `Watchlist/WatchlistTablePresenter.cs`, `Analysis/AnalysisInsightPresenter.cs`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Dashboard data composition

#### Automated

- [x] 1.1 Solution builds: `dotnet build .bootstrap-scaffold.csproj --no-restore` — 5c80324
- [x] 1.2 Presenter unit tests (ordering, limits, tie-break, missing data) pass — 5c80324

#### Manual

- [ ] 1.3 Reviewing the diff shows no change to `/watchlist` or `/analysis` behavior

### Phase 2: Dashboard view

#### Automated

- [x] 2.1 Literal scan of `Pages/Dashboard/Index.cshtml` finds no colors or arbitrary sizes — 5c80324
- [x] 2.2 Contract tests for rows, empty state, storage failure, market failure and unavailable assets pass — 5c80324
- [x] 2.3 Whole auth test project passes in Release — 5c80324

#### Manual

- [ ] 2.4 `/dashboard` at narrow and wide widths shows the watchlist card and insights without overlap or horizontal scroll
- [ ] 2.5 Empty watchlist shows the call to action; saved assets show real prices

### Phase 3: Docs, roadmap and guardrails

#### Automated

- [x] 3.1 Wording test (no advice terms) covers new dashboard copy and passes — 5c80324
- [x] 3.2 Full auth test project passes in Release — 5c80324

#### Manual

- [ ] 3.3 Final read-through of Dashboard copy confirms it is descriptive, not advice
