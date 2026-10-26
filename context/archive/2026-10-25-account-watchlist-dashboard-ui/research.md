---
change_id: account-watchlist-dashboard-ui
date: 2026-10-25
---

# Research: Account and watchlist dashboard UI

Target mockup: `mockup.png` (this folder). Variant: **existing design system**
(tokens in `wwwroot/css/site.css`, already dark; Razor partials under
`Pages/Analysis/`). No new design system, no new dependency.

## Findings

- Token source: `wwwroot/css/site.css:root` (`--color-*`, `--space-*`,
  `--radius-*`, `--workspace-*`, positive/negative/warning, asset accents).
  Hard-coded literal scan on `Pages/Account/Index.cshtml`,
  `Pages/Watchlist/Index.cshtml`, `Pages/Shared/_Layout.cshtml`: 0 hits.
- Real data already available for the mockup's watchlist table, without new
  providers: `MarketDataService` + `AnalysisService` give per-asset 24h, 7d and
  30d change (`Analysis/AnalysisContracts.cs:10-14`), a daily close series for
  sparklines (`AnalysisInsightPresenter.BuildSparkline`), current quote
  (`CurrentQuotes` in `Pages/Analysis/Index.cshtml.cs`), and asset glyph/name
  (`AssetIdentityCatalog`, `Pages/Analysis/_AssetGlyph.cshtml`).
- No data exists for: virtual portfolio, allocation, key metrics, recent
  activity, named lists (Tech Giants, ...), S&P 500 comparison, plan/upgrade,
  notifications, header search. These become labelled placeholders
  ("Przykładowe dane") — never presented as real results.
- Existing contracts to preserve: `AccountAccessContractTests` (claims
  rendered, `/account/signout`, `href="/analysis"`), `WatchlistContractTests`
  (nav hrefs, save/remove, empty state), analysis tests, disclaimer wording.

## Charges

1. **Missing tokens** — the app shell has no sidebar/top-bar/table/tab/metric
   tokens; `Pages/Shared/_Layout.cshtml:12-27` is a single top header, so the
   mockup's shell cannot be expressed without new semantic tokens. Impact:
   users get no persistent navigation between Analiza / Obserwowane / Konto.
2. **Missing shared component** — `_AssetGlyph` and the sparkline markup live
   under `Pages/Analysis/` (`_AssetGlyph.cshtml`, `_InsightCard.cshtml`); the
   watchlist table would duplicate them. Impact: inconsistent asset marks.
   Fix: move glyph and sparkline partials to `Pages/Shared/` and reuse.
3. **Accidental architecture** — `Pages/Account/Index.cshtml:1-20` is an
   unstyled English `<dl>` with the raw subject id and a bare log-out link;
   `Pages/Watchlist/Index.cshtml:36-72` lists only name and saved date, no
   price or change. Impact: the two most visited post-login views look
   unfinished next to `/analysis`. Fix: restructure both pages; present Polish
   copy; do not display the raw subject id prominently (keep it in markup for
   the existing test, e.g. a small "ID konta" row).
4. **Deferred** — real virtual portfolio, strategy profiles, named lists,
   S&P 500 comparison and notifications: no data source or PRD slice yet
   (roadmap S-05 owns the monitoring dashboard). Rendered as labelled
   placeholders only.
