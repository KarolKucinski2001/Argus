---
date: 2026-10-20T15:57:43+02:00
researcher: GitHub Copilot CLI
git_commit: 59b7ab705c2c5786d235fdf9b0d5674ce1978bd0
branch: master
repository: KarolKucinski2001/Argus
topic: "Audit and refine the existing authenticated /analysis view"
tags: [research, codebase, analysis-ui, css-tokens, accessibility, responsive-layout]
status: complete
last_updated: 2026-10-20
last_updated_by: GitHub Copilot CLI
---

# Research: Audit and refine the existing authenticated `/analysis` view

**Date**: 2026-10-20T15:57:43+02:00  
**Researcher**: GitHub Copilot CLI  
**Git Commit**: `59b7ab705c2c5786d235fdf9b0d5674ce1978bd0`  
**Branch**: `master`  
**Repository**: `KarolKucinski2001/Argus`

## Research Question

What should be improved in the existing authenticated `/analysis` view while
preserving its current analysis, authentication, evidence, and responsive
contracts?

## Summary

The inspected `/analysis` view already uses the repository's semantic CSS
variables and produced zero candidates in the hardcoded-value scan described by
the 10x UI skill. The value source is `wwwroot/css/site.css`; no separate
component directory was found in the repository. (`wwwroot/css/site.css:1-39`,
`Pages/Analysis/Index.cshtml:1-442`)

The useful next change is therefore a hierarchy and state pass, not a new
palette. Five charges are recorded below: asset-option hover feedback,
competing nested result surfaces, mobile metric density, authenticated
navigation mismatch, and absent loading/empty-assets states. The existing
authentication and completed-result contracts should remain intact while these
states are made clearer. (`tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs:16-81`,
`Pages/Analysis/Index.cshtml.cs:33-100`)

## Charges

### Charge 1 — Add hover feedback to asset options

- **File and lines:** `Pages/Analysis/Index.cshtml:32-41`;
  `wwwroot/css/site.css:394-418`
- **Evidence:** The asset row has a checked-state selector and the checkbox has
  a focus-visible outline, but the `.asset-option` rule has no hover state.
- **User impact:** Before selecting an asset, users receive no visual signal
  that the whole row is interactive rather than merely the small checkbox.
- **Category:** Missing token/state coverage.
- **Recommendation:** Add a token-backed hover treatment to `.asset-option`
  without changing its checked or focus-visible behavior.

### Charge 2 — Reduce competing inner surfaces in result cards

- **File and lines:** `Pages/Analysis/Index.cshtml:104-200`;
  `wwwroot/css/site.css:486-505`, `wwwroot/css/site.css:552-567`,
  `wwwroot/css/site.css:601-610`, `wwwroot/css/site.css:729-736`,
  `wwwroot/css/site.css:759-767`
- **Evidence:** A result card contains repeated bordered and rounded metric,
  factor, chart-point, and summary blocks using multiple surface levels.
- **User impact:** The repeated containers give secondary evidence similar
  visual weight to the asset name, score, and primary result, making the card
  harder to scan.
- **Category:** Missing shared visual hierarchy.
- **Recommendation:** Consolidate the directly repeated inner blocks onto the
  existing surface/divider roles before introducing any new visual values.

### Charge 3 — Preserve readable metric scanning on mobile

- **File and lines:** `wwwroot/css/site.css:552-567`,
  `wwwroot/css/site.css:883-885`
- **Evidence:** The desktop metric grid uses three columns, while the
  `max-width: 40rem` rule changes the same six metrics to one column.
- **User impact:** On a narrow screen, the six metrics become a long vertical
  sequence and push factors, chart, and summary content far below the result
  heading.
- **Category:** Responsive hierarchy.
- **Recommendation:** Test a compact two-column mobile metric layout using
  existing spacing and minimum-width tokens, and retain one column only if
  actual rendered content becomes unreadable.

### Charge 4 — Align authenticated navigation with authenticated state

- **File and lines:** `Pages/Shared/_Layout.cshtml:14-18`;
  `Pages/Account/Index.cshtml:6-19`
- **Evidence:** The shared layout renders the sign-in link unconditionally,
  while the account page exposes the signed-in state and logout action.
- **User impact:** Signed-in users entering `/analysis` see a contradictory
  “Zaloguj się” navigation item and no direct account/logout affordance.
- **Category:** Accidental authenticated-workspace architecture.
- **Recommendation:** Add an authenticated navigation branch that exposes only
  real destinations, Analysis and Account, while preserving the public landing
  navigation for anonymous users.

### Charge 5 — Represent loading and empty configured-asset states

- **File and lines:** `Pages/Analysis/Index.cshtml:21-43`,
  `Pages/Analysis/Index.cshtml:386`;
  `Pages/Analysis/Index.cshtml.cs:74-106`
- **Evidence:** The POST handler awaits market-data and analysis calls, but the
  form has no pending indicator or disabled submit state. `LoadAssets()` can
  produce an empty asset collection, while the view only loops over the
  collection and still renders the submit button.
- **User impact:** During a slow request users receive no progress feedback, and
  an empty configured asset list looks like a broken picker with an action that
  can only return a validation error.
- **Category:** Missing state coverage.
- **Recommendation:** Add a bounded loading treatment for the existing submit
  flow and a clear empty-assets state with a non-misleading action. Preserve
  the server-side validation and provider-error contracts.

## Detailed Findings

### Token and component source

- The repository stores semantic colors, spacing, radii, content width, and
  analysis sizing variables in `wwwroot/css/site.css:1-39`.
- The inspected view references semantic classes such as `button`,
  `analysis-form`, `asset-option`, `analysis-result-card`, and chart classes;
  the view contains zero matches for the required hardcoded color/function,
  Tailwind palette, or arbitrary spacing candidate scan.
- No repository component directory matched the inspected `components` glob;
  the existing CSS class layer is the local reusable styling mechanism.
- `AGENTS.md:33-36` requires landing-page tokens in `wwwroot/css/site.css` and
  views without literal colors or arbitrary layout values; the same token source
  is already used by `/analysis`.

### Existing behavior to preserve

- Unauthenticated `/analysis` requests issue an explicit OIDC challenge at
  `Pages/Analysis/Index.cshtml.cs:33-42`, covered by
  `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs:16-32`.
- Authenticated GET requests render the configured asset list and selection
  limit at `Pages/Analysis/Index.cshtml.cs:45-50`, covered by
  `AnalysisAccessContractTests.cs:34-50`.
- Validation, provider-error, partial-result, ranked-result, factor, chart,
  summary, and insufficient-evidence states are rendered by the view and
  covered by the existing analysis access contract tests in
  `AnalysisAccessContractTests.cs:52-510` and `AnalysisAccessContractTests.cs:547-704`.
- The result page keeps the educational boundary visible at
  `Pages/Analysis/Index.cshtml:79-84` and `Pages/Analysis/Index.cshtml:389-392`.

## Code References

- `Pages/Analysis/Index.cshtml:21-43` — asset-selection form and configured
  asset list.
- `Pages/Analysis/Index.cshtml:46-84` — validation, provider status, started
  state, and educational boundary near results.
- `Pages/Analysis/Index.cshtml:104-376` — result-card hierarchy, metrics,
  factors, chart, warnings, and summary states.
- `Pages/Analysis/Index.cshtml.cs:33-100` — authentication guard, asset loading,
  request execution, and result state assignment.
- `Pages/Shared/_Layout.cshtml:9-21` — shared header and global navigation.
- `Pages/Account/Index.cshtml:6-19` — authenticated account and logout surface.
- `wwwroot/css/site.css:1-39` — semantic token source.
- `wwwroot/css/site.css:339-451` — analysis form, asset option, validation,
  and status-card styling.
- `wwwroot/css/site.css:478-807` — result-card hierarchy, chart, summary, and
  state styling.
- `wwwroot/css/site.css:859-910` — narrow-screen result layout rules.

## Architecture Insights

The current view is server-rendered Razor with a shared CSS class layer and a
small chart JavaScript controller. The safest UI change is to preserve the
existing view and data contracts, add visual-state hooks where the current
markup already owns the state, and keep new values in `wwwroot/css/site.css`.

The authenticated shell is not yet separated from the public shell:
`Pages/Shared/_Layout.cshtml:9-21` is shared by the public and authenticated
pages. Any authenticated-navigation improvement must branch on the current
principal without turning the public landing page into an application
dashboard.

## Historical Context

- `context/foundation/roadmap.md` currently lists
  `authenticated-analysis-workspace` as the proposed S-02A slice and identifies
  Analysis as the primary authenticated destination.
- `docs/superpowers/specs/2026-10-20-authenticated-analysis-workspace-design.md`
  records the approved decision that successful sign-in should lead to
  `/analysis`, with only Analysis and Account as real authenticated
  destinations.
- `context/changes/visual-market-history-chart/plan.md` records the existing
  chart accessibility, responsive, and text-fallback contract; this change
  should not replace that chart implementation.

## Related Research

- `context/changes/visual-market-history-chart/plan.md`
- `context/changes/explained-market-ranking/plan.md`
- `docs/superpowers/specs/2026-10-20-authenticated-analysis-workspace-design.md`

## Open Questions

- A browser-rendered desktop and mobile inspection is still required before
  finalizing spacing or metric-column changes; static source inspection cannot
  establish the actual visual result.
- The approved authenticated shell design should be reconciled with the
  one-view scope before implementation: navigation changes may require shared
  layout edits, but the product target remains the `/analysis` experience.
- A loading treatment needs to decide whether client-side form state is
  acceptable for this server-rendered page or whether the first iteration
  should be limited to a submit-button state that does not alter the backend
  contract.
