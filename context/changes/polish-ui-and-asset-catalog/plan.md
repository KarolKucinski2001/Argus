# Polish UI copy, larger asset catalog and open access Implementation Plan

## Overview

Three user requests (2026-11-04): every user-visible string is Polish, the
analysis picker offers a larger asset list, and every signed-in user can use
every feature.

## Approach

### 1. Polish copy (user-visible only; logs/ProblemDetails stay English)

- Nav and pages: "Dashboard" → "Pulpit", "Strategy Profiles" → "Profile strategii",
  "Przejdź do Dashboardu" → "Przejdź do pulpitu", "Dashboard analizy" → "Pulpit analizy",
  "Opportunity Score" → "Wskaźnik okazji", "Free" → "Bezpłatny".
- Strategy profile names in `StrategyProfileCatalog`: Large Funds → Duże fundusze,
  Company Insiders → Osoby z wewnątrz spółek, Congress Disclosures → Zgłoszenia
  Kongresu USA, Broad Market Benchmark → Szeroki rynek (benchmark).
- Account pages (`SignIn`, `SignOut`, `Error`) translated, including error messages.
- Factor labels: "Momentum 24h/7d", "Trend 30d" → "Impet 24h", "Impet 7 dni",
  "Trend 30 dni"; "score" in visible copy → "wynik". `OpportunityScorer` factor
  labels/explanations translated (shown via API/explanations).
- Tests that assert on English copy are updated in the same step.

### 2. Asset catalog (17 assets)

- Equities/ETFs: AAPL, MSFT, NVDA, GOOGL, AMZN, META, TSLA, JPM, V, KO, SPY, QQQ.
- Crypto: BTC-USD, ETH-USD, SOL-USD, XRP-USD, ADA-USD (CoinGecko ids added).
- Names/monograms in `AssetIdentityCatalog`; new equities use the equity accent,
  new crypto use a new `Crypto` accent token.
- Development provider gets deterministic shapes for new assets.
- Per-request cost is unchanged: analysis still caps selection at 5 assets and
  the dashboard overview uses its fixed list.
- Picker list scrolls inside the sidebar on wide screens so the actions stay visible.

### 3. Access for every signed-in user

- Today: no roles/policies; every authenticated user reaches every page; data is
  scoped per `sub`. The limit is who Entra lets sign in (single workforce tenant).
- Add opt-in `Entra:AllowAnyTenant` that validates the issuer against the token's
  `tid` so the `common` authority works; document portal steps in
  `docs/authentication.md` (portal change is the user's decision).

## Progress

### Phase 1: Implementation

#### Automated

- [x] 1.1 Release tests pass (338): `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj -c Release`
- [x] 1.2 No English visible copy left in Razor views (grep review)

#### Manual

- [ ] 1.3 User reviews Polish wording and the new asset list
- [ ] 1.4 User decides on Entra sign-in audience (portal change)
