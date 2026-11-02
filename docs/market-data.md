# Market-data contract and compliance boundary

Argus consumes market data through the provider-neutral contracts in
`MarketData/`. Provider symbols, payloads, credentials, quotas, and retry
details belong behind an adapter and must not appear in an application
endpoint response.

## MVP contract

The first market-data integration is limited to a configured allowlist of US
equities and crypto assets. Requests are bounded by the configured maximum
asset count and a **12-month** historical window. Each normalized per-asset
result may contain:

- an Argus asset identifier and asset class;
- a current value and historical points with currency and source timestamps;
- the requested date range and granularity;
- provider attribution and delayed/latest-data metadata;
- freshness and data-quality state; and
- stable warning or error categories with retryability.

Missing, stale, incomplete, delayed, quota-limited, or unavailable evidence
must remain visible to downstream ranking and explanation code. A partial
request must not discard usable results for other assets.

Performance periods such as 24-hour, 7-day, and 30-day change are intentionally
not part of this access contract. The future analysis layer derives them from
the normalized current quote and historical points so provider access remains
focused on transporting evidence rather than product-specific calculations.

## Provider approval gate

No provider is enabled for public Argus traffic until the following checklist
is complete for the exact plan/tier and asset allowlist:

- [ ] The fixed US-equity and crypto allowlist is covered.
- [ ] Delayed or latest-data behavior is documented and suitable for display.
- [ ] At least 12 months of history is available at the required granularity.
- [ ] Quota, rate-limit, timeout, and retry behavior is documented.
- [ ] Required provider attribution is available in the normalized source metadata.
- [ ] Official terms explicitly permit public display in Argus.
- [ ] Missing, stale, incomplete, and unavailable data have documented semantics.
- [ ] Credentials are supplied only through environment, App Service settings,
      Key Vault, or another approved secret store.

The `PublicDisplayApproved` option is deliberately false by default. It is a
configuration gate, not a legal determination: it may be enabled only after
the official terms and the checklist above have been reviewed and recorded.
The current research does not approve a provider; the free tiers reviewed so
far do not establish the required public-display permission.

## Release-readiness record

A provider adapter must not be enabled in production until a release record
links the exact provider plan and asset allowlist to evidence for every item
below:

- Official terms explicitly permit Argus to publicly display the returned
  values, historical data, attribution, and delayed/latest-data state.
- The fixed US-equity and crypto allowlist, required endpoint coverage, and
  twelve months of history at the requested granularity have been exercised.
- Quota and rate-limit ceilings, request timeout behavior, freshness guarantees,
  retryability, and upstream error mappings have been verified from official
  documentation and deterministic adapter tests.
- Normalized responses contain the required attribution and source timestamps,
  and missing, stale, partial, delayed, quota-limited, and unavailable evidence
  remains visible to callers.
- Provider credentials are supplied only through Azure App Service settings,
  Key Vault, or another approved secret store; no credential is stored in this
  repository.

Until that record is complete and reviewed, `PublicDisplayApproved` must remain
false. The application must report the explicit `ProviderNotApproved` or
configuration-unavailable state rather than presenting a provider response as
fresh market evidence.

### Provider replies

- **Twelve Data (2026-11-02, support reply):** the free Basic plan is personal,
  non-display only and may not be used to show prices, charts, or analytics to other
  users. Display in a web or mobile app requires a business plan (Venture or higher),
  with attribution to Twelve Data. Caching rules are not specified.
  Decision: live providers are used only locally by the project owner through user
  secrets (`PublicDisplayApproved=true` on the developer machine). They must not be
  enabled in any deployed environment, which keeps the illustrative provider or the
  unavailable state.

Development and Testing environments register a deterministic fake provider
that returns clearly labelled illustrative data for local protected-flow
testing. The fake provider has no credentials or network calls and is never
registered outside those environments. Its results do not constitute provider
approval or permission for public display. Production and other environments
continue to use the unavailable-provider boundary until an explicitly approved
provider is registered; production must not present fake results.

## S-03 handoff

S-03 product metrics, scoring, chart projection, and grounded AI summaries must
derive only from the normalized `MarketDataResponse` evidence already produced
by this provider-neutral contract. Only evidence that is normalized and
displayed by the result page may enter an AI summary request; raw provider
payloads, provider identifiers, credentials, and hidden fields are excluded.
The evidence boundary must preserve source metadata, freshness, quality, stable
warnings, and per-asset errors all the way into ranking, summary generation,
and result rendering, so missing or partial evidence remains distinguishable
from a zero score.

S-03 must not add a provider-specific request or response contract to implement
ranking, factors, charts, or grounded AI summaries. Review the handoff by
confirming that:

- 24-hour, 7-day, and 30-day metrics are calculated from normalized current
  quotes and historical points;
- opportunity scores and factors preserve insufficient-evidence states instead
  of coercing missing evidence to zero;
- chart data is projected from normalized historical points without introducing
  provider fields; and
- grounded summary generation is invoked once per asset only when at least two
  factors are available, and unavailable, timeout, cancellation, or malformed
  output preserves the calculated analysis; and
- result views carry bounded warning/error and summary states forward without
  rendering raw provider identifiers or messages.

## Operational boundary

`MarketDataOptions` keeps the 12-month window, selected-asset limit, timeout,
cache duration, and application request budget explicit. The budget is enforced
by a process-local rolling one-minute window; Azure App Service instances do
not share this counter, so distributed enforcement remains future work.
Provider API keys are
represented only by a configuration-key name (`ProviderApiKeyConfigurationKey`)
and must never be committed to `appsettings*.json`, source code, tests, or
workflow files.

Argus displays educational analysis, not financial advice. Provider
activation decisions, AI Copilot/chat, and follow-up questions are outside S-03.
Provider attribution and evidence-quality warnings must remain available to the
analysis UI and summary layers.

## Live free-tier adapters (S-06)

Adapters live in `MarketData/Live/` and are routed by `RoutingMarketDataProvider`
per `MarketAssetClass`. `AddMarketDataProvider` (`MarketDataProviderRegistration`)
selects them only when `MarketData:PublicDisplayApproved=true` **and**
`MarketData:ProviderId=argus-live-free`; otherwise Development/Testing keep the
illustrative provider and other environments keep the unavailable provider.
**No file in the repository sets `PublicDisplayApproved=true`**, and
`MarketDataOptions` rejects a `ProviderId` while approval is false.

| Asset class | Quote | Daily history |
| --- | --- | --- |
| Equity | Finnhub `GET /quote` (free) | Twelve Data `GET /time_series` (Basic, free) |
| Crypto | CoinGecko Demo `GET /simple/price` | CoinGecko Demo `GET /coins/{id}/market_chart/range` (daily, 00:00 UTC) |

Why Twelve Data: Finnhub's `/stock/candle` and `/crypto/candle` are marked
"Premium Access Required" in its official API docs, so the free tier cannot serve
12 months of daily candles. Stooq's CSV endpoint now sits behind a JavaScript
challenge and is unusable from a server. CoinGecko Basic/Demo history covers the
past 2 years, which includes the 12-month window.

Keys are sent in headers (`X-Finnhub-Token`, `Authorization: apikey ...`,
`x-cg-demo-api-key`), never in URLs or logs. Failures map to
`MarketDataErrorCategory`: 401/403 authentication, 429 quota, 400/404 unsupported
asset, 5xx upstream, timeout, malformed JSON invalid response. A failing quote
degrades the asset to `Incomplete` with a warning; a failing history call yields an
error result for that asset only.

### Enabling (only after licensing is confirmed)

1. Obtain free keys: Finnhub, Twelve Data (Basic), and optionally CoinGecko Demo.
   Without a CoinGecko key, crypto assets are served by Twelve Data (`BTC-USD` is requested as `BTC/USD`).
2. Store them outside the repository, e.g.
   `dotnet user-secrets set "MarketData:Live:FinnhubApiKey" "<key>" --project .bootstrap-scaffold.csproj`
   (likewise `MarketData:Live:TwelveDataApiKey`, `MarketData:Live:CoinGeckoApiKey`), or
   App Service settings such as `MarketData__Live__FinnhubApiKey`.
3. Set `MarketData:ProviderId=argus-live-free` and `MarketData:PublicDisplayApproved=true`
   in environment-specific configuration, not in committed defaults.
4. Add new assets to `MarketData:AllowedAssets`; crypto assets also need a CoinGecko id
   in `MarketData:Live:CoinGeckoIds` (defaults: BTC-USD, ETH-USD).

### Attribution text

Show wherever the data is displayed (also returned in `MarketDataSource.Attribution`):

- Equities: "Quotes: Finnhub (finnhub.io). Daily history: Twelve Data (twelvedata.com)."
- Crypto: "Powered by CoinGecko API (coingecko.com)." with a link to CoinGecko.

### Licensing status: blocked

Public display is **not** confirmed in writing. Confirmation emails to Finnhub and
CoinGecko support are pending. Twelve Data's Basic plan is described as "internal
non-display usage", so it needs the same confirmation (or a paid plan). Until then
keep the gate closed. Free-tier rate limits (Twelve Data 8/min and 800/day,
CoinGecko Demo about 30/min, Finnhub 60/min) apply on top of the application
budget; the cache absorbs most load. Equity quote delay is not guaranteed by the
free plans, so the source is marked delayed.
