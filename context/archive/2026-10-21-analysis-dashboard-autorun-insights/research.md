---
date: 2026-10-21T13:11:19+02:00
researcher: Karol Kuciński (with GitHub Copilot CLI)
git_commit: 1535f2ee0d1bc746efe745ddcae1ac2c48c3e15f
branch: feature/insight-dashboard
repository: KarolKucinski2001/Argus
topic: "10x-ui audit of /analysis for autorun, comparison-first insights (analysis-dashboard-autorun-insights)"
tags: [research, ui, analysis, razor-pages, insight-card, summary-strip, autorun]
status: complete
last_updated: 2026-10-21
last_updated_by: Karol Kuciński (with GitHub Copilot CLI)
---

# Research: 10x-ui audit of `/analysis` for autorun and comparison-first insights

**Date**: 2026-10-21T13:11:19+02:00
**Researcher**: Karol Kuciński (with GitHub Copilot CLI)
**Git Commit**: 1535f2ee0d1bc746efe745ddcae1ac2c48c3e15f
**Branch**: feature/insight-dashboard (PR #9, still open at research time; this change starts after it merges)
**Repository**: KarolKucinski2001/Argus

## Research Question

For the one view `/analysis` (token source `wwwroot/css/site.css :root`), what has to change, and where, to deliver the open items and decisions recorded in `change.md`? The items are:
- autorun with the 4 preselected assets;
- a smaller sidebar title and a tighter content header, without "Ranking analizy";
- a "N aktywa • Ostatnia aktualizacja" subline;
- a working sticky panel;
- "Dlaczego miejsce #N?" instead of "Szczegóły i metodologia";
- a descriptive trend label;
- Akcje/Krypto badges;
- NVDA/TSLA glyphs with distinct accents;
- "Najsilniejszy trend" in the summary strip.

The audit runs in two directions (source → views, view → source) and produces 3–5 charges.

## Summary

- **Autorun is feasible in the page model only.**
  - Today `OnGet()` loads assets and returns an empty page (`Pages/Analysis/Index.cshtml.cs:35-46`). All analysis happens in `OnPostAsync()` (`:48-101`).
  - The request/market-data/analysis block can be extracted into one shared private method that both GET (with defaults) and POST call. No change to `OpportunityScorer`, `AnalysisContracts` or `AnalysisService` is needed.
  - Both `appsettings.json:12-25` and `appsettings.Development.json:12-25` configure exactly the 4 requested assets.
  - One existing test encodes the empty GET state and will need rewriting (`AnalysisAccessContractTests.cs:212-256`, asserts at `:246-247`).
- **The PRD needs a note, not a reversal.**
  - FR-003 keeps a separate start action. The rejected counter-argument was "start automatically after asset selection" (`context/foundation/prd.md:70-72`).
  - A preloaded default dashboard on GET still leaves "Generuj ranking" as the explicit start action for any other selection. The plan should record this reading.
- **"Szczegóły i metodologia" cannot simply be deleted.** The block (`Pages/Analysis/_InsightCard.cshtml:126-224`) is the only place on the card that renders:
  - current price (`:130-133`), which PRD AC-06 requires (`prd.md:61`);
  - the generated/fallback summary (`:195-222`), which PRD AC-05 requires (`prd.md:60`).
  - The replacement must move these out of the block, or keep them in a collapsed section, while the weights/contributions list (`:170-191`) becomes the "Dlaczego miejsce #N?" bullets.
- **All inputs for the new insights already exist:**
  - Asset class: `AssetReference.AssetClass` (`MarketData/MarketDataContracts.cs:3-6, 48-51`).
  - Per-factor direction: `AnalysisInsightPresenter.DirectionFor` (`Analysis/AnalysisInsightPresenter.cs:110-119`).
  - The 30-day trend factor, per result in `AssetAnalysis.Factors` (`Analysis/AnalysisContracts.cs:18-26`).
  - Missing: an aggregate trend-label rule and a "Najsilniejszy trend" stat.
- **The sticky sidebar is already declared** (`wwwroot/css/site.css:602-606`).
  - No inspected ancestor (`body` `site.css:96-101`, `.landing-main` `:114-117`, `_Layout.cshtml:34`, `.analysis-workspace` `:593-599`) sets overflow, contain or transform, so it should stick on viewports wider than 64rem.
  - It is deliberately disabled at ≤64rem (`site.css:1414-1421`).
  - Remaining risk: a sidebar taller than the viewport (e.g. a 768px-high laptop) sticks with its bottom, the "Generuj ranking" button, cut off. Shrinking the title helps; it needs a browser check.
- **The literal scan of `Pages/Analysis/*.cshtml` found 0 hits per file** (regex `#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(|oklch\(|style="`, 7 files). `AGENTS.md:28` already holds the analysis-UI rule (semantic tokens, fixture, split workspace), so no rule causes drift.

## Charges

| # | Category | Evidence (file:line) | User impact | Planned fix |
|---|---|---|---|---|
| C1 | Accidental architecture | `Pages/Analysis/Index.cshtml.cs:35-46` (GET returns no results); `Index.cshtml:105-147` renders cards only when `HasResults` | A logged-in user landing on `/analysis` sees an empty dashboard and must configure before seeing any value | GET preselects AAPL/MSFT/BTC-USD/ETH-USD and runs the shared analysis method; POST unchanged |
| C2 | Missing tokens / hierarchy | `--workspace-title-size: 2rem` (`site.css:74`) used by `.analysis-workspace__title` (`site.css:407-409`); redundant `<h2 id="analysis-results-title">Ranking analizy</h2>` (`Index.cshtml:110`, fixture `UiStateFixture.cshtml:36-37`); content gap `--space-4` 1.5rem (`site.css:612-615`) and results gap 1.5rem (`site.css:682-686`) | The sidebar title outweighs the dashboard and stacked headings push cards below the fold | Smaller title token; remove the visible "Ranking analizy" (keep the accessible section label); tighten gap tokens; add a "N aktywa • Ostatnia aktualizacja" subline |
| C3 | Accidental architecture | `<details>` "Szczegóły i metodologia" `_InsightCard.cshtml:126-224`: weights/contributions list `:170-191`; current price only at `:130-133`; summary only at `:195-222` | Users get developer documentation (weights, contributions) instead of "why is this asset #N"; required content (AC-05 summary, AC-06 current price) is hidden | Replace with "Dlaczego miejsce #N?" (2–3 Polish bullets derived from factors/score/quality); surface current price and keep the summary reachable |
| C4 | Missing shared identity / component data | NVDA/TSLA fall back to monogram (`_AssetGlyph.cshtml:26-28`); all 4 equities share `AssetAccent.Equity` (`Analysis/AssetIdentityCatalog.cs:20-21`, classes `site.css:1074-1084`, tokens `site.css:49-52`); no class badge though `AssetReference.AssetClass` exists (`MarketDataContracts.cs:48-51`) | Two of six assets still look like placeholders, equities are indistinguishable by color, and asset type has to be inferred from the ticker | Own NVDA/TSLA glyphs; per-equity accent tokens + catalog entries; "Akcje"/"Krypto" badge from `AssetClass` |
| C5 | Missing insight (presenter gap) | Summary strip computes count/best 7d/worst 7d/average only (`Analysis/AnalysisSummaryStrip.cs:14-49`, view `_SummaryStrip.cshtml:5-46`); presenter has per-factor `DirectionFor` (`AnalysisInsightPresenter.cs:110-119`) but no aggregate trend label | Users must interpret three bars themselves; the dashboard does not name the strongest trend | Add an aggregate "Trend wzrostowy / mieszany / spadkowy" rule and a "Najsilniejszy trend" (30d) stat in the presenter, unit-tested |

**Deferred / no-op:** sticky panel. The CSS is already correct for viewports wider than 64rem (`site.css:602-606`), and disabling it at ≤64rem is intentional. Planned action: a browser scroll check only, plus a check for sidebar height against a short viewport (see Open Questions).

## Detailed Findings

### Page flow and autorun

- `SelectedAssetIds` is `[BindProperty] string[]` and binds from POST only (`Index.cshtml.cs:20-22`). Validation covers empty, over-limit (max 5, `MarketData/MarketDataOptions.cs:11`), duplicate and unknown IDs (`Index.cshtml.cs:108-132`).
- The analysis block in POST: request building, market data and `AnalyzeAsync`, then setting `CurrentQuotes`/`AnalysisResults`/`HasStarted`/`StartedMessage` (`Index.cshtml.cs:66-101`). There is no PRG redirect, and both paths return `Page()`.
- The form posts with Razor antiforgery (test `AnalysisAccessContractTests.cs:1075-1086`). Authentication is enforced manually in both handlers via `EnsureAuthenticated()` with an OIDC challenge (`Index.cshtml.cs:10-18, 35-57`; unauthenticated test `:187-206`).
- Cost per run:
  - Development/Testing: the deterministic `DevelopmentMarketDataProvider` (`Program.cs:39-42`, `MarketData/DevelopmentMarketDataProvider.cs:5-36`).
  - Production: `UnavailableMarketDataProvider` (`Program.cs:39-42`).
  - `MarketDataService`: a 10 s timeout, a process-local budget of 100 requests/min and a 5-minute cache (`MarketData/MarketDataService.cs:42-78, 107-148`).
  - Inference: autorun spends one budget unit per uncached GET. That is negligible today and should be revisited when a real provider lands.
- The summary generator defaults to `UnavailableAnalysisSummaryGenerator` (`Program.cs:34-39`), so summaries use the deterministic `AnalysisExplanationBuilder` fallback (`Analysis/AnalysisExplanationBuilder.cs:6-32`; `AnalysisService.cs:135-173`).
- `wwwroot/js/analysis-form.js:15-28` only handles submit UX: pending state, disabled button, status text. It needs no change for GET autorun.

### Header and layout

- Sidebar title `<h1 id="analysis-title" class="analysis-workspace__title">Nowa analiza</h1>`, `Index.cshtml:19-22`. Content header `<h2>Dashboard analizy</h2>`, `:100-104`.
- The results section is `aria-labelledby="analysis-results-title"` (`Index.cshtml:109`; fixture `UiStateFixture.cshtml:36`). Removing the visible heading must keep an accessible name, for example a visually-hidden heading or an `aria-label`.
- Tokens: `--space-1` 0.5rem … `--space-4` 1.5rem (`site.css:30-36`); `--workspace-title-gap` (`:78`); summary label/value sizes (`:80-81`); header gap `--space-2` (`:618-620`).
- Timestamp for the subline: there is no response-level `AsOf` (`AnalysisContracts.cs:41-44`).
  - Per-result `Freshness` (`AnalysisContracts.cs:29-40`) and chart-point `Timestamp` (`:14-19`) exist.
  - The page model passes `DateTimeOffset.UtcNow` into the service without exposing it (`Index.cshtml.cs:85-90`).
  - The plan must choose a source: the analysis time captured in the page model, or the latest freshness timestamp.

### Card details and insights

- Current details block contents, in `_InsightCard.cshtml:126-224`:
  - price (`:130-133`);
  - 24h/7d/30d changes (`:134-145`);
  - quality/freshness (`:146-153`);
  - warnings (`:155-168`);
  - factors with weight/contribution (`:170-191`);
  - chart partial (`:193`);
  - summary (`:195-222`).
- The visible takeaway "Wniosek z danych" sits outside the block (`:121-124`).
- Takeaway logic: `BuildTakeaway` uses the max-|%| factor plus a rank suffix (`AnalysisInsightPresenter.cs:150-187`; tests `AnalysisInsightPresenterTests.cs:179-204`). The "why" bullets should give evidence (strongest contribution, supporting/opposing factors, score band, data quality) and must not repeat the takeaway sentence.
- Bands and words: `BandFor` (`AnalysisInsightPresenter.cs:94-101`), thresholds ±1% (`:110-119`), words (`:125-133`), `FactorLabel` (`:122-123`).
- Fixture already renders "Akcje"/"Krypto" labels (`UiStateFixture.cshtml:76-78, 116-118, 148-150, 200-203`) after the PR #9 review fix.

### Tests that encode current behavior

All in `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`:

| Behavior | Test | Lines | Effect of this change |
|---|---|---|---|
| Empty GET state | `Authenticated_analysis_page_renders_the_configured_assets_and_limit` | `:212-256`; asserts no summary strip/scores at `:246-247`, "Nowa analiza" at `:239` | Must change for autorun |
| Empty configuration | `Empty_asset_configuration_renders_an_explanatory_disabled_action` | `:259-290` | Stays valid if autorun runs only when the defaults are configured |
| "Ranking analizy" heading | 2 tests | `:320`, `:887` | Assertions must change |
| `analysis-results-title` | ranked fixture test | `:130-142` | Must change |
| Details markup | 4 tests, incl. `Analysis_page_renders_full_factor_and_summary_details`, `Analysis_details_are_collapsed_and_include_factor_contributions` | `:83-88`, `:329-371`, `:399-436`, `:761-765` | Must change for C3 |

## Code References

- `Pages/Analysis/Index.cshtml.cs:35-101` - GET/POST handlers; analysis block to extract
- `Pages/Analysis/Index.cshtml:19-22, 100-147` - sidebar title, content header, results section
- `Pages/Analysis/_InsightCard.cshtml:121-224` - takeaway and details block
- `Pages/Analysis/_AssetGlyph.cshtml:1-31` - glyph SVGs and monogram fallback
- `Analysis/AnalysisInsightPresenter.cs:94-187` - bands, directions, words, takeaway
- `Analysis/AnalysisSummaryStrip.cs:3-53` - summary stats
- `Analysis/AssetIdentityCatalog.cs:3-42` - identities and accents
- `MarketData/MarketDataContracts.cs:3-6, 48-51` - `MarketAssetClass`, `AssetReference.AssetClass`
- `wwwroot/css/site.css:30-36, 49-52, 74-81, 407-409, 593-620, 682-698, 1074-1099, 1414-1421` - tokens and rules involved

## Architecture Insights

- Presentation logic lives in `Analysis/*Presenter*`/`AnalysisSummaryStrip` with unit tests, while views stay thin partials. New rules (aggregate trend label, why-bullets, strongest trend, class badge text) belong there, not in Razor.
- Tokens follow a single `:root` block in `site.css`. Accents are role tokens mapped through `AssetAccent` → CSS class. New per-equity accents should extend that enum/class/token chain rather than adding view literals.
- `UiStateFixture` (Testing-only, `?view=ranked`) is the visual gate named in `AGENTS.md:28`.

## Historical Context (from prior changes)

- `context/archive/2026/10-14-select-and-start-analysis/plan.md:32, 219-221`: analysis is a Razor POST handler and authenticated GET renders the asset list. Superseded for the initial view by this change's autorun decision (`change.md` Notes); POST remains.
- `context/archive/2026/10-14-select-and-start-analysis/plan-brief.md:31-38, 93`: the 1–5 selection, caching, request budget, provider errors and the educational boundary. All still binding.
- `context/archive/2026/06-market-data-access-contract/reviews/impl-review.md:59-64`: the request budget is process-local; there is a scale-out caveat.
- `context/foundation/prd.md:70-72`: FR-003 keeps the separate start action (see Summary for the reading).

## Related Research

- `context/changes/analysis-workspace-ui-refinement/` - previous UI refinement of the same view (split workspace, fixture states)

## Open Questions

1. **Subline timestamp source:** the analysis time captured in the page model, or the newest `Freshness` timestamp? Plan decision.
2. **Where AC-05/AC-06 content goes after C3:** current price in the card header, and the summary as a collapsed "Podsumowanie" or inside "Dlaczego miejsce #N?". Plan decision; must keep the PRD acceptance criteria true.
3. **Aggregate trend label rule:** for example, all available factors rising → wzrostowy, all falling → spadkowy, otherwise mieszany. Thresholds must reuse ±1% (`AnalysisInsightPresenter.cs:110-119`).
4. **Sticky on short viewports:** sidebar height against an approximately 768px-high viewport. Not measurable statically; check in the browser during implementation.
5. **GET failure state:** if market data fails on autorun, show the existing provider message rather than an empty dashboard. Confirm the copy in the plan.
