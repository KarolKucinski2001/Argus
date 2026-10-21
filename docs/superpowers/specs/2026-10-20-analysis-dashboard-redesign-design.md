# Analysis Dashboard Redesign

## Status

Approved design. Ready for implementation planning.

## Goal

Redesign the authenticated `/analysis` view into a dark fintech-style split
dashboard inspired by the supplied reference image. The result should make
asset selection and explained analysis feel like one coherent workspace while
preserving Argus's existing server-rendered data, authentication, analysis,
chart, error, and educational-boundary contracts.

## Product decisions

- `/analysis` becomes a full split dashboard.
- Desktop uses a persistent asset-selection sidebar beside the analysis
  content.
- Mobile stacks the asset-selection panel above the results and keeps it
  expanded.
- Result cards remain visible without tabs; all existing sections stay
  available in the page flow.
- The visual direction is dark blue fintech UI with restrained borders,
  semantic surfaces, blue actions/focus, and green positive status accents.
- No watchlist, prediction, drag-and-drop, new provider, new data source, or
  placeholder destination is added.

## Layout architecture

The existing Razor Page and `IndexModel` remain the source of page behavior.
The view structure becomes:

```text
.analysis-workspace
├── .analysis-workspace__sidebar
│   ├── asset-selection header
│   ├── asset-option list
│   ├── validation/provider/empty states
│   └── analysis submit action
└── .analysis-workspace__content
    ├── dashboard heading and description
    ├── educational boundary/status content
    └── .analysis-results__list
        └── .analysis-result-card
            ├── asset/rank/score header
            ├── metrics
            ├── warnings and factors
            ├── price history chart
            └── grounded summary
```

On desktop, CSS Grid places the sidebar and content beside each other. On
mobile, the same regions become one column with selection first. Results use a
two-column card grid where the available width supports it and collapse to one
column on narrow screens. Existing chart controls and the exact-value fallback
remain inside each result card.

## Visual system

Use the existing semantic token source in `wwwroot/css/site.css`. Extend roles
only where needed for the dashboard hierarchy:

- application background and raised surfaces;
- primary action and action-hover;
- focus ring;
- disabled action and disabled surface;
- feedback/error surface;
- secondary evidence surface and divider.

Razor views must contain no literal colors or arbitrary layout values. The
asset picker uses a clear hover, checked, and focus-visible treatment. The
result header, asset identity, rank, score, and current status receive the
strongest hierarchy. Metrics, factors, charts, warnings, and summaries use
secondary surfaces and dividers without competing with the primary result.

## Interaction and state behavior

The implementation preserves the seven-state contract:

- **default**: token-backed picker, cards, metrics, and content;
- **hover**: full asset rows and actionable controls visibly respond;
- **focus-visible**: navigation, picker controls, submit, chart controls, and
  chart points expose a visible semantic focus treatment;
- **disabled**: submit is disabled during pending submission and when no assets
  are configured;
- **error**: validation and provider errors remain adjacent to selection;
- **empty**: an empty configured-asset collection explains the blocker and
  presents no misleading active action;
- **loading**: the client-only pending state disables duplicate submission,
  exposes a status message, and does not alter server validation or result
  rendering.

No tabs are introduced. Existing stale-data, partial-result,
insufficient-evidence, asset-error, provider-error, chart fallback, and
educational-boundary states remain in the normal page flow.

## Data flow and boundaries

`IndexModel`, `OnGet`, `OnPostAsync`, market-data services, scoring, ranking,
summary generation, chart calculations, and authentication routing remain
unchanged unless a markup hook is strictly required. The redesign adds no
additional requests and does not move analysis logic into JavaScript.

The shared authenticated layout continues to expose only real Analysis,
Account, and logout destinations. Anonymous landing navigation remains
unchanged. The Testing-only UI state fixture remains outside production
availability.

## Testing and verification

Automated coverage will:

- verify the new workspace/sidebar/card structure through focused Razor
  contract assertions;
- preserve authenticated and anonymous navigation contracts;
- preserve result, factor, chart, summary, provider-error, and validation
  contracts;
- cover empty-assets and pending-submit hooks;
- keep the hardcoded-value scan at zero for production analysis views;
- run the focused .NET test project and production build.

Manual verification will inspect:

1. the seven-state fixture at desktop and narrow mobile widths;
2. the dashboard hierarchy and two-column result cards at desktop width;
3. the stacked mobile selector and result cards without horizontal overflow;
4. keyboard focus across navigation, asset rows, submit, and chart controls;
5. pending, empty, error, partial, stale, insufficient-evidence, and result
   states;
6. unchanged anonymous landing navigation.

No screenshot dependency is introduced. The existing Testing-only fixture is
the visual gate.

## Out of scope

- new market-data providers or production credentials;
- new scoring, ranking, chart, or AI-summary behavior;
- watchlist persistence or future navigation;
- prediction cards or predictive language;
- tabs, drag-and-drop selection, frontend build tooling, or a component
  framework;
- redesign of unrelated views or a global brand rework.
