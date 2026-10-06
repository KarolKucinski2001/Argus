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

## Operational boundary

`MarketDataOptions` keeps the 12-month window, selected-asset limit, timeout,
cache duration, and application request budget explicit. Provider API keys are
represented only by a configuration-key name (`ProviderApiKeyConfigurationKey`)
and must never be committed to `appsettings*.json`, source code, tests, or
workflow files.

Argus displays educational analysis, not financial advice. Provider
attribution and evidence-quality warnings must remain available to the future
UI and explanation layers.
