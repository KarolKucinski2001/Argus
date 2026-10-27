# Plan: production-market-data

## Progress

- [x] Verify free-tier history routes (Finnhub candles premium; Twelve Data and CoinGecko Demo OK)
- [x] Equity and crypto adapters with header auth, error mapping and attribution
- [x] `RoutingMarketDataProvider` and config-driven DI registration (gate preserved)
- [x] Deterministic tests with fake `HttpMessageHandler`
- [x] `docs/market-data.md` (enablement, attribution, pending licensing)
- [ ] Confirm public-display licensing in writing, then enable via secrets and config
