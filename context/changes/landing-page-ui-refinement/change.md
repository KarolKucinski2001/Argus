---
change_id: landing-page-ui-refinement
title: Refine landing page UI
status: impl_reviewed
created: 2026-10-12
updated: 2026-10-14
archived_at: null
---

## Notes

Landing page UI audit targets:

- Resolve responsive layout issues: hero and following sections should scale without clipping on desktop or mobile.
- Add the existing Argus logo from `C:\Users\KUCINK01\Downloads\argus_logo_assets.zip` instead of rendering only the text `ARGUS`.
- Improve hero hierarchy so the primary product message leads and the Opportunity Score preview supports it.
- Add visual proof of product value to the process section instead of relying only on text cards.
- Verify explicit mobile states for the preview and other landing-page components.

The change covers one view, `Pages/Index.cshtml`, with shared visual values in
`wwwroot/css/site.css`. Preserve the educational-not-investment-advice boundary.
