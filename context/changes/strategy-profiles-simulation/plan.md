# Strategy profiles and simulated portfolio Implementation Plan

## Overview

First slice of PRD v2: browse curated profiles (FR-001), profile detail with
explained factors (FR-002), copy to a private simulated portfolio (FR-003),
list and delete own portfolios (FR-004). Educational simulation only.

## Approach

- Static `CuratedProfileCatalog` (3 profiles, approximate data, as-of date and source label).
- `StrategyProfilePresenter` derives all copy and is unit-tested; views only bind.
- `ISimulatedPortfolioStore` with `AzureTableSimulatedPortfolioStore` (reuses the
  Watchlist storage account, table `ArgusSimulatedPortfolios`, max 10 per user,
  holdings snapshot stored as JSON) and an in-memory fake in tests.
- Pages: `/strategies/profiles`, `/strategies/profiles/{id}` (POST `?handler=Copy`),
  `/strategies/portfolios` (POST `?handler=Delete`); antiforgery via form tag helper.

## Progress

### Phase 1: Profiles, store and pages

#### Automated

- [x] 1.1 Solution builds: `dotnet build .bootstrap-scaffold.csproj`
- [x] 1.2 Presenter, store-contract and page tests pass (291 total)

#### Manual

- [x] 1.3 Check pages against Azurite and `/strategies/ui-states` visually
- [ ] 1.4 Review curated data accuracy and source terms (PRD v2 Open Questions 1, 4)

### Phase 2: Deferred

- [ ] 2.1 FR-005 explained analysis of a simulated portfolio
