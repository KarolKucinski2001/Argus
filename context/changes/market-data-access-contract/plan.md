# Market data access contract Implementation Plan

## Overview

Establish the provider-neutral market-data contract required by Argus's first explained-analysis flow. The change will define normalized current and historical data, freshness and evidence warnings, explicit failure categories, bounded access settings, and a protected endpoint seam without enabling an unapproved provider.

## Current State Analysis

Argus is an ASP.NET Core .NET 10 application whose startup currently maps authentication, `/api/me`, and starter weather data; it has no market-data service, DTOs, provider adapter, cache, persistence, or market-data error contract (`Program.cs:1-105`). The project has no market-data, resilience, cache, database, or provider-client package (`.bootstrap-scaffold.csproj:1-16`).

The product contract requires selected-asset analysis with current price, 24-hour/7-day/30-day performance, historical chart data, explainable factors, and explicit insufficient-evidence handling (`context/foundation/prd.md:50-89`). The agreed MVP narrows this to a small fixed list of US equities and crypto, delayed/latest data, 12 months of history, free-tier access, public display, and visible provider attribution. No provider is approved until its official terms explicitly permit that display model (`context/changes/market-data-access-contract/research.md`).

Existing API conventions include named routes, authorization on protected endpoints, and RFC-style Problem Details for authentication failures (`Program.cs:28-48`, `Program.cs:73-96`, `tests/Argus.Authentication.Tests/AuthenticationContractTests.cs:15-31`). Deployment uses Azure App Service and GitHub Actions; provider credentials must remain configuration/secret-store values rather than source-controlled settings (`.github/workflows/deploy.yml:1-48`, `appsettings.json:1-18`).

## Desired End State

Argus has a documented and testable application-facing market-data contract that does not expose provider payloads or symbols. A protected normalized endpoint can return per-asset current and historical data, freshness/provenance metadata, and evidence warnings; unavailable assets fail explicitly without discarding usable results.

The contract is ready for a real provider adapter, but production provider access remains disabled until a documented compliance gate confirms coverage, quota, 12-month history, attribution, and public-display permission. Tests run deterministically without network access, API keys, or production secrets.

### Key Discoveries:

- No market-data implementation or reusable provider abstraction exists (`Program.cs:73-105`).
- The roadmap explicitly requires a verified source and evidence contract while avoiding provider-specific commitment (`context/foundation/roadmap.md:85-95`).
- Public display rights, not five-year history, are the remaining provider-selection blocker (`context/changes/market-data-access-contract/research.md`).
- Existing authentication tests use `WebApplicationFactory` and deterministic test handlers, which is the appropriate pattern for endpoint contract tests (`tests/Argus.Authentication.Tests/AuthenticationContractTests.cs:34-137`).

## What We're NOT Doing

- Selecting or enabling a real market-data provider without official public-display permission.
- Implementing asset-selection UI, ranking, scoring, factor calculation, charts, AI explanations, or the complete analysis workflow.
- Adding a database, durable market-data observations, migrations, background refresh jobs, watchlists, alerts, or provider fallback.
- Supporting the full market; the contract targets a small configured allowlist of US equities and crypto.
- Storing API keys, tenant credentials, or provider secrets in source control or committed appsettings files.

## Implementation Approach

Create a small `MarketData` application boundary containing normalized contracts, options, provider and cache interfaces, and explicit result/error semantics. Register the boundary from `Program.cs`, expose a protected named endpoint for the normalized result, and use in-memory caching with bounded request settings. Keep the production provider unconfigured until the compliance gate passes; use a fake HTTP-backed provider in tests to exercise success, partial evidence, stale data, quota, timeout, and upstream failure paths without network access.

## Critical Implementation Details

The endpoint must preserve usable per-asset results when another requested asset is unsupported, unavailable, stale, or incomplete. A provider adapter cannot be considered production-ready merely because it returns the required fields: the compliance record must also verify public-display permission and attribution requirements for the exact free tier.

## Phase 1: Define the normalized contract and compliance boundary

### Overview

Define the provider-neutral market-data types, options, interfaces, and documentation needed by the analysis layer. Make licensing approval and provider enablement explicit configuration/documentation gates rather than implicit assumptions.

### Changes Required:

#### 1. Market-data contracts and options

**File**: `MarketData/MarketDataContracts.cs`

**Intent**: Add normalized asset, quote, historical-point, result, freshness, warning, and error contracts so downstream ranking and explanation code can consume evidence without knowing provider payloads.

**Contract**: Represent an asset with an Argus-facing identifier and asset class; represent current and historical values with currency, timestamps, requested range/granularity, source metadata, freshness, and data-quality state; represent warnings/errors with stable categories and retryability.

#### 2. Provider and cache abstractions

**File**: `MarketData/IMarketDataProvider.cs`

**Intent**: Define the application-facing provider operation and cache boundary while keeping provider symbols, HTTP payloads, credentials, and quotas behind an adapter.

**Contract**: The provider accepts validated asset references and a bounded history request and returns a normalized per-asset result; it must not expose provider-specific DTOs through the application endpoint.

**File**: `MarketData/IMarketDataCache.cs`

**Intent**: Define the cache operations needed to reduce repeated provider calls without committing the MVP to durable storage.

**Contract**: Cache keys include the normalized asset and requested range/granularity; entries carry freshness/expiry metadata and cannot hide stale or incomplete evidence from the caller.

#### 3. Configuration and compliance documentation

**File**: `MarketData/MarketDataOptions.cs`

**Intent**: Centralize bounded operational settings and the provider-approval gate so limits are explicit and validated at startup or request time.

**Contract**: Include the configured history window of 12 months, maximum selected assets, request timeout, cache duration, application request budget, provider identifier, and an explicit public-display approval flag; secrets are represented by configuration keys only.

**File**: `docs/market-data.md`

**Intent**: Record the MVP contract, attribution requirement, provider approval checklist, and the current state that no provider is enabled until official terms support public display.

**Contract**: The checklist must require coverage for the fixed asset allowlist, delayed/latest data, 12-month history, quota/rate-limit behavior, attribution, public-display permission, missing-data semantics, and safe secret configuration.

### Success Criteria:

#### Automated Verification:

- The solution builds with the new contracts and options without adding an unapproved provider dependency.
- Contract tests compile against the normalized types and verify that provider-specific fields are not required by the application result.
- Options validation rejects invalid history, asset-count, timeout, cache, or provider-approval settings.

#### Manual Verification:

- Review `docs/market-data.md` and confirm it matches the agreed public-display, attribution, 12-month, and free-tier constraints.
- Confirm no provider credential or tenant secret is present in committed configuration.

**Implementation Note**: After completing this phase and automated verification, pause for manual confirmation that the compliance boundary and contract terminology are acceptable before proceeding.

## Phase 2: Add the protected endpoint and in-memory cache

### Overview

Wire the normalized contract into the ASP.NET Core application with explicit validation, authorization, per-asset results, warnings, and in-memory caching. Keep real provider access disabled until Phase 3's compliance evidence exists.

### Changes Required:

#### 1. Application registration and endpoint

**File**: `Program.cs`

**Intent**: Register the market-data options, cache, provider boundary, and protected endpoint using the existing top-level startup and authorization conventions.

**Contract**: Add a named protected route for a normalized market-data request; validate the requested allowlist, asset count, date/range bounds, and configured history window before invoking the provider; return explicit Problem Details for invalid requests or unavailable service configuration.

#### 2. In-memory cache implementation

**File**: `MarketData/InMemoryMarketDataCache.cs`

**Intent**: Implement the selected in-memory cache strategy with bounded expiry and visible freshness metadata.

**Contract**: Cache hits must retain the original observation/source timestamps; expired entries must be distinguishable from fresh entries; cache behavior must not convert an upstream failure into a false successful result.

#### 3. Configuration defaults

**File**: `appsettings.json`

**Intent**: Add non-secret market-data operational defaults that make the 12-month contract and bounded request behavior explicit.

**Contract**: Store only safe defaults and provider-independent limits; provider keys and approval values requiring deployment control must be supplied through environment/App Service configuration.

**File**: `appsettings.Development.json`

**Intent**: Provide safe local defaults for deterministic development and tests without inserting real provider credentials.

**Contract**: Local configuration must permit the endpoint and fake-provider tests to run without network access or secrets.

### Success Criteria:

#### Automated Verification:

- An unauthenticated request to the market-data route returns the repository-standard `401` Problem Details response.
- A valid authenticated request returns normalized per-asset results and warnings without provider-shaped payloads.
- Invalid asset, over-limit, invalid-range, unconfigured-provider, timeout, quota, and upstream-error cases return stable explicit error categories.
- Fresh cache hits avoid a second provider call; expired or incomplete entries expose freshness/evidence warnings.

#### Manual Verification:

- Inspect the endpoint response and confirm current data, 24-hour/7-day/30-day values, historical points, timestamps, attribution metadata, and warnings are understandable to a downstream analysis/UI consumer.
- Confirm a mixed request with one usable and one unavailable asset preserves the usable result and clearly identifies the evidence gap.

**Implementation Note**: After completing this phase and automated verification, pause for manual confirmation of the response shape and partial-result behavior.

## Phase 3: Add deterministic provider tests and readiness gate

### Overview

Build the test seam around a fake HTTP provider and document the exact evidence required before any real provider adapter can be enabled.

### Changes Required:

#### 1. Market-data contract and endpoint tests

**File**: `tests/Argus.Authentication.Tests/MarketDataContractTests.cs`

**Intent**: Extend the existing `WebApplicationFactory` test project with deterministic tests for authorization, normalization, partial results, cache behavior, and explicit failure semantics.

**Contract**: Tests must use an in-memory/fake HTTP handler or provider fixture; they must not require network access, API keys, live provider responses, or production secrets.

#### 2. Provider readiness record

**File**: `docs/market-data.md`

**Intent**: Add a release-readiness section that prevents accidental activation of an unapproved provider.

**Contract**: A provider is ready only when official terms support public display with attribution and the documented endpoint coverage, 12-month history, quota, freshness, and error behavior have been verified; otherwise the application reports an explicit unavailable/configuration state.

#### 3. Deployment configuration handoff

**File**: `.github/workflows/deploy.yml`

**Intent**: Document or validate the future provider configuration handoff without provisioning a provider prematurely.

**Contract**: The workflow must not echo secrets or hard-code provider credentials; any future provider settings are supplied through Azure App Service configuration/secret management and remain outside source control.

### Success Criteria:

#### Automated Verification:

- The focused authentication and market-data test project passes with `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`.
- Tests cover successful normalized data, one-asset and multi-asset requests, partial evidence, stale cache, invalid assets, quota exhaustion, timeout, upstream outage, and provider-not-approved states.
- The application build passes with `dotnet build .bootstrap-scaffold.csproj --no-restore`.

#### Manual Verification:

- Review the provider readiness checklist and confirm that no real provider is marked approved without official public-display evidence.
- Run an authenticated smoke test with the fake provider and verify visible attribution metadata, evidence warnings, and the educational-not-advice boundary remain available to downstream UI work.

**Implementation Note**: This phase does not authorize production provider activation; that requires a separately verified provider decision and a follow-up implementation change if the adapter contract remains compatible.

## Testing Strategy

### Unit Tests:

- Normalize valid quote and historical responses, including timestamps, currency, adjusted/unadjusted semantics, and freshness.
- Reject unsupported assets, invalid date ranges, over-limit requests, and malformed provider payloads.
- Preserve per-asset warnings for missing, stale, incomplete, or unavailable evidence.
- Verify cache key isolation, expiry, and no false-success behavior after provider errors.
- Verify stable error categories and retryability flags for timeout, quota, authentication, and upstream-unavailable cases.

### Integration Tests:

- Use `WebApplicationFactory<Program>` with deterministic authentication configuration, matching existing authentication contract tests.
- Exercise the protected market-data endpoint with a fake provider and fake HTTP responses.
- Verify no network or provider secret is needed for the focused test suite.

### Manual Testing Steps:

1. Authenticate through the existing test/development path and request one supported equity and one supported crypto asset.
2. Verify normalized current values, 24-hour/7-day/30-day metrics, 12-month historical points, timestamps, and attribution metadata.
3. Simulate one unavailable asset and confirm usable assets remain visible with an explicit evidence warning.
4. Simulate stale cache, quota exhaustion, timeout, and provider-not-approved states and verify the response does not present them as fresh successful data.

## Performance Considerations

Use bounded request sizes, timeouts, cache expiry, and application-level request budgets. In-memory caching is intentionally scoped to the App Service instance and does not provide cross-instance consistency; durable or distributed caching is out of scope for this foundation.

## Migration Notes

No database migration is required because the selected foundation stores no durable market-data observations, asset mappings, or analysis snapshots. A later provider implementation may add provider-specific configuration through deployment settings without changing the normalized endpoint contract.

## References

- Related research: `context/changes/market-data-access-contract/research.md`
- Product requirements: `context/foundation/prd.md:50-89`
- Roadmap blocker: `context/foundation/roadmap.md:85-95`
- Existing startup and route conventions: `Program.cs:1-105`
- Existing test convention: `tests/Argus.Authentication.Tests/AuthenticationContractTests.cs:34-137`
- Deployment/configuration constraints: `.github/workflows/deploy.yml:1-48`, `appsettings.json:1-18`
- Provider documentation reviewed in research: `https://twelvedata.com/pricing`, `https://twelvedata.com/docs`, `https://www.alphavantage.co/documentation/`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append — <commit sha> when a step lands. Do not rename step titles.

### Phase 1: Define the normalized contract and compliance boundary

#### Automated

- [x] 1.1 Solution builds with the new contracts and options without adding an unapproved provider dependency — 4f4f92f
- [x] 1.2 Contract tests compile against normalized types without requiring provider-specific fields — 4f4f92f
- [x] 1.3 Options validation rejects invalid history, asset-count, timeout, cache, and provider-approval settings — 4f4f92f

#### Manual

- [x] 1.4 Review the market-data contract documentation and confirm the agreed public-display, attribution, 12-month, and free-tier constraints — 4f4f92f
- [x] 1.5 Confirm no provider credential or tenant secret is present in committed configuration — 4f4f92f

### Phase 2: Add the protected endpoint and in-memory cache

#### Automated

- [ ] 2.1 Unauthenticated market-data requests return the repository-standard 401 Problem Details response
- [ ] 2.2 Valid authenticated requests return normalized per-asset results and warnings
- [ ] 2.3 Invalid asset, over-limit, invalid-range, unconfigured-provider, timeout, quota, and upstream-error cases return stable explicit error categories
- [ ] 2.4 Fresh cache hits avoid a second provider call and expired entries expose freshness/evidence warnings

#### Manual

- [ ] 2.5 Inspect the endpoint response for understandable values, timestamps, attribution metadata, and warnings
- [ ] 2.6 Confirm mixed usable/unavailable requests preserve usable results and identify the evidence gap

### Phase 3: Add deterministic provider tests and readiness gate

#### Automated

- [ ] 3.1 Focused authentication and market-data tests pass without network access, API keys, or production secrets
- [ ] 3.2 Tests cover success, multi-asset requests, partial evidence, stale cache, invalid assets, quota exhaustion, timeout, upstream outage, and provider-not-approved states
- [ ] 3.3 The application build passes with the repository's configured project

#### Manual

- [ ] 3.4 Review the provider readiness checklist and confirm no provider is approved without official public-display evidence
- [ ] 3.5 Run an authenticated fake-provider smoke test and verify attribution metadata, evidence warnings, and educational boundary data
