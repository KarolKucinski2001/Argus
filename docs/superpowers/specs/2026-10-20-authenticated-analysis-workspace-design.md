# Authenticated Analysis Workspace Design

## Status

Approved design. Ready for implementation planning.

## Goal

After successful authentication, Argus should open as an analysis workspace
rather than a generic account or dashboard screen. The workspace should get a
beginner to asset selection quickly while providing a shell that can grow into
future product areas.

## Product decisions

- Successful OIDC sign-in redirects to `/analysis`.
- `/account` remains available as the authenticated profile and logout screen.
- The authenticated shell exposes only real destinations: **Analysis** and
  **Account**.
- Watchlist, Learn, and dashboard navigation are deferred until those areas
  have usable behavior.
- The public landing page keeps its current presentation and navigation.

## Layout

### Shared authenticated shell

The workspace shell provides:

- Argus branding;
- an active **Analysis** destination;
- an **Account** menu or link;
- consistent content width, spacing, surfaces, focus states, and responsive
  behavior;
- a desktop-only guidance panel beside the primary analysis content;
- a single-column mobile layout with collapsed account/navigation controls.

The shell should be implemented as a reusable authenticated layout or
equivalent shared view structure. It must not turn the public landing page
into an application dashboard.

### Analysis page

The analysis page remains responsible for product content:

1. asset search and selection;
2. selected-asset summary and analyze action;
3. ranking and results;
4. current and historical metrics;
5. contributing factors, chart, and grounded explanation;
6. the educational-not-investment-advice boundary.

The desktop guidance panel should orient the user without competing with the
selection form. On mobile, the guidance content becomes secondary or moves
below the main action so the flow remains a focused single column.

## Authentication and routing

The successful provider callback should use `/analysis` as its safe
application destination. `/account` remains a stable authenticated route for
available claims, profile context, and logout. Existing API behavior is
unchanged: unauthenticated API requests continue to return `401
application/problem+json` rather than redirecting.

## State and error behavior

The shell must keep analysis feedback in context. Existing validation,
provider-error, loading/start, partial-result, stale-data, insufficient-
evidence, and asset-error states should remain visible within the analysis
workspace and should not redirect the user to another page.

The educational boundary should appear near the analysis controls and remain
available near result content without dominating the workspace.

## Styling and boundaries

- Reuse the existing design tokens in `wwwroot/css/site.css`.
- Keep view markup free of literal colors and arbitrary layout values.
- Preserve the current landing-page contract.
- Avoid creating empty or placeholder navigation destinations for future
  features.
- Avoid introducing a dashboard, watchlist persistence, or new market-data
  behavior as part of this layout change.

## Validation

The implementation should verify that:

- unauthenticated users still see the public landing page;
- successful login lands on `/analysis`;
- `/analysis` and `/account` use the authenticated shell;
- logout returns to `/`;
- existing validation and analysis evidence states remain understandable;
- desktop and mobile layouts preserve readable content and keyboard focus;
- existing authentication contract tests continue to pass.
