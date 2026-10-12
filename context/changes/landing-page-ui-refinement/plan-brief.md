# Landing Page UI Refinement — Plan Brief

> Full plan: `context/changes/landing-page-ui-refinement/plan.md`
> Research: `context/changes/landing-page-ui-refinement/research.md`

## What & Why

This plan refines the existing Argus landing page rather than creating a new
view. It addresses five observed problems: responsive clipping, missing brand
identity, competing hero emphasis, a text-only process section, and incomplete
mobile/state verification.

The work preserves the current educational analysis positioning and keeps the
landing preview explicitly illustrative rather than turning it into a live
investment recommendation.

## Starting Point

The application uses Razor Pages and ordinary CSS. `Pages/Index.cshtml`
contains the hero, illustrative ranking, three text-only process cards, and the
educational boundary; `Pages/Shared/_Layout.cshtml` currently renders only the
text brand `Argus`.

The repository already has semantic CSS tokens in `wwwroot/css/site.css` and
static asset mapping in `Program.cs`, but it has no image directory, shared view
component library, or screenshot runner.

## Desired End State

The header shows the selected white Argus symbol beside the text brand. The
hero leads with a clear product message, while the Opportunity Score preview
supports that message without dominating it.

The process section connects its steps to the existing illustrative ranking and
factors. At `1440x900` and `390x844`, the page remains readable without clipping
or horizontal scrolling; long preview rows stack on the mobile layout.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Header branding | White symbol + text `Argus` | Contrasts with the dark header and preserves an accessible text name without creating a new logo asset | Research / Plan |
| Process value proof | Reuse the existing ranking/factors | Avoids a second competing data story while making the process outcome visible | Research / Plan |
| Screenshot viewports | `1440x900` and `390x844` | Covers a representative desktop and phone layout while keeping the gate reproducible | Plan |
| Mobile preview rows | Stack labels and values | Prevents clipping of long Polish labels and avoids horizontal page scrolling | Plan |
| State gate | Manual matrix and screenshots | Matches the existing repository constraints without adding a frontend testing stack | Research / Plan |
| N/A states | Disabled, loading, error, empty | The public landing page has no corresponding controls or data-loading/error flow | Research |

## Scope

**In scope:**

- Add `argus-symbol-white.svg` under `wwwroot/img/`.
- Integrate the symbol into the shared header with accessible text.
- Refine token-backed responsive sizing, wrapping, spacing, and hero hierarchy.
- Connect the process section to the existing illustrative ranking/factors.
- Extend stable landing-page contract tests.
- Add a concise UI rule to `AGENTS.md`.
- Verify the agreed state matrix and screenshot pair.

**Out of scope:**

- Backend, authentication, market-data, scoring, and roadmap changes.
- New frontend frameworks, component libraries, screenshot runners, or build
  dependencies.
- Account-page redesign, favicon rollout, or full MVP rebranding.
- Live landing-page data, personalized advice, or new interactive loading/error
  flows.

## Architecture / Approach

The implementation stays within the current server-rendered Razor + BEM-style
CSS architecture. A repository-owned SVG is served by the existing static asset
pipeline; the layout uses it as a decorative mark beside accessible text. CSS
tokens remain the source of visual values, and the existing contract-test
project protects stable markup while manual screenshots protect layout and
hierarchy.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Asset and environment contract | Versioned white symbol integrated into the shared header | Asset contrast or focus treatment is missed |
| 2. Token-backed responsive values and hierarchy | Explicit desktop/mobile layout and clearer hero emphasis | A fix for clipping could make the hero too tall or too sparse |
| 3. Landing-page value proof | Process connected to the existing illustrative result | The added proof competes with the hero or implies live advice |
| 4. State matrix, documentation, and visual gate | Rule, tests, screenshots, and N/A state record | Manual evidence is incomplete or not reproducible |

**Prerequisites:** Existing landing page, token source, static asset mapping, and
the supplied logo archive.

**Estimated effort:** Approximately 1–2 implementation sessions across four
phases, including manual screenshot verification.

## Open Risks & Assumptions

- Browser measurements have not yet been captured; the source audit identifies
  likely causes and the implementation must validate rendered pixels.
- The selected white symbol is intentionally paired with text; a full
  horizontal light-on-dark wordmark is not part of this change.
- The process proof must remain illustrative and subordinate to the hero
  message.
- The existing application can continue serving SVG files through its current
  `MapStaticAssets` configuration.

## Success Criteria (Summary)

- The landing page renders the brand mark and remains readable at both agreed
  viewport sizes without clipping or horizontal scrolling.
- The hero communicates the primary product value before the supporting
  illustrative ranking, and the process visibly connects to that result.
- Existing build/tests pass, focus and hover remain usable, and inapplicable
  static-page states are explicitly documented as N/A.
