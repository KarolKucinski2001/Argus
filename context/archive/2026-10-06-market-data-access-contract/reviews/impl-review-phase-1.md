<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Market data access contract

- **Plan**: `context/changes/market-data-access-contract/plan.md`
- **Scope**: Phase 1 of 3
- **Reviewed phases**: 1
- **Date**: 2026-10-06
- **Verdict**: NEEDS ATTENTION
- **Findings**: 0 critical 7 warnings 1 observation

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | WARNING |
| Scope Discipline | WARNING |
| Safety & Quality | WARNING |
| Architecture | PASS |
| Pattern Consistency | PASS |
| Success Criteria | WARNING |

## Findings

### F1 — Phase 1 contract criteria lack dedicated automated evidence

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Success Criteria
- **Location**: `context/changes/market-data-access-contract/plan.md:185-187`
- **Detail**: The Phase 1 Progress section marks contract-test compilation and options-validation rejection as complete, but the Phase 1 commit adds no contract test file and the existing 12-test suite contains no `MarketData` references. The implementation compiles and the validator contains the intended checks, but the plan's stated test evidence is not present.
- **Fix**: Add focused deterministic tests for the normalized contracts and `MarketDataOptions.Validate`, either as a narrow Phase 1 follow-up or in Phase 3, and record the scope adjustment in the plan.
  - Strength: Makes the checked Progress items directly reproducible and protects the contract before endpoint work.
  - Tradeoff: Adds a small test surface now or leaves the phase criteria partially evidenced until Phase 3.
  - Confidence: HIGH — verified by the committed file list and test-project search.
  - Blind spot: The current test project may require a separate test-file organization decision.
- **Decision**: FIXED — Added `tests/Argus.Authentication.Tests/MarketDataContractTests.cs`; focused tests now cover normalized contract construction and invalid/valid options validation. The suite passes with 15 tests.

### F2 — Approval gate can reject the intentionally disabled default

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `MarketData/MarketDataOptions.cs:16,53-56`
- **Detail**: `PublicDisplayApproved` is safely false by default, but validation rejects false. Once Phase 2 registers these options with `ValidateOnStart`, an intentionally unapproved provider configuration may fail startup rather than keep the service explicitly unavailable, despite the documentation describing the flag as a gate.
- **Fix**: Keep `PublicDisplayApproved` false as a valid disabled state and require it to be true only when a provider is enabled; validate provider settings conditionally at the activation boundary.
  - Strength: Preserves fail-closed behavior while allowing the application to start without an approved provider.
  - Tradeoff: Requires a clear disabled-provider state in Phase 2 instead of a single unconditional startup failure.
  - Confidence: HIGH — follows the documented “no provider enabled” state.
  - Blind spot: The exact Phase 2 provider registration shape is not implemented yet.
- **Decision**: FIXED — Validation now accepts the disabled default, rejects an unapproved configured provider, and requires a provider ID only when public display is approved; tests cover all three states.

### F3 — Cache identity includes mutable display metadata

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: `MarketData/IMarketDataCache.cs:3-7`
- **Detail**: `MarketDataCacheKey` uses the entire `AssetReference`, including `DisplayName`, as part of identity. A display-name change can fragment the cache for the same asset, and a future client-controlled display name must not influence identity.
- **Fix**: Key the cache by a canonical asset identifier and asset class; resolve display metadata from the configured allowlist.
- **Decision**: FIXED — Cache keys now use canonical asset ID and asset class instead of the full display-bearing asset record.

### F4 — Operational options have no upper bounds

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `MarketData/MarketDataOptions.cs:23-50`
- **Detail**: Validation rejects zero and negative values but permits arbitrarily large asset counts, timeouts, cache durations, and request budgets. A bad deployment configuration could create excessive provider work, latency, memory retention, or quota consumption.
- **Fix**: Add explicit upper bounds aligned with the MVP request budget and deployment model.
  - Strength: Makes the bounded-access contract enforceable at configuration time.
  - Tradeoff: Requires choosing operational ceilings before provider selection is complete.
  - Confidence: MED — the need for bounds is clear, but exact values are not yet established.
  - Blind spot: Provider-specific quotas remain unresolved.
- **Decision**: SKIPPED — Exact upper bounds depend on the unresolved provider quota and will be addressed with Phase 2 operational configuration.

### F5 — Request validation is not null-safe

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: `MarketData/MarketDataContracts.cs:66-73`
- **Detail**: `MarketDataRequest.IsValid` dereferences `Assets` and `History` without runtime null guards. Malformed JSON or programmatic callers can produce null values despite nullable annotations and receive a `NullReferenceException` instead of a controlled invalid-request result.
- **Fix**: Guard `Assets` and `History` explicitly in `IsValid`, then map invalid input to Problem Details at the Phase 2 endpoint.
- **Decision**: FIXED — `MarketDataRequest.IsValid` now guards null assets/history at runtime; focused tests remain green with 16 passing tests.

### F6 — Raw warning/error text could cross the application boundary

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: `MarketData/MarketDataContracts.cs:109-118`
- **Detail**: `MarketDataWarning.Message` and `MarketDataError.Message` are unconstrained strings, and `UpstreamStatusCode` is exposed. A provider adapter could accidentally pass through provider response text, URLs, internal identifiers, or diagnostic details to clients.
- **Fix**: Define safe user-facing codes/messages at the adapter boundary and keep raw provider diagnostics in redacted structured logs.
  - Strength: Preserves the normalized response boundary described in `docs/market-data.md`.
  - Tradeoff: Requires an explicit logging/diagnostic contract in a later provider phase.
  - Confidence: HIGH — the current types permit leakage even though no provider exists yet.
  - Blind spot: Endpoint serialization and adapter mapping are deferred to later phases.
- **Decision**: SKIPPED — No provider adapter or endpoint exists yet; safe mapping will be addressed at the external boundary in later phases.

### F7 — Roadmap synchronization is outside the Phase 1 file list

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Scope Discipline
- **Location**: `context/foundation/roadmap.md:31,90`
- **Detail**: The Phase 1 commit modifies the roadmap even though it is not listed in the Phase 1 Changes Required files. This is consistent with the implementation workflow's mandatory roadmap synchronization, but it is an undocumented process artifact in the phase plan.
- **Fix**: Treat the roadmap status synchronization as an expected implementation bookkeeping change and document it in the plan or review notes.
- **Decision**: SKIPPED — Roadmap synchronization is mandated by the implementation workflow and is intentionally retained as bookkeeping.

### F8 — Result invariants are implicit

- **Severity**: ℹ️ OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Architecture
- **Location**: `MarketData/MarketDataContracts.cs:120-129`
- **Detail**: `MarketDataResult` permits `Quality.Complete` with no quote, no historical points, or no source, and permits a null source for otherwise successful data. Downstream consumers must rely on convention rather than a validated evidence state.
- **Fix**: Add explicit result validation or documented construction invariants when the endpoint and provider adapter are implemented.
- **Decision**: SKIPPED — Defer explicit result invariants until the provider adapter and endpoint define the construction boundary.

## Verification Evidence

- `dotnet build .bootstrap-scaffold.csproj --no-restore` — passed with 0 warnings and 0 errors.
- `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore` — passed, 16 tests after triage fixes.
- Manual Phase 1 checks were confirmed by the user.
- No provider dependency, provider credential, or tenant secret was added.
