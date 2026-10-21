# Analysis Insight-First Dashboard — Design

Supersedes the layout decisions in
`2026-10-20-analysis-dashboard-redesign-design.md` after manual review rejected
that result as "a technical report, not a dashboard".

## Goal

Make `/analysis` read as a premium dark fintech dashboard: ranking reasons
visible at a glance, distinct charts per asset, clear asset identity, and
roughly 75% less explanatory text. Approved mockup:
`.superpowers/brainstorm/640-1792563901/content/dashboard-insight-first-v1.html`.

## Non-goals

- No predictions or forecast cards.
- No new scoring factors; scorer and analysis contracts stay unchanged.
- No new production market-data providers.
- No client-side chart library or JS rendering of charts.

## Components

### `Analysis/AnalysisInsightPresenter.cs` (new)

Pure, I/O-free class. `Present(AssetAnalysis analysis)` returns `AssetInsightView`:

- `Name`, `Monogram`, `Tone` from a static ticker map:
  `AAPL` → "Apple", "A"; `MSFT` → "Microsoft", "M";
  `NVDA` → "NVIDIA", "N"; `TSLA` → "Tesla", "T";
  `BTC-USD` → "Bitcoin", "₿"; `ETH-USD` → "Ethereum", "Ξ".
  Unknown tickers use the ticker as name and its first character as monogram.
- `FactorBars`: one entry per existing factor (`momentum-24h`, `momentum-7d`,
  `trend-30d`) with label, raw percentage, and 0–100 value computed with the
  same formula as `OpportunityScorer` (`(pct + 20) / 40 * 100`, clamped).
  Unavailable factors have `IsAvailable = false` and no value.
- `Insight`: one deterministic sentence naming the strongest and weakest
  available factor with their percentages, e.g.
  "Trend 30d był najsilniejszym czynnikiem (+8,2%), a momentum 24h
  najsłabszym (-1,1%)."
  With one available factor it names only that factor; with none it returns a
  Polish insufficient-data message.
- `Tone` maps the 7-day change sign to `positive`, `negative`, or `neutral`.

### `Pages/Analysis/_InsightCard.cshtml` (new partial)

Order inside the card:

1. Identity row: monogram badge, name, ticker, rank, status chips.
2. Score badge (numeric score or "Wynik nierankingowy").
3. Headline 7-day % change.
4. SVG sparkline from `ChartPoints` (polyline + gradient fill, colored by tone).
5. Three factor bars.
6. Insight line: the AI summary when available, otherwise the presenter
   insight labeled "Wniosek z danych".
7. Collapsed `<details>` containing methodology, factor explanations,
   detailed metrics, and chart range controls.

### `Pages/Analysis/Index.cshtml`

- Compact sidebar: per asset monogram, ticker, rank, and score.
- Content: two-column grid of `_InsightCard` partials, single column on mobile.
- Educational boundary reduced to one line.
- Remove standalone methodology/normalization paragraphs and large tables
  outside `<details>`.

### `MarketData/DevelopmentMarketDataProvider.cs`

Replace the shared saw-tooth with a per-asset deterministic series:
trend + sine wave + seeded noise, with per-asset trend, amplitude, period, and
seed. Output remains identical across calls for the same request.
Development/Testing only, as today.

### `wwwroot/css/site.css`

All new colors, spacing, and bar/badge styles as tokens or classes here;
views contain no literal colors or arbitrary layout values.

## States

| State | Card rendering |
| --- | --- |
| Completed | Full card |
| Partial | Missing bars show muted "n/d"; warning chip |
| Insufficient evidence | "Wynik nierankingowy", insufficient-data message; chart if ≥ 2 points |
| Stale / delayed | Warning chip next to the name |
| Failed | Compact error row with retry hint; no chart |
| Chart < 2 points | "Brak danych do wykresu" placeholder |

Page-level fixture states (empty assets, loading, validation, etc.) remain and
adopt the compact styling. The text "chwilowo niedostępne" never renders.

## Testing

- Presenter unit tests: name/monogram map and unknown fallback; normalization
  (0% → 50, ±20% clamps to 0/100); strongest/weakest selection; one-factor and
  no-factor insight.
- Dev provider tests: the four assets produce pairwise different normalized
  series; repeated calls are deterministic.
- Contract tests: card contains name, score badge, 7-day change, sparkline,
  three factor bars, insight; methodology and chart live inside `<details>`;
  no "chwilowo niedostępne"; fixture still covers all states.
- Gates: all tests pass, build has 0 warnings, hardcoded-value scan clean,
  screenshot compared against the mockup before the manual gate.
