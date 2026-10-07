<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Market data access contract

- **Plan**: `context/changes/market-data-access-contract/plan.md`
- **Scope**: Full plan
- **Reviewed phases**: 1, 2, 3
- **Date**: 2026-10-07
- **Verdict**: APPROVED
- **Findings**: 0 critical 0 warnings 0 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | PASS |
| Scope Discipline | PASS |
| Safety & Quality | PASS |
| Architecture | PASS |
| Pattern Consistency | PASS |
| Success Criteria | PASS |

## Findings

### F1 — Provider results are not validated before caching

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `Program.cs:214-231,249-250`
- **Detail**: Successful provider results are cached and returned without verifying that each result matches the requested asset ID, asset class, history, date range, or granularity. Response selection matches only the asset ID, so an incorrect provider result could be served from the shared cache.
- **Fix**: Validate every provider result against the normalized request and required evidence invariants. Map invalid results to `InvalidProviderResponse` and never cache them.
  - Strength: Protects the shared cache and downstream analysis from malformed or mismatched upstream data.
  - Tradeoff: Requires defining the exact normalized-result invariants at the adapter boundary.
  - Confidence: HIGH — the current code has no such validation.
  - Blind spot: Provider-specific normalization rules are not yet defined.
- **Decision**: FIXED — validated provider result identity, requested history, date bounds, and granularity before caching; added deterministic rejection/non-caching coverage.

### F2 — Unexpected provider exceptions escape the error contract

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `Program.cs:190-212`
- **Detail**: Only cancellation and `HttpRequestException` are normalized. Authentication failures, malformed provider responses, parsing failures, and other adapter exceptions can escape as generic HTTP 500 responses even though stable error categories exist for them.
- **Fix**: Map the provider adapter's expected exception types to stable per-asset error categories while allowing unexpected programming failures to remain distinguishable and non-leaky.
  - Strength: Preserves the declared error contract for external-boundary failures.
  - Tradeoff: Requires a small exception taxonomy at the provider boundary.
  - Confidence: HIGH — the declared categories are currently unused by the endpoint.
  - Blind spot: No real provider adapter exists yet.
- **Decision**: FIXED — added an adapter-boundary provider exception taxonomy and normalized invalid-data exceptions without swallowing unexpected failures.

### F3 — Application request budget is not enforced

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Plan Adherence
- **Location**: `MarketData/MarketDataOptions.cs:14,50-53`; `Program.cs:127-262`
- **Detail**: `ApplicationRequestBudget` is configured and validated as positive, but no request counter, time window, rejection path, or provider-call budget enforcement exists.
- **Fix**: Enforce the configured budget at the endpoint/provider boundary, or amend the plan and documentation to state that the property is reserved configuration only.
  - Strength: Makes the bounded-access contract operational rather than declarative.
  - Tradeoff: Requires selecting a process-local or distributed counting strategy.
  - Confidence: HIGH — no enforcement path exists in the implementation.
  - Blind spot: Distributed App Service scaling is out of scope for the current cache.
- **Decision**: FIXED — enforced a process-local rolling one-minute request budget with a stable 429/QuotaExceeded response and documented the scale-out limitation.

### F4 — Expired cached evidence can be discarded on an empty provider response

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `Program.cs:157-185,214-257`
- **Detail**: An expired entry is restored only when the provider returns an explicit error result. If the provider returns no result for that asset, the final projection creates a new generic unavailable result and loses the stale cached quote/history and stale warning.
- **Fix**: When an expired asset has no provider result, return the expired result with a stale/evidence-unavailable warning and explicit error category.
  - Strength: Preserves previously observed evidence while clearly marking it stale.
  - Tradeoff: Downstream consumers must handle stale data deliberately.
  - Confidence: HIGH — the current final projection drops the retained expired entry.
  - Blind spot: The desired UI treatment of stale data remains future work.
- **Decision**: FIXED — preserved expired cached evidence when refresh returned no result, with explicit stale and upstream-unavailable signaling.

### F5 — Progress claims exceed deterministic test coverage

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Success Criteria
- **Location**: `tests/Argus.Authentication.Tests/MarketDataContractTests.cs:20-234`; `context/changes/market-data-access-contract/plan.md:267-281`
- **Detail**: Tests cover the main success, partial, cache, quota, timeout, outage, invalid-asset, and approval paths, but do not cover over-limit requests, invalid date/range requests, configured-provider-but-unavailable behavior, or all Phase 1 options-validation branches. Those cases are included in checked Progress criteria.
- **Fix**: Add deterministic tests for every listed missing case and update the evidence references.
  - Strength: Aligns checked Progress rows with directly reproducible behavior.
  - Tradeoff: Adds focused tests before a real provider is selected.
  - Confidence: HIGH — verified against the committed test file.
  - Blind spot: Some options branches are already covered by the earlier review-fix tests but not all endpoint paths.
- **Decision**: FIXED — added deterministic endpoint coverage for over-limit, invalid-range, and configured-but-unavailable cases, plus options-validation coverage.

### F6 — Required performance metrics are absent from the normalized result

- **Severity**: ⚠️ WARNING
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: Plan Adherence
- **Location**: `MarketData/MarketDataContracts.cs:72-84`; `Program.cs:127-259`
- **Detail**: The Phase 2 manual criterion requires current data plus 24-hour, 7-day, and 30-day values. `MarketQuote` contains only one value and `MarketDataResult` has no normalized performance-period fields, so the endpoint cannot expose those metrics.
- **Fix**: Add explicit normalized performance fields, or revise the plan/manual criterion to defer those metrics to the later analysis layer.
  - Strength: Preserves the product evidence contract at the market-data boundary.
  - Tradeoff: Adds derived metric semantics before a real provider is chosen.
  - Confidence: HIGH — the fields are absent from the contract and endpoint.
  - Blind spot: The PRD does not fully specify whether metrics belong in market data or analysis.
- **Decision**: ACCEPTED — deferred 24-hour, 7-day, and 30-day performance calculations to the future analysis layer; updated the plan criterion and market-data documentation to make this boundary explicit.

### F7 — Provider-enabled configuration can still return not-configured results

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Architecture
- **Location**: `Program.cs:23,141-152,259`; `MarketData/UnavailableMarketDataProvider.cs:14-20`
- **Detail**: DI always registers `UnavailableMarketDataProvider`, while endpoint enablement checks only `PublicDisplayApproved` and `ProviderId`. A deployment could claim approval and receive HTTP 200 results containing `NotConfigured` errors.
- **Fix**: Validate actual provider readiness at activation, or keep the endpoint unavailable until a real approved provider is registered.
- **Decision**: FIXED — added a fail-closed endpoint guard that rejects approved configuration when the unavailable provider remains registered.

### F8 — Allowlist metadata is not canonicalized

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Architecture
- **Location**: `Program.cs:159-166,341-355`
- **Detail**: Allowlist lookup is case-insensitive, but caller-supplied ID casing and display metadata are passed through to the provider and cache key. Equivalent requests can create separate cache entries, and response display metadata is not resolved from configured asset metadata.
- **Fix**: Canonicalize ID/class and resolve display metadata from the configured allowlist before provider and cache operations.
- **Decision**: FIXED — canonicalized accepted asset IDs, classes, and display fallbacks before cache/provider use, with casing-equivalence coverage.

## Verification Evidence

- `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore` — passed, 39 tests.
- `dotnet build .bootstrap-scaffold.csproj --no-restore` — passed with 0 warnings and 0 errors.
- The provider remains disabled by default; no real provider dependency or secret was added.
- The deployment workflow does not provision or echo provider credentials.
