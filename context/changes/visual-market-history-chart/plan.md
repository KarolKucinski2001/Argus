---
change_id: visual-market-history-chart
title: Add a visual market history chart
status: planned
created: 2026-10-20
updated: 2026-10-20
archived_at: null
---

# Implementation Plan: Visual Market History Chart

## Goal

Render the existing normalized analysis history as an accessible SVG line
chart with a small local tooltip controller, while retaining the current
textual list as a no-script and screen-reader fallback.

## Current state

- `Pages/Analysis/Index.cshtml` renders `ChartPoints` as a list of dates and
  closes under the existing chart section.
- `AnalysisChartPoint` already provides ordered timestamp and close values.
- `wwwroot/css/site.css` owns token-backed analysis-card styling.
- `AnalysisAccessContractTests` verifies result markup, chart data attributes,
  responsive hooks, and safe evidence-state rendering.
- No chart library or frontend build pipeline is present.

## Decisions

- Use server-rendered SVG; do not add a chart dependency.
- Keep chart data provider-neutral and reuse `ChartPoints` without changing
  analysis calculations.
- Add local JavaScript for hover, keyboard focus, and touch point selection.
- Keep the text list visible or available as a semantic fallback.
- Use CSS tokens for all chart and tooltip styling.
- Treat missing or thin history as a bounded evidence state, never as a
  fabricated trend.

## Phases

### Phase 1: Render accessible chart geometry

#### Changes Required:

- `Pages/Analysis/Index.cshtml`
  - **Intent:** Add a server-rendered SVG line chart to each successful result
    card using the existing ordered chart points.
  - **Contract:** Render stable chart container, axis/label hooks, line/path,
    focusable point elements, invariant timestamp data, formatted close data,
    accessible labels, and the existing text fallback. Handle no points and
    limited history without inventing values.
- `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`
  - **Intent:** Protect the rendered chart contract and fallback behavior.
  - **Contract:** Assert SVG/chart markup, point metadata, accessible labels,
    existing list fallback, and safe no-history output.

#### Automated Verification:

- The focused authentication test project passes.
- Chart markup is rendered for a result with historical points.
- Timestamp/close metadata is present for each chart point.
- Text fallback and no-history behavior remain covered.

#### Manual Verification:

- Confirm the chart communicates historical observations without predictive
  or recommendation language.

### Phase 2: Add tooltip interaction and responsive styling

#### Changes Required:

- `Pages/Analysis/Index.cshtml`
  - **Intent:** Load the local chart controller and expose the point hooks it
    needs without coupling the view to provider-specific data.
  - **Contract:** Hover, focus, and touch selection reveal the selected point's
    date and close; focusable points expose equivalent accessible text.
- `wwwroot/js/analysis-chart.js`
  - **Intent:** Provide a small dependency-free controller for point selection
    and bounded tooltip placement.
  - **Contract:** The controller handles pointer hover, keyboard focus,
    pointer/touch selection, changing active points, and dismissal without
    throwing when optional markup is absent.
- `wwwroot/css/site.css`
  - **Intent:** Style the SVG, points, focus state, tooltip, axis labels, and
    small-screen layout using existing design tokens.
  - **Contract:** No literal colors outside token definitions; no horizontal
    overflow at approximately 390px; visible keyboard focus.
- `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`
  - **Intent:** Protect script/style integration and accessibility hooks.
  - **Contract:** Assert script reference, chart classes, focus hooks, tooltip
    hooks, and token-backed CSS selectors.

#### Automated Verification:

- The focused authentication test project passes.
- Build succeeds with zero warnings and zero errors.
- Static view/CSS checks reject literal colors outside the token area.
- Chart and tooltip assets are referenced by the rendered analysis page.

#### Manual Verification:

- Test hover tooltip on desktop.
- Test keyboard focus and tooltip access without a mouse.
- Test touch selection on a narrow viewport.
- Confirm tooltip placement does not clip or create horizontal scrolling.

### Phase 3: Final verification and documentation

#### Changes Required:

- `docs/market-data.md`
  - **Intent:** Clarify that the visual chart is a projection of normalized
    historical evidence and that Development values remain illustrative.
  - **Contract:** Documentation keeps the provider boundary and
    educational-not-advice language explicit.

#### Automated Verification:

- Run the complete `Argus.Authentication.Tests` project.
- Run `dotnet build .bootstrap-scaffold.csproj --no-restore`.
- Run repository leakage and literal-color checks relevant to the analysis
  surface.

#### Manual Verification:

- Verify desktop layout at approximately 1440x900.
- Verify mobile layout at approximately 390x844.
- Verify populated, limited-history, and no-history states.
- Verify the text fallback remains understandable when JavaScript is
  disabled.

## What we're not doing

- No real market-data provider integration.
- No live refresh or polling.
- No chart zoom, pan, range picker, technical indicators, or dashboard.
- No changes to scoring, ranking, summaries, authentication, or API contracts.
- No AI/LLM integration.

## Risks and mitigations

- **Tooltip accessibility:** keep every value in the textual fallback and give
  points keyboard focus and accessible labels.
- **Misleading visual certainty:** chart only projects supplied observations;
  thin history is labelled and no forecast line is rendered.
- **Small-screen clipping:** use bounded SVG dimensions, token-backed layout,
  and manual verification at the target narrow viewport.

## Progress

### Phase 1: Render accessible chart geometry

#### Automated

- [x] 1.1 Focused tests cover SVG markup, point metadata, fallback, and no-history behavior.

#### Manual

- [ ] 1.2 Confirm the chart communicates historical observations without predictive language.

### Phase 2: Add tooltip interaction and responsive styling

#### Automated

- [x] 2.1 Focused tests cover assets, accessibility hooks, and token-backed styling.
- [x] 2.2 Build succeeds with zero warnings and zero errors.

#### Manual

- [ ] 2.3 Verify hover, keyboard focus, touch selection, and tooltip placement.

### Phase 3: Final verification and documentation

#### Automated

- [ ] 3.1 Full tests, build, and relevant leakage/style checks pass.

#### Manual

- [ ] 3.2 Verify desktop, mobile, populated, limited-history, no-history, and no-script states.
