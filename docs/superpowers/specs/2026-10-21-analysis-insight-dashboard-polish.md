# Analysis insight dashboard — polish round

Addendum to `2026-10-21-analysis-insight-dashboard-design.md`, based on user
review of the implemented insight cards. Everything in the base spec still
applies unless changed here. All copy is Polish; no predictions or advice.

## Decisions (approved by the user)

| Topic | Decision |
| --- | --- |
| Asset logos | Own inline SVG glyphs in brand colors (Apple-like apple shape, four-square Microsoft-like mark, ₿ on orange, Ξ on purple); no third-party logo files. Unknown tickers keep the monogram fallback. |
| Takeaway | Each card ends with one deterministic, past-tense sentence built from the data, e.g. "Silny trend 30d — najwyżej oceniane aktywo w tej analizie." Never advice or prediction. |
| Factor display | Factors show descriptive words — "Rosnący" / "Stabilny" / "Malejący" — as the primary label; the exact % is secondary. No "Bullish/Bearish". |
| Price table | The historical price table is removed from the card. |
| Grid | Cards in a 2-column grid (2×2 for four assets) on desktop, 4 columns on very wide screens (≥ 1800px viewport), 1 column on mobile. |

## Changes

1. **Sidebar headline**: the oversized "Wybierz aktywa do sprawdzenia" becomes
   the smaller "Nowa analiza" (≈ 40–50% smaller). The sidebar is supporting,
   not the hero.
2. **Sidebar asset options**: simplified to `[glyph] Name` plus score when
   available, with a clearly highlighted selected state. Less crowding.
3. **Submit button**: "Rozpocznij analizę" → "Generuj ranking".
4. **Header summary strip** (above the cards, when results exist): number of
   assets analyzed, best performer (7-day change), worst performer, average
   score. Labels: "Przeanalizowane aktywa", "Najlepszy wynik 7d",
   "Najsłabszy wynik 7d", "Średni score".
5. **Score emphasis**: larger score block in the card header: rank "#1",
   score number, and band label — "Silny" (≥ 70), "Umiarkowany" (40–69),
   "Słaby" (< 40). Band colors via tokens.
6. **Per-asset accent color**: equities blue, Bitcoin orange, Ethereum purple
   (token per accent); used for glyph background and card accent.
7. **Consistent naming**: short names everywhere — Apple, Microsoft, Bitcoin,
   Ethereum, NVIDIA, Tesla — ticker shown beneath.
8. **Denser cards**: ~30–40% shorter; reduce padding/gaps, compact sparkline
   and bars, tighter header.
9. **Factor words**: thresholds — > +1% "Rosnący", < −1% "Malejący",
   otherwise "Stabilny"; unavailable stays muted "n/d".
10. **Takeaway sentence**: replaces the "Wniosek z danych" block content as the
    card's closing line; built from the strongest factor and rank.
11. **Price table removed** from `_PriceHistoryChart` (the detailed chart in
    `<details>` stays).

## Unchanged constraints

Scoring/contracts/services untouched; dev provider remains dev-only; no client
chart library; no literal colors or arbitrary layout values in views; existing
asserted CSS classes stay (update a test only when this addendum deliberately
removes the element, e.g. the price table); "chwilowo niedostępne" never
renders; "prognoz", "buy", "guaranteed", "best investment", "top opportunity"
never render.
