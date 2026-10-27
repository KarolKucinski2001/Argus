---
change_id: production-market-data
status: implemented
created: 2026-10-27
updated: 2026-10-27
---

# production-market-data (roadmap S-06)

Free-tier live market-data adapters behind `IMarketDataProvider`: Finnhub quotes
and Twelve Data daily history for equities (Finnhub candles are premium-only),
CoinGecko Demo for crypto, routed by `MarketAssetClass` and selected through
`MarketDataOptions`. The `PublicDisplayApproved` gate is untouched and never set
to true, so the app still uses the development provider. Licensing for public
display is unconfirmed; see `docs/market-data.md`.
