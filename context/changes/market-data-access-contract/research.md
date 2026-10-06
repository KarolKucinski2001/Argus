---
date: 2026-10-06T20:31:30+02:00
researcher: GitHub Copilot CLI
git_commit: ed3dca8025f688064dde7be0abd7468af8bde092
branch: master
repository: Argus
topic: "Determine the MVP market-data source and access limits for Argus"
tags: [research, codebase, market-data, provider-contract, roadmap]
status: partial
last_updated: 2026-10-06
last_updated_by: GitHub Copilot CLI
last_updated_note: "Confirmed a 12-month historical-data requirement and retained the public-display licensing check."
---

# Research: MVP market-data source and access limits

**Date**: 2026-10-06T20:31:30+02:00  
**Researcher**: GitHub Copilot CLI  
**Git Commit**: `ed3dca8025f688064dde7be0abd7468af8bde092`  
**Branch**: `master`  
**Repository**: Argus

## Research Question

Which market-data source and access limits satisfy the MVP's current and historical evidence contract without committing the roadmap to a provider-specific implementation?

## Summary

The roadmap marks F-02, `market-data-access-contract`, as blocked until the market-data source and acceptable access limits are chosen (`context/foundation/roadmap.md:85-95`). The MVP requires data for user-selected assets, including current price, 24-hour, 7-day, and 30-day performance, historical chart data, and evidence suitable for explained ranking (`context/foundation/prd.md:50-62`, `context/foundation/prd.md:72-89`).

The inspected application has no market-data integration, provider abstraction, asset identifier model, cache, quota tracking, persistence, or market-data error contract (`Program.cs:73-105`; `.bootstrap-scaffold.csproj:9-12`; `context/foundation/roadmap.md:65-67`). The implementation should therefore begin with a provider-neutral application contract and isolate provider symbols, payloads, credentials, quotas, and retry behavior behind an adapter. This is an architectural implication of the inspected baseline, not an existing repository decision.

The user has narrowed the target to a small fixed list of US equities and crypto, delayed/latest data, 12 months of history, free tier only, and direct public display in Argus. Visible provider attribution is acceptable. The shorter history and fixed asset set reduce request volume and remove the previously identified five-year-depth concern, but they do not by themselves resolve display rights. Twelve Data still documents its free Basic plan as internal non-display usage, and the checked Alpha Vantage documentation directs commercial users to sales. No checked official provider page currently verifies all revised requirements together. The research remains partial pending provider-specific terms confirmation.

## Detailed Findings

### Product evidence contract

- The first milestone is an explained market analysis for selected assets, not full-market analysis (`context/foundation/roadmap.md:23-31`; `context/foundation/prd.md:120-126`).
- The result must include current price and 24-hour, 7-day, and 30-day performance metrics (`context/foundation/prd.md:57-62`; `context/foundation/prd.md:80-82`).
- The result must include a historical chart (`context/foundation/prd.md:80-82`).
- The product may show at least two contributing factors when sufficient data is available and must state when evidence is insufficient (`context/foundation/prd.md:84-89`). The acceptance criterion wording is stricter and says “at least two factors” without repeating that qualification (`context/foundation/prd.md:54-57`); this is an existing specification inconsistency to resolve during planning.
- AI explanations must be grounded in the data and factors displayed by Argus and acknowledge uncertainty or missing data (`context/foundation/prd.md:86-89`). The market-data contract must therefore expose provenance, freshness, and data-quality information to the analysis layer.

### Current implementation surface

- `Program.cs:73-96` maps `/api/me` and the starter `/weatherforecast`; no market-data route or service is present in the inspected startup path.
- `.bootstrap-scaffold.csproj:9-12` contains authentication/OpenAPI dependencies but no cache, resilience, database, or market-data client dependency.
- `Program.cs:1-105` contains no asset model, quote DTO, historical-series DTO, provider interface, `HttpClient`, cache registration, rate limiter, or retry policy.
- Existing API failure handling establishes a `401` Problem Details contract for unauthenticated requests (`Program.cs:28-48`; `tests/Argus.Authentication.Tests/AuthenticationContractTests.cs:15-31`), but it does not define market-data-specific categories such as unsupported asset, timeout, quota exhaustion, incomplete history, stale data, or upstream outage.

### Deployment and configuration constraints

- The selected stack is .NET with Azure App Service and GitHub Actions (`context/foundation/tech-stack.md:1-14`).
- The deployment workflow builds and publishes the scaffold application and does not provision market-data settings, persistence, migrations, or provider validation (`.github/workflows/deploy.yml:1-48`).
- `appsettings.json:1-18` and `appsettings.Development.json:1-15` contain no provider URL, credential reference, timeout, cache duration, or quota configuration.
- The infrastructure guidance recommends environment/App Service settings or Key Vault for future external-data secrets and calls out timeouts, bounded retries, and caching as future concerns (`context/foundation/infrastructure.md:100-109`). These are recommendations, not implemented behavior.

### Current provider checks

- **Twelve Data** documents a free Basic plan with 8 API credits per minute and 800 per day, real-time US equities/ETFs and crypto market data, and “internal non-display usage” (`https://twelvedata.com/pricing`). Its documentation exposes time-series, latest-price, asset-catalog, cryptocurrency-pair, and earliest-timestamp endpoints (`https://twelvedata.com/docs`). The checked pages do not establish that the free plan permits public display in Argus or guarantee five years of history for every selected instrument.
- **Alpha Vantage** documents daily, weekly, and monthly equity time series with 25+ years of historical depth and separate cryptocurrency exchange-rate, daily, weekly, and monthly endpoints (`https://www.alphavantage.co/documentation/`). The same documentation labels realtime, 15-minute delayed, and historical intraday equity data as premium and says commercial use requires contacting sales (`https://www.alphavantage.co/documentation/`). Its free access therefore does not satisfy the confirmed free-only/public-display constraint without a separate licensing confirmation.

These checks are provider documentation observations, not legal conclusions. They are sufficient to identify a decision blocker but not to approve either provider for a public MVP.

### Additional bounded provider check

- Marketstack's official pricing FAQ documents a free plan limited to 100 requests per month, end-of-day data, and up to 12 months of history (`https://marketstack.com/pricing`). The checked page does not document crypto coverage; its 12-month history limit is now sufficient for the revised history requirement.
- EODHD's official pricing FAQ documents 20 free API calls per day and says some data types are unavailable on the free package (`https://eodhd.com/pricing`). The checked page does not establish that the free package supplies the required five-year mixed-asset contract or permits public display.
- StockData.org's checked official pricing page describes a paid Standard plan with 7+ years of intraday and end-of-day data but does not document a qualifying free public-display plan (`https://www.stockdata.org/pricing`).

Within this bounded check of six providers, no free tier was verified against the earlier combined constraints of US equities and crypto, at least five years of history, and permission to display the data in a public Argus web app. The history requirement has since been reduced to 12 months, but the display-permission gap remains. This is not an exhaustive market-wide provider survey.

The revised 12-month/fixed-list target removes the five-year and broad-universe obstacles, but the display-rights requirement remains the deciding constraint. A provider may be technically adequate for a small request budget while still prohibiting public display under its free plan; endpoint capability is not treated as licensing approval.

### Provider-neutral contract boundary

The inspected baseline supports introducing an application-facing contract with these responsibilities:

- `AssetReference`: Argus identifier, display name, asset class, and provider/exchange mapping.
- `MarketQuote`: price, currency, observed/source timestamps, freshness, and data-quality status.
- `HistoricalPricePoint`: timestamp, OHLC values where available, volume where available, and adjusted/unadjusted semantics.
- `MarketDataResult`: requested range/granularity, current quote, historical points, source metadata, freshness, and warnings.
- `MarketDataError`: stable category, retryability, safe user-facing message, and upstream quota/status details where safe.

This boundary keeps provider-specific symbols, HTTP payloads, credentials, quotas, and retry rules out of endpoint DTOs. It is a recommended design derived from the absence of an existing integration (`Program.cs:73-105`; `.bootstrap-scaffold.csproj:9-12`), not a pre-existing project convention.

## Code References

- `context/foundation/roadmap.md:85-95` — F-02 outcome, blocker, and unresolved source/access-limit question.
- `context/foundation/roadmap.md:117-123` — S-02 dependency on F-02 and the same unresolved market-data question.
- `context/foundation/prd.md:50-62` — user story and acceptance criteria for ranking, metrics, and historical data.
- `context/foundation/prd.md:72-89` — FR-004, FR-009 through FR-013, and insufficient-evidence behavior.
- `Program.cs:73-105` — current API route and model surface.
- `.bootstrap-scaffold.csproj:9-12` — current package baseline.
- `.github/workflows/deploy.yml:1-48` — current deployment workflow.
- `appsettings.json:1-18` — current application configuration.
- `context/foundation/infrastructure.md:100-109` — future external-data operational recommendations.

## Architecture Insights

The MVP should separate the analysis-facing market-data contract from the provider adapter because the inspected codebase has no provider-specific commitment and the roadmap explicitly requires avoiding provider lock-in (`context/foundation/roadmap.md:85-89`). The contract should carry timestamps, freshness, warnings, and data-quality state so the ranking and explanation layers can distinguish usable evidence from stale, incomplete, or unavailable data.

The access-limit decision must cover both provider limits and Argus limits. The repository has unknown QPS and data volume (`context/foundation/prd.md:7-15`), so a provider quota cannot be selected from codebase evidence alone. The MVP needs an explicit request budget, timeout/retry policy, cache duration, and behavior when the provider cannot supply enough evidence before implementation planning can be complete.

## Historical Context (from prior changes)

- `context/changes/market-data-access-contract/change.md:1-11` contains only the new change identity and no prior decision.
- `context/foundation/roadmap.md:151-152` links the unresolved decision to GitHub issue `#1`, “Choose the MVP market-data source and access limits”; the issue remains open without a decision record according to the inspected issue state.
- The inspected archived authentication changes and deployment/authentication documentation contain no market-data provider or access-limit decision.

## Related Research

No related market-data research artifact was found under the inspected `context/changes/` or `context/archive/` paths.

## Open Questions

1. Which exchanges, currencies, and identifier format are in the MVP's fixed US-equity and crypto list?
2. What request volume, concurrency, and per-user/global quota must the MVP support?
3. The user confirmed that free-only remains required, visible provider attribution is acceptable, and no provider is approved until a compliant free tier is found. Which provider's official terms explicitly permit this public educational display?
4. Is transient provider/cache storage sufficient, or must raw observations, asset mappings, or analysis snapshots be persisted?
5. What user-visible behavior is required for stale, incomplete, rate-limited, or unavailable data, and is a fallback provider required?
