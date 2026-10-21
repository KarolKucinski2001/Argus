# Autorun analysis dashboard with comparison-first insights — Plan Brief

> Full plan: `context/changes/analysis-dashboard-autorun-insights/plan.md`
> Research: `context/changes/analysis-dashboard-autorun-insights/research.md`

## What & Why

`/analysis` still focuses on explaining the analysis process instead of helping users compare assets fast. This plan makes the page open on a ranked dashboard of the four default assets. It also replaces developer-style methodology details with plain-language insights: badge, price, trend label and "Dlaczego miejsce #N?".

## Starting Point

PR #9 delivered the dark insight dashboard: glyph cards, rank/score/band, summary strip and the 2×2 grid. GET still renders an empty form. The header is tall. Price and summary are hidden in a collapsed "Szczegóły i metodologia" block next to weights/contributions. NVDA/TSLA use monograms.

## Desired End State

Signing in and opening `/analysis` shows 4 ranked cards immediately, under a compact header (`4 aktywa • Dane z 21.10.2026`) and a strip that includes "Najsilniejszy trend". Each card shows:
- glyph, name and `Akcje`/`Krypto` badge;
- price, rank and score;
- `Trend wzrostowy/mieszany/spadkowy`;
- chart and factor bars;
- the takeaway, the summary and 2–3 "why" bullets.

The page has no expandable methodology block.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Initial state | Preselect AAPL/MSFT/BTC-USD/ETH-USD and run the analysis on GET | User lands on analysis, not configuration | Plan (earlier round) |
| FR-003 compatibility | Keep "Generuj ranking" for re-runs; no auto-run on checkbox change | PRD rejects auto-start after selection | Research |
| Details block | Remove it; price goes in the card header, summary above the why-list | AC-05/AC-06 require them visible | Plan |
| Trend label | Majority of available factors (±1%), else `mieszany` | Informative and reuses `DirectionFor` | Plan |
| Signal wording | Descriptive only, never BUY/Bullish | Educational-not-advice boundary | Plan (earlier round) |
| Subline date | Newest market-data `ObservedAt` | Truthful about data freshness; no service change | Plan |
| Equity identity | NVDA/TSLA glyphs + per-equity accent tokens | Every card visually distinct | Plan |
| Backend | No changes to scorer, contracts or service | Presentation-only change | Research |

## Scope

**In scope:**
- autorun on GET;
- compact header with subline and visually hidden `Ranking analizy`;
- strongest-trend stat;
- badge, price, trend label and why-bullets on cards;
- NVDA/TSLA glyphs and accents;
- tokens, fixture sync, the 7-state check and the sidebar check on short screens;
- `AGENTS.md` rule.

**Out of scope:** scoring/service changes, recommendation wording, page-render timestamps, real trademark logos, mobile sticky behavior, new JS.

## Architecture / Approach

The page model gets one shared `RunAnalysisAsync` used by GET (defaults) and POST (user selection). The pure presenters (`AnalysisInsightPresenter`, `AnalysisSummaryStripPresenter`, `AssetIdentityCatalog`) compute every new label. The Razor partials only bind them. Visual values live as tokens in `site.css :root`. The fixture `/analysis/ui-state-fixture?view=ranked` is the screenshot gate.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Autorun on GET | Ranked cards on first visit | Production provider is unavailable, so visitors land on the error message |
| 2. Presenter logic | Trend label, why-bullets, badge, strongest trend, data date, accents | Bullets repeating the takeaway |
| 3. Tokens | Smaller title, tighter gaps, accent/badge/trend tokens | Contrast of the new accents |
| 4. View | Header, cards, strip and fixture updated; screenshots | Card height grows with price + summary + bullets |
| 5. States and finish | 7-state check, sidebar on short screens, `AGENTS.md` rule | Sidebar taller than 768px viewport |

**Prerequisites:** merge PR #9 and branch from updated `master` (e.g. `feature/analysis-autorun-insights`).
**Estimated effort:** about 2 sessions across 5 phases.

## Open Risks & Assumptions

- In Production, `UnavailableMarketDataProvider` means GET shows the provider message on landing. This is accepted until a real provider is wired.
- Each GET now hits the market data service. The 5-minute cache and 100 req/min budget are assumed sufficient.
- Explanation-builder summaries are assumed to stay short. The 800-char maximum could make a card tall.

## Success Criteria (Summary)

- The first authenticated visit shows a ranked comparison with no clicks.
- Users can tell which asset leads and why from the strip and the card bullets, without reading methodology.
- The UI uses only tokens and descriptive wording, and all tests pass.
