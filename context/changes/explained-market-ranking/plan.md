# Explained Market Ranking Implementation Plan

## Overview

Complete roadmap slice S-03 by extending the existing protected `/analysis`
flow with a provider-neutral, grounded AI summary for each asset that has
sufficient evidence. Preserve the existing score, ranking, performance,
factor, chart, and market-data safety contracts while making insufficient
evidence, AI unavailability, and the educational boundary explicit to the
user.

## Current State Analysis

The repository already contains the core deterministic analysis path:

- `Analysis/PerformanceCalculator.cs` derives 24-hour, 7-day, and 30-day
  metrics and ordered chart points from normalized market data.
- `Analysis/OpportunityScorer.cs` calculates the bounded 0–100 score with
  fixed weights, missing-factor renormalization, and a minimum of two
  available factors.
- `Analysis/AnalysisService.cs` ranks scored assets, preserves submitted order
  for ties, and keeps successful results beside errored assets.
- `Pages/Analysis/Index.cshtml.cs` protects `/analysis`, validates selection,
  requests the fixed daily history range, invokes analysis, and preserves
  partial results.
- `Pages/Analysis/Index.cshtml` renders scores, metrics, factors, contributions,
  chart data, quality/freshness states, and safe provider-error messages.
- Existing tests cover scoring, ranking, historical boundaries, partial
  results, chart rendering, authentication, and raw provider-detail
  suppression.

The current `IAnalysisExplanationBuilder` is deterministic and returns a text
summary from the displayed metrics. The PRD still requires an AI-generated
summary grounded in displayed data (`FR-011` and `FR-013`), and the result
surface does not yet show a visible educational-not-investment-advice
boundary. The earlier S-03 design excluded real AI; the decisions recorded
for this plan supersede that exclusion.

## Desired End State

An authenticated user can submit selected assets and see a ranked result where
each sufficiently evidenced asset has a per-asset AI summary generated only
from the metrics, factors, quality, freshness, and warnings shown for that
asset. No AI request is made when fewer than two reliable factors exist.

If the AI provider is unavailable, the ranking and all deterministic evidence
remain visible while the summary shows a bounded unavailable state. Provider
identifiers, prompts, raw responses, credentials, and exception messages never
reach the browser. The result page visibly states that the analysis is
educational and is not investment advice.

### Key Discoveries:

- `Analysis/AnalysisService.cs:9-48` is the single composition point for
  ranking and summary assignment; it already preserves per-asset failures.
- `Analysis/AnalysisContracts.cs:27-48` has an optional `Summary` but no
  explicit summary status, so the UI cannot currently distinguish
  insufficient evidence from AI unavailability.
- `Pages/Analysis/Index.cshtml.cs:89-111` invokes analysis after normalized
  market data succeeds; the AI boundary belongs after scoring, not inside the
  market-data provider contract.
- `Pages/Analysis/Index.cshtml:77-318` already renders the evidence needed for
  a grounded prompt and has tests for safe provider-state rendering.
- `tests/Argus.Authentication.Tests/AnalysisContractTests.cs` and
  `AnalysisAccessContractTests.cs` provide the established unit and
  authenticated-page test patterns.

## What We're NOT Doing

- No conversational AI Copilot, follow-up questions, or secondary chat flow.
- No provider-specific SDK or provider-specific types in the analysis domain.
- No change to the existing score weights, normalization range, ranking
  tiebreak, historical-period semantics, or market-data provider contract.
- No production market-data provider activation, persistence, watchlists,
  alerts, portfolio management, trading, or personalized investment advice.
- No AI call for assets with fewer than two available factors.
- No failure of the complete analysis page solely because AI generation fails.
- No client-side dashboard or separate per-asset route.

## Implementation Approach

Keep `AnalysisService` responsible for composing the already normalized
market-data result, score, and factors. Introduce a small application port for
grounded summary generation and a provider-neutral configuration/adapter
boundary outside the scoring logic. The port receives a constrained,
structured representation of one asset's displayed evidence and returns either
an accepted summary or a bounded unavailable/invalid result.

The orchestration calls the port only after a successful score with sufficient
evidence. It validates and bounds the returned text before placing it in
`AssetAnalysis`; an exception or invalid response becomes a summary status,
not a page-level failure. The Razor Page renders the summary status and the
educational boundary alongside the existing evidence cards.

## Critical Implementation Details

### Grounding and failure sequencing

The prompt/input contract must be assembled from the same normalized values
that are rendered for the asset. The service must decide
insufficient-evidence status before invoking AI, and must convert timeout,
transport, malformed-response, and cancellation outcomes into safe
per-asset states without replacing the score or factors.

## Phase 1: Define the grounded AI contract

### Overview

Add the provider-neutral application contract and configuration boundary for
one grounded summary per asset. Keep the domain contract independent of any
vendor SDK and make the accepted result states explicit.

### Changes Required:

#### 1. Analysis summary contracts

**File**: `Analysis/AnalysisContracts.cs` and new analysis summary contract
files under `Analysis/`

**Intent**: Represent the input evidence sent to the summary generator and
distinguish a generated summary, insufficient evidence, and unavailable/invalid
AI output without overloading a nullable string.

**Contract**: The request must contain only the asset identity needed for the
user-facing result plus displayed metrics, available factors and
contributions, quality/freshness, delay state, and safe warning categories. The
result must expose a bounded user-facing summary or a stable status that the
PageModel can render; it must not expose prompts, raw model payloads, provider
IDs, credentials, or exception text.

#### 2. Provider-neutral summary port and adapter boundary

**File**: New `Analysis/IAnalysisSummaryGenerator.cs` plus the selected
application/infrastructure registration files

**Intent**: Isolate model invocation from scoring and Razor rendering while
allowing a configured provider-neutral adapter to be used in production and a
fake generator in tests.

**Contract**: The port is asynchronous, accepts cancellation, returns a
structured bounded result, and has no provider-specific types. Configuration
must be optional and must fail closed to an unavailable state when the
provider is not approved or configured; secrets remain configuration-only and
are never committed.

### Success Criteria:

#### Automated Verification:

- Summary request and result contracts compile with nullable safety enabled.
- The generator port has a cancellation-aware contract and can be replaced by a
  deterministic test fake without network access.
- Configuration without an approved provider produces a bounded unavailable
  state rather than a startup failure or secret-shaped fallback.

#### Manual Verification:

- Review the contract and confirm that every prompt input is also visible in
  the corresponding result card.
- Confirm no provider name, prompt, raw response, credential, or exception text
  is part of the public result contract.

**Implementation Note**: Pause after automated verification so the contract
shape can be reviewed before orchestration begins.

## Phase 2: Integrate grounded summaries into analysis

### Overview

Update analysis composition so summaries are generated per asset only when the
score has sufficient evidence, while preserving ranking and partial results
when AI is unavailable.

### Changes Required:

#### 1. Analysis orchestration

**File**: `Analysis/AnalysisService.cs`

**Intent**: Add the summary-generator call after performance calculation and
scoring, without moving provider access or score calculation into the AI
adapter.

**Contract**: Fewer than two available factors must produce an explicit
insufficient-evidence summary status and must not invoke the generator. A
generator exception, cancellation, timeout, malformed output, or unavailable
configuration must preserve the existing score, factors, metrics, and chart
points while assigning a bounded unavailable summary status. Errored market
data results must continue to have no score, factors, or AI summary.

#### 2. Safe output validation

**File**: New summary validation/helper code under `Analysis/`

**Intent**: Prevent model output from becoming an unbounded or misleading
  browser result and ensure the generated text remains grounded.

**Contract**: Reject empty, oversized, or structurally invalid responses;
normalize only the permitted user-facing text; do not silently substitute
facts or a deterministic score explanation. The safe fallback is an explicit
summary-unavailable state.

#### 3. Dependency registration and cancellation

**File**: `Program.cs`

**Intent**: Register the provider-neutral generator and its configuration using
the existing startup pattern, preserving test replacement and request
cancellation.

**Contract**: `/analysis` cancellation must flow through the generator call.
The registration must not make local tests or development startup require
production AI credentials.

### Success Criteria:

#### Automated Verification:

- Unit tests prove that sufficient evidence invokes the generator with only
  displayed evidence.
- Unit tests prove that insufficient evidence skips the generator.
- Unit tests prove that generator failure preserves score, ranking, factors,
  metrics, and chart points while returning an unavailable summary status.
- Unit tests prove that errored market-data results never invoke the generator.
- Unit tests prove output bounds and safe handling of empty or malformed
  generator responses.
- Existing analysis and market-data contract tests remain green.

#### Manual Verification:

- Submit multiple assets where one has sufficient evidence and another has
  insufficient evidence; confirm both remain visible with distinct states.
- Simulate AI unavailability and confirm the ranked evidence remains usable.
- Confirm cancelling the request does not leave a partial successful summary
  rendered as if it had completed.

## Phase 3: Render the complete result surface

### Overview

Expose the new summary states in the protected Razor result cards and make the
educational-not-investment-advice boundary visible without changing the
existing chart or score presentation.

### Changes Required:

#### 1. Analysis PageModel mapping

**File**: `Pages/Analysis/Index.cshtml.cs`

**Intent**: Map analysis summary statuses into stable localized UI messages
  while retaining current selection, provider, quality, and partial-result
  behavior.

**Contract**: The PageModel must not expose raw AI/provider errors. It must
  preserve selected IDs and successful assets when one summary is unavailable,
  and it must keep the existing authentication challenge and antiforgery
  behavior unchanged.

#### 2. Result cards and educational boundary

**File**: `Pages/Analysis/Index.cshtml`

**Intent**: Render generated summaries per asset, explicit insufficient/
  unavailable states, and a visible educational disclaimer adjacent to the
  ranking/explanation content.

**Contract**: The page must not use “buy”, “best investment”, “guaranteed”,
  “top opportunity”, or equivalent personalized-advice language. It must
  preserve score, ranking, metrics, factors, chart, quality/freshness, and
  accessible text fallback behavior already covered by the existing tests.

#### 3. Existing visual contract

**File**: `wwwroot/css/site.css` only if the current tokens cannot support the
  new bounded states

**Intent**: Add only the smallest token-based styling needed for readable
  summary status states and the disclaimer.

**Contract**: Keep colors and layout values in the existing stylesheet;
  Razor markup must not introduce arbitrary literal visual tokens.

### Success Criteria:

#### Automated Verification:

- Authenticated page tests assert generated summary rendering per asset.
- Page tests assert distinct insufficient-evidence and AI-unavailable states.
- Page tests assert the educational-not-investment-advice boundary is present.
- Page tests assert raw prompts, provider identifiers, raw responses,
  credentials, and exception messages are absent.
- Existing chart, ranking, quality, partial-result, and validation tests remain
  green.

#### Manual Verification:

- Complete the account → asset selection → analysis → result flow over HTTPS.
- Check keyboard navigation, readable focus order, responsive cards, and
  disclaimer visibility on desktop and mobile widths.
- Verify that generated copy reads as an explanation of shown evidence, not a
  personalized recommendation or prediction.

## Phase 4: Verify and document the S-03 contract

### Overview

Finish the focused test matrix and update the S-03 documentation so the
provider-neutral AI boundary and explicit non-goals match the implemented
behavior.

### Changes Required:

#### 1. Analysis and page contract tests

**File**: `tests/Argus.Authentication.Tests/AnalysisContractTests.cs` and
`tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

**Intent**: Extend the existing test helpers and assertions rather than
  creating a second test harness.

**Contract**: Cover grounding input, per-asset invocation, insufficient
  evidence, AI failure/timeout/cancellation, malformed output, mixed success
  results, ranking/tie preservation, safe rendering, and the educational
  boundary. Keep all tests deterministic and network-free.

#### 2. S-03 documentation

**File**: `docs/superpowers/specs/2026-10-16-explained-market-ranking-design.md`,
`docs/superpowers/plans/2026-10-16-explained-market-ranking.md`, and
`docs/market-data.md` where the evidence boundary is referenced

**Intent**: Replace the superseded “no real AI” statement with the approved
  provider-neutral, grounded-summary contract and document what happens when
  evidence or AI availability is insufficient.

**Contract**: Documentation must retain the existing scoring and market-data
  rules, explicitly exclude chat/Copilot and provider activation decisions, and
  state that only displayed normalized evidence can enter a summary.

### Success Criteria:

#### Automated Verification:

- `dotnet build .bootstrap-scaffold.csproj --no-restore` passes with zero
  warnings and errors.
- `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
  passes.
- Focused analysis/page tests cover all new summary states without network or
  production secrets.
- The final plan references every changed contract and every acceptance
  criterion exactly once in `## Progress`.

#### Manual Verification:

- Review the final result against all S-03 acceptance criteria in
  `context/foundation/prd.md`.
- Verify the configured provider path with approved non-production
  credentials, and separately verify the unavailable-provider path.
- Confirm the UI never implies a guaranteed result or personalized advice.

## Testing Strategy

### Unit Tests:

- Summary request construction from normalized, displayed evidence.
- Minimum-evidence gate and no-call behavior.
- Generator success, unavailable configuration, timeout, cancellation,
  malformed output, and bounded-output validation.
- Preservation of score, factor contributions, ranking, and chart points when
  summary generation fails.
- No raw provider or model details in returned contracts.

### Integration Tests:

- Authenticated `/analysis` POST with a test generator returning a grounded
  summary.
- Mixed assets with generated, insufficient-evidence, and unavailable-summary
  states.
- Existing market-data provider error and partial-result paths.
- Rendered disclaimer, chart fallback, and safe warning/error text.

### Manual Testing Steps:

1. Sign in and select multiple configured assets.
2. Submit with enough history for at least two factors and verify one
   grounded summary per asset.
3. Repeat with an asset lacking sufficient history and verify no AI call and
   an explicit evidence state.
4. Disable or misconfigure the AI provider and verify the ranking remains
   visible with a bounded unavailable summary state.
5. Check mobile layout, keyboard access, chart text fallback, and disclaimer
   visibility.

## Performance Considerations

AI calls are per asset and occur only after scoring. Use the existing request
cancellation and bounded selection limit; do not add background jobs or
unbounded retries. The plan should avoid duplicate market-data requests and
should keep the analysis page responsive by preserving partial results when a
summary call fails.

## Migration Notes

No database migration is required. AI configuration must be supplied through
existing environment/configuration mechanisms; no secrets or tenant/provider
credentials are committed. Existing deterministic analysis results remain
backward-compatible when the AI adapter is unavailable.

## References

- Roadmap: `context/foundation/roadmap.md` — S-03 / `explained-market-ranking`
- Product requirements: `context/foundation/prd.md` — US-01, FR-005, FR-006,
  FR-008–FR-014
- Market-data contract: `docs/market-data.md`
- Existing design and plan: `docs/superpowers/specs/2026-10-16-explained-market-ranking-design.md`,
  `docs/superpowers/plans/2026-10-16-explained-market-ranking.md`
- Existing tests: `tests/Argus.Authentication.Tests/AnalysisContractTests.cs`,
  `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Define the grounded AI contract

#### Automated

- [x] 1.1 Summary request and result contracts compile with nullable safety enabled. — 6b8a4bb
- [x] 1.2 The generator port supports cancellation and deterministic test replacement. — 6b8a4bb
- [x] 1.3 Unconfigured AI fails closed to a bounded unavailable state. — 6b8a4bb

#### Manual

- [ ] 1.4 Review that prompt inputs are limited to evidence visible in the result.
- [ ] 1.5 Review that provider/model details cannot enter the public result contract.

### Phase 2: Integrate grounded summaries into analysis

#### Automated

- [x] 2.1 Sufficient evidence invokes the generator with only displayed evidence.
- [x] 2.2 Insufficient evidence skips the generator.
- [x] 2.3 Generator failure preserves score, ranking, factors, metrics, and chart points.
- [x] 2.4 Errored market-data results never invoke the generator.
- [x] 2.5 Empty, oversized, or malformed generator output is handled safely.
- [x] 2.6 Existing analysis and market-data contract tests remain green.

#### Manual

- [ ] 2.7 Mixed sufficient/insufficient assets remain visible with distinct states.
- [ ] 2.8 AI unavailability leaves the ranked evidence usable.
- [ ] 2.9 Cancellation does not render an incomplete summary as successful.

### Phase 3: Render the complete result surface

#### Automated

- [ ] 3.1 Generated summaries render per asset.
- [ ] 3.2 Insufficient-evidence and AI-unavailable states render distinctly.
- [ ] 3.3 The educational-not-investment-advice boundary is present.
- [ ] 3.4 Raw prompts, provider details, credentials, and exception messages are absent.
- [ ] 3.5 Existing chart, ranking, quality, partial-result, and validation tests remain green.

#### Manual

- [ ] 3.6 Account-to-results flow works over HTTPS.
- [ ] 3.7 Keyboard navigation, focus order, responsive cards, and disclaimer visibility are acceptable.
- [ ] 3.8 Generated copy explains shown evidence without personalized-advice language.

### Phase 4: Verify and document the S-03 contract

#### Automated

- [ ] 4.1 The focused analysis and page contract matrix is deterministic and network-free.
- [ ] 4.2 `dotnet build .bootstrap-scaffold.csproj --no-restore` passes with zero warnings and errors.
- [ ] 4.3 `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore` passes.
- [ ] 4.4 S-03 documentation matches the provider-neutral grounded-summary behavior.

#### Manual

- [ ] 4.5 All S-03 acceptance criteria are reviewed against the final result.
- [ ] 4.6 Approved non-production AI and unavailable-provider paths are both verified.
- [ ] 4.7 The UI does not imply guaranteed results or personalized advice.
