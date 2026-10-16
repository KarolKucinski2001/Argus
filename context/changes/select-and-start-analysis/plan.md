# Select and Start Analysis Implementation Plan

## Overview

Deliver the first protected analysis-entry flow for Argus. An authenticated
user will open `/analysis`, choose one or more configured assets, and submit a
bounded analysis request using the existing provider-neutral market-data
contract. Development and test environments will use deterministic fake data;
production will retain the existing provider-approval gate and will not enable
the fake provider.

This slice stops at a clear analysis-start/result status. Ranking, Opportunity
Score, factors, charts, and grounded AI summaries remain in S-03.

## Current State Analysis

The application has Razor Pages, cookie/OIDC authentication, and a protected
`POST /api/market-data` route in `Program.cs`. That route already validates the
configured allowlist, duplicate assets, the maximum selection count, the
fixed 12-month history window, provider approval, request budgets, caching, and
provider error mappings.

The repository has no asset-selection page, analysis PageModel, browser form,
analysis-start service, or runtime fake provider. The configured allowlist is
currently the only asset catalog. The account page is the existing protected
workflow handoff, while the market-data contract tests provide reusable fake
provider and authenticated-host patterns.

## Desired End State

An authenticated user can navigate from `/account` to `/analysis`, see the
configured assets as accessible checkboxes, select between one and the
configured maximum (currently five), and submit the form. The page builds the
existing fixed 12-month daily request, starts the provider-neutral market-data
operation, and renders a bounded success, unavailable, or validation state
without losing the submitted selection.

Development and test environments return deterministic, clearly illustrative
fake results through the same provider contract. Production and non-development
environments continue to use the unavailable/provider-approval behavior until
an approved provider is configured.

### Key Discoveries:

- `Program.cs:129-337` contains the existing protected market-data route and
  most of the validation, caching, budget, and provider error behavior.
- `MarketData/MarketDataContracts.cs:40-108` defines the provider-neutral
  request and per-asset result contracts; `MarketDataOptions.cs:5-65` defines
  the fixed 12-month window, five-asset limit, and allowlist.
- `Pages/Account/Index.cshtml.cs:9-31` is the current authenticated browser
  handoff, while `AccountAccessContractTests.cs:82-122` covers authenticated
  rendering and unauthenticated challenge behavior.
- `MarketDataContractTests.cs:445-566` contains reusable fake-provider,
  seeded-cache, and configured test-host patterns.
- `docs/market-data.md` currently states that deterministic fake-provider use
  is test-only; this plan updates that boundary to Development/Test only,
  never production.

## What We're NOT Doing

- No production market-data provider selection, credentials, licensing
  approval, or public-display approval.
- No database, persistence, saved analyses, watchlists, jobs, queues, or
  background processing.
- No Opportunity Score, ranking, factors, chart, AI summary, or full analysis
  result page; those belong to S-03.
- No user-configurable history dates or granularity; S-02 uses the required
  12-month daily request.
- No separate asset database or presentation catalog; the configured allowlist
  remains the source of selectable assets.
- No frontend framework or client-side state-management dependency.

## Implementation Approach

Extract the reusable market-data request execution from the top-level route
into a provider-neutral application service. Keep the existing API route as a
thin adapter and let the protected Razor Page handler use the same service,
avoiding self-HTTP calls and duplicated validation. Register a deterministic
fake provider only when the host environment is Development or Testing; all
other environments retain `UnavailableMarketDataProvider`.

Build `/analysis` as a server-rendered Razor Page with checkbox form binding.
The PageModel projects `MarketDataOptions.AllowedAssets` into the form,
constructs the fixed history request on submit, and maps stable market-data
error categories to bounded inline messages. Tests will cover authentication,
selection validation, successful fake start, provider-unavailable behavior,
and the production environment boundary.

## Critical Implementation Details

The provider environment guard must be enforced in service registration, not
only in the page, so a future API or background caller cannot accidentally
receive the Development/Test fake in production. The UI must call the shared
application service rather than issuing an HTTP request back into the same
process; this preserves the existing contract tests and keeps ProblemDetails
mapping at the appropriate adapter boundary.

## Phase 1: Shared analysis start contract and safe fake provider

### Overview

Create the reusable service boundary needed by both the existing API endpoint
and the new Razor Page, then add deterministic fake data only to Development
and Testing. Preserve the current validation, cache, budget, provider approval,
and error semantics.

### Changes Required:

#### 1. Market-data application service

**Files**: `MarketData/IMarketDataService.cs`,
`MarketData/MarketDataService.cs`, `Program.cs`

**Intent**: Move the existing market-data request execution behind a
testable service so browser and API callers share one authoritative contract.

**Contract**: The service accepts a `MarketDataRequest` and cancellation token,
returns a `MarketDataResponse` or a stable validation/provider failure result,
and preserves the current allowlist normalization, duplicate/limit/history
validation, cache, request-budget, timeout, partial-result, and error-category
behavior. `POST /api/market-data` remains protected and keeps its current
ProblemDetails response contract.

#### 2. Development/Test fake provider

**File**: `MarketData/DevelopmentMarketDataProvider.cs`

**Intent**: Provide deterministic complete and illustrative market-data
results so the local protected flow can be exercised without a real provider.

**Contract**: The provider implements `IMarketDataProvider`, returns results
only for the requested normalized assets and requested 12-month history, uses
explicit illustrative source metadata, and does not contain credentials,
network calls, or production-provider identifiers.

#### 3. Environment-bound registration

**Files**: `Program.cs`, `docs/market-data.md`

**Intent**: Register the fake provider only in Development/Test and document
the changed local-test boundary without weakening production approval rules.

**Contract**: Development and Testing resolve the deterministic fake provider;
other environments resolve `UnavailableMarketDataProvider` unless a future
approved provider is explicitly registered. `PublicDisplayApproved` remains
false in tracked configuration, and the documentation states that fake data
is illustrative and never provider approval.

### Success Criteria:

#### Automated Verification:

- The application builds with `dotnet build .bootstrap-scaffold.csproj --no-restore`.
- Existing market-data contract tests pass with the API route using the shared
  service.
- New tests prove Development/Test resolve the fake provider and a
  non-development host does not resolve it.

#### Manual Verification:

- The existing `/api/market-data` behavior remains unchanged for valid,
  invalid, unauthorised, unavailable, and partially successful requests.
- Local Development results visibly identify themselves as illustrative and
  never appear as approved production data.

## Phase 2: Protected asset selection and analysis start page

### Overview

Add the `/analysis` Razor Page and account handoff using the selected
server-rendered checkbox, fixed-history, inline-error design.

### Changes Required:

#### 1. Analysis page model and route

**Files**: `Pages/Analysis/Index.cshtml`,
`Pages/Analysis/Index.cshtml.cs`

**Intent**: Give authenticated users a focused entry point for selecting
configured assets and starting the first analysis operation.

**Contract**: The page is reachable at `/analysis`, requires an authenticated
user, renders one checkbox per `MarketDataOptions.AllowedAssets` entry, and
binds selected canonical IDs on POST. A submission with one to the configured
maximum asset count constructs a 12-month daily request ending on the current
UTC date and invokes the shared service.

#### 2. Selection states and bounded messages

**Files**: `Pages/Analysis/Index.cshtml`,
`Pages/Analysis/Index.cshtml.cs`

**Intent**: Keep the page useful without JavaScript and preserve user input
when validation or provider errors occur.

**Contract**: Empty selection, duplicate/invalid selection, and over-limit
selection render inline validation without invoking the provider. Provider
unavailable, not-approved, timeout, quota, and upstream failures map to
bounded user-facing messages without exposing credentials, raw exceptions, or
provider payloads. Successful Development/Test execution renders a
clearly illustrative started state and the selected assets, without ranking.

#### 3. Account navigation and token-backed styling

**Files**: `Pages/Account/Index.cshtml`, `wwwroot/css/site.css`

**Intent**: Make the protected analysis entry discoverable from the signed-in
account page and visually consistent with the existing Argus shell.

**Contract**: The account page links to `/analysis`; the new page uses existing
layout, focus, button, surface, spacing, and responsive tokens. No view-local
literal colors or arbitrary layout values are introduced.

### Success Criteria:

#### Automated Verification:

- An unauthenticated GET `/analysis` follows the repository's protected-page
  challenge contract.
- An authenticated GET `/analysis` renders every configured asset and the
  configured maximum selection count.
- Valid POST selection invokes the shared service with canonical assets, a
  12-month daily history, and the authenticated request context.
- Empty, duplicate, unsupported, and over-limit selections do not invoke the
  provider and render bounded inline validation.
- Provider-unavailable and successful fake-provider states render without
  leaking raw provider details.

#### Manual Verification:

- A signed-in user can reach `/analysis` from `/account`, select multiple
  assets, submit, and see the illustrative started state.
- Keyboard users can reach every checkbox and the submit control, and focus
  remains visible at desktop and mobile widths.
- Validation and provider errors remain understandable, preserve selected
  assets, and do not create horizontal overflow.

## Phase 3: Contract coverage, documentation, and release boundary

### Overview

Complete deterministic tests and documentation for the protected selection
flow, fake-provider boundary, and future handoff to S-03.

### Changes Required:

#### 1. Browser and API contract tests

**Files**: `tests/Argus.Authentication.Tests/AccountAccessContractTests.cs`,
`tests/Argus.Authentication.Tests/MarketDataContractTests.cs`, and a focused
analysis contract test file if the existing fixtures cannot remain clear

**Intent**: Protect the user-visible route, authentication behavior,
selection contract, service invocation, and safe error mapping.

**Contract**: Tests use the existing `WebApplicationFactory` authentication
handlers and fake market-data fixtures. They must cover unauthenticated
challenge, authenticated asset rendering, account link, valid multi-asset
submit, empty/over-limit validation, provider unavailable, successful
illustrative fake response, and no leakage of raw provider details.

#### 2. Market-data documentation and handoff

**Files**: `docs/market-data.md`, `context/changes/select-and-start-analysis/change.md`

**Intent**: Record that the slice is local/testable without implying provider
approval, and leave a precise boundary for S-03.

**Contract**: Documentation states that Development/Test fake data is
illustrative only, production approval remains false, and S-03 consumes
normalized evidence rather than a new provider-specific contract. Change
metadata is updated through the planning lifecycle without changing its
identity.

### Success Criteria:

#### Automated Verification:

- `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore` passes.
- `dotnet build .bootstrap-scaffold.csproj --no-restore` passes with no warnings
  introduced by the slice.
- A source/configuration scan confirms no provider credentials, production fake
  registration, or new literal colors in the analysis view.

#### Manual Verification:

- The complete local flow is understandable: account → analysis → select
  assets → start → illustrative status or bounded error.
- Production configuration still communicates unavailable/unapproved data
  rather than presenting fake results.
- The page remains usable at `1440x900` and `390x844`, including visible focus,
  selected-state feedback, and educational-not-advice language.

## Testing Strategy

### Unit Tests:

- Validate the shared service's request normalization and preservation of the
  existing market-data error categories.
- Validate the Development/Test fake provider returns deterministic,
  in-range, complete illustrative results for the requested assets.
- Validate PageModel selection binding, fixed history construction, state
  mapping, and no-provider-on-invalid-input behavior.

### Integration Tests:

- Exercise `/analysis` through `WebApplicationFactory` with the existing test
  authentication handler.
- Exercise valid and invalid form POSTs with the fake provider and verify the
  rendered state.
- Exercise the existing `/api/market-data` endpoint after service extraction
  to prove API behavior remains compatible.

### Manual Testing Steps:

1. Run the application in Development and sign in through the configured local
   Entra flow or the deterministic test host.
2. Open `/account`, follow the analysis link, select one asset, and submit.
3. Repeat with multiple assets, no assets, and more than five assets.
4. Verify the success state identifies data as illustrative and does not show
   a ranking or recommendation.
5. Verify provider-unavailable/error states preserve the selection and expose
   no raw provider or credential details.
6. Traverse the page with the keyboard at desktop and mobile widths.

## Performance Considerations

The slice remains synchronous and bounded by the existing request timeout,
cache, and application request budget. It adds no database, background job,
client framework, or unbounded provider call. The fake provider must generate
only the requested assets and fixed history range.

## Migration Notes

No data migration is required. The service extraction must preserve the
existing API route and response semantics. Production configuration remains
unchanged except for documentation of the Development/Test fake boundary.

## References

- Product contract: `context/foundation/prd.md` (FR-002, FR-003, FR-004,
  US-01, educational guardrails)
- Roadmap slice: `context/foundation/roadmap.md` (S-02)
- Authentication contract: `docs/authentication.md`
- Market-data contract: `docs/market-data.md`
- Existing API implementation: `Program.cs:129-337`
- Market-data types and options: `MarketData/MarketDataContracts.cs`,
  `MarketData/MarketDataOptions.cs`
- Existing test fixtures: `tests/Argus.Authentication.Tests/MarketDataContractTests.cs`
- Account access patterns: `Pages/Account/Index.cshtml.cs`,
  `tests/Argus.Authentication.Tests/AccountAccessContractTests.cs`

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Shared analysis start contract and safe fake provider

#### Automated

- [x] 1.1 The application builds with `dotnet build .bootstrap-scaffold.csproj --no-restore`. — 24ce858
- [x] 1.2 Existing market-data contract tests pass with the API route using the shared service. — 24ce858
- [x] 1.3 New tests prove Development/Test resolve the fake provider and a non-development host does not resolve it. — 24ce858

#### Manual

- [x] 1.4 The existing `/api/market-data` behavior remains unchanged for valid, invalid, unauthorised, unavailable, and partially successful requests. — 24ce858
- [x] 1.5 Local Development results visibly identify themselves as illustrative and never appear as approved production data. — 24ce858

### Phase 2: Protected asset selection and analysis start page

#### Automated

- [x] 2.1 An unauthenticated GET `/analysis` follows the repository's protected-page challenge contract. — 4e5e675
- [x] 2.2 An authenticated GET `/analysis` renders every configured asset and the configured maximum selection count. — 4e5e675
- [x] 2.3 Valid POST selection invokes the shared service with canonical assets, a 12-month daily history, and the authenticated request context. — 4e5e675
- [x] 2.4 Empty, duplicate, unsupported, and over-limit selections do not invoke the provider and render bounded inline validation. — 4e5e675
- [x] 2.5 Provider-unavailable and successful fake-provider states render without leaking raw provider details. — 4e5e675

#### Manual

- [x] 2.6 A signed-in user can reach `/analysis` from `/account`, select multiple assets, submit, and see the illustrative started state. — 4e5e675
- [x] 2.7 Keyboard users can reach every checkbox and the submit control, and focus remains visible at desktop and mobile widths. — 4e5e675
- [x] 2.8 Validation and provider errors remain understandable, preserve selected assets, and do not create horizontal overflow. — 4e5e675

### Phase 3: Contract coverage, documentation, and release boundary

#### Automated

- [x] 3.1 `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore` passes. — 879667e
- [x] 3.2 `dotnet build .bootstrap-scaffold.csproj --no-restore` passes with no warnings introduced by the slice. — 879667e
- [x] 3.3 A source/configuration scan confirms no provider credentials, production fake registration, or new literal colors in the analysis view. — 879667e

#### Manual

- [x] 3.4 The complete local flow is understandable: account → analysis → select assets → start → illustrative status or bounded error. — 879667e
- [x] 3.5 Production configuration still communicates unavailable/unapproved data rather than presenting fake results. — 879667e
- [x] 3.6 The page remains usable at `1440x900` and `390x844`, including visible focus, selected-state feedback, and educational-not-advice language. — 879667e
