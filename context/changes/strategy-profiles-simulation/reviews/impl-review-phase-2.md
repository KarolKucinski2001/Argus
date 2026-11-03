<!-- IMPL-REVIEW-REPORT -->
# Implementation Review: Strategy profiles and simulated portfolio

- **Plan**: context/changes/strategy-profiles-simulation/plan.md
- **Scope**: Phase 2 of 2
- **Reviewed phases**: 2
- **Date**: 2026-11-02
- **Verdict**: APPROVED
- **Findings**: 0 critical, 1 warning, 4 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| Plan Adherence | PASS |
| Scope Discipline | PASS |
| Safety & Quality | PASS |
| Architecture | PASS |
| Pattern Consistency | WARNING |
| Success Criteria | WARNING |

## Findings

### F1 — Roadmap and change notes did not mention FR-005

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Success Criteria
- **Location**: context/foundation/roadmap.md:47, :126
- **Detail**: S-07 "At a glance" and issue rows still listed FR-001–FR-004 only. Manual item 2.4 (visual check, needs Entra sign-in) stays open.
- **Fix**: Update both roadmap rows to include FR-005.
- **Decision**: FIXED

### F2 — Hand-built analysis link

- **Severity**: OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Pattern Consistency
- **Location**: Pages/Strategies/Portfolios/Index.cshtml:60
- **Detail**: Used a literal href while sibling pages use the asp-page tag helper.
- **Fix**: Use `asp-page="/Strategies/Portfolios/Analysis" asp-route-id`.
- **Decision**: FIXED

### F3 — Effective positions assumed weights sum to 100

- **Severity**: OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Safety & Quality
- **Location**: Strategies/SimulatedPortfolioAnalysisPresenter.cs:90
- **Detail**: Rounded weights summing to 99/101 would skew the value near thresholds.
- **Fix**: Normalise by the actual total weight; regression test added.
- **Decision**: FIXED

### F4 — Threshold wording ambiguous at 7

- **Severity**: OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Pattern Consistency
- **Location**: Strategies/SimulatedPortfolioAnalysisPresenter.cs:31
- **Detail**: "od 4 do 7" and "od 7" overlapped.
- **Fix**: Reword to "od 4 do poniżej 7" / "od 7 wzwyż".
- **Decision**: FIXED

### F5 — Inline challenge instead of ChallengeTo helper

- **Severity**: OBSERVATION
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Pattern Consistency
- **Location**: Pages/Strategies/Portfolios/Analysis.cshtml.cs:23
- **Detail**: Sibling pages wrap the OIDC challenge in a helper.
- **Fix**: Extract `ChallengeTo(id)`.
- **Decision**: FIXED
