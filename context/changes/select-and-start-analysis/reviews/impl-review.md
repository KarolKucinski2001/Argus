<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Select and Start Analysis

- **Plan**: `context/changes/select-and-start-analysis/plan.md`
- **Scope**: Full plan
- **Reviewed phases**: 1, 2, 3
- **Date**: 2026-10-16
- **Verdict**: PASS WITH ACCEPTED RISKS
- **Findings**: 0 critical 4 warnings 1 observation

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | WARNING |
| Scope Discipline | PASS |
| Safety & Quality | WARNING |
| Architecture | WARNING |
| Pattern Consistency | PASS |
| Success Criteria | PASS |

## Findings

### F1 — API can expose raw provider exception messages

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `MarketData/MarketDataService.cs:182-191`
- **Detail**: `MarketDataProviderException.Message` is copied into normalized
  `MarketDataError.Message`. The API adapter returns the response directly,
  so a future provider adapter could expose upstream URLs, response bodies,
  quota details, or credential fragments through `/api/market-data`.
- **Fix**: Map provider exceptions to stable user-safe messages by error
  category and keep the original exception only in protected, redacted
  diagnostics.
  - Strength: Preserves the provider-neutral contract and prevents accidental
    disclosure at every current and future caller.
  - Tradeoff: Provider-specific diagnostic detail is no longer available to
    clients and must be retrieved from logs.
  - Confidence: HIGH — the service already has stable error categories.
  - Blind spot: No live provider adapter currently exercises this path.
- **Decision**: FIXED — provider exception messages are now mapped to stable user-safe messages; raw details remain available only to protected diagnostics/logging.

### F2 — Analysis route uses manual rather than declarative authorization

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Architecture
- **Location**: `Pages/Analysis/Index.cshtml.cs:11,29-35`
- **Detail**: The PageModel is marked `[AllowAnonymous]` and performs its own
  `User.Identity.IsAuthenticated` check before explicitly challenging OIDC.
  Runtime behavior is protected and covered by tests, but the route is
  declaratively anonymous, so future handlers or filters could accidentally
  bypass the manual guard.
- **Fix**: Preserve the explicit OIDC browser challenge while introducing a
  dedicated authenticated-page policy/filter or shared protected-page
  base pattern that keeps authorization declarative and prevents handler drift.
  - Strength: Makes the security boundary visible to framework tooling and
    future handlers.
  - Tradeoff: Requires reconciling the existing explicit OIDC challenge
    behavior with the repository's cookie/API challenge defaults.
  - Confidence: MED — the current account page uses a similar explicit pattern.
  - Blind spot: The best shared Razor authorization abstraction is not yet
    established in this repository.
- **Decision**: ACCEPTED RISK — the explicit challenge pattern is retained for this slice because it matches the current account-page pattern. Revisit when a shared protected-page authorization abstraction is introduced.

### F3 — Partial provider results are collapsed into a page-level error

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Plan Adherence
- **Location**: `Pages/Analysis/Index.cshtml.cs:78-85`
- **Detail**: The shared service preserves per-asset partial results, but the
  page finds the first item error and renders only a generic provider message.
  Successful selected assets are not shown and the usable partial evidence is
  discarded at the UI boundary.
- **Fix**: Add a bounded partial state that keeps successful selected assets
  visible and summarizes failed assets without exposing provider details.
  - Strength: Preserves the market-data contract's partial-result semantics
    and gives the user actionable feedback.
  - Tradeoff: Adds one UI state before the ranking/result surface exists.
  - Confidence: HIGH — partial results are explicitly preserved by Phase 1.
  - Blind spot: The exact S-03 presentation model is not yet defined.
- **Decision**: DEFERRED TO S-03 — partial-result presentation is intentionally kept out of the selection/start slice until the result-surface model is defined.

### F4 — Roadmap status was not closed with the implemented change

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: `context/foundation/roadmap.md:47,113-123`
- **Detail**: `change.md` is `implemented`, but the S-02 roadmap entries still
  say `in-progress`. The implementation workflow updated the roadmap on entry
  but did not close it when the change reached its implemented state.
- **Fix**: Set both S-02 roadmap status fields to `done` and update the
  roadmap date, without changing its outcome or prerequisites.
- **Decision**: FIXED — S-02 is now marked `done` in both roadmap status locations.

### F5 — Several planned analysis contract assertions are indirect

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Success Criteria
- **Location**: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs:54-191`
- **Detail**: The tests cover successful, empty, duplicate, over-limit,
  unavailable, and no-leakage rendering, but do not directly assert unsupported
  asset rejection, canonical request contents, fixed 12-month daily history,
  selected-state preservation after errors, or successful-state suppression of
  raw provider identifiers.
- **Fix**: Add focused assertions or a service spy in a follow-up test pass
  before S-03 expands the result surface.
- **Decision**: FIXED — added direct coverage for unsupported assets, canonical daily history requests, preserved selection after provider errors, and raw provider-message suppression.

## Evidence

- `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore` — PASS, 52/52.
- `dotnet build .bootstrap-scaffold.csproj --no-restore` — PASS, 0 warnings, 0 errors.
- Release-boundary scan — PASS: no non-empty credential settings and no literal colors in `Pages/Analysis/Index.cshtml`.
- Manual verification — PASS for the complete local flow, bounded validation,
  illustrative status, keyboard focus, responsive layouts, and educational
  boundary.
