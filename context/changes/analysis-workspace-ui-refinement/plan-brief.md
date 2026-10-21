# Refine the Authenticated Analysis Workspace — Plan Brief

> Full plan: `context/changes/analysis-workspace-ui-refinement/plan.md`
> Research: `context/changes/analysis-workspace-ui-refinement/research.md`

## What & Why

This change refines the existing `/analysis` view after its first complete
vertical slice is already working. It focuses on visual hierarchy, authenticated
navigation, responsive scanning, and explicit loading/empty states rather than
introducing a new palette or changing analysis behavior.

## Starting Point

The view already renders asset selection, ranked evidence, factors, charts,
summaries, provider errors, and educational-boundary copy using
`wwwroot/css/site.css`. The current audit found zero hardcoded-value candidates,
but identified missing hover feedback, competing inner surfaces, one-column
mobile metrics, public navigation shown to authenticated users, and missing
loading/empty-assets states.

## Desired End State

Authenticated users enter a coherent Analysis/Account shell and can scan the
analysis from asset selection through score and evidence without competing
surfaces. Slow submissions, empty configuration, focus, disabled controls, and
the other required UI states are explicit and reviewable in a Testing-only
fixture.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Navigation | Branch the shared layout for authenticated users | The approved workspace design requires Analysis and Account as the only real authenticated destinations | Research / Plan |
| Loading | Client-only pending submit state | Gives immediate feedback without changing the existing POST contract | Plan |
| Empty assets | Explicit explanatory state with disabled or removed action | Prevents a misleading validation dead end | Research / Plan |
| Mobile metrics | Two compact columns below the narrow breakpoint | Improves scanning while retaining all displayed evidence | Research / Plan |
| Visual gate | Testing-only seven-state fixture | Provides repeatable review without adding a screenshot dependency | Plan |
| Design system | Existing `wwwroot/css/site.css` semantic tokens | The view already uses this source and the scan found no literal candidates | Research |

## Scope

**In scope:**

- Existing `/analysis` markup and shared authenticated navigation.
- Existing CSS token roles and analysis-specific layout rules.
- Client-only pending-submit behavior.
- Empty configured-assets rendering.
- Contract tests and a Testing-only seven-state fixture.

**Out of scope:**

- Market data, scoring, ranking, chart calculations, AI summaries, persistence,
  APIs, dashboards, watchlists, and new dependencies.
- Production-visible fixture routes or a screenshot-runner installation.

## Architecture / Approach

Razor remains responsible for server-rendered semantic states. CSS variables in
`wwwroot/css/site.css` remain the single visual value source. The shared layout
branches on the current principal, while a small client-side controller handles
only the pending form state. Existing analysis and authentication test
factories remain the verification foundation.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Visual fixture | Testing-only seven-state review surface | Fixture could become a production route if the environment guard is incomplete |
| 2. Token hierarchy | Semantic hover, surface, focus, disabled, error, and spacing roles | Accidental palette drift or reduced contrast |
| 3. Workspace layout | Authenticated nav, clearer cards, hover affordance, mobile metrics | Shared layout changes could regress public landing navigation |
| 4. State coverage | Pending submit, empty assets, tests, and final visual checks | Client feedback must not hide server errors or validation |

**Prerequisites:** Existing `/analysis` flow, `wwwroot/css/site.css`, current
authentication test factory, and the approved workspace design.

**Estimated effort:** Approximately 2–3 implementation sessions across four
phases, with manual desktop/mobile review after visual phases.

## Open Risks & Assumptions

- The shared layout is used by public and authenticated pages, so navigation
  assertions must cover both states.
- The visual fixture is a review aid and must be guarded from production.
- No browser screenshot runner is installed; manual fixture review is the
  chosen visual gate.

## Success Criteria (Summary)

- Authenticated and anonymous navigation communicate the correct state without
  changing the public landing page.
- `/analysis` remains readable and keyboard-usable on desktop and one mobile
  width, with the score and primary evidence visually prioritized.
- Loading, empty, error, disabled, focus, and result states are explicit,
  tested, and represented in the seven-state fixture.
