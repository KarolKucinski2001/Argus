---
bootstrapped_at: 2026-09-25T13:05:48+02:00
starter_id: dotnet
starter_name: ".NET (ASP.NET Core webapi)"
project_name: argus
language_family: dotnet
package_manager: dotnet
cwd_strategy: subdir-then-move
bootstrapper_confidence: verified
phase_3_status: ok
audit_command: "dotnet list package --vulnerable"
---

## Hand-off

```yaml
starter_id: dotnet
package_manager: dotnet
project_name: argus
hints:
  language_family: dotnet
  team_size: solo
  deployment_target: azure-app-service
  ci_provider: github-actions
  ci_default_flow: auto-deploy-on-merge
  bootstrapper_confidence: verified
  path_taken: standard
  quality_override: false
  self_check_answers: null
  has_auth: true
  has_payments: false
  has_realtime: false
  has_ai: true
  has_background_jobs: false
```

## Why this stack

The standard `.NET` recommendation fits Argus's large-scale web application with a one-week MVP timeline, authentication, and AI-generated explanations. ASP.NET Core provides strong typing, recognizable conventions, OpenAPI, and Entity Framework integration, while the verified bootstrapper confidence reduces scaffolding risk. Azure App Service is the starter's default deployment target, with GitHub Actions and auto-deploy on merge selected for the delivery flow.

## Pre-scaffold verification

| Signal | Value | Severity | Notes |
| --- | --- | --- | --- |
| npm package | not run | n/a | Non-JavaScript starter |
| GitHub repo | not run | n/a | Card docs_url is Microsoft Learn, not a GitHub repository |

No recency signal was available for this starter. Proceeded as instructed.

## Scaffold log

**Resolved invocation**: `dotnet new webapi -n .bootstrap-scaffold --no-restore`
**Strategy**: subdir-then-move
**Exit code**: 0
**Files moved**: 6
**Files moved**: `.bootstrap-scaffold.csproj`, `.bootstrap-scaffold.http`, `appsettings.Development.json`, `appsettings.json`, `Program.cs`, `Properties\launchSettings.json`
**Conflicts (.scaffold siblings)**: none
**.gitignore handling**: absent in scaffold
**.bootstrap-scaffold cleanup**: deleted

## Post-scaffold audit

**Tool**: `dotnet list .bootstrap-scaffold.csproj package --vulnerable --include-transitive`
**Summary**: 0 CRITICAL, 0 HIGH, 0 MODERATE, 0 LOW
**Direct vs transitive**: not distinguished by the tool; no vulnerable packages reported

The project was restored as part of the audit command using the configured NuGet sources.

## Hints recorded but not acted on

| Hint | Value |
| --- | --- |
| bootstrapper_confidence | verified |
| quality_override | false |
| path_taken | standard |
| self_check_answers | null |
| team_size | solo |
| deployment_target | azure-app-service |
| ci_provider | github-actions |
| ci_default_flow | auto-deploy-on-merge |
| has_auth | true |
| has_payments | false |
| has_realtime | false |
| has_ai | true |
| has_background_jobs | false |

## Next steps

Next: a future skill will set up agent context (the project's AI configuration file (AGENTS.md), AGENTS.md). For now, your project is scaffolded and verified — happy hacking.

Useful manual steps in the meantime:
- `git init` (if you have not already) to start your own repo history.
- Review any `.scaffold` siblings the conflict policy created and decide which version of each file to keep.
- Address audit findings per your project's risk tolerance — the full breakdown is in this log.
