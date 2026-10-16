# Explained Market Ranking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the protected `/analysis` flow so selected assets receive a transparent ranked analysis with deterministic score, factors, performance metrics, chart data, and grounded local explanation.

**Architecture:** Add a focused `_bootstrap_scaffold.Analysis` application layer that consumes the existing provider-neutral `MarketDataResponse`. Keep `/analysis` as a server-rendered Razor Page, calculate a bounded `0–100` score from 24-hour, 7-day, and 30-day performance, preserve per-asset partial results, and render ranked cards without adding a JavaScript dashboard or real AI integration.

**Tech Stack:** ASP.NET Core/.NET 10, C# records and dependency injection, Razor Pages, xUnit, `WebApplicationFactory`, existing CSS token system.

## Global Constraints

- Consume `MarketDataResponse`; do not add provider-specific request or response types.
- Use fixed score weights: 24-hour momentum 25%, 7-day momentum 35%, 30-day trend 40%.
- Normalize each available performance factor from `-20%..+20%` to `0..100`, clamping outside values.
- Require at least two available factors for a meaningful score; renormalize weights across available factors when one factor is missing.
- Preserve submitted asset order for equal scores and sort complete scored results descending by score.
- Never convert missing evidence into zero performance or a zero score.
- Never render raw provider exception text, upstream payloads, provider credentials, or provider-specific identifiers.
- Keep the production provider-approval gate and Development/Test fake-provider boundary unchanged.
- Use deterministic local explanations only; do not add an LLM, network call, API key, or new frontend dependency.
- Keep all score, factor, chart, and summary copy educational and avoid “buy”, “best investment”, “guaranteed”, and “top opportunity” language.
- Preserve the existing authentication, antiforgery, account-to-analysis navigation, and `/api/market-data` contracts.

---

## File Map

Create these focused analysis-layer files:

- `Analysis/AnalysisContracts.cs` — immutable result, factor, metric, chart, and error records used by the service and Razor Page.
- `Analysis/IPerformanceCalculator.cs` — performance calculation interface.
- `Analysis/PerformanceCalculator.cs` — deterministic 24-hour, 7-day, and 30-day calculations plus chart-point projection.
- `Analysis/IOpportunityScorer.cs` — scoring interface.
- `Analysis/OpportunityScorer.cs` — fixed weights, clamping, missing-factor handling, and factor contribution calculation.
- `Analysis/IAnalysisExplanationBuilder.cs` — explanation interface.
- `Analysis/AnalysisExplanationBuilder.cs` — bounded deterministic summaries grounded in analysis values.
- `Analysis/IAnalysisService.cs` — application-layer entry point.
- `Analysis/AnalysisService.cs` — maps market-data results, preserves partial errors, ranks usable results, and creates the final response.

Modify these existing files:

- `Program.cs` — register the analysis services.
- `Pages/Analysis/Index.cshtml.cs` — invoke the analysis service after market-data retrieval and expose ranked results/error states.
- `Pages/Analysis/Index.cshtml` — render ranked cards, metrics, factors, chart, summaries, partial states, and disclaimer.
- `wwwroot/css/site.css` — add token-backed responsive result-card, metric, factor, chart, and state styles.
- `docs/market-data.md` — document that S-03 consumes normalized evidence and derives product metrics in the analysis layer.
- `tests/Argus.Authentication.Tests/AnalysisContractTests.cs` — unit tests for calculation, scoring, explanation, ranking, and partial results.
- `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs` — extend authenticated browser-flow assertions for the result surface.

---

### Task 1: Add analysis contracts and deterministic performance calculation

**Files:**
- Create: `Analysis/AnalysisContracts.cs`
- Create: `Analysis/IPerformanceCalculator.cs`
- Create: `Analysis/PerformanceCalculator.cs`
- Test: `tests/Argus.Authentication.Tests/AnalysisContractTests.cs`

**Interfaces:**
- Consumes: `MarketDataResult`, `DateTimeOffset asOf`.
- Produces: `AssetPerformance Calculate(MarketDataResult result, DateTimeOffset asOf)`.

Define these exact records in `AnalysisContracts.cs`:

```csharp
public sealed record PerformanceMetric(
    string Period,
    decimal? Percentage,
    bool IsAvailable);

public sealed record AssetPerformance(
    PerformanceMetric Change24Hours,
    PerformanceMetric Change7Days,
    PerformanceMetric Change30Days,
    IReadOnlyList<AnalysisChartPoint> ChartPoints);

public sealed record AnalysisChartPoint(
    DateTimeOffset Timestamp,
    decimal Close);
```

Define:

```csharp
public interface IPerformanceCalculator
{
    AssetPerformance Calculate(MarketDataResult result, DateTimeOffset asOf);
}
```

- [ ] **Step 1: Write failing calculation tests.** Add tests named:
  - `Calculate_uses_latest_history_point_at_or_before_each_period_boundary`
  - `Calculate_returns_percentage_change_from_historical_close_to_current_quote`
  - `Calculate_marks_period_unavailable_when_no_eligible_point_exists`
  - `Calculate_projects_chart_points_in_timestamp_order`
  - `Calculate_does_not_use_points_after_a_period_boundary`

Use a fixed `asOf` timestamp, a current quote value of `120m`, and historical
closes at known timestamps so each expected percentage is exact. Assert that a
missing period has `Percentage == null` and `IsAvailable == false`.

- [ ] **Step 2: Run the focused tests and verify the expected failure.**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter FullyQualifiedName~AnalysisContractTests
```

Expected: FAIL because `PerformanceCalculator` and the analysis contracts do not yet exist.

- [ ] **Step 3: Implement the minimal performance calculator.** Use the current
  quote as the numerator and choose the latest historical close whose timestamp
  is less than or equal to `asOf - period`. Calculate:

```csharp
percentage = (currentValue - historicalClose) / historicalClose * 100m;
```

Return `null` for missing quote, non-positive historical close, or no eligible
point. Include every valid historical point as a chart point sorted ascending.

- [ ] **Step 4: Run the focused tests and verify they pass.**

Run the same focused command. Expected: all `AnalysisContractTests` calculation
tests PASS.

- [ ] **Step 5: Commit the calculation unit.**

```powershell
git add Analysis\AnalysisContracts.cs Analysis\IPerformanceCalculator.cs Analysis\PerformanceCalculator.cs tests\Argus.Authentication.Tests\AnalysisContractTests.cs
git commit -m "feat(explained-market-ranking): calculate analysis performance metrics"
```

---

### Task 2: Add transparent Opportunity Score and factor contributions

**Files:**
- Create: `Analysis/IOpportunityScorer.cs`
- Create: `Analysis/OpportunityScorer.cs`
- Modify: `Analysis/AnalysisContracts.cs`
- Test: `tests/Argus.Authentication.Tests/AnalysisContractTests.cs`

**Interfaces:**
- Consumes: `AssetPerformance`.
- Produces: `ScoreAssessment Assess(AssetPerformance performance)`.

Add these records:

```csharp
public sealed record AnalysisFactor(
    string Key,
    string Label,
    decimal? Percentage,
    decimal Weight,
    decimal? Contribution,
    bool IsAvailable,
    string Explanation);

public sealed record ScoreAssessment(
    decimal? Score,
    IReadOnlyList<AnalysisFactor> Factors,
    bool HasSufficientEvidence);
```

Define:

```csharp
public interface IOpportunityScorer
{
    ScoreAssessment Assess(AssetPerformance performance);
}
```

- [ ] **Step 1: Write failing scoring tests.** Add tests named:
  - `Assess_normalizes_each_factor_to_zero_to_one_hundred`
  - `Assess_applies_fixed_weights_and_returns_contributions`
  - `Assess_clamps_performance_outside_the_normalization_range`
  - `Assess_renormalizes_weights_when_one_factor_is_missing`
  - `Assess_returns_insufficient_evidence_when_fewer_than_two_factors_exist`
  - `Assess_returns_zero_score_for_three_negative_boundary_factors`

Use the exact normalization formula:

```csharp
normalized = Math.Clamp((percentage + 20m) / 40m * 100m, 0m, 100m);
```

Use weights `0.25m`, `0.35m`, and `0.40m`. When a factor is missing, divide
each available weight by the sum of available weights before calculating its
contribution. Round score and contributions to two decimal places.

- [ ] **Step 2: Run the focused tests and verify they fail for the missing scorer.**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter FullyQualifiedName~AnalysisContractTests
```

Expected: the new scoring tests FAIL because `OpportunityScorer` is absent.

- [ ] **Step 3: Implement `OpportunityScorer`.** Create factors with stable keys
  `momentum-24h`, `momentum-7d`, and `trend-30d`, labels suitable for the UI,
  the original percentage, normalized weight, contribution, availability, and
  a bounded explanation. Require two available factors. Return `Score == null`
  when the evidence threshold is not met.

- [ ] **Step 4: Run all analysis unit tests.**

Run the focused command again. Expected: all performance and scoring tests PASS.

- [ ] **Step 5: Commit the scoring unit.**

```powershell
git add Analysis\AnalysisContracts.cs Analysis\IOpportunityScorer.cs Analysis\OpportunityScorer.cs tests\Argus.Authentication.Tests\AnalysisContractTests.cs
git commit -m "feat(explained-market-ranking): add transparent opportunity scoring"
```

---

### Task 3: Build deterministic explanations and the application analysis service

**Files:**
- Create: `Analysis/IAnalysisExplanationBuilder.cs`
- Create: `Analysis/AnalysisExplanationBuilder.cs`
- Create: `Analysis/IAnalysisService.cs`
- Create: `Analysis/AnalysisService.cs`
- Modify: `Analysis/AnalysisContracts.cs`
- Modify: `Program.cs`
- Test: `tests/Argus.Authentication.Tests/AnalysisContractTests.cs`

**Interfaces:**
- Consumes: `AssetPerformance`, `ScoreAssessment`, `MarketDataResult`.
- Produces:

```csharp
public interface IAnalysisExplanationBuilder
{
    string Build(
        MarketDataResult marketData,
        AssetPerformance performance,
        ScoreAssessment assessment);
}

public interface IAnalysisService
{
    AnalysisResponse Analyze(MarketDataResponse marketDataResponse, DateTimeOffset asOf);
}
```

Add:

```csharp
public sealed record AssetAnalysis(
    AssetReference Asset,
    decimal? Score,
    AssetPerformance Performance,
    IReadOnlyList<AnalysisFactor> Factors,
    IReadOnlyList<AnalysisChartPoint> ChartPoints,
    string? Summary,
    MarketDataQuality Quality,
    MarketDataFreshness Freshness,
    IReadOnlyList<MarketDataWarning> Warnings,
    MarketDataError? Error);

public sealed record AnalysisResponse(
    IReadOnlyList<AssetAnalysis> Results);
```

- [ ] **Step 1: Write failing explanation and service tests.** Add tests named:
  - `Build_mentions_available_metrics_and_avoids_unshown_facts`
  - `Build_acknowledges_insufficient_evidence`
  - `Analyze_preserves_provider_errors_without_raw_provider_messages`
  - `Analyze_ranks_scored_assets_descending`
  - `Analyze_preserves_submitted_order_for_score_ties`
  - `Analyze_keeps_successful_results_when_another_asset_has_an_error`

Assert that summaries contain only values supplied by the test data, never
provider IDs or raw error messages, and use bounded insufficient-evidence copy.

- [ ] **Step 2: Run the focused tests and verify they fail.**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter FullyQualifiedName~AnalysisContractTests
```

Expected: FAIL because the explanation builder and analysis service are absent.

- [ ] **Step 3: Implement the deterministic explanation builder.** Build short
  Polish or English-consistent text from the factor labels, percentage values,
  score availability, quality, and freshness. Do not interpolate
  `MarketDataSource.ProviderId`, `MarketDataError.Message`, or any unbounded
  provider field. If fewer than two factors are available, explicitly state
  that the evidence is insufficient for a meaningful score.

- [ ] **Step 4: Implement `AnalysisService`.** For each market-data result:
  - preserve `Error`, quality, freshness, warnings, and chart-safe data;
  - calculate performance and score for successful results;
  - build the summary from the normalized values;
  - order results by descending non-null score using stable input order for ties;
  - keep insufficient-evidence and errored assets in the response after scored
    assets so no selected asset silently disappears.

- [ ] **Step 5: Register the service graph in `Program.cs`.** Register the
  calculator, scorer, explanation builder, and analysis service as singletons
  beside the existing market-data services. Do not alter provider registration
  or production approval checks.

- [ ] **Step 6: Run all analysis unit tests and verify they pass.**

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter FullyQualifiedName~AnalysisContractTests
```

- [ ] **Step 7: Commit the analysis application layer.**

```powershell
git add Analysis\AnalysisContracts.cs Analysis\IAnalysisExplanationBuilder.cs Analysis\AnalysisExplanationBuilder.cs Analysis\IAnalysisService.cs Analysis\AnalysisService.cs Program.cs tests\Argus.Authentication.Tests\AnalysisContractTests.cs
git commit -m "feat(explained-market-ranking): compose grounded analysis results"
```

---

### Task 4: Integrate analysis results into the protected Razor Page

**Files:**
- Modify: `Pages/Analysis/Index.cshtml.cs`
- Modify: `Pages/Analysis/Index.cshtml`
- Modify: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

**Interfaces:**
- Consumes: `IMarketDataService.GetAsync(...)` and `IAnalysisService.Analyze(...)`.
- Produces: PageModel properties `HasResults`, `AnalysisResults`, and bounded
  `ProviderMessage`/`StartedMessage` state for the existing form.

- [ ] **Step 1: Write failing browser-contract tests.** Add tests named:
  - `Valid_analysis_submission_renders_ranked_scores_and_metrics`
  - `Analysis_results_render_factors_summary_and_chart_data`
  - `Partial_analysis_renders_successful_asset_and_bounded_failed_asset`
  - `Insufficient_evidence_renders_bounded_state_without_zero_score`
  - `Analysis_results_do_not_render_provider_identifiers_or_raw_errors`

Use `AnalysisProviderFactory` with deterministic `FakeMarketDataProvider` data
for two assets with deliberately different history slopes. Assert score order,
metric labels, factor labels, summary text, chart marker data, and absence of
raw provider details.

- [ ] **Step 2: Run the new browser tests and verify they fail.**

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter FullyQualifiedName~AnalysisAccessContractTests
```

Expected: FAIL because the page still renders only `HasStarted` and does not
inject or invoke `IAnalysisService`.

- [ ] **Step 3: Update the PageModel.** Inject `IAnalysisService`, replace the
  single page-level success-only path with:
  - `AnalysisResponse? AnalysisResults`;
  - a `HasResults` computed property;
  - bounded provider/error state only when the market-data service itself
    fails;
  - analysis-service results for successful and partial responses;
  - selected asset preservation on every validation, provider, and analysis
    result state.

- [ ] **Step 4: Update the Razor view.** Keep the selection form and antiforgery
  token. When `AnalysisResults` exists, render:
  - a ranked `ol` or equivalent semantic list;
  - one card per result;
  - score and rank only when score is available;
  - current price, 24-hour, 7-day, and 30-day values;
  - factors and contributions;
  - chart points in an accessible textual/inline representation;
  - summary, quality/freshness state, per-asset error or insufficient-evidence
    message;
  - the educational disclaimer.

  Keep copy bounded and do not render raw `MarketDataError.Message` or source
  provider identifiers.

- [ ] **Step 5: Run the focused browser tests and fix only integration defects.**

Run the same `AnalysisAccessContractTests` filter. Expected: all existing auth,
selection, provider-error, and new result-surface tests PASS.

- [ ] **Step 6: Commit the protected-page integration.**

```powershell
git add Pages\Analysis\Index.cshtml.cs Pages\Analysis\Index.cshtml tests\Argus.Authentication.Tests\AnalysisAccessContractTests.cs
git commit -m "feat(explained-market-ranking): render ranked analysis results"
```

---

### Task 5: Add token-backed responsive result-card styling

**Files:**
- Modify: `wwwroot/css/site.css`
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs` only if a
  deterministic rendered-class assertion is needed

- [ ] **Step 1: Add a CSS contract test or source assertion before styling.**
  Assert that the rendered result surface uses named classes for result cards,
  score, metric grid, factor list, chart, insufficient evidence, and per-asset
  error states. Do not assert literal colors.

- [ ] **Step 2: Run the focused test and verify it fails for missing result classes.**

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter FullyQualifiedName~AnalysisAccessContractTests
```

- [ ] **Step 3: Add styles using existing tokens only.** Add responsive rules
  for:
  - ranked card list and card header;
  - score badge and rank marker;
  - three-column-to-one-column metric grid;
  - factor rows and contribution values;
  - bounded chart container with overflow protection;
  - success, insufficient-evidence, stale, and error states;
  - visible keyboard focus at desktop and `390x844`.

  Do not add literal colors or arbitrary view-local layout values. Reuse the
  variables already defined in `:root`, adding a named token only when the
  existing token set cannot express the contract.

- [ ] **Step 4: Run the focused browser tests and inspect the rendered class contract.**

Expected: tests PASS and no source scan finds literal colors in
`Pages\Analysis\Index.cshtml`.

- [ ] **Step 5: Commit the styling unit.**

```powershell
git add wwwroot\css\site.css tests\Argus.Authentication.Tests\AnalysisAccessContractTests.cs
git commit -m "feat(explained-market-ranking): style ranked analysis cards"
```

---

### Task 6: Document the S-03 evidence boundary and run the complete verification suite

**Files:**
- Modify: `docs/market-data.md`
- Modify: `docs/superpowers/specs/2026-10-16-explained-market-ranking-design.md`
- Test: `tests/Argus.Authentication.Tests/AnalysisContractTests.cs`
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

- [ ] **Step 1: Add documentation assertions or review checklist entries for the
  S-03 handoff.** Document that product metrics, scoring, chart projection, and
  deterministic explanations derive from normalized evidence and must preserve
  freshness, quality, warnings, and per-asset errors.

- [ ] **Step 2: Run the complete test suite.**

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore
```

Expected: all tests PASS with no failures.

- [ ] **Step 3: Build the application.**

```powershell
dotnet build .bootstrap-scaffold.csproj --no-restore
```

Expected: `0 Warning(s), 0 Error(s)`.

- [ ] **Step 4: Run the release-boundary source scan.** Confirm:
  - no provider credentials or API keys were added;
  - no fake provider registration exists outside Development/Testing;
  - no raw provider error/message field is rendered by the result view;
  - no literal colors were added to `Pages\Analysis\Index.cshtml`;
  - no new package or frontend dependency was introduced.

- [ ] **Step 5: Perform manual verification.**

Run the Development app over HTTPS, sign in, and verify:

1. `/account` links to `/analysis`.
2. Selecting two assets and submitting renders ranked cards.
3. Scores, 24h/7d/30d metrics, at least two factors, chart, and summary are
   readable and consistent with the displayed fake data.
4. A partial provider response keeps the successful card and shows a bounded
   error for the failed asset.
5. Missing evidence shows an insufficient-evidence state rather than zero.
6. The educational disclaimer remains visible and no recommendation language
   appears.
7. Keyboard focus is visible and layouts work at `1440x900` and `390x844`.

- [ ] **Step 6: Commit documentation and final verification updates.**

```powershell
git add docs\market-data.md docs\superpowers\specs\2026-10-16-explained-market-ranking-design.md tests\Argus.Authentication.Tests\AnalysisContractTests.cs tests\Argus.Authentication.Tests\AnalysisAccessContractTests.cs
git commit -m "docs(explained-market-ranking): document S-03 evidence boundary"
```

---

## Final Verification Checklist

- [ ] All analysis unit tests pass.
- [ ] All authentication and browser contract tests pass.
- [ ] Application builds with zero warnings and errors.
- [ ] Existing `/api/market-data` behavior remains compatible.
- [ ] No raw provider detail, credential, or provider-specific identifier leaks
  into the result surface.
- [ ] Partial, stale, delayed, unavailable, and insufficient evidence states are
  visible and bounded.
- [ ] Manual desktop, mobile, keyboard, and educational-boundary checks pass.
