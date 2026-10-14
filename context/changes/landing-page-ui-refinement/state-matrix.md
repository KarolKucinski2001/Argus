# Landing Page State Matrix

## Verified states

| State | Result | Evidence |
| --- | --- | --- |
| Default | Pass | Public landing page renders the hero, hero ranking, workflow cards, and educational boundary. |
| Hover | Pass | Existing token-backed link and button hover rules remain applicable. |
| Focus-visible | Pass | Existing global focus-visible outline applies to the brand and CTA links. |
| Desktop | Pass | User manually verified the landing page at `1440x900` without clipping or unintended horizontal overflow; no screenshot artifact is persisted because the repository has no screenshot runner or stored capture workflow. |
| Mobile | Pass | User manually verified the landing page at `390x844` without clipping or unintended horizontal overflow; no screenshot artifact is persisted because the repository has no screenshot runner or stored capture workflow. |

## Not applicable states

| State | Result | Reason |
| --- | --- | --- |
| Disabled | N/A | The public landing page has no disabled controls. |
| Loading | N/A | The public landing page does not load data at runtime. |
| Error | N/A | The public landing page has no analysis request or inline error path. |
| Empty | N/A | The public landing page displays fixed illustrative content, not a query result. |

The hero ranking retains the `Dane ilustracyjne` label, and the educational
disclaimer remains visible. This page does not present live or personalized
investment advice.
