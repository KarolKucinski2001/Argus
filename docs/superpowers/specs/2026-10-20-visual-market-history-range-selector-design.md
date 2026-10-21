# Visual Market History Range Selector

## Goal

Improve the existing market-history chart so it presents a clean, readable
straight-line trend and lets users choose the visible history range:
`1M`, `3M`, `6M`, `1Y`, or `Maks.`. The default range is `1Y`.

This remains a frontend presentation change. It does not change market-data
retrieval, scoring, ranking, summaries, authentication, or the provider
boundary.

## Decisions

- Keep the chart as a server-rendered SVG with a dependency-free local
  JavaScript controller.
- Draw straight line segments between observed points; do not smooth the line
  or invent intermediate values.
- Render the complete normalized history into the page so the text fallback
  remains complete and JavaScript can switch ranges locally.
- Use `1Y` as the default active range.
- Provide `1M`, `3M`, `6M`, `1Y`, and `Maks.` controls above the chart.
- Mark the active range with both visual styling and `aria-pressed`.
- Keep each chart point's accessible date/close label available without
  JavaScript; do not add a standalone price table.
- Use `Maks.` or the nearest available history when a selected range cannot be
  represented by the available observations.

## Interaction and data flow

Each chart point retains its invariant timestamp, close value, and asset
metadata. The local controller:

1. Reads the complete point collection from the chart container.
2. Selects points at or after the cutoff for the active range.
3. Falls back to the available history when the dataset is shorter than the
   requested range.
4. Recalculates bounded SVG coordinates and the straight `path` for the
   selected points.
5. Updates point visibility, active range state, axis labels, and tooltip
   behavior.

The range controls are native buttons, keyboard accessible, and grouped with
an accessible label. Hover, focus, and touch point selection continue to work
after every range change.

## Visual design

The chart follows the approved “clean trend” direction:

- full-width responsive chart with a bounded aspect ratio;
- accent-colored straight line;
- subtle axis lines and labels;
- small, quiet points with a clear active/focus state;
- no gradient, filled area, heavy grid, prediction line, or recommendation
  language;
- compact range controls directly above the chart;
- tooltip constrained to the chart container with no horizontal overflow.

The chart remains legible on desktop and at approximately 390px wide.

## States

- **Populated history:** render the selected range and tooltip points.
- **Limited history:** show all available points and retain the limited-history
  message.
- **No history:** keep the existing safe empty state and omit range controls
  that have no chart to operate.
- **Range with insufficient history:** keep the control usable but show the
  nearest available observations rather than an empty or fabricated trend.
- **JavaScript disabled:** preserve the complete textual fallback and static
  server-rendered chart.

## Testing

Contract tests will cover:

- range controls and default `1Y` markup;
- `aria-pressed` state and stable range values;
- asset/script/style integration;
- complete point metadata and text fallback preservation;
- safe limited-history and no-history output;
- token-backed chart styling and no literal colors outside token definitions.

JavaScript behavior will be covered with the repository's available
deterministic test approach, or with static contract assertions where no
browser test runner exists. Manual verification will cover each range, hover,
keyboard focus, touch selection, narrow viewport layout, and the no-script
fallback.

## Out of scope

- real market-data provider integration;
- server-side range query endpoints;
- live refresh or polling;
- zooming, panning, technical indicators, or custom date ranges;
- scoring, ranking, summaries, or AI behavior changes.
