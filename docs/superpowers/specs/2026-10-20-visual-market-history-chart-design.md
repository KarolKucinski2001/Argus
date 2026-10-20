# Visual Market History Chart

## Goal

Replace the current rendered list-only presentation of analysis history with a
simple, readable line chart while preserving the existing normalized chart
points and the text-based data fallback.

The chart is a frontend presentation improvement. It does not change market
data retrieval, performance calculations, scoring, ranking, summaries, or the
Development/Test fake-provider boundary.

## Decisions

- Use a server-rendered SVG line chart inside each analysis result card.
- Keep the implementation local to the existing Razor Page and site stylesheet.
- Do not add a charting library or other frontend dependency.
- Add a small local JavaScript controller for hover, keyboard focus, and touch
  selection of points.
- Keep the existing text list of dates and closes as a semantic and
  no-script fallback.
- Keep the current educational disclaimer and explicitly retain the
  illustrative-data boundary in Development.

## Rendering and data flow

The existing `AnalysisChartPoint` collection remains the only chart input.
Razor will:

1. Sort points by timestamp as it does today.
2. Derive bounded SVG coordinates from the available close values.
3. Render an accessible chart container, axes/labels, a line, and focusable
   point elements.
4. Store the invariant timestamp and formatted close value on each point for
   the tooltip controller.
5. Render the existing textual list below or alongside the SVG as fallback.

The SVG is a visualization of evidence already displayed by the analysis
result. It must not imply predictions, recommendations, or extra precision.
If there are too few points to establish a meaningful trend, the chart will
still show available observations with a bounded limited-history message. If
there are no points, the existing no-history state remains.

## Interaction and accessibility

The local script will:

- show a tooltip for the hovered point;
- show the same tooltip when a point receives keyboard focus;
- keep the selected point visible after touch interaction;
- dismiss or move the tooltip when focus/selection changes;
- avoid requiring hover as the only way to read a value.

Each point will have an accessible label containing its date and close. The
textual fallback remains available to screen readers and when JavaScript is
disabled. The chart must remain usable at approximately 1440x900 and
390x844, with no horizontal overflow.

## Styling

All visual values will use the existing CSS token system. The chart will use:

- a bounded responsive aspect ratio or height;
- token-backed axis, line, point, and tooltip colors;
- visible focus styling;
- a readable small-screen layout;
- overflow-safe tooltip positioning.

No literal colors will be added to the Razor view or stylesheet outside the
existing token definitions.

## Testing and verification

Automated contract tests will verify:

- SVG chart markup renders when points exist;
- chart points expose stable timestamp and close data;
- the text fallback remains present;
- no-history output remains safe;
- existing result-card classes and color-token constraints remain intact.

Manual verification will cover:

- desktop and mobile layout;
- hover tooltip;
- keyboard focus and tooltip access;
- touch selection;
- limited and missing history;
- preservation of the educational, illustrative-data wording.

## Out of scope

- real market-data provider integration;
- live prices or refresh polling;
- a JavaScript dashboard;
- chart zooming, panning, date-range controls, or technical indicators;
- scoring or ranking changes;
- AI or LLM integration.
