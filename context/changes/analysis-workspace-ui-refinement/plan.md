# Refine the Authenticated Analysis Workspace Implementation Plan

## Overview

Improve the existing `/analysis` experience without changing market-data,
scoring, ranking, authentication contracts, or chart calculations. The change
will use the existing semantic CSS token source, align shared navigation with
authenticated state, improve result-card hierarchy and narrow-screen scanning,
and make loading and empty configuration states explicit.

## Current State Analysis

The `/analysis` Razor Page already renders asset selection, validation,
provider errors, partial results, ranked evidence, charts, summaries, and the
educational boundary. Its visual values are sourced from
`wwwroot/css/site.css`, and the initial hardcoded-value scan found zero
candidate literals in `Pages/Analysis/Index.cshtml`.

The shared layout currently renders public navigation for both anonymous and
authenticated users. Asset rows have checked and focus-visible states but no
hover affordance. Result cards contain several repeated bordered surfaces, and
the mobile breakpoint changes six metrics to a single column. The POST flow
has no pending feedback, and an empty configured asset list has no explanatory
state.

## Desired End State

An authenticated user entering `/analysis` sees a coherent Analysis/Account
shell, a clearly interactive asset picker, a result card whose score and
primary evidence dominate secondary details, and a compact but readable mobile
layout. Slow submissions visibly enter a pending state, while an empty asset
configuration explains the problem without presenting a misleading action.

The seven-state UI contract is reviewable in a non-production fixture:
default, hover, focus-visible, disabled, error, empty, and loading. Existing
authentication, analysis, chart, and provider-error tests continue to pass.

### Key Discoveries:

- `wwwroot/css/site.css:1-39` is the repository's semantic token source.
- `Pages/Shared/_Layout.cshtml:9-21` is shared by public and authenticated
  pages, so authenticated navigation must branch without changing the public
  landing experience.
- `Pages/Analysis/Index.cshtml.cs:74-100` awaits provider and analysis work
  synchronously from the browser's perspective; loading feedback can remain
  client-only.
- `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs:16-704`
  covers the current completed analysis and authentication contracts but has no
  loading or empty-configured-assets contract.

## What We're NOT Doing

- No new color palette, theme, chart library, frontend build pipeline, or
  component framework.
- No changes to market-data providers, scoring, ranking, AI summary contracts,
  chart calculations, persistence, or API routes.
- No dashboard, watchlist, Learn destination, or placeholder navigation.
- No production-visible UI fixture; the seven-state fixture is test/development
  support only and must be unavailable outside the Testing environment.
- No screenshot dependency installation; the visual gate uses the fixture plus
  manual desktop/mobile inspection.

## Implementation Approach

Keep Razor responsible for semantic structure and server-rendered states, keep
all visual values in the existing CSS token layer, and use a small client-side
form controller only for pending-submit feedback. Branch the shared layout from
the current principal so public and authenticated navigation remain distinct.
Use the existing analysis contract tests for server behavior and add focused
markup/state assertions for the new navigation, empty, loading, and fixture
contracts.

## Critical Implementation Details

The loading state must be applied before the form submits and must not replace
server-side validation or provider-error handling. The state fixture must be
guarded by the Testing environment so it cannot become a production product
surface.

## Phase 1: Establish the visual verification fixture

### Overview

Create a non-production fixture that renders the `/analysis` selection and
result surfaces in the seven required UI states using the same stylesheet and
semantic classes as the real view.

### Changes Required:

#### 1. Test-only analysis UI state fixture

**Files**: `Pages/Analysis/UiStateFixture.cshtml`,
`Pages/Analysis/UiStateFixture.cshtml.cs`, and focused tests under
`tests/Argus.Authentication.Tests/`

**Intent**: Provide a deterministic browser-review surface for the seven-state
matrix without adding a screenshot runner or exposing a new product area.

**Contract**: The fixture is reachable only in the Testing environment, uses
real `/analysis` classes and token variables, renders default/hover/
focus-visible/disabled/error/empty/loading examples, and returns not found
outside Testing.

### Success Criteria:

#### Automated Verification:

- The focused test project proves the fixture renders in Testing and is not
  available in the non-Testing application configuration.
- The fixture references the existing stylesheet and does not add literal
  colors or arbitrary layout values.

#### Manual Verification:

- The fixture displays all seven states together at desktop width and remains
  readable at one narrow mobile width.

## Phase 2: Refine the semantic token hierarchy

### Overview

Extend or reorganize only the existing semantic roles needed to distinguish
interactive, primary, secondary, warning, disabled, and focus states.

### Changes Required:

#### 1. Shared analysis token roles

**File**: `wwwroot/css/site.css`

**Intent**: Make the existing palette and spacing roles express the hierarchy
needed by the analysis workspace instead of adding view-local values.

**Contract**: Asset hover, checked, focus-visible, disabled, error, result
primary content, secondary evidence, and responsive spacing use CSS variables
from the existing root token block. The hardcoded-value scan remains at zero
for the edited view.

#### 2. Agent UI guidance

**File**: `AGENTS.md`

**Intent**: Preserve the discovered UI contract for future agents.

**Contract**: Add a concise rule outside the managed 10x block naming
`wwwroot/css/site.css` as the token source, forbidding literal colors and
arbitrary view values, and pointing to the non-production analysis fixture.

### Success Criteria:

#### Automated Verification:

- The hardcoded-value scan for `Pages/Analysis/Index.cshtml` reports zero
  candidates.
- CSS syntax remains valid and the focused test project passes.

#### Manual Verification:

- Token changes preserve readable contrast and visible focus in the existing
  dark color scheme.

## Phase 3: Improve the authenticated analysis workspace

### Overview

Apply the token hierarchy to the shared authenticated shell and the existing
`/analysis` view while preserving current Razor data flow.

### Changes Required:

#### 1. Authenticated navigation branch

**File**: `Pages/Shared/_Layout.cshtml`

**Intent**: Make global navigation reflect whether the current principal is
anonymous or authenticated.

**Contract**: Anonymous users retain the public landing navigation and sign-in
link. Authenticated users see only real Analysis and Account destinations,
including a direct account/logout path; no future placeholder destinations are
added.

#### 2. Analysis selection and result hierarchy

**Files**: `Pages/Analysis/Index.cshtml`, `wwwroot/css/site.css`

**Intent**: Improve interactive affordance, content hierarchy, and responsive
scanning without changing analysis semantics.

**Contract**: Asset options expose hover, checked, and focus-visible states.
Result cards visually prioritize asset/rank/score and group metrics, factors,
chart, warnings, and summaries as secondary evidence. At the narrow breakpoint,
metrics use two readable columns and do not introduce horizontal overflow.

#### 3. Authenticated navigation tests

**File**: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`
or the established account-access contract test file

**Intent**: Protect the public-versus-authenticated navigation distinction.

**Contract**: Authenticated analysis markup contains Analysis and Account
  navigation, while anonymous landing markup continues to contain the public
  sign-in navigation.

### Success Criteria:

#### Automated Verification:

- Existing authentication and analysis access tests pass.
- New assertions cover authenticated navigation and preserve public landing
  navigation.
- The hardcoded-value scan remains at zero for the analysis view.

#### Manual Verification:

- At desktop width, the shell, picker, score, and primary evidence read in a
  clear order without competing borders.
- At the selected mobile width, the two-column metrics remain legible and the
  chart controls do not overflow.
- Keyboard focus is visible on navigation, asset options, submit, and chart
  controls.

## Phase 4: Complete state coverage and verification

### Overview

Add pending-submit and empty-assets behavior, extend automated contracts, and
run the final visual and regression checks.

### Changes Required:

#### 1. Pending form state

**Files**: `Pages/Analysis/Index.cshtml`,
`wwwroot/js/analysis-form.js`, and relevant CSS in `wwwroot/css/site.css`

**Intent**: Give immediate feedback while the existing server POST is in
  flight and prevent accidental duplicate submissions.

**Contract**: On submit, the primary action becomes disabled, exposes a
  pending label/status, and remains accessible. The controller tolerates
  missing optional markup and does not alter server validation or result
  rendering.

#### 2. Empty configured-assets state

**Files**: `Pages/Analysis/Index.cshtml`,
`Pages/Analysis/Index.cshtml.cs`, and analysis access tests

**Intent**: Explain when no allowed assets are configured and avoid presenting
  a submit action that cannot succeed.

**Contract**: When `Assets` is empty, the view renders an explicit status,
  disables or removes the analysis action, and does not suppress existing
  server-side validation for malformed POST requests.

#### 3. Final UI contract checks

**Files**: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`,
`tests/js/` if the existing JavaScript test setup is used, and the relevant
fixture files

**Intent**: Make the new states and visual guard repeatable.

**Contract**: Tests cover empty assets, pending-state hooks, disabled action
  semantics, fixture availability, and unchanged result/chart markup. The
  seven-state fixture is manually checked at desktop and one mobile width.

### Success Criteria:

#### Automated Verification:

- `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
  passes.
- `dotnet build .bootstrap-scaffold.csproj --no-restore` passes with zero
  warnings and zero errors.
- Existing JavaScript tests pass if the pending controller is covered there.
- The hardcoded-value scan reports zero candidates for the analysis view.

#### Manual Verification:

- The fixture shows default, hover, focus-visible, disabled, error, empty, and
  loading states.
- A slow submission shows pending feedback and prevents duplicate activation.
- An empty configured-assets page is explanatory rather than a dead-end form.
- The authenticated shell is coherent on desktop and mobile; public landing
  navigation remains unchanged.

## Testing Strategy

### Unit Tests:

- Add focused assertions for empty configured assets, pending-state hooks,
  authenticated navigation, and Testing-only fixture access.
- Preserve existing analysis result, provider-error, chart, summary, and
  authentication contract tests.

### Integration Tests:

- Exercise authenticated GET `/analysis`, unauthenticated challenge behavior,
  valid POST results, invalid selection, provider failure, and empty asset
  configuration through the existing test factories.

### Manual Testing Steps:

1. Run the application in Testing configuration and open the UI state fixture.
2. Inspect the analysis page at a desktop width and one narrow mobile width.
3. Keyboard-tab through navigation, asset options, submit, and chart controls.
4. Submit a valid analysis and confirm pending feedback appears before results.
5. Verify public landing navigation remains unchanged when signed out.

## Performance Considerations

The pending controller is local DOM behavior and does not add network work.
The fixture is Testing-only and has no production request path. The analysis
result data, provider calls, and chart calculations remain unchanged.

## Migration Notes

No data or configuration migration is required. The change adds only UI
markup, CSS, a small client-side pending controller, test coverage, and a
Testing-only visual fixture.

## References

- Research: `context/changes/analysis-workspace-ui-refinement/research.md`
- Approved workspace design:
  `docs/superpowers/specs/2026-10-20-authenticated-analysis-workspace-design.md`
- Existing analysis view: `Pages/Analysis/Index.cshtml`
- Existing token source: `wwwroot/css/site.css`
- Existing analysis contracts:
  `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Establish the visual verification fixture

#### Automated

- [x] 1.1 Testing-only fixture renders through the focused test application
- [x] 1.2 Fixture is unavailable outside the Testing environment and uses the existing token source

#### Manual

- [ ] 1.3 Seven-state fixture is readable at desktop and narrow mobile widths

### Phase 2: Refine the semantic token hierarchy

#### Automated

- [x] 2.1 Analysis-view hardcoded-value scan reports zero candidates
- [x] 2.2 Focused tests pass after token and agent-guidance changes

#### Manual

- [ ] 2.3 Token changes preserve contrast and visible focus in the dark theme

### Phase 3: Improve the authenticated analysis workspace

#### Automated

- [x] 3.1 Authenticated and public navigation contracts pass
- [x] 3.2 Existing analysis access contracts pass without result or chart regressions
- [x] 3.3 Analysis-view hardcoded-value scan remains at zero candidates

#### Manual

- [ ] 3.4 Desktop hierarchy and mobile two-column metrics are readable without overflow
- [ ] 3.5 Keyboard focus is visible across navigation, picker, submit, and chart controls

### Phase 4: Complete state coverage and verification

#### Automated

- [x] 4.1 Pending-submit and empty-assets contracts pass
- [x] 4.2 Full focused test project passes
- [x] 4.3 Production build passes with zero warnings and errors
- [x] 4.4 Final hardcoded-value scan reports zero candidates

#### Manual

- [ ] 4.5 Seven-state fixture and final analysis flow pass desktop/mobile review
- [ ] 4.6 Public landing, authenticated shell, loading, empty, error, and result states are coherent
