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
  — profile wording now explicitly marks unverified S&P 500 data as illustrative
  and All Weather as an unofficial educational style; public-display terms remain
  unapproved.

### Phase 2: FR-005 explained analysis of a simulated portfolio

Approach (decided autonomously; composition-based because live market data covers
only AAPL, MSFT, NVDA, BTC-USD, ETH-USD and the production gate stays closed):

- `SimulatedPortfolioAnalysisPresenter` (unit-tested) derives a descriptive
  concentration label from the effective number of positions (1 / Σ wᵢ²:
  < 4 high, < 7 moderate, otherwise spread), metrics (largest position, top-3
  share, effective positions, sector and asset-class counts), sector/class
  shares, reused composition factors, and coverage of assets Argus can analyze
  (`MarketDataOptions.AllowedAssets`, capped at `MaximumSelectedAssets`).
- Page `/strategies/portfolios/{id}/analysis` loads only the signed-in user's
  portfolio via `ListAsync`; unknown or foreign ids return 404; storage failure
  renders an error card. Covered assets post to the existing `/analysis?handler=Analyze`.
- Every view keeps the simulation disclaimer, as-of date and no advice/forecast wording.

#### Automated

- [x] 2.1 Presenter tests: effective positions, labels, coverage, banned wording
- [x] 2.2 Page tests: auth, own portfolio, foreign/unknown 404, storage failure
- [x] 2.3 Full Release test suite passes (330)

#### Manual

- [ ] 2.4 Visual check of `/strategies/portfolios/{id}/analysis` against Azurite
