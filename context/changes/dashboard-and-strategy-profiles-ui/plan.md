# Dashboard and Strategy Profiles UI Implementation Plan

## Overview

Add `/dashboard` and `/strategies` Razor Pages to the authenticated shell,
matching the top row of `mockup.png`. Reuse the shell, tokens and `watchlist-*`
primitives from `account-watchlist-dashboard-ui`. Placeholders are labelled
"Przykładowe dane"; copy is Polish, descriptive, never advice.

## Desired End State

- Sidebar (authenticated shell in `Pages/Shared/_Layout.cshtml`): Dashboard
  -> `/dashboard`, Analiza, Strategy Profiles (-> `/strategies`, new),
  Obserwowane, Konto. Active highlighting works for the new links.
- `/dashboard`: greeting with the user's first name, welcome card with CTA to
  `/strategies`, market overview tiles (real data via the market data service
  for a fixed set of assets, with sparklines; graceful fallback when
  unavailable), virtual-portfolio card (placeholder), top insights
  (placeholder), strategy profile tiles linking to `/strategies`, educational
  disclaimer card.
- `/strategies`: header, row of profile cards (Large Funds, Company Insiders,
  Congress Disclosures, Broad Market Benchmark, two neutral public-figure
  examples labelled as examples), allocation builder card (sliders rendered
  as static tokenised bars, donut, "Zapisz alokację" disabled), portfolio
  overview metrics (placeholder), holdings table (placeholder), data quality
  and limits strip. All placeholder blocks carry the badge.
- No literal colors or arbitrary px/rem in views; tokens in `wwwroot/css/site.css`.

## What We're NOT Doing

Real strategy data, persistence, simulation, Model Portfolio Builder,
Portfolio Analysis, search behaviour, changes to existing pages.

## Phase 1: Navigation and /dashboard

### Overview

Update sidebar links, add `Pages/Dashboard/Index.cshtml(.cs)` and a presenter
`Dashboard/DashboardPresenter.cs` (greeting/first name, market tile
formatting; unit-tested). Authorized like `/watchlist`. Use synchronous
`IAnalysisService.Analyze` / existing market data service patterns from
`Pages/Watchlist/Index.cshtml.cs`; never call the AI summary generator.

### Success Criteria

#### Automated
- Auth test project (Release) green incl. new contract tests: anonymous 302,
  authenticated 200, sidebar hrefs `/dashboard` and `/strategies`, badge shown.
- Literal scan on changed views empty.

## Phase 2: /strategies

### Overview

Add `Pages/Strategies/Index.cshtml(.cs)` and `Strategies/StrategyProfileCatalog.cs`
(static placeholder data + presenter helpers, unit-tested). Allocation weights
must sum to 100 in the sample data (asserted by a test).

### Success Criteria

#### Automated
- Contract tests: anonymous 302, authenticated 200, all profile names and
  "Przykładowe dane" present, no advice wording (`kup`, `sprzedaj`, `buy`, `sell`).
- Literal scan empty.

## Phase 3: States and guardrails

### Overview

Testing-only fixture `/strategies/ui-states` (pattern: `Pages/Watchlist/UiStateFixture`)
with the seven states; extend the AGENTS.md UI rule to mention `/dashboard`
and `/strategies`; full test suites.

### Success Criteria

#### Automated
- Full suites green; production project test command exit 0.

#### Manual
- Visual match with mockup at desktop and mobile widths.

## Progress

### Phase 1: Navigation and /dashboard

#### Automated
- [x] 1.1 Contract and presenter tests pass for nav and /dashboard
- [x] 1.2 Literal scan on changed views is empty

#### Manual
- [ ] 1.3 /dashboard matches the mockup at desktop and one mobile width

### Phase 2: /strategies

#### Automated
- [x] 2.1 Catalog and contract tests pass
- [x] 2.2 Literal scan on changed views is empty

#### Manual
- [ ] 2.3 /strategies matches the mockup at desktop and one mobile width

### Phase 3: States and guardrails

#### Automated
- [x] 3.1 State fixture tests and full suites pass
- [x] 3.2 AGENTS.md UI rule extended

#### Manual
- [ ] 3.3 Final visual review of both views
