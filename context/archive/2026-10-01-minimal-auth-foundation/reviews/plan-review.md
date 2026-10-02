<!-- PLAN-REVIEW-REPORT -->
# Plan Review: Minimum Authenticated Access

- **Plan**: `context/changes/minimal-auth-foundation/plan.md`
- **Mode**: Deep
- **Date**: 2026-10-01
- **Verdict**: REVISE
- **Findings**: 1 critical, 3 warnings, 0 observations

## Verdicts

| Dimension | Verdict |
|-----------|---------|
| End-State Alignment | FAIL |
| Lean Execution | PASS |
| Architectural Fitness | WARNING |
| Blind Spots | WARNING |
| Plan Completeness | WARNING |

## Grounding

Grounding: 5/5 paths ✓, 3/3 existing symbols ✓, brief↔plan ✓

## Findings

### F1 — Manual OIDC session flow has no entry point

- **Severity**: ❌ CRITICAL
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: End-State Alignment
- **Location**: Desired End State; Phase 1 manual verification; `What We're NOT Doing`
- **Detail**: The plan requires manually completing an Entra flow to establish a cookie session (`plan.md:5,29-30,156-158`), but excludes login routes/UX and defines no challenge entry point or callback contract. OIDC callback handling does not initiate authorization by itself, so the documented manual smoke test cannot be performed from the planned application surface.
- **Fix A ⭐ Recommended**: Add a minimal non-UI sign-in challenge route and callback/session contract to F-01, while keeping registration and login screens/user-account UX in S-01.
  - Strength: Makes the manual smoke test executable and preserves the intended slice boundary.
  - Tradeoff: Adds one small auth route and its contract to the foundation.
  - Confidence: HIGH — an OIDC flow needs an explicit challenge entry point.
  - Blind spot: Exact route naming still needs to be specified.
- **Fix B**: Remove the real-provider manual session criterion from F-01 and verify only deterministic test authentication until S-01 adds login.
  - Strength: Keeps F-01 strictly infrastructure-only.
  - Tradeoff: Delays validation of the real Entra cookie flow and can defer provider integration failures.
  - Confidence: MED — technically coherent, but weakens the stated end-state verification.
  - Blind spot: Provider configuration errors would remain untested until S-01.
- **Decision**: PENDING

### F2 — 401 ProblemDetails is asserted but not specified

- **Severity**: ⚠️ WARNING
- **Impact**: 🔎 MEDIUM — real tradeoff; pause to reason through it
- **Dimension**: Architectural Fitness
- **Location**: Phase 1 middleware and current-user route contracts
- **Detail**: The plan promises HTTP 401 ProblemDetails and forbids redirects (`plan.md:34,64-74`), but does not specify cookie redirect suppression, ProblemDetails registration/serialization, content type, or response body contract. Default cookie challenge behavior can redirect instead of returning the promised API response.
- **Fix**: Specify the exact API challenge behavior: suppress cookie redirects for API endpoints, return status 401 with `application/problem+json`, and assert the required ProblemDetails fields in integration tests.
  - Strength: Converts the security/error guarantee into an implementable and testable contract.
  - Tradeoff: Adds explicit middleware configuration and a response-shape decision.
  - Confidence: HIGH — current `Program.cs:1-41` has none of these behaviors.
  - Blind spot: The exact ASP.NET Core .NET 10 redirect event/API behavior should be verified during implementation.
- **Decision**: PENDING

### F3 — Key authentication contracts remain underspecified

- **Severity**: ⚠️ WARNING
- **Impact**: 🔬 HIGH — architectural stakes; think carefully before deciding
- **Dimension**: Plan Completeness
- **Location**: Phase 1 project/configuration/current-user route; Phase 2 test project
- **Detail**: The plan leaves the exact route path/name, configuration key names and validation timing, OIDC/cookie scheme names, callback path/scopes, claims mapping, current-user JSON properties, test project path/name, and test-host setup open (`plan.md:54-74,92-114`). These are not low-level implementation details: downstream S-01 and automated tests need stable contracts.
- **Fix**: Name the route, configuration section/keys, callback path, required claims/fallback behavior, response schema, test project path, and executable test command in the plan.
  - Strength: Removes implementation guesswork and makes Progress commands runnable.
  - Tradeoff: Couples the foundation plan to explicit contract names that later slices must preserve.
  - Confidence: HIGH — no existing auth configuration anchors these choices (`appsettings*.json:1-10`).
  - Blind spot: Provider-specific authority format may vary by Entra tenant type.
- **Decision**: PENDING

### F4 — CI and repository guidance correction is optional despite a known contradiction

- **Severity**: ⚠️ WARNING
- **Impact**: 🏃 LOW — quick decision; fix is obvious and narrowly scoped
- **Dimension**: Plan Completeness
- **Location**: Phase 2 configuration/deployment handoff and repository guidance alignment
- **Detail**: `AGENTS.md:20-36` says there is no test project or GitHub Actions workflow, while `.github/workflows/deploy.yml:1-48` exists and the plan says workflow changes happen only “if needed” (`plan.md:110-114`). Leaving this optional can preserve contradictory instructions after implementation.
- **Fix**: Make the documentation correction mandatory and name `AGENTS.md`, `docs/authentication.md`, and the exact CI test step (or explicitly document why CI remains build-only).
- **Decision**: PENDING
