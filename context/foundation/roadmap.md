---
project: "Argus"
version: 1
status: draft
created: 2026-09-30
updated: 2026-10-06
prd_version: 1
main_goal: speed
top_blocker: time
milestone_id: first-explained-market-analysis
milestone_seq: 1
milestone_status: open
---

# Roadmap: Argus

> Derived from `context/foundation/prd.md` + auto-researched and user-confirmed codebase baseline.
> Edit-in-place; archive when superseded.
> Slices below are listed in dependency order. The "At a glance" table is the index.

## Milestone

**M-1: First explained market analysis** — Status: open

- **Intent:** Deliver the first complete Argus workflow in which an authenticated beginner selects assets and receives a ranked analysis with visible data, contributing factors, grounded explanation, and the educational boundary.
- **Source materials:** `context/foundation/prd.md` (v1)
- **Done when:** every F-NN and S-NN below is `done`.
- **Scope anchors:** US-01, FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-008, FR-009, FR-010, FR-011, FR-012, FR-013, FR-014, FR-015, FR-016.

## Vision recap

Argus helps beginners or prospective first-time investors understand how selected assets may change without forcing them to assemble information from many pages, charts, and comparisons. The product combines selected market data, transparent scoring factors, and grounded AI summaries while remaining an educational analysis tool rather than personalized investment advice.

## North star

**S-03: User can view a grounded, explained ranking for selected assets** — this is the smallest complete value flow that validates the primary success criterion while keeping the launch path focused on speed.

> Here, "north star" means the smallest end-to-end slice whose successful delivery proves the product works; it is placed as early as its prerequisites allow.

## At a glance

| ID | Change ID | Outcome (user can …) | Prerequisites | PRD refs | Status |
| --- | --- | --- | --- | --- | --- |
| F-01 | minimal-auth-foundation | (foundation) authenticate users with the minimum protected-route contract needed by the MVP | — | Access Control, FR-015, FR-016 | done |
| F-02 | market-data-access-contract | (foundation) verify the minimum market-data access and evidence contract needed for analysis | — | FR-004, NFR | blocked |
| S-01 | account-access-flow | create an account, log in, and understand Argus's educational purpose | F-01 | FR-001, FR-014, FR-015, FR-016 | done |
| S-02 | select-and-start-analysis | select one or more assets and start an analysis | S-01, F-02 | FR-002, FR-003, FR-004 | blocked |
| S-03 | explained-market-ranking | view scores, ranking, market context, factors, chart, and a grounded AI explanation for selected assets | S-02 | FR-005, FR-006, FR-008, FR-009, FR-010, FR-011, FR-012, FR-013, FR-014, US-01 | proposed |

## Streams

Navigation aid — groups items that share a prerequisites chain. Canonical ordering still lives in the dependency graph below.

| Stream | Theme | Chain | Note |
| --- | --- | --- | --- |
| A | Account and protected workflow | `F-01` → `S-01` → `S-02` → `S-03` | The strict path to the first user-visible value supports the speed goal. |
| B | Market evidence contract | `F-02` → `S-02` | Joins Stream A at `S-02`; resolving the data-source decision unlocks the north star's evidence path. |

## Baseline

What's already in place in the codebase as of `2026-09-30` (auto-researched + user-confirmed). Foundations below assume these are present and do not re-scaffold them.

- **Frontend:** absent — no UI project or frontend dependencies are present.
- **Backend / API:** partial — minimal ASP.NET Core startup in `Program.cs`; only the starter `/weatherforecast` route exists.
- **Data:** absent — no database driver, ORM, schema, migration, or market-data integration is present.
- **Auth:** partial — authentication is declared in `tech-stack.md`, but the code has no authentication, authorization, or identity provider integration.
- **Deploy / infra:** present — `.github/workflows/deploy.yml` builds and deploys the current .NET application to Azure App Service.
- **Observability:** absent — no dedicated logging, metrics, tracing, or error-monitoring integration is present.

## Foundations

### F-01: Minimum authenticated access

- **Outcome:** (foundation) the application has the minimum authentication and protected-route contract required for the MVP workflow.
- **Change ID:** minimal-auth-foundation
- **PRD refs:** Access Control, FR-015, FR-016
- **Unlocks:** S-01; protected-workflow verification for US-01.
- **Prerequisites:** —
- **Parallel with:** F-02
- **Blockers:** —
- **Unknowns:** —
- **Risk:** The MVP cannot begin its stated user story without authenticated access, but the foundation must stay minimal so it does not consume the analysis path's scope.
- **Status:** done

### F-02: Minimum market-data access contract

- **Outcome:** (foundation) the analysis path has a verified source and evidence contract for current and historical data without committing the roadmap to a provider-specific implementation.
- **Change ID:** market-data-access-contract
- **PRD refs:** FR-004, NFR
- **Unlocks:** S-02 and the data-verification path for S-03.
- **Prerequisites:** —
- **Parallel with:** F-01
- **Blockers:** —
- **Unknowns:** Which market-data source and access limits are acceptable for the MVP? Owner: user. Block: yes.
- **Risk:** Choosing a source implicitly without resolving reliability, coverage, and access constraints could make the first ranking unverifiable or exceed the launch scope.
- **Status:** blocked

## Slices

### S-01: Account access and purpose

- **Outcome:** user can create an account, log in, and see a short explanation that Argus provides educational analysis rather than investment advice.
- **Change ID:** account-access-flow
- **PRD refs:** FR-001, FR-014, FR-015, FR-016
- **Prerequisites:** F-01
- **Parallel with:** F-02
- **Blockers:** —
- **Unknowns:**
  - —
- **Risk:** Delivering this before analysis keeps the protected entry path explicit while avoiding guest-flow rework later.
- **Status:** done

### S-02: Asset selection and analysis start

- **Outcome:** user can select one or more supported assets and start a market analysis.
- **Change ID:** select-and-start-analysis
- **PRD refs:** FR-002, FR-003, FR-004
- **Prerequisites:** S-01, F-02
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:**
  - Which market-data source and access limits are acceptable for the MVP? Owner: user. Block: yes.
- **Risk:** This slice exposes the external-data dependency early; postponing it would allow UI work that cannot produce trustworthy results.
- **Status:** blocked

### S-03: Explained market ranking

- **Outcome:** user can view each selected asset's score, ranking, current and historical context, contributing factors, chart, grounded AI summary, and educational-not-advice boundary.
- **Change ID:** explained-market-ranking
- **PRD refs:** FR-005, FR-006, FR-008, FR-009, FR-010, FR-011, FR-012, FR-013, FR-014, US-01
- **Prerequisites:** S-02
- **Parallel with:** —
- **Blockers:** —
- **Unknowns:**
  - How should the product represent insufficient evidence when fewer than two reliable factors are available? Owner: team. Block: no.
- **Risk:** This is the north star, so it is sequenced immediately after selection and data access; the main risk is that score explanations appear more certain than the available evidence.
- **Status:** proposed

## Backlog Handoff

| Roadmap ID | Change ID | Suggested issue title | GitHub issue | Ready for `/10x-plan` | Notes |
| --- | --- | --- | --- | --- | --- |
| F-01 | minimal-auth-foundation | Establish minimum authenticated access | [#3](https://github.com/KarolKucinski2001/Argus/issues/3) | yes | Run `/10x-plan minimal-auth-foundation`. |
| F-02 | market-data-access-contract | Resolve and verify MVP market-data access | [#4](https://github.com/KarolKucinski2001/Argus/issues/4) | no | Resolve the blocking market-data question first. |
| S-01 | account-access-flow | Deliver account access and Argus purpose screen | [#5](https://github.com/KarolKucinski2001/Argus/issues/5) | no | Requires F-01 to be completed. |
| S-02 | select-and-start-analysis | Let users select assets and start analysis | [#6](https://github.com/KarolKucinski2001/Argus/issues/6) | no | Requires S-01 and F-02. |
| S-03 | explained-market-ranking | Show grounded scores, factors, and ranking | [#7](https://github.com/KarolKucinski2001/Argus/issues/7) | no | North star; requires S-02. |

## Open Roadmap Questions

1. **Which market-data source and access limits are acceptable for the MVP?** — Owner: user. Block: F-02, S-02, S-03. GitHub decision issue: [#1](https://github.com/KarolKucinski2001/Argus/issues/1).
2. **Should AI Copilot be included in the MVP?** — Owner: user. Block: roadmap-wide only if included; otherwise parked as a later capability. GitHub decision issue: [#2](https://github.com/KarolKucinski2001/Argus/issues/2).

## Parked

- **AI Copilot conversational questions** — Parked because it is secondary in the PRD and has no retained functional requirement for the first milestone.
- **Watchlist monitoring** — Parked because FR-017 is explicitly nice-to-have and is not part of the first value flow.
- **Daily email report** — Parked because FR-018 is explicitly nice-to-have and would add a separate delivery workflow.
- **Search-trend scoring** — Parked because FR-019 is explicitly nice-to-have and is not required to validate the primary workflow.
- **News-sentiment scoring** — Parked because FR-020 is explicitly nice-to-have and adds another uncertain evidence source.
- **Personalized advice, automated trading, portfolio management, full-market analysis, real-time alerts, and guaranteed predictions** — Parked by the PRD Non-Goals.

## Milestone History

(Empty on the first milestone.)

## Done

- **F-01: (foundation) the application has the minimum authentication and protected-route contract required for the MVP workflow.** — Archived 2026-10-02 → `context/archive/2026-10-01-minimal-auth-foundation/`. Lesson: —.
- **S-01: user can create an account, log in, and see a short explanation that Argus provides educational analysis rather than investment advice.** — Archived 2026-10-06 → `context/archive/2026-10-05-account-access-flow/`. Lesson: —.
