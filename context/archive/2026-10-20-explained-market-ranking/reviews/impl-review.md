<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Explained Market Ranking

- **Plan**: `context/changes/explained-market-ranking/plan.md`
- **Scope**: Full plan
- **Reviewed phases**: 1, 2, 3, 4
- **Date**: 2026-10-20
- **Verdict**: NEEDS ATTENTION
- **Findings**: 0 critical, 5 warnings, 2 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | WARNING |
| Scope Discipline | WARNING |
| Safety & Quality | WARNING |
| Architecture | WARNING |
| Pattern Consistency | PASS |
| Success Criteria | PASS |

## Findings

### F1 — Configured AI provider is never activated

- **Severity**: ⚠️ WARNING
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: Architecture
- **Location**: `Program.cs:31-33`
- **Detail**: `AnalysisSummaryOptions` is bound, but dependency injection always registers `UnavailableAnalysisSummaryGenerator`. No approved provider adapter is selected when configuration is valid, so every runtime summary remains unavailable. This does not satisfy the desired end state of a real grounded AI summary when configured.
- **Fix**: Register an approved provider-neutral adapter for valid configuration, with the unavailable generator reserved for the fail-closed path. If provider activation is intentionally deferred, revise the plan and acceptance criteria before shipping this slice.
  - Strength: Aligns the runtime behavior with the explicitly approved real-AI requirement and the plan's configured-provider path.
  - Tradeoff: Requires selecting and implementing an adapter without leaking vendor types into the analysis layer.
  - Confidence: HIGH — the current registration unconditionally selects the unavailable implementation.
  - Blind spot: The approved provider SDK and deployment target were not specified in this change.
- **Decision**: ACCEPTED — provider activation deferred because no approved provider/SDK was specified; the current slice remains fail-closed and needs a follow-up before claiming configured AI availability.

### F2 — No bounded timeout protects sequential summary calls

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `Analysis/AnalysisService.cs:35-43,139-151`
- **Detail**: Summary calls are processed sequentially and receive only the request cancellation token. A provider that hangs or ignores cancellation can hold the analysis request indefinitely, with latency multiplied across selected assets.
- **Fix**: Add a bounded per-summary timeout linked to the request token and map timeout to `Unavailable` while preserving caller cancellation.
  - Strength: Establishes a deterministic upper bound for the existing per-asset orchestration.
  - Tradeoff: Adds timeout configuration and linked-token lifecycle management.
  - Confidence: HIGH — the plan explicitly requires timeout-safe behavior.
  - Blind spot: Provider-specific latency limits are not yet known.
- **Decision**: FIXED — added a bounded per-summary timeout linked to request cancellation; timeout becomes `Unavailable` while caller cancellation remains propagating.

### F3 — Summary options bind a raw API key

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `Analysis/AnalysisSummaryOptions.cs:15`
- **Detail**: The options model contains `ApiKey`, while the repository's configuration boundary documents secret references/configuration keys rather than exposing credential values as application options. The field is currently unused, but it creates a future accidental-binding or logging risk.
- **Fix**: Replace `ApiKey` with a secret-store/configuration-key reference and resolve credentials only inside the provider adapter.
  - Strength: Matches the existing market-data secret-handling contract.
  - Tradeoff: Requires the adapter boundary to own secret resolution.
  - Confidence: HIGH — the field is not needed by the current unavailable implementation.
  - Blind spot: The final provider's SDK authentication mechanism is unspecified.
- **Decision**: FIXED — replaced the raw `ApiKey` option with `ApiKeyConfigurationKey`, matching the repository's secret-reference boundary.

### F4 — Generated text has no semantic grounding or advice-policy validation

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `Analysis/AnalysisSummaryResultValidator.cs:5-37`, `Pages/Analysis/Index.cshtml:225-228`
- **Detail**: Validation limits size and control characters but does not reject ungrounded claims, provider details, or personalized investment advice. Any accepted generated text is rendered as a summary.
- **Fix**: Use a structured grounded output contract and enforce the educational-language/evidence policy before rendering, retaining Razor encoding as defense in depth.
  - Strength: Makes grounding and non-advice requirements enforceable rather than prompt-only.
  - Tradeoff: Requires a stricter output schema and validation rules that must evolve with the approved adapter.
  - Confidence: MED — the plan requires grounded output, but the exact semantic policy is not specified.
  - Blind spot: A reliable claim-to-evidence validator may require provider-side structured output.
- **Decision**: FIXED — added bounded policy filtering for explicit advice language and provider/prompt/model leakage terms before rendering; structured provider output remains a deferred follow-up.

### F5 — Roadmap status change remains outside the implementation commits

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Scope Discipline
- **Location**: `context/foundation/roadmap.md`
- **Detail**: The worktree contains an unstaged S-03 status/timestamp change. Roadmap synchronization is required by the implementation workflow, but the file is not included in the change's committed file set and remains dirty after the completed plan.
- **Fix**: Commit the roadmap status update with the change or explicitly leave it for the parent worktree owner and record that decision before archiving.
- **Decision**: SKIPPED — leave the roadmap status/timestamp change for the parent worktree owner.

### F6 — Synchronous compatibility path reports deterministic text as AI-generated

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: `Analysis/AnalysisService.cs:15-89`
- **Detail**: The retained synchronous `Analyze(...)` path bypasses the generator and records the legacy deterministic explanation as `AnalysisSummaryStatus.Generated`. `/analysis` uses `AnalyzeAsync`, so the path is not currently user-visible there, but the public service contract has two different summary semantics.
- **Fix**: Remove the compatibility path or make it return an explicit unavailable/legacy state until all callers use the async orchestration.
- **Decision**: SKIPPED — retain the synchronous compatibility path for existing callers in this slice.

### F7 — Provider failures are swallowed without diagnostic telemetry

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `Analysis/AnalysisService.cs:145-151`
- **Detail**: All provider exceptions become `Unavailable` without a sanitized log or correlation signal. The browser-safe fallback is correct, but operational diagnosis is difficult.
- **Fix**: Inject a logger and record sanitized failure category/correlation data while preserving the bounded user-facing state.
- **Decision**: SKIPPED — preserve the user-safe fallback without adding telemetry in this review follow-up.

## Verification Evidence

- `dotnet build .bootstrap-scaffold.csproj --no-restore` — PASS, 0 warnings and 0 errors.
- `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore` — PASS, 93/93.
- All Progress automated and manual rows are marked complete in the canonical plan.
- No changes were made to archived paths.
