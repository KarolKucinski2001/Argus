# Plan Brief: Visual Market History Chart

Full plan: [plan.md](plan.md)

## Outcome

Argus analysis cards will show the existing historical observations as a
simple line chart. Users can inspect an exact date and close value with a
tooltip, while the current text list remains available as a semantic and
no-script fallback.

## Starting point

The analysis layer already produces ordered `AnalysisChartPoint` values. The
Razor page currently renders those values as a list under “Dane wykresu”.
Analysis result cards and their responsive styles already have automated
contract coverage. There is no frontend build pipeline or chart dependency.

## Key decisions

| Decision | Choice | Why |
| --- | --- | --- |
| Chart form | Server-rendered SVG line chart | Uses existing data and works without a new framework |
| Interaction | Local tooltip controller for hover, focus, and touch | Makes exact values discoverable while preserving control |
| Accessibility | Focusable points, accessible labels, and text fallback | Tooltip is not the only way to read data |
| Data boundary | Existing normalized points only | Avoids changing provider, scoring, or evidence contracts |
| Thin history | Show available observations with a bounded message | Does not fabricate a trend or imply certainty |
| Styling | Existing CSS token system | Preserves the landing/result surface contract |

## Phases

| Phase | Result | Prerequisites |
| --- | --- | --- |
| 1. Chart geometry | SVG, points, labels, metadata, and fallback are rendered | Existing analysis result surface |
| 2. Interaction and styling | Tooltip, focus state, touch selection, and responsive layout work | Phase 1 markup |
| 3. Verification and docs | Full checks pass and evidence boundary is documented | Phases 1–2 |

## Success criteria

- A populated analysis result renders a readable line chart.
- Hover, keyboard focus, and touch selection expose exact date/close values.
- The text fallback remains available and the chart is usable without relying on hover.
- No-history and limited-history states remain bounded and safe.
- Desktop and mobile layouts have no horizontal overflow.
- Automated tests and build pass with zero warnings.

## Out of scope

Real prices, provider integration, polling, zooming, panning, technical
indicators, scoring changes, API changes, and AI/LLM work remain out of scope.

## Verification

Automated checks cover rendered markup, assets, accessibility hooks, CSS token
usage, tests, and build. Manual checks cover desktop/mobile layout, hover,
keyboard, touch, limited/no history, and JavaScript-disabled fallback.
