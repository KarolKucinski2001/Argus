# Autorun analysis dashboard with comparison-first insights — Implementation Plan

## Overview

Make the authenticated `/analysis` page open directly on a ranked dashboard of the four default assets and finish the comparison-first card design. A lighter header and summary strip help users compare assets quickly. Each card carries an asset-type badge, current price, a descriptive trend label and a short "Dlaczego miejsce #N?" explanation. These replace the developer-style "Szczegóły i metodologia" block.

## Current State Analysis

- `OnGet` (`Pages/Analysis/Index.cshtml.cs:35-46`) returns an empty selection. The analysis runs only in `OnPostAsync` (`:48-101`), so users land on configuration instead of results.
- Header: the `Nowa analiza` title uses `--workspace-title-size: 2rem` (`wwwroot/css/site.css:74`, rule `:407-409`). The visible `Dashboard analizy` h2 (`Index.cshtml:102`) is followed by a redundant `Ranking analizy` h2 (`Index.cshtml:110`, referenced by `aria-labelledby`). Both gaps are `--space-4` (`site.css:612-615`, `:682-686`).
- Card details (`Pages/Analysis/_InsightCard.cshtml:126-224`) is a collapsed `<details>` holding weights/contributions. It is also the **only** place showing current price (PRD AC-06, `prd.md:61`) and the summary (AC-05, `prd.md:60`).
- Identity: NVDA/TSLA render monograms (`_AssetGlyph.cshtml` default branch), and all equities share `AssetAccent.Equity` (`Analysis/AssetIdentityCatalog.cs:3-9, 20-23`). No asset-class badge exists, although `AssetReference.AssetClass` is available (`MarketData/MarketDataContracts.cs:3-6, 48-51`).
- Summary strip (`Analysis/AnalysisSummaryStrip.cs:14-49`) has count/best/worst/average. It has no "Najsilniejszy trend" stat.
- The sticky sidebar is already declared (`site.css:602-606`) and disabled at ≤64rem (`:1414-1421`).

## Desired End State

An authenticated GET to `/analysis` immediately shows ranked cards for AAPL, MSFT, BTC-USD and ETH-USD (those configured), with the checkboxes pre-checked and "Generuj ranking" still available for re-runs (FR-003).

- **Header:** a compact title with the subline `4 aktywa • Dane z 21.10.2026`, and a summary strip that adds "Najsilniejszy trend".
- **Each card shows:**
  - glyph, name, ticker and an `Akcje`/`Krypto` badge;
  - current price;
  - rank, score and band;
  - a trend label: `Trend wzrostowy`, `Trend mieszany` or `Trend spadkowy`;
  - chart and 3 factor bars;
  - the takeaway;
  - the summary;
  - a "Dlaczego miejsce #N?" list with 2–3 bullets.
- **No** weights/contributions and no `<details>` block.

Verified by tests plus screenshots of `/analysis/ui-state-fixture?view=ranked` at 1440/1920/390px.

### Key Discoveries:

- Analysis block to extract: `Index.cshtml.cs:57-100` (validation → request → `marketDataService.GetAsync` → `analysisService.AnalyzeAsync` → messages).
- `DirectionFor` ±1% thresholds: `Analysis/AnalysisInsightPresenter.cs` (`> 1m` Rising, `< -1m` Falling).
- `BuildTakeaway` names the factor with the largest absolute %. The why-bullets must not repeat that factor.
- `MarketDataFreshness.ObservedAt` (`MarketData/MarketDataContracts.cs:77-83`) is on every `AssetAnalysis.Freshness` (`Analysis/AnalysisContracts.cs:43`).
- `AnalysisSummaryResult.Text` can be up to 800 chars (`AnalysisSummaryContracts.cs`). `AnalysisExplanationBuilder` output is 1–2 sentences.
- Test anchors to update:
  - `AnalysisAccessContractTests.cs:212-256` (GET renders empty);
  - `:130-142`, `:320`, `:887` (`Ranking analizy`);
  - `:83-88`, `:329-371`, `:399-436`, `:761-765` (details/weights).
  - `:259-290` (empty config) must stay green.

## What We're NOT Doing

- No changes to `OpportunityScorer`, `AnalysisContracts`, `AnalysisService`, `MarketDataService` or providers.
- No BUY/SELL/Bullish/recommendation wording. Trend labels stay descriptive (PRD educational boundary).
- No auto-run on checkbox change. GET preloads once; re-runs remain an explicit "Generuj ranking" POST (FR-003).
- No page-render timestamp. The subline uses market data observation time only.
- No trademark logo files. Glyphs remain simple inline SVG shapes.
- No changes to the sticky behavior below 64rem (stacked layout stays intentional).
- No new external assets, JS frameworks or chart libraries. `analysis-form.js` is untouched.

## Implementation Approach

The work follows the 10x-ui order: behavior/lib → presenters (pure, unit-tested) → tokens → one view → state matrix. Presenters produce every derived string, so the Razor views only bind values and stay free of literals. The fixture page mirrors the live page and serves as the visual gate after each visual phase.

## Critical Implementation Details

- **Autorun selection:** defaults are `["AAPL","MSFT","BTC-USD","ETH-USD"]` intersected with `options.AllowedAssets` (configured order), then capped at `MaximumSelectedAssets`. If the intersection is empty, GET renders the existing empty state and does not call the service (keeps `:259-290` valid).
- **GET failures:** reuse the POST path. A provider failure sets `ProviderMessage` and keeps the selection checked. Production currently wires `UnavailableMarketDataProvider`, so production visitors will see the provider message on landing instead of an empty form. This is accepted and called out in the brief.
- **Trend label rule (majority):** among the available factors (24h, 7d, 30d), count `Rising` and `Falling` via `DirectionFor`.
  - Rising count > available/2 → `Trend wzrostowy`.
  - Falling count > available/2 → `Trend spadkowy`.
  - Otherwise → `Trend mieszany`.
  - No available factors → no label.
  - Boundary cases: {up, down} → mieszany; {up, up} → wzrostowy; {up, up, stable} → wzrostowy; {up, stable, down} → mieszany.

## Phase 1: Autorun on GET

### Overview

Run the same analysis on GET that POST runs, using the default selection.

### Changes Required:

#### 1. Page model

**File**: `Pages/Analysis/Index.cshtml.cs`

**Intent**: Move the validated-selection → market data → analysis → messages block into one private method used by both handlers. Make `OnGet` async: it preselects defaults and runs it.

**Contract**:
- `public async Task<IActionResult> OnGetAsync(CancellationToken)` replaces `OnGet`.
- `private async Task RunAnalysisAsync(CancellationToken)` sets `CurrentQuotes`, `AnalysisResults`, `HasStarted`, `StartedMessage` and `ProviderMessage` exactly as POST does today.
- Add a `private static readonly string[] DefaultAssetIds`.
- The authentication challenge stays first in both handlers.

#### 2. Tests

**File**: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

**Intent**: Replace the "GET renders empty" expectation with "GET renders ranked results with the defaults checked". Add a test that GET with a restricted config only preselects configured defaults, and keep the empty-config test.

**Contract**: test at `:212-256` is rewritten; `:259-290` stays unchanged and green.

### Success Criteria:

#### Automated Verification:

- Release build passes: `dotnet build .bootstrap-scaffold.csproj -c Release`
- Tests pass: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj -c Release`

#### Manual Verification:

- Signed-in GET `/analysis` in Development shows 4 ranked cards without clicking "Generuj ranking"

**Implementation Note**: Pause for manual confirmation after each phase's automated checks pass.

---

## Phase 2: Presenter logic

### Overview

Add every new derived value as pure presenter output with unit tests.

### Changes Required:

#### 1. Insight presenter

**File**: `Analysis/AnalysisInsightPresenter.cs`

**Intent**: Add the trend label, asset-class badge text and why-bullets to the card view model, so views only bind values.

**Contract**:
- `AssetInsightView` gains:
  - `string? TrendLabel` and `TrendDirection? Trend` (Rising/Mixed/Falling, for the accent class);
  - `string AssetClassLabel`: `Akcje` for Equity, `Krypto` for Crypto;
  - `IReadOnlyList<string> WhyBullets`;
  - `string WhyHeading`: `Dlaczego miejsce #N?`, or `Co wiemy o tym aktywie` when unranked.
- `Build` gains an optional `decimal? averageScore`.
- Bullets, at most 3:
  1. If score and average exist: `Wynik X — o Y pkt powyżej/poniżej średniej analizy`.
  2. For the remaining available factors, excluding the takeaway factor, in contribution order: a descriptive phrase per factor with its signed %, e.g. `Momentum 7d rośnie (+3,1%)`.
  3. If no factors are available: `Za mało danych rynkowych, aby uzasadnić pozycję.`
- Trend rule as in "Critical Implementation Details".
- Every string must pass the forbidden-word list.

#### 2. Summary strip

**File**: `Analysis/AnalysisSummaryStrip.cs`

**Intent**: Add the strongest rising 30d trend and the data date for the header subline.

**Contract**:
- `AnalysisSummaryStripView` gains `SummaryPerformer? StrongestTrend`: the max signed `trend-30d` % among non-error results, only when > 1%.
- It also gains `DateOnly? DataAsOf`: the max `Freshness.ObservedAt` UTC date among non-error results.
- Add a static `AssetCountLabel(int)`: 1 aktywo / 2–4 aktywa / 5+ aktywów (12–14 → aktywów).

#### 3. Identity catalog and glyphs

**Files**: `Analysis/AssetIdentityCatalog.cs`, `Pages/Analysis/_AssetGlyph.cshtml`

**Intent**: Give each known equity its own accent and add NVDA/TSLA SVG glyphs.

**Contract**:
- `AssetAccent` gains `Apple`, `Microsoft`, `Nvidia` and `Tesla`. `Equity` stays as the fallback for unknown equities.
- `AccentClass` maps them to `asset-accent--apple|microsoft|nvidia|tesla`.
- The glyph partial gets `NVDA` and `TSLA` cases (simple shapes, `aria-hidden`).

#### 4. Unit tests

**File**: `tests/Argus.Authentication.Tests/AnalysisInsightPresenterTests.cs` (plus a summary-strip test file if one exists)

**Intent**: Cover the trend-rule boundary cases, the takeaway factor never appearing in bullets, the bullet cap of 3, the unranked heading, badge labels, the strongest-trend >1% threshold, DataAsOf ignoring error results, Polish plurals (1, 2, 4, 5, 12, 22) and the accent classes.

### Success Criteria:

#### Automated Verification:

- Tests pass: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj -c Release`
- Presenter tests include all 4 trend boundary cases listed in "Critical Implementation Details"

#### Manual Verification:

- Bullet copy reviewed by the user for tone (descriptive, no advice)

---

## Phase 3: Tokens

### Overview

Add or adjust every visual value in `site.css :root` before the views change.

### Changes Required:

#### 1. Design tokens

**File**: `wwwroot/css/site.css`

**Intent**: Shrink the header and add tokens for the new card elements.

**Contract**:
- `--workspace-title-size` goes from 2rem to about 1.25rem.
- A new `--workspace-header-gap` replaces the `--space-4` gaps at `:612-615` and `:682-686`.
- Accent tokens `--accent-apple|microsoft|nvidia|tesla` (equities, near `:49-52`) with matching `.asset-accent--*` classes (near `:1074-1084`).
- Tokens for the badge (background, text, radius), the trend label (rising, mixed, falling, reusing existing positive/negative/neutral tokens where possible) and the why-list spacing.
- Utility `.visually-hidden` if one doesn't exist.

### Success Criteria:

#### Automated Verification:

- Build passes: `dotnet build .bootstrap-scaffold.csproj -c Release`
- No literal colors in views: `rg -n "#[0-9a-fA-F]{3,8}|rgb\(|hsl\(" Pages/Analysis` returns 0 hits

---

## Phase 4: View

### Overview

Bind the new presenter output in the page, card, strip and fixture.

### Changes Required:

#### 1. Page header

**Files**: `Pages/Analysis/Index.cshtml`, `Pages/Analysis/UiStateFixture.cshtml`

**Intent**: Keep a compact title, render the subline, and keep `Ranking analizy` only as an accessible name.

**Contract**:
- Subline: `{AssetCountLabel} • Dane z {dd.MM.yyyy}`. The date segment is omitted when `DataAsOf` is null.
- `#analysis-results-title` keeps its id and text but gets the `visually-hidden` class.
- The fixture mirrors the live markup.

#### 2. Insight card

**File**: `Pages/Analysis/_InsightCard.cshtml`

**Intent**: Remove the `<details>` block. Show price, badge and trend label in the header region, and the summary plus the why-list after the takeaway.

**Contract**:
- Keep the `analysis-result-card__*` classes.
- Current price uses the existing `CurrentQuotes` formatting.
- Summary keeps the existing unavailable/insufficient-evidence copy.
- Why-list markup: `<section aria-label>` with an `h4` and a `ul`.
- Weights/contributions markup is deleted.

#### 3. Summary strip and card callers

**Files**: `Pages/Analysis/_AnalysisSummaryStrip.cshtml` (or the current strip partial), the card caller in `Index.cshtml`

**Intent**: Add the "Najsilniejszy trend" stat, and pass the average score into `Build`.

**Contract**: the stat is omitted when `StrongestTrend` is null.

#### 4. Contract tests

**File**: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

**Intent**: Update the tests to the new markup.
- Price and summary are visible without `<details>` (AC-05/AC-06).
- No `waga`/`wkład` text.
- `Ranking analizy` present with `visually-hidden`.
- Badge, trend label, why-heading and subline are present.
- The forbidden-word scan still passes.

### Success Criteria:

#### Automated Verification:

- Tests pass: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj -c Release`
- Literal scan of `Pages/Analysis/*.cshtml` finds 0 literal colors or px layout values
- Screenshots captured at 1440/1920/390 via `.superpowers/sdd/shot2.mjs` against `/analysis/ui-state-fixture?view=ranked`

#### Manual Verification:

- At 1440px, the header + strip + first card row fit above the fold, and the 2×2 grid shows all 4 cards with minimal scrolling
- Cards no taller than the current 365px plus about 15%, despite the added price, summary and bullets
- User approves the visual result against the reference image

---

## Phase 5: States and finish

### Overview

Verify the 7-state matrix, the sticky sidebar on short viewports, and leave a rule for future agents.

### Changes Required:

#### 1. State verification and fixes

**Files**: `Pages/Analysis/UiStateFixture.cshtml`, `wwwroot/css/site.css` (only if fixes are needed)

**Intent**: Confirm each state renders sensibly with the new header and cards.

**States checked**:
- autorun success;
- partial results;
- per-asset error;
- provider failure on GET;
- validation error on POST;
- empty config;
- single asset.

If the sidebar is taller than a 768px viewport, cap it with a token-based `max-height` and internal scroll so "Generuj ranking" stays reachable.

#### 2. Agent rule

**File**: `AGENTS.md` (outside the 10x-cli block)

**Intent**: Record that analysis card copy and derived labels come from the presenters in `Analysis/`, and that views only bind them. Trend wording stays descriptive, never advice.

### Success Criteria:

#### Automated Verification:

- Full tests pass: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj -c Release`
- Forbidden-string scan over `Pages/` and `Analysis/` finds none of: `chwilowo niedostępne`, `prognoz`, `buy`, `guaranteed`, `best investment`, `top opportunity`

#### Manual Verification:

- At a 1440×768 viewport, the sticky sidebar keeps "Generuj ranking" reachable while the cards scroll
- Each of the 7 states is reviewed in the fixture or live page and accepted by the user

---

## Testing Strategy

### Unit Tests:

- Trend rule boundaries, why-bullet composition and cap, takeaway-factor exclusion, unranked heading, badge text
- Strongest-trend threshold, DataAsOf ignoring errors, Polish plural forms, accent mapping

### Integration Tests:

- Authenticated GET autoruns with the defaults checked; restricted config preselects only the configured defaults; empty config makes no service call
- Rendered card contains price, summary, badge, trend label and why-list, with no `<details>`/weights

### Manual Testing Steps:

1. Start the Testing fixture server (recipe in research/session notes) and open `/analysis/ui-state-fixture?view=ranked` at 1440, 1920 and 390px.
2. Sign in on Development, open `/analysis`, and confirm the results appear without a click; change the selection and press "Generuj ranking".
3. Resize to 1440×768, scroll the cards, and confirm the sidebar stays put and the button is reachable.

## Performance Considerations

Every authenticated GET now calls the market data service. The existing 5-minute cache and 100 req/min process-local budget absorb repeat visits; no extra caching is added.

## Migration Notes

None — no persisted data or configuration changes.

## References

- Research: `context/changes/analysis-dashboard-autorun-insights/research.md`
- PRD: `context/foundation/prd.md:60-61` (AC-05/AC-06), `:70-71` (FR-003)
- Prior UI change: `context/changes/analysis-workspace-ui-refinement/`
- Decisions: session file `ui-round3-decisions.md` (autorun, why-section, descriptive signal)

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles. See `references/progress-format.md`.

### Phase 1: Autorun on GET

#### Automated

- [x] 1.1 Release build passes
- [x] 1.2 Tests pass

#### Manual

- [ ] 1.3 Signed-in GET /analysis shows 4 ranked cards without clicking Generuj ranking

### Phase 2: Presenter logic

#### Automated

- [ ] 2.1 Tests pass
- [ ] 2.2 Presenter tests include all 4 trend boundary cases

#### Manual

- [ ] 2.3 Bullet copy reviewed by the user for tone

### Phase 3: Tokens

#### Automated

- [ ] 3.1 Build passes
- [ ] 3.2 No literal colors in views

### Phase 4: View

#### Automated

- [ ] 4.1 Tests pass
- [ ] 4.2 Literal scan of Pages/Analysis finds 0 literal colors or px layout values
- [ ] 4.3 Screenshots captured at 1440/1920/390

#### Manual

- [ ] 4.4 At 1440px header, strip and first card row fit above the fold
- [ ] 4.5 Cards no taller than 365px plus about 15%
- [ ] 4.6 User approves the visual result against the reference image

### Phase 5: States and finish

#### Automated

- [ ] 5.1 Full tests pass
- [ ] 5.2 Forbidden-string scan finds none

#### Manual

- [ ] 5.3 At 1440x768 the sticky sidebar keeps Generuj ranking reachable
- [ ] 5.4 Each of the 7 states reviewed and accepted by the user
