# Account and watchlist dashboard UI Implementation Plan

## Overview

Restyle `/account` and `/watchlist` to match `mockup.png`: authenticated app
shell with left sidebar and top bar, a data-rich watchlist table, and an
account dashboard. Real data is used wherever the app already has it
(profile claims, watchlist rows, price, 24h/7d/30d change, sparkline, movers).
Everything else is a visibly labelled placeholder ("Przykładowe dane").
Existing design system and tokens are extended; no new dependency.

## Current State Analysis

See `research.md` (Charges 1-4). Tokens exist in `wwwroot/css/site.css`; the
layout is a top header only; `/account` is unstyled; the watchlist lists names
and save dates only. Analysis services already provide price, 24h/7d/30d
change and chart points.

## Desired End State

- Authenticated pages render in a shell: sidebar (Dashboard placeholder link
  to `/analysis`, Analiza, Obserwowane, Konto, Wyloguj), top bar with search
  placeholder and user chip. Anonymous pages keep the current header.
- `/watchlist`: header with "Dodaj aktywo" (link to `/analysis`), list tabs
  (active "Moja lista" with real count; other tabs placeholders), table with
  glyph, symbol, name, last price, 24h/7d/30d change, sparkline, remove action,
  selection checkboxes and "Generuj ranking" (existing handoff, max 5),
  Top movers from real 24h data, performance-chart placeholder, disclaimer.
- `/account`: profile card (name, email, initials, member-info placeholder),
  overview, virtual portfolio, allocation, key metrics, recent activity and
  quick actions; only profile is real, the rest labelled as examples.
- Views contain no literal colors or arbitrary layout values; copy is Polish
  and descriptive, never advice.

### Key Discoveries

- `Pages/Analysis/Index.cshtml.cs` already orchestrates market data and analysis.
- `AnalysisInsightPresenter` builds sparklines; reuse, do not reimplement.
- `Pages/Analysis/UiStateFixture.cshtml` is the existing seven-state fixture pattern.

## What We're NOT Doing

New data providers, real portfolio or strategy logic, persistence of lists,
search behavior, notifications, dark/light switching, changes to `/analysis`
visuals beyond moving shared partials.

## Implementation Approach

Phase by phase: shell and tokens, shared partials, watchlist, account, states
and guardrails. Presentation logic lives in presenters under `Watchlist/` with
unit tests; Razor binds results only.

## Phase 1: App shell and shared partials

### Overview

Introduce shell tokens and the authenticated layout; relocate glyph and
sparkline partials to `Pages/Shared/`.

### Changes Required:

**Files**: `Pages/Shared/_Layout.cshtml`, `Pages/Shared/_AssetGlyph.cshtml`,
`Pages/Shared/_Sparkline.cshtml` (new), `Pages/Analysis/*.cshtml` (partial
references), `wwwroot/css/site.css`.

**Contract**: Authenticated users see sidebar + top bar; anonymous users see
the existing header and landing unchanged. Nav hrefs stay `/analysis`,
`/watchlist`, `/Account`, `/Account/SignOut`. Mobile: sidebar collapses to a
top strip in CSS. Focus-visible uses the focus token.

### Success Criteria:

#### Automated Verification:

- Build passes; focused test suite passes, including nav href assertions.

#### Manual Verification:

- Shell looks right at desktop and one mobile width.

## Phase 2: Watchlist view

### Overview

Rebuild `/watchlist` per mockup with real data and labelled placeholders.

### Changes Required:

**Files**: `Pages/Watchlist/Index.cshtml(.cs)`, `Watchlist/WatchlistTablePresenter.cs`
(new), `wwwroot/css/site.css`, tests.

**Contract**: Rows show price, 24h/7d/30d (formatted with sign, positive and
negative tokens), sparkline; missing data shows a safe "brak danych" state and
never fails the page; storage failure keeps the explicit error. Remove,
selection (1-5), "Generuj ranking" and empty/unavailable states keep current
behavior. Top movers computed from real 24h values (gainers/losers). Presenter
unit tests cover formatting, ordering, missing data.

### Success Criteria:

#### Automated Verification:

- Presenter unit tests and updated watchlist contract tests pass.
- Full focused suite and production project test command pass.

#### Manual Verification:

- Table reads like the mockup; remove and ranking handoff still work.

## Phase 3: Account view

### Overview

Rebuild `/account` per mockup; profile real, the rest labelled placeholders.

### Changes Required:

**Files**: `Pages/Account/Index.cshtml(.cs)`, `wwwroot/css/site.css`, tests.

**Contract**: Subject id, display name, email, `/account/signout` and
`href="/analysis"` remain present. Placeholder cards carry a visible
"Przykładowe dane" badge and a note that no real portfolio exists. No advice or
forecast wording.

### Success Criteria:

#### Automated Verification:

- Account contract tests pass, plus a test asserting the placeholder badge.

#### Manual Verification:

- Account looks like the mockup at desktop and mobile widths.

## Phase 4: States, guardrails, documentation

### Overview

Seven-state coverage, agent rule, and verification.

### Changes Required:

**Files**: `Pages/Watchlist/UiStateFixture.cshtml` (Development only, mirroring
the analysis fixture), `AGENTS.md` (UI rule outside the 10x CLI block),
`wwwroot/css/site.css`.

**Contract**: default, hover, focus-visible, disabled, error, empty, loading
shown in the fixture or marked N/A with reason. Hard-coded value scan on the
changed views shows 0 hits.

### Success Criteria:

#### Automated Verification:

- Full test suites pass; literal scan on changed views returns nothing.

#### Manual Verification:

- Final visual review of both views and the fixture.

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: App shell and shared partials

#### Automated

- [x] 1.1 Build and focused tests pass with the new shell and relocated partials — d5b0d33
- [x] 1.2 Hard-coded value scan on layout and partials finds no literals — d5b0d33

#### Manual

- [ ] 1.3 Shell renders correctly at desktop and one mobile width

### Phase 2: Watchlist view

#### Automated

- [x] 2.1 Presenter unit tests cover formatting, mover ordering, and missing data — 8003320
- [x] 2.2 Watchlist contract tests pass for table data, remove, empty, and handoff — 8003320
- [x] 2.3 Focused suite and production project test command pass — 8003320

#### Manual

- [ ] 2.4 Watchlist matches the mockup and its remove and ranking flows work

### Phase 3: Account view

#### Automated

- [x] 3.1 Account contract tests pass, including the placeholder badge assertion

#### Manual

- [ ] 3.2 Account matches the mockup at desktop and one mobile width

### Phase 4: States, guardrails, documentation

#### Automated

- [ ] 4.1 Full test suites pass and the literal scan on changed views is empty
- [ ] 4.2 UI rule added to AGENTS.md outside the 10x CLI block

#### Manual

- [ ] 4.3 Final visual review of both views and the state fixture
