# Explained Market Ranking — Design Specification

**Date:** 2026-10-16  
**Roadmap slice:** S-03  
**Change ID:** explained-market-ranking

## Goal

Extend the protected `/analysis` flow so an authenticated user receives a
transparent, explained ranking for the selected assets. The first version
uses the existing provider-neutral `MarketDataResponse`, a deterministic local
scoring model, server-rendered Razor cards, and one provider-neutral AI summary
per asset grounded only in the normalized evidence shown on the page. Provider
activation remains outside this slice.

## Decisions

- Use an application-level analysis service rather than a provider-specific
  contract or a self-HTTP call.
- Keep the existing `/analysis` route and render results in the same protected
  Razor Page response after POST.
- Use a `0–100` Opportunity Score with fixed, documented weights:
  - 24-hour momentum: 25%
  - 7-day momentum: 35%
  - 30-day trend: 40%
- Show the score contributors and their numeric contribution so the score is
  inspectable rather than presented as a prediction.
- Request one grounded AI summary per asset only when at least two factors are
  available. The AI boundary accepts normalized metrics, factors, freshness,
  quality, warnings, and score; it never accepts raw provider payloads,
  provider identifiers, credentials, or hidden evidence.
- Treat missing evidence, unavailable AI, timeout, cancellation, and malformed
  output as bounded states. Preserve the score, factors, metrics, chart, and
  ranking whenever those were calculated.
- Sort cards by score descending; preserve the submitted asset order for ties.
- Keep partial results per asset. A usable result remains visible when another
  selected asset has an error.
- Treat missing evidence as insufficient evidence, never as zero performance or
  an artificial zero score.
- Keep the educational-not-investment-advice boundary visible on the result
  surface.

## Architecture

The change adds a focused analysis layer:

1. `AnalysisService` consumes the normalized market-data response and produces
   an analysis response for each selected asset.
2. `PerformanceCalculator` derives 24-hour, 7-day, and 30-day performance from
   the normalized quote and historical points. It returns an explicit missing
   value when the required evidence is unavailable.
3. `OpportunityScorer` converts available performance values into a bounded
   score and returns factor records containing the factor name, value, weight,
   contribution, and evidence status.
4. A provider-neutral summary generator receives a bounded request containing
   only normalized evidence already displayed by the UI. Its adapter is
   replaceable and may be unavailable; no provider SDK or activation decision
   belongs in the analysis domain.
5. The `/analysis` PageModel invokes the existing market-data service, passes
   the response to the analysis layer, and exposes ranked cards plus per-asset
   error states to the Razor view.

The market-data service remains responsible for provider access, caching,
budgets, normalization, freshness, quality, warnings, and stable error
categories. The analysis layer must not duplicate provider validation or
introduce provider symbols.

## Result surface

The page shows a ranked list of responsive cards. Each usable card contains:

- rank and asset identifier;
- Opportunity Score with a clear educational/illustrative label;
- current price and currency;
- 24-hour, 7-day, and 30-day performance values;
- freshness and quality state;
- at least two factors when sufficient evidence exists;
- each factor's direction, weight, and contribution;
- a simple historical chart using the normalized historical points;
- an AI-generated summary grounded in the displayed evidence, when available;
  otherwise a bounded unavailable or insufficient-evidence state.

An asset without enough evidence receives a bounded insufficient-evidence state
and explains what is missing. An asset with a provider error receives a bounded
error state without raw exception text, upstream payloads, or credential
details. Successful assets in the same request remain visible.

The result surface does not use “best investment”, “buy”, “guaranteed”, or
“top opportunity” language. It does not provide personalized advice, execute
trades, save analyses, or add a client-side dashboard dependency.

## Data and scoring rules

Performance calculations must use timestamps and the requested history range,
not provider-specific fields. The implementation must define deterministic
selection rules for the closest available historical point at or before each
period boundary and must expose insufficient evidence when no valid point
exists.

Each available factor is normalized to the scoring range before its fixed
weight is applied. The score is bounded to `0–100`. If the minimum evidence
needed for a meaningful score is not available, the result is marked
insufficient instead of silently filling missing values. The exact
normalization constants and rounding rules belong in the implementation plan
and must be covered by unit tests.

## Error and safety behavior

- Provider errors remain categorized and user-safe.
- Raw `MarketDataProviderException.Message` values never reach the browser.
- Partial results are preserved per asset.
- Missing, stale, delayed, or incomplete evidence remains visible to the
  analysis and explanation layers.
- Summaries must acknowledge uncertainty and must not invent facts absent from
  the normalized response. Only displayed normalized evidence may enter a
  summary.
- AI failures, timeouts, cancellations, and malformed output must never remove
  otherwise valid analysis results.
- The production provider-approval gate and fake-provider environment boundary
  remain unchanged.

## Verification

Automated tests will cover:

- period performance calculation with complete history;
- missing and out-of-range history points;
- score normalization, fixed weights, rounding, and bounds;
- factor contribution output and insufficient-evidence behavior;
- grounded summary input, per-asset invocation, insufficient evidence,
  unavailable/timeout/cancellation handling, malformed output, and no invented
  data;
- ranking order and stable tie handling;
- preservation of successful partial results alongside per-asset errors;
- Razor rendering of score, metrics, factors, chart, summary, quality states,
  and educational disclaimer;
- authentication and existing market-data contract compatibility;
- absence of raw provider details and credentials in rendered output.

Manual verification will cover the complete account → analysis → results flow,
desktop and mobile layouts, keyboard focus, readable uncertainty states, chart
legibility, and the educational-not-advice boundary.

## Explicit non-goals

- AI Copilot, chat, follow-up questions, or conversational interaction.
- Provider activation, provider selection, and production credential decisions.
- JavaScript dashboard or client-side state management.
- Persistent analyses, watchlists, alerts, or portfolio features.
- Separate per-asset routes.
