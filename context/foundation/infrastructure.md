---
project: Argus
researched_at: 2026-09-28T10:01:54+02:00
recommended_platform: Azure App Service
runner_up: Railway
context_type: mvp
tech_stack:
  language: C#
  framework: ASP.NET Core Web API
  runtime: .NET 10
---

## Recommendation

**Deploy on Azure App Service.**

Azure App Service is the strongest fit for Argus because the selected stack is a .NET 10 ASP.NET Core Web API, Azure is already the stack's deployment target, and the developer has practical Azure experience plus Visual Studio benefits access. The recommendation is also supported by current official .NET 10 quickstart coverage, Azure CLI/API operations, deployment slots, and readable Microsoft Learn Markdown sources. Treat the free F1 plan as a learning/demo option only: it provides 60 shared CPU minutes per day, has no SLA, and is not supported for production workloads. Confirm the exact Visual Studio benefit subscription, credit, region, and expiry before committing to paid resources.

## Platform Comparison

Scoring uses the five agent-friendly criteria. `Pass` = 2, `Partial` = 1, `Fail` = 0. Runtime compatibility is a hard filter: Cloudflare Workers, Vercel, and Netlify do not provide a normal .NET 10 ASP.NET Core hosting runtime for this application.

| Platform | CLI-first | Managed/Serverless | Documentation readable for agents | Stable deployment API | MCP / Integration | Weighted result |
|---|---|---|---|---|---|---:|
| Azure App Service | Pass | Pass | Pass | Pass | Partial | 9/10 |
| Railway | Pass | Pass | Pass | Pass | Partial | 8/10 |
| Render | Partial | Pass | Pass | Pass | Partial | 7/10 |
| Fly.io | Pass | Partial | Pass | Pass | Partial | 7/10 |
| Cloudflare Workers + Pages | Pass | Pass | Pass | Pass | Pass | Rejected: runtime mismatch |
| Vercel | Pass | Pass | Pass | Pass | Partial (Beta) | Rejected: runtime mismatch |
| Netlify | Pass | Pass | Partial | Pass | Pass | Rejected: runtime mismatch |

The interview supplied neutral cost/DX weighting, Azure familiarity, one-region deployment, external managed services being acceptable, and uncertainty about persistent connections. That favors a conventional, continuously running .NET host over an edge JavaScript runtime. Since the PRD does not yet specify QPS, request volume alone cannot produce a reliable cost estimate; compute time, memory, database, outbound data, and observability must be measured.

### Research notes and evidence

- **Azure App Service — Pass overall.** Microsoft documents .NET 10 ASP.NET deployment on App Service, including Linux and Windows hosting, and describes App Service as scalable and self-patching. The Azure CLI provides `az webapp` management, while App Service supports GitHub Actions, ZIP deployment, app settings, diagnostic logs, and staging slots. F1 is free but limited to 60 CPU minutes/day, shared compute, 1 GB RAM, no SLA, and no production support. Standard/Premium/Isolated are required for deployment slots. App Service has strong Microsoft/Azure integration, but no dedicated App-Service MCP server was verified, so MCP is scored Partial. Sources: [ASP.NET/.NET 10 quickstart](https://learn.microsoft.com/en-us/azure/app-service/quickstart-dotnetcore), [pricing](https://azure.microsoft.com/en-us/pricing/details/app-service/linux/), [CLI](https://learn.microsoft.com/en-us/cli/azure/webapp?view=azure-cli-latest), [deployment best practices](https://learn.microsoft.com/en-us/azure/app-service/deploy-best-practices), [slots](https://learn.microsoft.com/en-us/azure/app-service/deploy-staging-slots), [logs](https://learn.microsoft.com/en-us/azure/app-service/troubleshoot-diagnostic-logs).
- **Railway — Pass overall.** Railway runs containerized services, so ASP.NET Core can be deployed with a Dockerfile or detected build configuration. Its CLI exposes `railway up`, `redeploy`, `deployment list`, configuration plan/apply, and project/service commands. Current pricing lists a free plan with $1 monthly credit, Hobby at $5/month, and usage-based resource billing. It is strong for fast iteration and co-located PostgreSQL, Redis, and other templates, but Windows-native Azure familiarity does not transfer perfectly and the platform-specific agent setup path is currently documented through a shell installer/WSL on Windows. Sources: [CLI](https://docs.railway.com/cli), [pricing](https://docs.railway.com/pricing/plans), [docs](https://docs.railway.com/).
- **Render — Partial overall.** Render supports web services and Docker-based ASP.NET deployment, Git-connected auto-deploys, manual deploy triggers, zero-downtime deploys except with persistent disks, and deploy history. It has readable docs and API/deploy-hook automation, but several operational workflows remain dashboard-centered, and the free tier is not a dependable production baseline. It is a credible fallback when minimizing setup is more important than Azure continuity. Sources: [deploys](https://render.com/docs/deploys), [docs](https://render.com/docs).
- **Fly.io — Partial overall.** Fly.io supports long-running Machines and containers, gives strong `flyctl` coverage for launch, deploy, secrets, logs, status, releases, and volumes, and publishes an `llms.txt` documentation index. It is technically capable for .NET and persistent processes, but the operational model requires more networking, region, volume, and machine knowledge than App Service. Managed Postgres is documented, but the platform is less turnkey for a one-week MVP. Sources: [docs](https://docs.fly.io/), [flyctl](https://docs.fly.io/flyctl/), [managed Postgres](https://docs.fly.io/postgres/).
- **Cloudflare Workers + Pages — Rejected.** Cloudflare has excellent agent-readable docs, `llms.txt`, Wrangler, observability, queues, workflows, D1/R2, and MCP-related integrations. Its documented runtimes are JavaScript/TypeScript, Python, Rust/WebAssembly and related edge environments, not a normal .NET 10 ASP.NET Core server. Porting Argus would require a different backend architecture, so the strong platform score cannot overcome the hard runtime mismatch. Sources: [Workers docs](https://developers.cloudflare.com/workers/), [Workers `llms.txt`](https://developers.cloudflare.com/workers/llms.txt).
- **Vercel — Rejected.** Vercel has strong CLI, deployments, logs, agent resources, and a Vercel MCP marked **Beta** in the documentation checked on 2026-09-28. It is optimized for frontend/serverless runtimes and does not offer a normal .NET 10 ASP.NET Core hosting target for this API. Sources: [docs](https://vercel.com/docs), [Vercel MCP](https://vercel.com/docs/agent-resources/vercel-mcp).
- **Netlify — Rejected.** Netlify offers CLI/serverless workflows and an official MCP integration, but its primary runtime model is frontend plus functions rather than a continuously hosted ASP.NET Core .NET 10 process. Adapting Argus would mean replacing the selected API runtime or introducing an external container host. Source: [docs](https://docs.netlify.com/).

### Shortlisted Platforms

#### 1. Azure App Service (Recommended)

It matches the exact runtime, the existing tech-stack decision, the developer's Azure experience, and the one-week MVP constraint. It supports ordinary ASP.NET Core hosting, environment settings, deployment slots, CLI/API automation, GitHub Actions, and read-only operational diagnostics without requiring a new hosting model. The main gap is cost/plan complexity: the free tier is not a production target and useful staging/rollback features require a paid tier.

#### 2. Railway

Railway is the best speed-oriented alternative. It provides a simple CLI and usage-based pricing, and its container model can host the current API with minimal platform-specific code. It loses to Azure because .NET 10 is not a first-class native runtime in the same way, Azure familiarity reduces migration risk, and Railway's free credit is small and usage-based.

#### 3. Render

Render is a reasonable container-hosting fallback with Git deploys, deploy history, and simple web services. It ranks below Railway because its CLI/agent operational path is less complete and below Azure because it adds container packaging while offering fewer ecosystem advantages for this .NET/Azure-oriented project.

## Anti-Bias Cross-Check: Azure App Service

### Devil's Advocate — Weaknesses

1. The F1 plan can exhaust its 60 CPU-minute daily allowance during development or an AI/market-data burst, causing confusing throttling or downtime; it is explicitly not a production plan.
2. A useful staging/rollback workflow needs Standard, Premium, or Isolated because deployment slots are unavailable on lower tiers, making the safe release path more expensive than the headline free offer.
3. Visual Studio benefits may provide credits or a dev/test subscription rather than an unrestricted production allowance; expiry, tenant ownership, region availability, and spending caps can break an otherwise working deployment.
4. Adding Azure SQL, Key Vault, Application Insights/Azure Monitor, outbound data, and AI or market-data calls can dominate App Service compute cost at very low request volumes.
5. Azure CLI and identity/RBAC setup are more operationally broad than Railway or Render; an agent can make a destructive resource-group or configuration change unless permissions are narrowly scoped.

### Pre-Mortem — How This Could Fail

The team treated “Azure App Service” as synonymous with “free Azure hosting” and selected F1 without measuring CPU time, memory, startup behavior, or external API latency. The minimal API worked locally, so they deployed it directly to the production slot and stored secrets in repository configuration rather than App Service settings or Key Vault. As Argus gained a few users, market-data calls and AI summaries increased request duration; the daily CPU allowance was exhausted, and there was no staging slot for a safe test or quick swap. A Visual Studio benefit then expired or was attached to a different tenant, exposing an unexpected bill. Database schema changes were applied manually and could not be reversed with the application deployment. Logs were enabled only locally, so diagnosing authentication failures and external API timeouts required portal investigation. The team eventually moved to another host, but the migration was delayed because deployment artifacts, environment names, and data ownership had never been documented. The failure was not caused by .NET or App Service itself; it came from confusing an introductory credit path with a production operating model and postponing cost, secrets, rollback, and observability decisions.

### Unknown Unknowns

- The exact Visual Studio benefit may have subscription, tenant, regional, or time-limit restrictions; verify it before choosing a paid App Service tier.
- Deployment slots are not available on every plan, and slot swaps do not automatically undo database migrations or external side effects.
- App Service app-setting changes restart the application; secret rotation therefore needs a maintenance-aware procedure.
- The current project is a minimal API scaffold with no database, authentication implementation, or background-job design yet; the eventual dependencies may cost more than the API host.
- .NET 10 support is documented, but the selected App Service operating system, region, and runtime image must be checked when the resource is created rather than assumed from a generic quickstart.

## Operational Story

- **Preview deploys**: Use a nonproduction App Service deployment slot on Standard/Premium/Isolated, or a separate dev App Service on a lower tier; deploy the branch through GitHub Actions or ZIP/CLI, then test its slot hostname before production. Do not expose secrets in fork pull-request workflows.
- **Secrets**: Store non-secret configuration in App Service application settings and sensitive values in Key Vault where the subscription supports it; grant the app a managed identity and rotate secrets in the vault/settings, remembering that app-setting changes restart the app.
- **Rollback**: With slots, swap the previous production slot back into production. Without slots, redeploy the last known-good package through GitHub Actions or ZIP deployment. Application rollback does not roll back database migrations, external AI calls, or market-data side effects.
- **Approval**: An agent may build, test, deploy to a nonproduction slot, read status, and read logs with a scoped identity. A human should approve production slot swaps, plan changes, subscription spending, primary secret rotation, destructive resource deletion, and irreversible data migrations.
- **Logs**: Read deployment and runtime diagnostics using Azure CLI/App Service log commands and Azure Monitor/Application Insights with read-only permissions; use the documented App Service diagnostic logs flow rather than relying on portal-only inspection. Deployment and application logs are distinct and should both be captured.

## Risk Register

| Risk | Source | Likelihood | Impact | Mitigation |
|---|---|---:|---:|---|
| F1 CPU allowance is exhausted | Devil's advocate / Research finding | H | H | Treat F1 as development only; measure CPU and request duration, then choose a paid plan or benefit-backed dev/test subscription before user testing. |
| Visual Studio benefit expires or is not valid for the target tenant | Devil's advocate / Unknown unknowns | M | H | Verify subscription type, credit amount, expiry, tenant, region, and spending alerts before provisioning. |
| Safe rollback is unavailable on the chosen tier | Devil's advocate / Pre-mortem | M | H | Require a staging slot or separate staging app before production; document package rollback and migration rollback separately. |
| Azure services make the MVP unexpectedly expensive | Devil's advocate / Pre-mortem | M | H | Start with an explicit monthly budget, cost alerts, managed identity, minimal observability retention, and external services only after comparing total cost. |
| App settings restart the application during secret rotation | Unknown unknowns / Research finding | M | M | Rotate during a maintenance window, use slot-specific settings where available, and add a startup health check. |
| .NET 10 runtime image or region differs from expectation | Unknown unknowns / Research finding | L | H | Confirm the runtime stack and region in the actual App Service resource; keep a tested ZIP/GitHub Actions deployment path. |
| Agents receive excessive Azure permissions | Devil's advocate | M | H | Use least-privilege RBAC, separate deployment and administration identities, and require human approval for destructive operations. |
| External data/AI latency exceeds App Service request limits | Pre-mortem / Research finding | M | M | Add timeouts, retries with budgets, caching, structured error responses, and asynchronous processing only if the product actually requires it. |
| Cloudflare/Vercel/Netlify is selected later despite runtime mismatch | Research finding | L | H | Keep ASP.NET Core/.NET 10 as a hard compatibility constraint; evaluate those platforms only for a separately designed frontend or service. |

## Getting Started

1. Confirm the Visual Studio benefit subscription and Azure region, then create a resource group and an App Service plan/app with a .NET 10 runtime using the current Azure CLI or Visual Studio flow. Do not assume the F1 plan is suitable for production.
2. Keep the current `.bootstrap-scaffold.csproj` deployment artifact explicit: run `dotnet restore`, `dotnet build --no-restore`, and `dotnet publish` locally, then deploy the published output as a ZIP package or through the documented GitHub Actions App Service workflow.
3. Configure environment-specific values as App Service application settings; keep secrets out of source control and use Key Vault/managed identity when the selected subscription supports it.
4. Add a `/health` endpoint and enable application/deployment diagnostics before connecting market-data or AI providers; verify the deployed API and OpenAPI behavior in the Development-only configuration.
5. Before production use, test a staging slot or separate staging app, define a last-known-good package, set a monthly budget alert, and record how database migrations will be handled independently of application rollback.

## Out of Scope

The following were not evaluated in this research:

- Docker image configuration
- CI/CD pipeline setup
- Production-scale architecture (multi-region, HA, DR)

