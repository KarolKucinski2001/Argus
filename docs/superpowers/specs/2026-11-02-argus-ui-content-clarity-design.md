# Argus UI and Content Clarity

## Goal

Make the signed-in experience clearer and reduce ambiguity around data sources,
strategy profiles, and watchlist terminology without changing product scope or
data behavior.

## User-visible changes

### Signed-in landing action

The public landing page keeps the authentication call to action for anonymous
visitors. When the current user is authenticated, that action becomes
“Przejdź do Dashboardu” and links to `/dashboard`.

### Data sources section

Add a public landing-page section above “Ważne ograniczenie” titled “Źródła
danych”. It explains, in plain Polish:

- market and historical data are normalized before analysis;
- strategy profiles use public filings or public strategy descriptions where
  available;
- some profile compositions are rounded educational illustrations rather than
  exact filings;
- AI summaries use the evidence displayed by Argus;
- production provider approval and public-display licensing are separate from
  illustrative or development data.

The section must not claim a provider is approved for public production display
when the repository keeps `PublicDisplayApproved` disabled.

### Dashboard action density

Remove duplicate “Przejdź do analizy” actions from Dashboard content cards. Keep
the existing “Zobacz wszystkie” navigation for the watchlist section and retain
the primary Watchlist-page action for adding an asset.

### Strategy-profile explanation

The strategy profile list and detail pages must make “przykładowe, przybliżone
dane” understandable: values are rounded and normalized to the displayed
positions, are educational examples, and do not reproduce an official filing or
constitute advice. Existing explicit labels for the S&P 500 illustration and
All Weather-style example remain.

### Watchlist terminology

Rename the Watchlist section heading “Najwięksi ruszyciele” to “Największe
zmiany”. The existing “Zyskujące” and “Tracące” groups remain unchanged.

## Boundaries

- No new market-data provider or licensing decision.
- No change to authentication, routes, persistence, or analysis calculations.
- No removal of the main Watchlist “Dodaj aktywo” action.
- No use of CSS-only hiding for semantic actions.

## Validation

- Add or update focused Razor contract tests for the authenticated landing CTA,
  source-section copy, Dashboard action removal, profile explanation, and
  Watchlist heading.
- Run the focused authentication/UI contract test project in Release.
- Manually inspect the affected views in authenticated and anonymous states,
  including `/watchlist` and `/strategies/profiles`.
