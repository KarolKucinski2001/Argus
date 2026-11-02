# Explained Market Ranking — Plan Brief

> Full plan: `context/changes/explained-market-ranking/plan.md`

## What & Why

S-03 completes Argus's first explained market-analysis result: a ranked set
of selected assets with scores, factors, market context, historical evidence,
and a grounded explanation. The approved scope adds one provider-neutral AI
summary per sufficiently evidenced asset while keeping the existing
educational-not-investment-advice boundary explicit.

## Starting Point

The repository already calculates and renders scores, rankings, performance
periods, factor contributions, chart points, quality/freshness states, and
partial provider results. The remaining gaps are the real grounded AI
boundary, explicit summary status handling, and a visible disclaimer on the
analysis result surface.

## Desired End State

Each asset with at least two reliable factors receives an AI summary generated
only from the evidence shown for that asset. Assets without enough evidence
skip AI and show an explicit insufficient-evidence state; AI failures preserve
the ranking and show a bounded unavailable state. No provider details,
credentials, raw model output, or personalized investment advice reaches the
browser.

## Key Decisions Made

| Decision | Choice | Why (1 sentence) | Source |
| --- | --- | --- | --- |
| AI scope | Real grounded AI summary | S-03 must satisfy PRD FR-011 and FR-013 rather than retain the earlier deterministic-only limitation. | Plan |
| Provider boundary | Provider-neutral interface and adapter | Keeps scoring and UI independent of a vendor and preserves test replacement. | Plan |
| Summary granularity | One summary per asset | Matches the PRD's asset-level explanation and keeps grounding inputs unambiguous. | Plan |
| Insufficient evidence | Skip AI and show explicit state | Avoids inventing certainty when fewer than two reliable factors exist. | Plan |
| AI failure | Preserve partial analysis | Ranking, score, and factors remain useful even when optional explanation generation fails. | Plan |
| Copilot/chat | Out of scope | It is secondary in the PRD and has no retained S-03 requirement. | Roadmap / PRD |

## Scope

**In scope:**

- Provider-neutral grounded-summary contract and configurable adapter boundary.
- Per-asset evidence gating, bounded output validation, and safe failure states.
- Razor result rendering for generated, insufficient, and unavailable summaries.
- Visible educational-not-investment-advice boundary.
- Deterministic unit/page tests and documentation updates.

**Out of scope:**

- Conversational Copilot, follow-up questions, or chat history.
- Production market-data provider activation.
- Persistence, watchlists, alerts, portfolio management, and trading.
- Provider-specific SDK types in the analysis domain.

## Architecture / Approach

The existing normalized market-data → performance → scoring → ranking flow
remains intact. A new summary-generation port is called by the analysis
composition layer only after sufficient evidence is available; a configured
provider-neutral adapter supplies the model response, while tests use a local
fake. The Razor Page maps the returned summary status into bounded UI states
without exposing transport or provider details.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Grounded AI contract | Stable evidence input, result states, provider-neutral port, and safe configuration boundary. | AI contract accidentally accepts facts not visible in the result. |
| 2. Analysis integration | Per-asset calls, evidence gate, bounded validation, and partial-result resilience. | AI failure could incorrectly discard otherwise valid analysis. |
| 3. Result surface | Generated summaries, explicit unavailable/insufficient states, and educational boundary. | UI could imply certainty or personalized advice. |
| 4. Verification and docs | Full deterministic test matrix and synchronized S-03 documentation. | Tests may cover only the happy path and miss leakage/failure states. |

**Prerequisites:** S-02 is archived and its protected asset-selection and
normalized market-data contract remain available. AI credentials are supplied
only through approved non-production configuration when manual verification is
performed.

**Estimated effort:** Not estimated; the plan is divided into four independently
verifiable phases and preserves the existing analysis implementation.

## Open Risks & Assumptions

- The approved AI transport must support a provider-neutral adapter contract;
  vendor-specific configuration remains outside the analysis domain.
- Model output cannot be treated as evidence; the application must constrain
  input and reject unsafe or malformed output.
- Per-asset calls must respect the existing selection limit and cancellation
  path; no retries or background jobs are introduced.
- The earlier deterministic explanation design is superseded only for the
  summary-generation portion; score and evidence rules remain unchanged.

## Success Criteria (Summary)

- Authenticated users see a ranked, explained result for selected assets with
  one grounded AI summary per sufficiently evidenced asset.
- Insufficient evidence and AI unavailability are visible, distinct, and do
  not remove valid scores, factors, metrics, or chart data.
- Automated tests prove grounding boundaries, no raw-provider leakage, safe
  partial results, and compatibility with existing market-data contracts.
