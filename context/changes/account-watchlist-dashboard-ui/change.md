---
change_id: account-watchlist-dashboard-ui
title: Account and watchlist dashboard UI per mockup
status: implementing
created: 2026-10-25
updated: 2026-10-25
archived_at: null
---

## Notes

Restyle the /account and /watchlist views to match `mockup.png` in this folder:
dark app shell with a left sidebar (Dashboard, Analiza, Obserwowane, Konto), top
search bar and user chip, Account page (profile, account overview, virtual
portfolio, allocation, key metrics, recent activity, quick actions) and
Watchlist page (list tabs, table with price and 24h/7d/1M change, sparkline,
performance chart, top movers, disclaimer).

Decisions made by the user: full mockup scope, with placeholder elements where
no data exists (clearly labelled as example data, never shown as real results);
sidebar replaces the top navigation; delivered as a separate change after
save-assets-to-watchlist. Wording stays descriptive, never advice. Keep tokens
in wwwroot/css/site.css, no literal colors in views.
