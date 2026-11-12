---
date: 2026-11-02T17:25:00+01:00
researcher: GitHub Copilot CLI
git_commit: 03024a857b87b141059cb1429552b0044f359548
branch: master
repository: Argus
topic: "Release evidence for curated strategy profiles"
tags: [research, strategy-profiles, curated-data, source-terms]
status: partial
last_updated: 2026-11-02
last_updated_by: GitHub Copilot CLI
---

# Research: Release evidence for curated strategy profiles

**Date**: 2026-11-02T17:25:00+01:00  
**Researcher**: GitHub Copilot CLI  
**Git Commit**: `03024a857b87b141059cb1429552b0044f359548`  
**Branch**: `master`  
**Repository**: Argus

## Research Question

Can the three curated strategy profiles in S-07 be released as approximate
educational examples, with defensible source labels and dates?

## Summary

The implementation deliberately presents rounded, approximate compositions rather
than exact filings data (`Strategies/CuratedProfileCatalog.cs:20-32`). The
Berkshire-style profile has a plausible SEC 13F basis: public search results
identify a Berkshire Hathaway Q2 2025 13F filed on 2025-08-14 for holdings as of
2025-06-30, matching the profile's as-of date and several displayed issuers.
The direct SEC archive request returned HTTP 403 in this environment, so the
filing bytes were not independently inspected here.

The S&P 500 and All Weather profiles have a reasonable conceptual basis, but this
research did not obtain an authoritative, directly inspectable source for the
exact displayed date and rounded weights. S-07 therefore remains partial: the
code and tests are complete, but the manual source-and-terms review in the plan
is not complete.

## Detailed Findings

### Local implementation boundary

- The catalog contains three user-facing curated profiles with an as-of date and
  source label (`Strategies/CuratedProfileCatalog.cs:34-85`).
- The catalog explicitly says weights are rounded, approximate, normalized to
  listed positions, and not exact filings data (`Strategies/CuratedProfileCatalog.cs:20-23`).
- The UI disclaimer says the result is an educational simulation and not
  investment advice (`Strategies/CuratedProfileCatalog.cs:26-32`).
- The plan marks automated tests and visual checks complete, while the
  curated-data/source-terms review remains unchecked
  (`context/changes/strategy-profiles-simulation/plan.md:23-35`).

### Berkshire-style profile

- The profile displays Berkshire-style holdings including AAPL, AXP, BAC, KO,
  CVX, OXY, and KHC, with an as-of date of 2025-06-30 and an SEC 13F source
  label (`Strategies/CuratedProfileCatalog.cs:36-52`).
- Public search results identify Berkshire Hathaway's Q2 2025 Form 13F-HR as
  filed on 2025-08-14 for holdings as of 2025-06-30, and list the displayed
  issuers among the reported positions. The search result links to the SEC EDGAR
  entity page and a filing accession, but the direct SEC archive request was
  blocked with HTTP 403 during this research.
- Release wording should remain “Berkshire-style” and “approximate”; it should
  not claim that the rounded weights reproduce the official filing.

### S&P 500 profile

- The profile displays eight large issuers, rounded weights totalling 100%, with
  an as-of date of 2025-06-30 and a generic “public index-provider data” source
  label (`Strategies/CuratedProfileCatalog.cs:53-70`).
- A web search result from a third-party index tracker gives materially different
  approximate weights from the displayed values and does not establish an
  authoritative 2025-06-30 source. The official S&P Dow Jones Indices page
  returned HTTP 403 in this environment, so the exact displayed composition was
  not verified.
- The source label is too broad for release evidence unless a dated official
  factsheet, licensed index data source, or an explicit “illustrative example”
  decision is recorded.

### All Weather profile

- The profile displays a 30/40/15/7.5/7.5 allocation using representative ETFs,
  with an as-of date of 2025-06-30 and a public-strategy source label
  (`Strategies/CuratedProfileCatalog.cs:71-85`).
- Public search results describe the commonly published retail approximation as
  30% stocks, 40% long-term Treasury bonds, 15% intermediate Treasury bonds,
  7.5% gold, and 7.5% commodities. The sources returned were secondary
  explanations, not a directly inspectable Bridgewater filing or official
  publication.
- The profile should be described as a public educational approximation of an
  “All Weather-style” allocation, not as Bridgewater's exact portfolio.

## Code References

- `Strategies/CuratedProfileCatalog.cs:20-32` - Approximation, disclaimer, and
  data-note boundaries.
- `Strategies/CuratedProfileCatalog.cs:36-85` - The three curated profiles,
  dates, source labels, and rounded holdings.
- `context/changes/strategy-profiles-simulation/plan.md:23-35` - Completed
  automated/manual checks and the remaining unchecked source review.

## Architecture Insights

The application separates curated profile data from presentation and simulation
storage. `StrategyProfilePresenter` derives display factors from the holdings,
while the page tests verify authentication, disclaimers, as-of dates, copying,
ownership, deletion, and capacity behavior. This supports closing the
implementation portion of S-07 without introducing provider-specific market-data
claims.

## Historical Context (from prior changes)

- `context/foundation/roadmap.md:115-119` - S-07 is marked in-progress and
  identifies source terms and curated-profile ownership as release blockers.
- `context/foundation/prd-v2.md:43-76` - PRD v2 requires descriptive factors,
  simulation labeling, and educational wording; it excludes real-money trading,
  live filing pipelines, advice, forecasts, and sharing.

## Open Questions

1. Can the project obtain and retain a dated authoritative source record for the
   S&P 500 top holdings shown in the profile?
2. Is the All Weather profile explicitly approved as a secondary-source,
   educational approximation, or should its label be changed to avoid implying
   an official Bridgewater composition?
3. Has a human reviewed the applicable terms for displaying the referenced
   public data and representative ETF names? The user confirmed that this review
   is not approved, so S-07 should not be marked release-complete.

## Continuation: release wording decision

- The user selected an explicitly illustrative label for the S&P 500 profile.
- The user selected “All Weather-style” educational wording rather than an
  official Bridgewater attribution.
- The user selected demo-only treatment until public-display terms are reviewed.
- The catalog was updated to preserve those boundaries
  (`Strategies/CuratedProfileCatalog.cs:53-85`); the source-review checklist
  remains open in the implementation plan.
