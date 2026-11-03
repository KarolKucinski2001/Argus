---
project: Argus
checked_at: 2026-11-02T12:00:00Z
health_status: healthy
context_type: brownfield
language_family: dotnet
stack_assessment_available: false
checks_run:
  - lockfile
  - dependency_audit
  - outdated_deps
  - test_runner
  - ci_cd
  - configuration
audit_findings:
  critical: 0
  high: 0
  moderate: 0
  low: 0
test_runner_detected: true
ci_provider: GitHub Actions
recommended_fixes: 5
---

# Health Check: Argus

## Dependency Health

### Lockfile

Status: missing (`packages.lock.json` not enabled)
Package manager: dotnet (NuGet)

Direct package versions are pinned exactly in `.bootstrap-scaffold.csproj`, so
direct dependencies are reproducible; transitive versions are not locked.
Fix: add `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` to both
project files, run `dotnet restore`, commit the generated `packages.lock.json`.

### Security Audit

Tool: `dotnet list package --vulnerable --include-transitive` (app and test project)
Summary: 0 CRITICAL, 0 HIGH, 0 MODERATE, 0 LOW
Direct vs transitive: both checked; no vulnerable packages in either.

### Outdated Dependencies

Packages with major version gaps: 0

Minor/patch updates available (informational): `Azure.Data.Tables` 12.11.0 → 12.13.0,
`Azure.Identity` 1.17.0 → 1.21.0, `Microsoft.AspNetCore.Authentication.OpenIdConnect`
and `Microsoft.AspNetCore.OpenApi` 10.0.11 → 10.0.14 (servicing patches).

## Test Suite

Test runner: xUnit (`dotnet test`)
Tests found: 330 tests
Test execution: passing (Release, ~30 s; `TableStoreUnreachableTests` take ~14 s each because of Azure SDK retries)

Configuration: `tests/Argus.Authentication.Tests/Argus.Authentication.Tests.csproj`
Framework: xUnit with `WebApplicationFactory<Program>` contract tests and presenter unit tests; no network or secrets required.

## CI/CD

Provider: GitHub Actions
Configuration: `.github/workflows/deploy.yml`

| Stage      | Status | Notes                                                        |
|------------|--------|--------------------------------------------------------------|
| Lint       | ✗      | not configured (`dotnet format --verify-no-changes` available) |
| Test       | ✗      | workflow builds and publishes without running `dotnet test`  |
| Build      | ✓      | `dotnet build` + `dotnet publish` (Release)                  |
| Type check | ✓      | C# compiler with `<Nullable>enable</Nullable>` during build  |
| Security   | ✗      | no Dependabot, CodeQL or vulnerable-package step             |

Actions are pinned to commit SHAs (good supply-chain practice).

## Configuration

### Medium severity

- **Analyzer/format rules** — no `.editorconfig`, `TreatWarningsAsErrors` or `AnalysisLevel`; agent output style is enforced only by convention. Fix: add a root `.editorconfig` (`dotnet new editorconfig`) and run `dotnet format --verify-no-changes` locally.

### Low severity

- **`.editorconfig`** — covered by the medium item above.
- **`.env.example`** — not applicable; Development secrets use `dotnet user-secrets`, documented in `docs/authentication.md` and `docs/watchlist.md`.

`.gitignore` and `AGENTS.md` are present.

## Stack Assessment Cross-Reference

No stack-assessment.md found. Run /10x-stack-assess for quality-gate analysis.

## Recommended Fixes

### Fix before agent work (Category A)

### 1. Lock transitive NuGet dependencies

**Impact**: Without a lockfile a fresh restore can resolve different transitive versions, so the agent's local results may differ from CI.
**Severity**: medium
**Effort**: quick (< 5 min)
**Fix**:

Add `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` to `.bootstrap-scaffold.csproj` and the test project, run `dotnet restore`, and commit both `packages.lock.json` files.

### 2. Add formatter/analyzer configuration

**Impact**: The agent cannot check style mechanically; consistency depends on reading neighbouring files.
**Severity**: medium
**Effort**: moderate (15–30 min)
**Fix**:

`dotnet new editorconfig`, then `dotnet format .bootstrap-scaffold.csproj --verify-no-changes` and fix the reported diffs.

### 3. Apply servicing updates

**Impact**: Low; patch releases of ASP.NET Core and Azure SDKs carry security and reliability fixes.
**Severity**: low
**Effort**: quick (< 5 min)
**Fix**:

`dotnet add .bootstrap-scaffold.csproj package Microsoft.AspNetCore.Authentication.OpenIdConnect --version 10.0.14` (same for `Microsoft.AspNetCore.OpenApi`), then rerun the test suite.

### Addressed in upcoming lessons (Category B)

### Run tests in the deploy workflow

**Lesson**: [Sprint Zero z Agentem: infrastruktura, walking skeleton i pierwszy deploy (M1L5)](https://platforma.przeprogramowani.pl/external/10xdevs-3/m1-l5)
**What you'll do there**: Gate deployment on CI; here that means adding `dotnet test tests/Argus.Authentication.Tests/Argus.Authentication.Tests.csproj -c Release` before `dotnet publish` in `.github/workflows/deploy.yml`.

### Add a security stage to CI

**Lesson**: [Sprint Zero z Agentem: infrastruktura, walking skeleton i pierwszy deploy (M1L5)](https://platforma.przeprogramowani.pl/external/10xdevs-3/m1-l5)
**What you'll do there**: Add Dependabot (`.github/dependabot.yml` for `nuget` and `github-actions`) or a `dotnet list package --vulnerable` step.

## Summary

Health status: healthy

Argus has no vulnerable packages, no major version gaps, nullable reference types enabled and a fast, deterministic xUnit suite of 330 tests with clear repository guidance in `AGENTS.md`. The gaps are operational: transitive dependencies are not locked, there is no formatter/analyzer config, and the deploy workflow publishes without running tests.

Next step: lock dependencies and gate the deploy workflow on the test suite, then continue agent-assisted feature work.

## Follow-up applied (2026-11-02)

- Fix 1 (lockfiles): done — `RestorePackagesWithLockFile` enabled in both projects; `packages.lock.json` files committed. 330 tests still pass.
- Category B "Run tests in the deploy workflow": done — `Test` step added between Build and Publish in `.github/workflows/deploy.yml` (not yet verified on the `ubuntu-latest` runner).
- Category B "Add a security stage": done — `.github/dependabot.yml` (weekly NuGet, monthly GitHub Actions).
- Fix 2 (`.editorconfig`/analyzers): deferred — a repo-wide `dotnet format` pass would create a large unrelated diff.
- Fix 3 (servicing updates): deferred to Dependabot PRs so runtime-aligned bumps are reviewed one at a time.
