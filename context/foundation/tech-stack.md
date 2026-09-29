---
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
---

## Why this stack

The standard `.NET` recommendation fits Argus's large-scale web application with a one-week MVP timeline, authentication, and AI-generated explanations. ASP.NET Core provides strong typing, recognizable conventions, OpenAPI, and Entity Framework integration, while the verified bootstrapper confidence reduces scaffolding risk. Azure App Service is the starter's default deployment target, with GitHub Actions and auto-deploy on merge selected for the delivery flow.
