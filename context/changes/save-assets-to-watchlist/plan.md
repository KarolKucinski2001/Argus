# Save assets to a personal watchlist Implementation Plan

## Overview

Add a durable, user-owned watchlist so an authenticated Argus user can save
selected configured assets, review and remove them, and select up to the
existing analysis maximum to start another analysis. Use Azure Table Storage
with a replaceable store contract, explicit failure states, and the existing
Razor Pages and authentication conventions.

## Current State Analysis

Argus has an authenticated Razor Pages analysis workspace, but no durable
application data store or user-owned domain model. `/analysis` validates
selected asset IDs against `MarketDataOptions.AllowedAssets`, limits a request
to five assets, and runs the existing market-data and analysis services
(`Pages/Analysis/Index.cshtml.cs:13-111,136-164`). The canonical asset identity
is centralized in `AssetIdentityCatalog` and the configured allowlist
(`Analysis/AssetIdentityCatalog.cs:15-35`,
`MarketData/MarketDataOptions.cs:11-18`).

Authentication already exposes a stable subject claim (`sub`, falling back to
`ClaimTypes.NameIdentifier`) and explicitly challenges browser pages
(`Program.cs:43-101,122-143`,
`Pages/Analysis/Index.cshtml.cs:36-52,75-82`). No EF Core, database client,
repository, migration, or ownership-check pattern exists
(`.bootstrap-scaffold.csproj:1-17`), so this change must establish the first
durable storage boundary without coupling the UI directly to Azure SDK calls.

The roadmap defines S-04 as the prerequisite for the Dashboard and leaves the
per-user storage choice to this plan (`context/foundation/roadmap.md:21-35,
66-76`). The existing analysis UI is server-rendered and token-backed; prior
analysis work explicitly excluded watchlists and persistence, so this plan
must preserve the existing selection, ranking, educational-boundary, and
provider-error contracts.

## Desired End State

An authenticated user can save the currently selected assets from `/analysis`,
see a confirmation or an explicit failure without losing the selection, open
`/watchlist`, remove entries immediately, and select up to five saved assets
to run the existing analysis flow. Each saved entry belongs only to the
subject claim that created it; duplicate saves are idempotent and the newest
saved entries appear first.

The watchlist uses Azure Table Storage through an application-owned store
interface. Local development can point the same implementation at an emulator
or configured endpoint, while missing or unavailable storage produces a
visible error rather than a false success. Assets removed from the configured
allowlist remain visible as unavailable and can be removed, but cannot be
submitted for analysis.

### Key Discoveries:

- `Pages/Analysis/Index.cshtml.cs:76-111` has one existing POST analysis flow;
  save must be an additional named handler, not a replacement for analysis.
- `Pages/Analysis/_AssetOption.cshtml:5-19` already posts canonical asset IDs
  through `SelectedAssetIds`.
- `Program.cs:122-143` is the canonical subject-claim extraction pattern; do
  not key data by display name or email.
- `Pages/Analysis/Index.cshtml:24-94` keeps the selection form and result
  workspace together; the new save action must preserve that layout and its
  semantic CSS token rules.
- `context/foundation/roadmap.md:66-76` makes this change the first durable
  per-user slice and the prerequisite for S-05.

## What We're NOT Doing

- No Dashboard view, tabbed workspace, or monitoring summary; those belong to
  `watchlist-monitoring-dashboard`.
- No real-time alerts, daily emails, background jobs, portfolio management,
  trading, or personalized investment advice.
- No provider-specific production market-data activation; saved entries use
  the existing configured asset allowlist.
- No local profile, password, token, or Entra credential persistence.
- No history of every save action, manual ordering, soft delete, restore, or
  unlimited watchlists.
- No silent in-memory fallback when durable storage is unavailable.

## Implementation Approach

Introduce a `Watchlist` application boundary with an options object, a
user-scoped store interface, and an Azure Table Storage implementation. Store
one row per `(subject ID, asset ID)` using the subject as the partition key and
the canonical asset ID as the row key; this enforces the selected duplicate
semantics at the storage boundary and keeps reads/removes user-scoped.

Register the store in `Program.cs` and bind `Watchlist` configuration without
putting credentials in tracked files. Add a save handler to `/analysis` and a
new `/watchlist` Razor Page. Both pages use the same configured asset catalog,
the existing maximum-selection validation, explicit authentication challenge
behavior, and semantic UI states. The watchlist page posts analysis selections
to the existing analysis path rather than creating a second analysis engine.

Use contract tests with the existing `WebApplicationFactory` authentication
fixture and focused store tests with a test double or emulator-compatible
configuration. Verify that two authenticated subjects cannot see or mutate
each other's rows, that duplicate saves remain one entry, and that storage
failures are surfaced explicitly.

## Critical Implementation Details

Azure Table keys are load-bearing: the subject claim must be the partition key
and the canonical case-insensitive asset ID must be normalized for the row key.
The store must enforce the 50-entry cap atomically enough to prevent normal
duplicate retries from consuming capacity, and must return a distinct
conflict/capacity result that page handlers can render without treating a
partial save as success.

The configured allowlist remains the authority for display metadata and
analysis eligibility, but it must not be used to delete persisted rows. A row
whose asset ID is no longer configured is rendered as unavailable, remains
removable, and is excluded from the analysis POST.

## Phase 1: Establish watchlist storage contract

### Overview

Create the durable storage boundary, Azure Table implementation, configuration
contract, and dependency registration without changing the user-facing
analysis workflow yet.

### Changes Required:

#### 1. Watchlist domain and store contract

**Files**: `Watchlist/WatchlistContracts.cs`,
`Watchlist/IWatchlistStore.cs`, `Watchlist/WatchlistOptions.cs`

**Intent**: Define the user-owned saved-asset record, operation results, the
50-entry limit, and the interface that pages use instead of depending on
Azure SDK types. Keep asset IDs canonical and case-insensitive at the boundary.

**Contract**: The store must support listing a subject's entries ordered by
saved time descending, idempotent save of one or more configured asset IDs,
removal by subject plus asset ID, and a capacity result. Its public contract
must carry enough state for pages to distinguish saved, unavailable, duplicate,
capacity, and storage-failure outcomes.

#### 2. Azure Table Storage implementation

**Files**: `Watchlist/AzureTableWatchlistStore.cs`,
`.bootstrap-scaffold.csproj`

**Intent**: Persist watchlist rows in Azure Table Storage while keeping the
application contract independent from the provider. Use the selected
partition/row-key design to make all operations user-scoped and duplicate-safe.

**Contract**: Add the `Azure.Data.Tables` dependency and implement the store
with `TableServiceClient`/`TableClient` configuration. The table entity contains
the subject partition key, normalized asset row key, canonical asset ID, and
UTC saved timestamp; it must not contain display names as the identity key or
any authentication secret.

#### 3. Configuration and service registration

**Files**: `Program.cs`, `appsettings.json`, `appsettings.Development.json`

**Intent**: Bind watchlist storage settings and register the production store
through dependency injection, while allowing local development to target an
Azurite/emulator or another configured endpoint.

**Contract**: Add a `Watchlist` configuration section with a connection
string/endpoint setting and table name. Validate required settings when the
store is used, preserve the existing Development/Testing startup behavior,
and never add a tracked secret or silently replace durable storage with an
in-memory fallback.

### Success Criteria:

#### Automated Verification:

- The project restores and builds with the Azure Tables dependency:
  `dotnet restore .bootstrap-scaffold.csproj` and
  `dotnet build .bootstrap-scaffold.csproj --no-restore`.
- Focused store tests verify partition/row-key normalization, idempotent
  duplicate saves, newest-first ordering, the 50-entry cap, and explicit
  storage-failure results.

#### Manual Verification:

- With a local emulator or configured Azure Table endpoint, a smoke check can
  create/read/remove a row without putting credentials in tracked files.
- Reviewing configuration confirms that a missing or invalid storage setting
  cannot report a successful save.

## Phase 2: Add save, review, remove, and analyze flows

### Overview

Wire the storage contract into the authenticated analysis workspace and add the
dedicated watchlist page with all agreed user-visible states.

### Changes Required:

#### 1. Save selected assets from analysis

**Files**: `Pages/Analysis/Index.cshtml.cs`,
`Pages/Analysis/Index.cshtml`, `Pages/Analysis/_AssetOption.cshtml`

**Intent**: Let the user save the current checkbox selection without changing
the existing explicit “Generuj ranking” action or its validation. Show saved
state and a clear success/error result while preserving selected IDs on
validation or storage failure.

**Contract**: Add a named Razor Page save handler that reuses the existing
selection validation and authenticated subject resolution. The save control
posts to that handler, accepts one or more selected assets, enforces the
watchlist cap, and does not claim success for a partial or failed operation.

#### 2. Add the watchlist page and route

**Files**: `Pages/Watchlist/Index.cshtml.cs`,
`Pages/Watchlist/Index.cshtml`, `Pages/Shared/_Layout.cshtml`,
`wwwroot/css/site.css`

**Intent**: Provide `/watchlist` as the user-facing review surface with an
empty state, newest-first rows, immediate removal, unavailable-asset state,
and the educational boundary already used by analysis.

**Contract**: The page explicitly challenges unauthenticated users, loads only
the current subject's entries, resolves display metadata from the configured
allowlist, and renders unavailable rows without deleting them. Remove posts
must target the current subject and canonical asset ID; successful removal
rerenders the list and exposes an empty state when no entries remain. Add only
semantic tokens/classes to the shared stylesheet; keep literal colors and
arbitrary layout values out of Razor views.

#### 3. Start analysis from the watchlist

**Files**: `Pages/Watchlist/Index.cshtml.cs`,
`Pages/Watchlist/Index.cshtml`, `Pages/Analysis/Index.cshtml.cs`

**Intent**: Reuse the existing analysis services and five-asset selection
contract instead of creating a second analysis path.

**Contract**: The watchlist page posts selected available assets to the
analysis page's named analysis handler. It rejects zero selections, more than
five selections, unavailable assets, duplicates, and assets outside the
current allowlist using the same user-visible validation rules as `/analysis`.

#### 4. Authenticated navigation

**Files**: `Pages/Shared/_Layout.cshtml`

**Intent**: Make the watchlist reachable from the existing authenticated shell
without exposing it as a public or placeholder destination.

**Contract**: Add a `/watchlist` navigation link only for authenticated users,
preserving current account and analysis links and existing responsive layout
behavior.

### Success Criteria:

#### Automated Verification:

- Contract tests verify authenticated save/list/remove behavior, explicit
  unauthenticated challenge behavior, duplicate idempotency, 50-entry limit,
  unavailable assets, and cross-subject isolation.
- Contract tests verify that watchlist analysis accepts at most five available
  assets and reaches the existing analysis service without changing ranking,
  partial-result, or educational-boundary contracts.

#### Manual Verification:

- Sign in, select assets on `/analysis`, save them, and confirm the selection
  and confirmation remain visible; force a storage failure and confirm an
  explicit error leaves the selection intact.
- Open `/watchlist`, verify newest-first rows, remove an entry immediately,
  verify the empty state, and verify an asset removed from configuration is
  marked unavailable but removable.
- Select one to five available watchlist assets and start analysis; verify the
  existing result cards, ranking, factors, summary, and disclaimer remain
  unchanged.

## Phase 3: Verification, configuration documentation, and regression protection

### Overview

Complete deterministic test coverage and document the local emulator and Azure
App Service configuration required to operate the new durable feature.

### Changes Required:

#### 1. Watchlist tests and test seams

**Files**: `tests/Argus.Authentication.Tests/WatchlistStoreTests.cs`,
`tests/Argus.Authentication.Tests/WatchlistContractTests.cs`,
`tests/Argus.Authentication.Tests/AuthenticationContractTests.cs` (only if
shared test-factory support must be extended)

**Intent**: Make the first per-user persistence feature verifiable without a
live Entra tenant or production Azure account.

**Contract**: Store tests cover duplicate, ordering, cap, removal, unavailable
asset handling, and storage errors. Web contract tests use deterministic
subject claims and prove that subject A cannot read, remove, or affect subject
B's rows; they also preserve the existing `/api/me` and analysis access
contracts.

#### 2. Operational configuration documentation

**Files**: `docs/watchlist.md`, `docs/authentication.md` (only the
configuration cross-reference section if needed)

**Intent**: Document secret-safe local emulator setup, Azure App Service
application settings, table naming, and the expected failure behavior.

**Contract**: Documentation names the `Watchlist` keys, explains that
connection strings belong in user secrets/environment/App Service settings,
describes the emulator/endpoint option, and does not include credentials.

### Success Criteria:

#### Automated Verification:

- The focused suite passes:
  `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`.
- The production project passes:
  `dotnet test .bootstrap-scaffold.csproj --no-restore`.
- A repository search confirms no watchlist credential, token, or secret is
  tracked and all new Razor routes have corresponding contract coverage.

#### Manual Verification:

- Follow the documented local setup and complete save, list, remove, and
  analysis-from-watchlist smoke tests over HTTPS.
- Review the UI at narrow and wide widths for accessible labels, clear focus
  states, empty/error/unavailable states, and no advice-like wording.

## Testing Strategy

### Unit Tests:

- Normalize asset IDs case-insensitively and reject invalid identity input.
- Verify one row per subject/asset, newest-first ordering, immediate removal,
  capacity at exactly 50, and explicit failure results.
- Verify unavailable configured-asset resolution is separate from persisted
  row retention.

### Integration Tests:

- Use the existing `WebApplicationFactory<Program>` testing environment and
  deterministic claims to test Razor Page handlers.
- Replace the storage interface with a deterministic test store for page
  contract tests; use an emulator-compatible configuration for Azure adapter
  tests where available.
- Cover unauthenticated challenge, authenticated isolation, save/list/remove,
  duplicate save, cap enforcement, storage failure, unavailable asset, and
  analysis handoff.

### Manual Testing Steps:

1. Configure the local emulator/endpoint and authenticate with the existing
   local Entra flow.
2. Save one asset, save it again, save additional assets, and confirm one row
   per asset with newest-first ordering.
3. Reach the 50-entry boundary, verify the next save is rejected visibly, and
   remove an entry to make capacity available again.
4. Remove an asset from the configured allowlist, verify it remains
   unavailable and removable, and verify it cannot be analyzed.
5. Select up to five available assets from `/watchlist` and verify the
   existing analysis result contract.

## Performance Considerations

All normal operations are bounded by a user's maximum of 50 rows. Reads and
removes should be partition-scoped by subject, and the UI should not issue one
storage request per rendered row. No cache is required for correctness; the
future Dashboard may add read optimization after this store contract is
proven.

## Migration Notes

There is no existing watchlist data or persistence schema to migrate. Azure
Table Storage creates or verifies the configured table as part of the store's
startup/use path. Deployment must provide the connection setting before the
feature is used; the feature must fail explicitly when it is absent rather
than creating a process-local substitute.

## References

- `context/foundation/prd.md` — FR-017 and the educational/non-advice guardrails
- `context/foundation/roadmap.md` — S-04 and its S-05 dependency
- `Program.cs:122-143` — canonical authenticated subject extraction
- `Pages/Analysis/Index.cshtml.cs:76-164` — existing selection validation and analysis flow
- `Analysis/AssetIdentityCatalog.cs:15-35` — canonical asset identity metadata
- `docs/authentication.md` — secret-safe authentication and App Service configuration

## Progress

> Convention: `- [ ]` pending, `- [x]` done. Append ` — <commit sha>` when a step lands. Do not rename step titles.

### Phase 1: Establish watchlist storage contract

#### Automated

- [x] 1.1 The project restores and builds with the Azure Tables dependency — e19c098
- [x] 1.2 Focused store tests verify key normalization, idempotent saves, ordering, the 50-entry cap, and explicit storage failures — e19c098

#### Manual

- [x] 1.3 A local emulator or configured Azure Table endpoint completes a smoke create/read/remove without tracked credentials — e19c098
- [x] 1.4 Configuration review confirms missing or invalid storage cannot report a successful save — e19c098

### Phase 2: Add save, review, remove, and analyze flows

#### Automated

- [x] 2.1 Contract tests cover authenticated save/list/remove, unauthenticated challenge, duplicate idempotency, cap, unavailable assets, and cross-subject isolation — e19c098
- [x] 2.2 Watchlist analysis contract tests accept at most five available assets and preserve existing analysis contracts — e19c098

#### Manual

- [x] 2.3 The browser flow saves selected assets, preserves selection on failure, lists and removes entries, and renders the empty/unavailable states — e19c098
- [x] 2.4 The browser flow starts the existing analysis from one to five available watchlist assets without changing result presentation or disclaimer wording — e19c098

### Phase 3: Verification, configuration documentation, and regression protection

#### Automated

- [x] 3.1 The focused authentication and watchlist suite passes without production credentials or network access — 42d4ae2
- [x] 3.2 The production project test command passes and repository checks find no tracked watchlist secrets — 42d4ae2

#### Manual

- [ ] 3.3 The documented local setup and HTTPS smoke test complete successfully at narrow and wide viewport sizes
