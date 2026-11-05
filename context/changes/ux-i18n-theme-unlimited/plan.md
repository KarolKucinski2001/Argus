# Plan: Unlimited asset selection, sticky navigation, two-column cards, English and light mode

## Overview

Six user requests from 2026-11-05, delivered as one change because they share the
layout shell and the analysis workspace:

1. No limit on the number of assets selected for analysis (was 5).
2. The left sidebar stays fixed while scrolling.
3. On a classic desktop screen the analysis shows two result cards per row.
4. Watchlist shows "brak danych" although analysis has data for the same assets.
5. Language switch to English that works across the app.
6. Light theme toggle.

## Root cause (item 4)

`MarketDataService.NormalizeRequest` rejected any request with more than
`MaximumSelectedAssets` (5) assets with HTTP 400. The watchlist requests all saved
assets in one call, so a watchlist with 6+ entries got no data at all and every row
rendered "brak danych". Removing the cap (item 1) fixes both.

## Approach

- **Selection cap:** remove `MarketDataOptions.MaximumSelectedAssets`, the service
  check, the page validation, and the strategy-portfolio truncation. The allowed-asset
  list (17) and duplicate checks still bound a request. Live providers fetch assets
  sequentially, so the provider timeout scales per uncached asset
  (`MarketDataService.BatchTimeout`, capped at 60 s); per-asset failures still degrade
  to partial results and the 5-minute cache plus the request budget stay in place.
- **Layout:** `.app-shell__sidebar` is `position: sticky` with its own scroll on
  desktop (static below 64rem). The analysis picker is sticky at any desktop height with
  `--workspace-sidebar-max-height` and internal scroll. On ≥64rem the analysis page uses
  `--workspace-wide-width` and a compact 20rem picker so the results container passes the
  existing 46rem two-column container query.
- **Light theme:** `:root[data-theme="light"]` overrides colour tokens only; the brand
  mark uses `--brand-mark-filter`. The server renders `data-theme` from the
  `argus-theme` cookie, so there is no flash and no script.
- **English:** `Localization/Lang.cs` holds a request-scoped `AsyncLocal` language set by
  middleware from the `argus-lang` cookie (Polish default). Polish source strings are the
  keys; per-area English catalogs (`EnglishShell`, `EnglishAnalysis`,
  `EnglishWatchlistDashboard`, `EnglishStrategies`) supply translations and missing keys
  fall back to Polish. Presenters use `Lang.Culture` for number formatting.
- **Preferences:** `/preferences/Language` and `/preferences/Theme` POST handlers (with
  antiforgery) set one-year cookies and redirect to a local `returnUrl` only.

## Progress

### Phase 1: Selection cap and watchlist data

#### Automated

- [x] 1.1 Regression test: an 8-asset request is not capped (`Request_with_more_than_five_assets_is_not_capped`)
- [x] 1.2 Remove the cap from options, service, analysis page, portfolio analysis, config, copy and tests
- [x] 1.3 Scale the provider timeout per uncached asset (`Provider_timeout_scales_with_batch_size_up_to_a_cap`)

#### Manual

- [ ] 1.4 Watchlist with 6+ assets shows prices and changes locally and in production

### Phase 2: Layout

#### Automated

- [x] 2.1 Sticky app navigation and analysis picker; wide analysis workspace on desktop

#### Manual

- [ ] 2.2 (Automated evidence: Playwright at 1440 px on the Testing fixture shows two cards per row and the picker sticky after scrolling; the signed-in app-shell sidebar still needs a manual check.) On a 1366–1920 px screen the analysis shows two cards per row and both sidebars stay in place while scrolling

### Phase 3: Light theme and English

#### Automated

- [x] 3.1 Light theme tokens and server-rendered `data-theme`
- [x] 3.2 `Lang` core, preference handlers, layout switches and guard tests (`LocalizationTests`)
- [x] 3.3 English catalogs for shell/account, analysis, watchlist/dashboard and strategies with English rendering tests (plus `LocalizationSweepTests`: 13 pages rendered in English contain no Polish text)

#### Manual

- [ ] 3.4 User switches EN/PL and light/dark on every tab and reviews the English wording
