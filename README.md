# Argus

Argus is an educational investment-analysis web app built with ASP.NET Core (.NET 10, Razor Pages, Polish UI).
Signed-in users pick assets, see 12 months of market history, get ranked results with explaining factors,
keep a personal watchlist, and browse curated public strategy profiles that they can copy into private
simulated portfolios. Argus is educational only and never gives investment advice.

## Features

- **Access control:** Microsoft Entra sign-in; all product pages require login (`docs/authentication.md`).
- **Analysis (`/analysis`):** asset picker, history chart, ranking with explaining factors.
- **Watchlist (`/watchlist`) and dashboard (`/dashboard`):** create, read and delete saved assets, with monitoring insights.
- **Strategies (`/strategies`):** curated profiles, copy into a simulated portfolio, list and delete portfolios.

## Run locally

Prerequisites: .NET 10 SDK, Node.js (for the Azurite storage emulator).

```powershell
npx -y azurite --silent --location $env:USERPROFILE\.azurite-data --skipApiVersionCheck
dotnet run --project .bootstrap-scaffold.csproj
```

Azurite must be running for the watchlist and simulated portfolios. Configure Entra values as described in
`docs/authentication.md`. Without further configuration the app uses a clearly labelled illustrative
market-data provider.

## Tests

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj
```

The suite covers authentication contracts, watchlist and portfolio flows, presenters and market-data adapters.

## Market data and licensing

Live data adapters exist (Finnhub quotes, Twelve Data history and crypto) but are off by default. The free
plans do not allow displaying data to other users, so live mode is for private local use through user
secrets only. See `docs/market-data.md`.

## Project context

Product and process documents live in `context/`: `foundation/prd.md`, `prd-v2.md`, `roadmap.md`,
`tech-stack.md`, `infrastructure.md`, and per-change plans under `context/changes/` and `context/archive/`.
Contributor and agent guidance is in `AGENTS.md`. CI/CD is `.github/workflows/deploy.yml` (Azure App Service).
