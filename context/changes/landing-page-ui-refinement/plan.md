# Landing Page UI Refinement Implementation Plan

## Overview

Refine the existing public Argus landing page so its first screen communicates
the product promise clearly, uses the prepared brand mark, remains readable
without clipping at desktop and mobile widths, and shows how the analysis
process produces an illustrative result. The work stays within the existing
Razor Pages + ordinary CSS approach and preserves the educational-not-
investment-advice boundary.

## Current State Analysis

The landing page is rendered by `Pages/Index.cshtml` through
`Pages/Shared/_Layout.cshtml`. Its visual values come from the token block in
`wwwroot/css/site.css`; the repository has no shared view-component directory
or frontend screenshot runner. The header currently renders only the text
`Argus`, the hero uses a large heading and a multi-row illustrative ranking,
and the process section contains three text-only cards.

Research identified five connected issues:

- The hero's `34rem` minimum, large block padding, heading scale, and preview
  card can exceed the intended first-viewport budget.
- The preview's flex rows have no narrow-screen stacking contract, while the
  body retains a `20rem` minimum width.
- The supplied horizontal logo is dark-on-light; the selected treatment is
  `argus-symbol-white.svg` beside the accessible text brand on the dark header.
- The hero copy and preview card compete for attention.
- The process section does not visually connect its steps to an analysis result.

## Desired End State

At `1440x900`, the landing page presents the primary product message and its
supporting illustrative preview within a deliberate first-screen composition.
At `390x844`, the header, hero, CTAs, preview, and following section remain
readable without horizontal clipping; preview labels and values stack when
needed.

The header contains the selected white symbol and the text `Argus`. The process
section reuses the existing illustrative ranking/factors as visual proof rather
than introducing a competing second data story. Hover and focus-visible states
remain visible, and static-page states that do not apply are documented as N/A.

### Key Discoveries:

- `wwwroot/css/site.css:1-27` is the existing semantic token source.
- `Pages/Shared/_Layout.cshtml:11` is the text-only brand insertion point.
- `Program.cs:107` already maps static assets through `app.MapStaticAssets()`.
- `Pages/Index.cshtml:64-87` is the text-only process section to connect to the
  existing preview.
- `Pages/Index.cshtml:89-99` contains the educational boundary that must remain.
- `docs/superpowers/specs/2026-10-12-landing-page-design.md:37-72` defines the
  existing visual, accessibility, and validation constraints.

## What We're NOT Doing

- No backend, authentication, market-data, scoring, or roadmap changes.
- No new frontend framework, component library, screenshot runner, or build
  dependency.
- No full MVP rebrand or redesign of account pages.
- No light header theme and no recoloring of the supplied SVG in Razor.
- No live market data in the landing preview; all displayed scores and factors
  remain clearly illustrative.
- No loading, error, empty, or disabled interaction model for this static page;
  those states are recorded as N/A with reasons.
- No favicon or app-store asset rollout unless it is required by a later,
  separate change.

## Implementation Approach

Keep the current server-rendered markup and BEM-style CSS. First add the
selected white symbol as a static asset and place it beside the text brand.
Then adjust the existing token-backed layout rules to reduce viewport pressure,
establish a mobile stacking contract, and lower the preview's visual dominance.
Finally connect the process section to the existing illustrative result without
duplicating a second ranking model, and verify the resulting page through the
existing contract tests plus manual screenshots at the agreed viewports.

## Critical Implementation Details

The preview must retain its `Dane ilustracyjne` labeling and the boundary card
must remain visible in the page flow. The screenshot gate is manual and uses
exactly `1440x900` and `390x844`; do not add a new browser-testing dependency
to automate it.

## Phase 1: Asset and environment contract

### Overview

Introduce the selected white Argus symbol as a repository-owned static asset
and wire it into the existing shared header without changing static asset
serving or introducing a second branding system.

### Changes Required:

#### 1. Logo asset

**File**: `wwwroot/img/argus-symbol-white.svg`

**Intent**: Copy the approved white symbol from the user-provided archive into
the repository so the landing page has a stable, versioned brand asset that
contrasts with the dark header.

**Contract**: The asset is served below `/img/argus-symbol-white.svg` through
the existing static asset mapping and retains the supplied vector viewBox and
white artwork.

#### 2. Shared header brand

**File**: `Pages/Shared/_Layout.cshtml`

**Intent**: Add the decorative symbol to the existing brand link while
retaining the visible `Argus` text as the accessible name and link label.

**Contract**: The brand remains one link to `/`; the image uses an empty alt
text because adjacent text names the link, has explicit dimensions through CSS,
and does not replace the text accessible name.

#### 3. Brand styling

**File**: `wwwroot/css/site.css`

**Intent**: Add token-backed sizing and alignment for the symbol without
introducing literal colors or a separate header treatment.

**Contract**: `.brand` and its image use existing spacing, text, and focus
  conventions; the logo does not change the established dark header surface.

### Success Criteria:

#### Automated Verification:

- The SVG exists at `wwwroot/img/argus-symbol-white.svg` and the application
  build can include it as a static asset.
- The existing authentication contract test project passes.

#### Manual Verification:

- The header shows the white symbol and `Argus` text at `1440x900` and
  `390x844`.
- Keyboard focus remains visible around the complete brand link.

**Implementation Note**: After this phase, pause for visual confirmation before
continuing to the token and layout phase.

## Phase 2: Token-backed responsive values and hierarchy

### Overview

Adjust the existing CSS contract so the hero, preview, workflow grid, CTAs,
and preview rows have explicit desktop/tablet/mobile behavior without
introducing one-off view literals.

### Changes Required:

#### 1. Hero and breakpoint contract

**File**: `wwwroot/css/site.css`

**Intent**: Reduce the hero's vertical pressure and prevent the two-column
layout from activating before the copy and preview have adequate space.

**Contract**: The hero remains token-backed and uses a responsive heading,
spacing, and column definition that can fit the selected desktop viewport;
the mobile path must not depend on a `20rem` body minimum to remain usable.

#### 2. Preview row states

**File**: `wwwroot/css/site.css`

**Intent**: Give the preview header, asset rows, and factor rows an explicit
narrow-screen stacking behavior for long Polish labels and values.

**Contract**: At the selected mobile width, labels and values may stack
vertically; no preview content may require horizontal page scrolling. Desktop
rows remain compact and aligned.

#### 3. Visual hierarchy

**File**: `wwwroot/css/site.css`

**Intent**: Make the product promise and primary CTA lead while retaining the
preview as supporting evidence.

**Contract**: The heading, CTA, card shadow, score scale, and inter-section
spacing continue to use semantic tokens; the preview remains visible and
clearly illustrative rather than being removed.

### Success Criteria:

#### Automated Verification:

- The view and stylesheet contain no new color literals or arbitrary spacing
  values outside the token declarations.
- `dotnet build .bootstrap-scaffold.csproj --no-restore` succeeds.

#### Manual Verification:

- At `1440x900`, the hero composition does not create unintended horizontal
  overflow and the primary message reads before the supporting card.
- At `390x844`, the preview rows stack or wrap without clipped labels, and the
  CTAs remain usable without horizontal scrolling.
- The workflow grid transitions before its columns become too narrow.

**Implementation Note**: After this phase, capture the agreed desktop and
mobile screenshots before changing the process section.

## Phase 3: Landing-page value proof

### Overview

Restructure the existing process section so its steps lead into the existing
illustrative ranking and factors, giving users a visible example of the value
produced by the workflow without creating a second scoring model.

### Changes Required:

#### 1. Process markup

**File**: `Pages/Index.cshtml`

**Intent**: Connect the three process steps to a compact visual result based on
the already-present illustrative assets, scores, and factors.

**Contract**: The section remains static and semantic, keeps the existing
`#how-it-works` anchor, identifies the result as illustrative, and does not
claim current or personalized investment advice.

#### 2. Process/result styling

**File**: `wwwroot/css/site.css`

**Intent**: Give the connected process/result composition a clear reading order
on desktop and a single-column reading order on mobile.

**Contract**: New classes use the existing semantic tokens and BEM naming
pattern; the result visualization does not duplicate a separate ranking or
introduce unscoped colors.

#### 3. Landing contract coverage

**File**: `tests/Argus.Authentication.Tests/AccountAccessContractTests.cs`

**Intent**: Extend the existing public landing contract to protect the selected
brand and value-proof copy while preserving the current educational boundary.

**Contract**: Tests assert stable semantic text/attributes and route behavior,
not brittle pixel-specific markup.

### Success Criteria:

#### Automated Verification:

- `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
  passes with the landing-page contract assertions.
- The public landing response still contains the educational boundary and
  illustrative-data label.

#### Manual Verification:

- The process section visibly explains how the steps lead to a ranking/result.
- The result remains secondary to the primary hero message and does not read as
  a live recommendation.
- The process/result composition remains readable at `1440x900` and
  `390x844`.

**Implementation Note**: Do not proceed to the final state gate until the
visual proof has been checked against the screenshot pair.

## Phase 4: State matrix, documentation, and visual gate

### Overview

Close the UI contract by verifying applicable interactive states, documenting
N/A states for the static page, adding the repository rule that preserves the
token/asset convention, and recording the final screenshot evidence.

### Changes Required:

#### 1. Agent UI rule

**File**: `AGENTS.md`

**Intent**: Preserve the discovered landing-page contract for future agents:
token values stay in `wwwroot/css/site.css`, assets stay in `wwwroot`, and
views do not add literal colors or arbitrary values.

**Contract**: Add the rule outside the managed 10x CLI block and point to the
landing-page files and manual screenshot evidence location without duplicating
framework documentation.

#### 2. State evidence

**Files**: `context/changes/landing-page-ui-refinement/plan.md` and
`context/changes/landing-page-ui-refinement/` screenshot evidence as available

**Intent**: Record the seven-state assessment and the agreed viewport checks
for the implementation review.

**Contract**: Default, hover, focus-visible, desktop, and mobile are shown or
verified; disabled, loading, error, and empty are marked N/A because this page
has no corresponding controls or data-loading/error path.

#### 3. Final validation

**Files**: `Pages/Index.cshtml`, `Pages/Shared/_Layout.cshtml`,
`wwwroot/css/site.css`, `wwwroot/img/argus-symbol-white.svg`,
`tests/Argus.Authentication.Tests/AccountAccessContractTests.cs`

**Intent**: Run the complete targeted validation after all visual changes and
leave the change ready for `/10x-impl-review`.

**Contract**: The final implementation preserves the public route, CSS static
asset serving, accessibility names, educational boundary, and selected
responsive viewport contract.

### Success Criteria:

#### Automated Verification:

- `dotnet build .bootstrap-scaffold.csproj --no-restore` passes.
- `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
  passes.
- The final view scan reports no new hardcoded color functions, hex values, or
  arbitrary spacing values in `Pages/Index.cshtml`.

#### Manual Verification:

- Screenshot gate passes at `1440x900` and `390x844` with no clipping or
  unintended horizontal overflow.
- Keyboard traversal shows a visible focus state for the brand and CTAs.
- Hover states are visible for applicable links and buttons.
- The seven-state record explicitly marks disabled/loading/error/empty as N/A
  with the static-page reason.
- The educational disclaimer and `Dane ilustracyjne` label remain visible and
  understandable.

## Testing Strategy

### Unit Tests:

No new unit-test model is required. Reuse the existing
`AccountAccessContractTests` project for stable landing markup, asset serving,
route, and educational-boundary assertions.

### Integration Tests:

- Request `/` through the existing `WebApplicationFactory` contract.
- Request `/css/site.css` and the selected SVG path to verify static serving.
- Preserve the existing account-access and redirect contract tests.

### Manual Testing Steps:

1. Run the application in Development and open `/` at `1440x900`.
2. Confirm the first viewport hierarchy, logo, CTA visibility, and process
   result relationship.
3. Open `/` at `390x844`; inspect header, hero, preview rows, workflow, and
   boundary card for clipping or horizontal scrolling.
4. Traverse the interactive links with the keyboard and verify visible focus.
5. Hover applicable links/buttons and confirm token-backed state changes.
6. Record loading, error, empty, and disabled as N/A for the static public page.

## Performance Considerations

The selected SVG is a small static asset and requires no runtime processing.
The change adds no client-side framework, data request, animation loop, or
additional build dependency.

## Migration Notes

No data or runtime migration is required. The change is backward-compatible with
the existing public route and static asset pipeline. If the selected symbol is
later replaced by an approved horizontal light-on-dark wordmark, that should be
handled as a separate asset decision rather than recoloring the view.

## References

- Research: `context/changes/landing-page-ui-refinement/research.md`
- Change identity: `context/changes/landing-page-ui-refinement/change.md`
- Landing view: `Pages/Index.cshtml:9-99`
- Shared layout: `Pages/Shared/_Layout.cshtml:7-11`
- Token source: `wwwroot/css/site.css:1-27`
- Static asset mapping: `Program.cs:107`
- Existing design constraints: `docs/superpowers/specs/2026-10-12-landing-page-design.md:37-72`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Asset and environment contract

#### Automated

- [x] 1.1 SVG asset exists at the selected static path and is included by the build — 3cad300
- [x] 1.2 Existing authentication contract tests pass — 3cad300

#### Manual

- [x] 1.3 White symbol and text brand render at desktop and mobile, with visible brand-link focus — 3cad300

### Phase 2: Token-backed responsive values and hierarchy

#### Automated

- [x] 2.1 View and stylesheet scan contains no new hardcoded view colors or arbitrary spacing values — d4f55a6
- [x] 2.2 Application build passes without errors — d4f55a6

#### Manual

- [x] 2.3 Desktop hierarchy and mobile preview/CTA behavior pass at the agreed viewports — d4f55a6

### Phase 3: Landing-page value proof

#### Automated

- [x] 3.1 Targeted authentication and landing contract tests pass
- [x] 3.2 Landing response retains the educational boundary and illustrative-data label

#### Manual

- [x] 3.3 Process steps visibly connect to a secondary illustrative result on desktop and mobile — 9c68d31

### Phase 4: State matrix, documentation, and visual gate

#### Automated

- [ ] 4.1 Final build and targeted tests pass
- [ ] 4.2 Final view scan reports no new hardcoded colors or arbitrary spacing values

#### Manual

- [ ] 4.3 Screenshot gate passes at 1440x900 and 390x844 without clipping
- [ ] 4.4 Keyboard focus and applicable hover states are visible
- [ ] 4.5 Static-page N/A states and educational boundary are recorded and verified
