# Market data access contract — Plan Brief

> Full plan: `context/changes/market-data-access-contract/plan.md`
> Research: `context/changes/market-data-access-contract/research.md`

## What & Why

Argus needs a trustworthy market-data boundary before it can build asset selection and explained ranking. This plan creates a normalized, provider-neutral contract for a small fixed list of US equities and crypto, with delayed/latest data, 12 months of history, explicit evidence warnings, and public attribution support.

The plan deliberately does not enable a real provider until its free-tier terms explicitly permit public display. This prevents a technically working integration from creating a licensing or trust failure later.

## Starting Point

The current .NET 10 application contains authentication, `/api/me`, and starter weather data, but no market-data route, provider interface, asset model, cache, persistence, or market-data tests. Existing protected-endpoint and `WebApplicationFactory` patterns provide the baseline for the new contract and tests.

## Desired End State

Argus exposes a protected normalized market-data endpoint that can return per-asset current and historical data, freshness/provenance metadata, and explicit warnings without leaking provider payloads. A deterministic fake provider and in-memory cache prove the contract while production provider activation remains gated by documented public-display permission.

## Key Decisions Made

| Decision | Choice | Why | Source |
| --- | --- | --- | --- |
| Asset scope | Small fixed list of US equities and crypto | Keeps the first contract bounded while covering the intended MVP categories | Research / Plan |
| History | 12 months | Removes the five-year depth obstacle and limits request/storage pressure | Plan |
| Freshness | Delayed/latest data acceptable | Matches the MVP educational analysis rather than real-time alerting | Research |
| Licensing | Free tier only, public display allowed | Avoids unapproved paid dependency while preserving the public product goal | Plan |
| Attribution | Visible provider attribution accepted | Makes compliant public display possible if provider terms allow it | Plan |
| Provider strategy | Provider-neutral contract plus compliance gate | No reviewed provider is yet approved for public display | Research / Plan |
| Storage | In-memory cache; no durable market-data storage | Fits the one-week MVP and avoids premature schema/migration work | Plan |
| Partial failures | Preserve usable assets and return per-asset warnings | Makes incomplete evidence explicit without hiding valid results | Research / Plan |
| Testing | Fake HTTP provider and deterministic contract tests | Preserves offline, secret-free verification | Existing test pattern / Plan |

## Scope

**In scope:**

- Normalized asset, quote, history, freshness, warning, and error contracts
- Provider, cache, and options boundaries
- Protected normalized market-data endpoint
- 12-month request bounds, asset-count limits, timeout, cache, and request-budget settings
- In-memory cache with visible freshness state
- Fake provider and deterministic endpoint/contract tests
- Provider compliance/readiness documentation and secret handoff rules

**Out of scope:**

- Selecting or enabling a real provider without official public-display permission
- Asset-selection UI, ranking, scoring, factors, charts, AI summaries, and full analysis workflow
- Durable data storage, migrations, background refresh, fallback providers, alerts, or watchlists

## Architecture / Approach

The application calls a normalized `MarketData` boundary. A provider adapter implements that boundary later, while an in-memory cache sits between the endpoint and provider. The endpoint returns per-asset results and warnings; configuration and documentation enforce the provider approval gate. Tests substitute a fake HTTP/provider implementation and never require network access or secrets.

## Phases at a Glance

| Phase | What it delivers | Key risk |
| --- | --- | --- |
| 1. Contract and compliance boundary | Normalized types, interfaces, options, and readiness checklist | Contract may encode provider assumptions |
| 2. Endpoint and cache | Protected normalized route with partial results and freshness-aware cache | Incorrect failure semantics could hide evidence gaps |
| 3. Tests and readiness gate | Offline contract coverage and deployment/provider handoff rules | No provider can be enabled until terms are verified |

**Prerequisites:** Existing authenticated application and test project; no provider credentials or database required.  
**Estimated effort:** ~2–3 sessions across 3 phases for a foundation-level implementation.

## Open Risks & Assumptions

- A compliant free provider may still be unavailable; the endpoint must represent provider-not-approved/configuration failure explicitly.
- In-memory cache is instance-local on Azure App Service; cross-instance consistency is intentionally deferred.
- The fixed asset list and exact provider symbols will be finalized by the later selection/provider change, not embedded in this foundation.
- Product requirements contain a mismatch between AC-04’s unconditional “at least two factors” and FR-012’s insufficient-evidence exception; this plan preserves the latter for data-contract warnings.

## Success Criteria (Summary)

- The protected endpoint returns normalized, freshness-aware per-asset data and explicit evidence warnings.
- Deterministic tests cover successful, partial, stale, invalid, quota, timeout, outage, and provider-not-approved states without network or secrets.
- Provider activation remains blocked until official terms verify public display with attribution.

