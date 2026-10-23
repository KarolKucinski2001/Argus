<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Autorun analysis dashboard with comparison-first insights

- **Plan**: context/changes/analysis-dashboard-autorun-insights/plan.md
- **Scope**: Full plan
- **Reviewed phases**: 1, 2, 3, 4, 5
- **Date**: 2026-10-22
- **Verdict**: NEEDS ATTENTION
- **Findings**: 0 critical, 2 warnings, 7 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | PASS |
| Scope Discipline | PASS |
| Safety & Quality | WARNING |
| Architecture | PASS |
| Pattern Consistency | WARNING |
| Success Criteria | PASS |

Automated gates re-run on 2026-10-22:
- Tests: 214/214 passed.
- Literal colors and px values in `Pages/Analysis/*.cshtml`: 0 hits.
- Forbidden wording: the only hits are the deny-list in `AnalysisSummaryResultValidator.cs`.
- Manual Progress rows: all ticked and confirmed by the user.

Adaptations disclosed and approved during implementation (not findings):
- The large price-history chart, along with its JS and CSS, was removed; the sparkline stays.
- Number copy uses the invariant format (`+3.1%`).
- The why-section uses `aria-labelledby`.
- The summary panel shows plain text.
- The sidebar max-height is a token.
- Cards are 481px tall against the ~420px target; the user accepted this.

## Findings

### F1 — Autorun GET spends shared request budget on every load

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Safety & Quality
- **Location**: Pages/Analysis/Index.cshtml.cs:37-54; MarketData/MarketDataService.cs (`requestBudget.TryAcquire` runs before the cache lookup)
- **Detail**: `MarketDataService.GetAsync` takes one unit of the app-wide `ApplicationRequestBudget` (100/min) before it checks the 5-minute cache, so cache hits also use budget. Before this change only an explicit POST spent budget. Now every authenticated page load or refresh does, so one user refreshing repeatedly can push every user into "limit zapytań". The summary generator also runs on each load; it is a no-op today but will cost money once an LLM is wired in.
- **Fix A ⭐ Recommended**: In `MarketDataService`, take budget only when there are uncached or expired assets to fetch.
  - Strength: Fixes the problem at its source; cache hits become free for GET and POST.
  - Tradeoff: Touches `MarketDataService`, which the plan listed as out of scope; needs a test for budget-not-spent-on-hit.
  - Confidence: HIGH — the cache loop is already separate from the provider call.
  - Blind spot: The intent of the original budget design (counting user requests vs provider calls) has not been confirmed.
- **Fix B**: Add a follow-up and accept the risk for now (demo traffic, budget 100/min).
  - Strength: No scope creep.
  - Tradeoff: Risk remains until it is addressed, and it grows when real providers or an LLM are wired in.
  - Confidence: MED — depends on the traffic level.
  - Blind spot: Production traffic is unknown.
- **Decision**: FIXED (Fix A) — budget now acquired only when the provider is called; test `Cached_requests_do_not_consume_application_request_budget` added (break-check red on old order)

### F2 — Unexpected analysis exception now breaks the landing GET

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Pages/Analysis/Index.cshtml.cs:53 (`RunAnalysisAsync` on GET)
- **Detail**: Provider failures are mapped to `ProviderMessage`, but any other exception from scoring or analysis bubbles up as a 500. Before this change, GET always rendered the form, so a fault now blocks the whole page instead of only the POST.
- **Fix**: On GET, catch non-cancellation exceptions around `RunAnalysisAsync`, log them, set a generic `ProviderMessage`, and render the form with the defaults checked.
- **Decision**: FIXED — GET catches non-cancellation exceptions, logs, shows generic provider message; test `Autorun_failure_on_get_still_renders_the_form_with_a_safe_message` added (break-check red)

### F3 — Subline date segment not asserted by tests

- **Severity**: 🔍 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Success Criteria
- **Location**: tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs:362
- **Detail**: Only `2 aktywa` is asserted. The `• Dane z dd.MM.yyyy` part, and its omission when `DataAsOf` is null, are untested at markup level.
- **Fix**: Assert `Dane z \d{2}\.\d{2}\.\d{4}` in the autorun contract test.
- **Decision**: FIXED — autorun GET test asserts `4 aktywa • Dane z dd.MM.yyyy` (break-check red)

### F4 — StrongestTrend reads Performance.Change30Days, not the trend-30d factor

- **Severity**: 🔍 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Adherence
- **Location**: Analysis/AnalysisSummaryStrip.cs:58-64
- **Detail**: The plan said "max signed `trend-30d` %". The implementation uses the same metric from a different source, so the values are equivalent today.
- **Fix**: Accept as is, or switch to the factor bar so the value always matches what the card shows.
- **Decision**: SKIPPED — values equivalent today

### F5 — "Analiza rozpoczęta" status announced on every page load

- **Severity**: 🔍 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Pages/Analysis/Index.cshtml:70-81; Index.cshtml.cs:110-116
- **Detail**: The `role="status"` started card shows and is announced on GET even though the user did nothing.
- **Fix**: Set `StartedMessage` only on POST, or hide the started card for autorun.
- **Decision**: FIXED — GET autorun clears HasStarted/StartedMessage; status shown only after explicit POST; test asserts absence on GET (break-check red)

### F6 — "Dane z" shows the freshest date, using the UTC day

- **Severity**: 🔍 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Analysis/AnalysisSummaryStrip.cs:74-76
- **Detail**: When some assets are stale, the subline still shows the newest date. Near midnight, the UTC date can differ from local time. This matches the plan as written.
- **Fix**: Keep as planned, or use the oldest date among healthy results.
- **Decision**: SKIPPED — keep freshest date as planned

### F7 — New accent tokens break the `--color-asset-*` naming

- **Severity**: 🔍 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Pattern Consistency
- **Location**: wwwroot/css/site.css:75-78
- **Detail**: `--accent-apple|microsoft|nvidia|tesla` sit next to the existing `--color-asset-*` tokens; the plan named them `--accent-*`. `.asset-accent--equity` is now used only as the fallback for unknown equities, which the plan intended.
- **Fix**: Rename to `--color-asset-apple` etc. for consistency.
- **Decision**: FIXED — tokens renamed to `--color-asset-apple|microsoft|nvidia|tesla`

### F8 — Subline duplicates the asset count and uses a foreign block class

- **Severity**: 🔍 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Pattern Consistency
- **Location**: Pages/Analysis/_SummaryStrip.cshtml:13-24
- **Detail**: "N aktywa" in the subline repeats the "Przeanalizowane aktywa" stat. The class `analysis-workspace__subline` sits inside the summary-strip block.
- **Fix**: Rename the class to `analysis-summary-strip__subline`; decide whether to keep both counts.
- **Decision**: FIXED — class renamed to `analysis-summary-strip__subline` (CSS, partial, test); both counts kept

### F9 — Minor leftovers: fixture recomputes summary per card; blank line in Scripts section

- **Severity**: 🔍 OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Pattern Consistency
- **Location**: Pages/Analysis/UiStateFixture.cshtml.cs:22-24; Pages/Analysis/Index.cshtml:142,148
- **Detail**: The `RankedSummary` getter rebuilds the summary for each card; this affects the fixture only. Removing the chart script left a blank line and a mis-indented `</ol>`.
- **Fix**: Cache the summary in a local variable; tidy the whitespace.
- **Decision**: FIXED — fixture caches RankedSummary; Index.cshtml whitespace tidied
