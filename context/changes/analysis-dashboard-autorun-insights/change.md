---
change_id: analysis-dashboard-autorun-insights
title: Autorun analysis dashboard with explained, comparison-first insights
status: implementing
created: 2026-10-21
updated: 2026-10-21
archived_at: null
---

## Notes

10x-ui change for ONE view: the authenticated `/analysis` page (`Pages/Analysis/Index.cshtml` and its partials). Token source: `wwwroot/css/site.css` `:root` (existing design system; extend it, no new palette). Visual gate: `/analysis/ui-state-fixture` (Testing-only, `?view=ranked` for the 2×2 grid). Builds on PR #9 (`feature/insight-dashboard`); start after it merges.

Open items from the user's UI feedback (2026-10-21):
1. Shrink the "Nowa analiza" sidebar title.
2. Remove the "Ranking analizy" heading and tighten header gaps; add subline "N aktywa • Ostatnia aktualizacja: …".
3. Sticky left asset panel while cards scroll (CSS already has `position: sticky`, verify by scrolling).
4. Preselect AAPL, MSFT, BTC-USD, ETH-USD.
6. Replace "Szczegóły i metodologia" with "Dlaczego miejsce #N?" (2–3 Polish bullets from the data, no implementation details).
7. NVDA/TSLA own glyphs and distinct per-equity accents (deferred from PR #9 review).
8. Asset type badges: Akcje / Krypto.
10. Descriptive trend label per card.
11. Add "Najsilniejszy trend" to the summary strip.

User decisions:
- preselect = autorun: the 4 assets are selected AND the analysis runs on GET, so the user lands on the dashboard.
- details = why: "Dlaczego miejsce #N?" with 2–3 bullets derived from the data.
- signal = descriptive: "Trend wzrostowy" / "Trend mieszany" / "Trend spadkowy"; never BUY/Bullish (educational-not-advice boundary).
- Keep the terms "Momentum 24h/7d", "Trend 30d", "score", "Dashboard analizy".

Already done in PR #9: price lists removed, own glyphs for Apple/Microsoft/₿/Ξ, score with #rank and band, summary strip, cards 365px tall in a 2×2 grid.
