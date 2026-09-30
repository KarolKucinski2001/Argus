# First Argus Deployment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deploy the current .NET 10 ASP.NET Core scaffold directly to a production Azure App Service, verify it manually, then enable GitHub Actions auto-deployment on pushes to `master`.

**Architecture:** Keep the current application unchanged and deploy the published output of `.bootstrap-scaffold.csproj` to a single Linux Azure App Service production app. Perform one manual ZIP deployment first; only after its smoke test passes, add a GitHub Actions workflow using OIDC and least-privilege Azure RBAC to repeat the same restore, build, publish, and deploy path.

**Tech Stack:** C#, ASP.NET Core Web API, .NET 10, Azure App Service, Azure CLI, GitHub Actions, `azure/login` OIDC authentication, ZIP deployment.

## Global Constraints

- Read `context/foundation/infrastructure.md` and `context/foundation/tech-stack.md` before changing deployment decisions.
- Deploy the existing `.bootstrap-scaffold.csproj`; do not infer production namespaces or domain architecture from the scaffold.
- Do not add authentication, a database, `/health`, Application Insights, staging slots, or domain functionality in this deployment change.
- Do not commit Azure tokens, publish profiles, client secrets, API keys, or other credentials.
- Validate the available .NET 10 App Service runtime before provisioning; do not silently select a different runtime.
- Do not assume the Azure F1 plan is suitable for production. Record the selected SKU and cost guardrails.
- The first deployment targets the production slot directly; production slot swaps are not part of this plan.
- The branch trigger is `push` to `master`.
- A human must approve resource deletion, SKU changes, subscription spending changes, secret rotation, and rollback.
- The deployed service is a scaffold/demo and is not treated as full production readiness because it lacks health checks, staging, observability, tests, and data rollback.

---

## Current Repository and Deployment Surface

The current application surface is intentionally small:

- `Program.cs` registers OpenAPI, redirects HTTP to HTTPS, and exposes `/weatherforecast`.
- `.bootstrap-scaffold.csproj` targets `net10.0` and contains the OpenAPI package reference.
- `appsettings.json` contains no application secrets.
- There is currently no test project, workflow, Dockerfile, database, authentication implementation, or health endpoint.
- `context/foundation/infrastructure.md` recommends Azure App Service and identifies GitHub Actions with auto-deploy on merge as the delivery flow.

The deployment work should therefore add only:

- `context/changes/deployment/deployment-plan.md` — this execution plan.
- `.github/workflows/deploy.yml` — created during the implementation, after manual deployment succeeds.

No application source file needs to change for the scaffold-only deployment.

## Required Inputs Before Execution

The implementer must obtain these values without committing them:

| Input | Example shape | Where it is used |
|---|---|---|
| Azure subscription ID | UUID | CLI account selection and GitHub OIDC |
| Azure tenant ID | UUID | CLI login context and GitHub OIDC |
| Azure region | `westeurope` | Resource Group and App Service Plan |
| Resource Group name | `argus-prod-rg` | All Azure CLI resource commands |
| App Service Plan name | `argus-prod-plan` | Web App provisioning |
| Web App name | globally unique DNS-safe name | `https://<name>.azurewebsites.net` and workflow |
| GitHub owner/repository | `owner/repository` | OIDC federated credential |
| Azure SKU | explicitly chosen non-F1 SKU | App Service Plan |

If any input is unknown, stop before provisioning and record the missing value. Do not substitute a guessed subscription, region, or globally unique Web App name.

## Task 1: Verify Local Tools, Azure Context, and GitHub Prerequisites

**Files:**
- Read: `.bootstrap-scaffold.csproj`
- Read: `context/foundation/infrastructure.md`
- Read: `context/foundation/tech-stack.md`

**Interfaces:**
- Produces: verified Azure subscription, tenant, region, runtime, repository, and branch values for later tasks.

- [x] **Step 1: Check the local SDK and CLI versions**

Run from the repository root:

```powershell
dotnet --info
az version
git status --short
git remote -v
```

Expected:

- .NET 10 SDK is installed and usable.
- Azure CLI is installed and usable.
- The repository status is understood before any deployment files are added.
- A GitHub remote exists before GitHub Actions setup begins.

- [x] **Step 2: Authenticate to Azure and select the intended subscription**

```powershell
az login
az account show --output table
az account list --output table
az account set --subscription "<SUBSCRIPTION_ID>"
az account show --query "{subscriptionId:id,tenantId:tenantId,name:name,user:user.name}" --output table
```

Expected: the selected subscription ID and tenant ID match the approved deployment inputs.

- [x] **Step 3: Configure CLI defaults without storing credentials**

```powershell
az configure --defaults group="<RESOURCE_GROUP>" location="<AZURE_REGION>"
az config set core.only_show_errors=true
```

Expected: future commands use the selected resource group and region by default; no token or secret is written to the repository.

- [x] **Step 4: Verify the App Service runtime**

```powershell
az webapp list-runtimes --os-type linux --output table
az provider show --namespace Microsoft.Web --query registrationState --output tsv
```

If `Microsoft.Web` is not registered, register it and wait for completion:

```powershell
az provider register --namespace Microsoft.Web
az provider show --namespace Microsoft.Web --query registrationState --output tsv
```

Expected: an available Linux App Service runtime corresponding to .NET 10 is identified. If it is unavailable, stop rather than deploying a different runtime.

- [x] **Step 5: Verify the GitHub repository and branch**

```powershell
git branch --show-current
git ls-remote --heads origin master
```

Expected: the intended GitHub repository is reachable and the deployment branch is `master`. If the branch does not exist remotely, create it through the normal repository workflow before enabling the deployment trigger.

## Task 2: Build and Preserve the Manual Publish Artifact

**Files:**
- Read: `.bootstrap-scaffold.csproj`
- Create: local ignored artifact directory, not committed

**Interfaces:**
- Consumes: verified .NET 10 SDK from Task 1.
- Produces: a known-good Release publish directory and ZIP package for the manual deployment.

- [x] **Step 1: Restore the project**

```powershell
dotnet restore .bootstrap-scaffold.csproj
```

Expected: restore completes without errors.

- [x] **Step 2: Build without restoring**

```powershell
dotnet build .bootstrap-scaffold.csproj --configuration Release --no-restore
```

Expected: build succeeds with zero errors.

- [x] **Step 3: Publish to a uniquely identified local directory**

```powershell
$artifactId = "argus-$((Get-Date).ToUniversalTime().ToString('yyyyMMdd-HHmmss'))"
$publishDir = Join-Path (Join-Path $PWD "artifacts") $artifactId
dotnet publish .bootstrap-scaffold.csproj --configuration Release --no-restore --output $publishDir
Write-Output $publishDir
```

Expected: the output directory contains the published application and the artifact ID is recorded in the deployment notes or terminal log.

- [x] **Step 4: Create a ZIP containing the publish output**

```powershell
$zipPath = Join-Path (Join-Path $PWD "artifacts") "$artifactId.zip"
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force
Get-Item $zipPath | Select-Object FullName, Length, LastWriteTimeUtc
```

Expected: the ZIP contains the contents of the publish directory at its root, not an extra parent directory.

## Task 3: Provision the Production Azure App Service

**Files:**
- Create: Azure Resource Group, App Service Plan, and Web App resources
- Modify: Azure App Service application settings only through CLI

**Interfaces:**
- Consumes: Azure identifiers and runtime from Task 1.
- Produces: a running production Web App with a stable HTTPS hostname.

- [x] **Step 1: Create or verify the Resource Group**

```powershell
az group create `
  --name "<RESOURCE_GROUP>" `
  --location "<AZURE_REGION>" `
  --tags project=argus environment=production managed-by=azure-cli `
  --output table
```

Expected: the command returns the intended resource group in the intended region.

- [x] **Step 2: Create the App Service Plan with the approved SKU**

For a Linux plan, use the approved non-F1 SKU:

```powershell
az appservice plan create `
  --name "<APP_SERVICE_PLAN>" `
  --resource-group "<RESOURCE_GROUP>" `
  --location "<AZURE_REGION>" `
  --is-linux `
  --sku "<APPROVED_SKU>" `
  --output table
```

Expected: the plan exists with the approved SKU. Record the SKU and its expected monthly cost; do not continue if the subscription or budget owner has not approved it.

- [x] **Step 3: Create the Web App with the verified .NET 10 runtime**

```powershell
az webapp create `
  --name "<WEB_APP_NAME>" `
  --resource-group "<RESOURCE_GROUP>" `
  --plan "<APP_SERVICE_PLAN>" `
  --runtime "<VERIFIED_DOTNET_10_RUNTIME>" `
  --https-only true `
  --tags project=argus environment=production `
  --output table
```

Expected: the Web App is created and its default hostname is available.

- [x] **Step 4: Configure non-secret production settings**

```powershell
az webapp config appsettings set `
  --name "<WEB_APP_NAME>" `
  --resource-group "<RESOURCE_GROUP>" `
  --settings ASPNETCORE_ENVIRONMENT=Production `
  --output none
```

Expected: only the required non-secret setting is configured. Do not add placeholder API keys or credentials.

- [x] **Step 5: Record the resource identity for later commands**

```powershell
az webapp show `
  --name "<WEB_APP_NAME>" `
  --resource-group "<RESOURCE_GROUP>" `
  --query "{name:name,hostName:defaultHostName,state:state,httpsOnly:httpsOnly,resourceId:id}" `
  --output table
```

Expected: the name, hostname, running state, HTTPS-only setting, and resource ID are recorded.

## Task 4: Deploy the Artifact Manually and Smoke-Test It

**Files:**
- Use: ZIP artifact from Task 2
- Modify: Azure Web App deployment state

**Interfaces:**
- Consumes: Web App from Task 3 and ZIP artifact from Task 2.
- Produces: a manually verified production deployment and a known-good rollback artifact.

- [x] **Step 1: Deploy the ZIP package**

```powershell
az webapp deploy `
  --name "<WEB_APP_NAME>" `
  --resource-group "<RESOURCE_GROUP>" `
  --src-path "<ZIP_PATH>" `
  --type zip `
  --clean true `
  --output table
```

Expected: deployment status is successful and the deployment ID/time is recorded.

- [x] **Step 2: Read deployment and application diagnostics**

```powershell
az webapp deployment list `
  --name "<WEB_APP_NAME>" `
  --resource-group "<RESOURCE_GROUP>" `
  --output table

az webapp log deployment show `
  --name "<WEB_APP_NAME>" `
  --resource-group "<RESOURCE_GROUP>" `
  --output table

az webapp show `
  --name "<WEB_APP_NAME>" `
  --resource-group "<RESOURCE_GROUP>" `
  --query "{state:state,hostName:defaultHostName}" `
  --output table
```

Expected: the app is running and the latest deployment is successful.

- [x] **Step 3: Verify HTTPS and the application endpoint**

```powershell
$baseUrl = "https://<WEB_APP_NAME>.azurewebsites.net"
Invoke-WebRequest "$baseUrl/weatherforecast" -MaximumRedirection 5
```

Expected: HTTPS returns HTTP 200 and a JSON weather forecast array.

Also request the HTTP URL:

```powershell
Invoke-WebRequest "http://<WEB_APP_NAME>.azurewebsites.net/weatherforecast" -MaximumRedirection 5
```

Expected: the request redirects to HTTPS because the application enables HTTPS redirection and the App Service is HTTPS-only.

- [x] **Step 4: Verify the production OpenAPI boundary**

```powershell
try {
  Invoke-WebRequest "$baseUrl/openapi/v1.json" -ErrorAction Stop
  throw "OpenAPI unexpectedly responded in Production."
} catch {
  if ($_.Exception.Response.StatusCode.value__ -ne 404) {
    throw
  }
}
```

Expected: OpenAPI is not exposed in Production because `Program.cs` maps it only inside the Development environment guard.

- [x] **Step 5: Preserve rollback information**

Record the ZIP path, artifact ID, deployment timestamp, Web App name, resource group, and successful smoke-test result. Rollback for this scaffold means redeploying the last known-good ZIP; it does not roll back any future database migration or external side effect.

### Manual deployment execution record

- Verified on: `2026-09-30`
- Subscription: `0ed38a37-bb2c-4424-b5e4-9452cd8ea351`
- Tenant: `074e3485-8e25-4d93-be5f-a5777052131c`
- Resource Group: `argus-prod-rg`
- App Service Plan: `argus-prod-plan` (`Linux`, `B1`, `Poland Central`)
- Web App: `argus-api-kucink01`
- Runtime: `DOTNETCORE|10.0`
- Artifact: `artifacts/argus-20260930-090356.zip` in the local deployment worktree
- Deployment ID: `89419dfd-a467-4f08-bdfd-6286e56181b3`
- Hostname: `argus-api-kucink01-fsccdxexckcqhwb6.polandcentral-01.azurewebsites.net`
- Smoke test: HTTPS `/weatherforecast` returned `200`; HTTP redirected with `301`; production `/openapi/v1.json` returned `404`

The artifact remains local and ignored by Git. Redeploy it with:

```powershell
az webapp deploy `
  --name "argus-api-kucink01" `
  --resource-group "argus-prod-rg" `
  --src-path ".\artifacts\argus-20260930-090356.zip" `
  --type zip `
  --clean true
```

## Task 5: Configure GitHub OIDC and Least-Privilege Azure Access

**Files:**
- Create: GitHub federated credential and deployment identity in Azure
- Configure: GitHub repository Actions variables/secrets

**Interfaces:**
- Consumes: GitHub owner/repository, subscription ID, tenant ID, Web App resource ID from Tasks 1 and 3.
- Produces: an OIDC identity usable only by the deployment workflow for `master`.

- [x] **Step 1: Create a deployment identity**

Use the approved identity-management path for the tenant. The identity must have:

- a federated credential with issuer `https://token.actions.githubusercontent.com`;
- subject restricted to `repo:<OWNER>/<REPOSITORY>:ref:refs/heads/master`;
- audience `api://AzureADTokenExchange`;
- no client secret stored in the repository;
- no subscription-wide Contributor role unless the owner explicitly approves that exception.

- [x] **Step 2: Assign the minimum deployment role**

Assign the narrowest role supported by the tenant at the Web App scope, such as an App Service deployment role, rather than granting broad subscription permissions:

```powershell
az role assignment create `
  --assignee-object-id "<DEPLOYMENT_IDENTITY_OBJECT_ID>" `
  --assignee-principal-type ServicePrincipal `
  --role "<APP_SERVICE_DEPLOYMENT_ROLE>" `
  --scope "<WEB_APP_RESOURCE_ID>"
```

Expected: the role assignment is visible at the Web App scope and does not grant unrelated resource access.

### Azure OIDC identity execution record

- App registration/service principal: `argus-github-deployer`
- Federated subject: `repo:KarolKucinski2001/Argus:ref:refs/heads/master`
- Audience: `api://AzureADTokenExchange`
- Role: `Website Contributor`
- Scope: Web App resource `argus-api-kucink01`
- Role assignment: `5f9b473f-312d-4dc5-b2f4-59c2d889bc24`

The GitHub repository values and workflow were not configured in this step. No client secret was created.

- [ ] **Step 3: Configure GitHub Actions values**

Configure these repository-level Actions values:

- `AZURE_CLIENT_ID` — deployment identity client ID;
- `AZURE_TENANT_ID` — Azure tenant ID;
- `AZURE_SUBSCRIPTION_ID` — selected subscription ID;
- `AZURE_WEBAPP_NAME` — Web App name;
- `AZURE_RESOURCE_GROUP` — resource group name.

The first three are credentials/identity values and must be protected as repository secrets. Resource names may be repository variables if the repository policy permits it.

- [ ] **Step 4: Verify OIDC before adding deployment steps**

Use a temporary, non-deploying workflow or the repository's normal validation process to confirm that `azure/login` can acquire an OIDC token for `master`. Do not test OIDC from an untrusted pull request or fork.

Expected: the workflow can authenticate without a client secret and cannot authenticate for an unrelated branch.

## Task 6: Add and Verify GitHub Actions Auto-Deploy

**Files:**
- Create: `.github/workflows/deploy.yml`

**Interfaces:**
- Consumes: OIDC values and resource names from Task 5.
- Produces: automatic deployment after each push to `master`.

- [x] **Step 1: Create the workflow with the fixed trigger and permissions**

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy Argus

on:
  push:
    branches:
      - master

permissions:
  id-token: write
  contents: read

env:
  DOTNET_VERSION: '10.0.x'
  PROJECT_PATH: '.bootstrap-scaffold.csproj'
  PUBLISH_PATH: '${{ github.workspace }}/publish'

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - name: Check out source
        uses: actions/checkout@v4

      - name: Set up .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore
        run: dotnet restore "$PROJECT_PATH"

      - name: Build
        run: dotnet build "$PROJECT_PATH" --configuration Release --no-restore

      - name: Publish
        run: dotnet publish "$PROJECT_PATH" --configuration Release --no-restore --output "$PUBLISH_PATH"

      - name: Log in to Azure
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Deploy to App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: ${{ vars.AZURE_WEBAPP_NAME }}
          package: ${{ env.PUBLISH_PATH }}
```

Pin action versions according to the repository's security policy if it requires commit SHAs. Do not add a publish profile or client secret to this workflow.

The workflow was created at `.github/workflows/deploy.yml` with a `master`-only trigger, OIDC permissions, the existing project path, Release configuration, and no embedded credentials.

- [x] **Step 2: Validate the workflow file locally**

Run:

```powershell
git diff --check
```

Then inspect the workflow for:

- only `master` as the deployment trigger;
- `id-token: write` and `contents: read`;
- no hard-coded credential;
- the same project path and Release configuration used by the manual deployment;
- a production environment if repository protection requires approval.

- [ ] **Step 3: Commit the workflow and push to `master`**

```powershell
git add .github/workflows/deploy.yml
git commit -m "ci: add Azure App Service deployment"
git push origin master
```

Expected: the push starts exactly one deployment workflow.

- [ ] **Step 4: Verify workflow and runtime results**

In GitHub Actions, verify restore, build, publish, Azure login, and deployment steps. Then repeat the production smoke test from Task 4.

Expected:

- the workflow completes successfully;
- the deployed endpoint returns HTTP 200 over HTTPS;
- the deployed application is the same scaffold artifact shape as the manual deployment;
- no credentials appear in workflow logs.

## Task 7: Record Rollback and Operational Limitations

**Files:**
- Modify: `context/changes/deployment/deployment-plan.md`
- Create if needed: a deployment run record outside source control or in the repository's approved change-log location

**Interfaces:**
- Consumes: successful manual and automated deployment evidence from Tasks 4 and 6.
- Produces: repeatable rollback instructions and an explicit limitation record.

- [ ] **Step 1: Document artifact rollback**

For an application-only rollback:

```powershell
az webapp deploy `
  --name "<WEB_APP_NAME>" `
  --resource-group "<RESOURCE_GROUP>" `
  --src-path "<LAST_KNOWN_GOOD_ZIP>" `
  --type zip `
  --clean true `
  --output table
```

After redeployment, repeat the HTTPS and `/weatherforecast` smoke tests.

- [ ] **Step 2: Record limitations**

Record that this first deployment does not provide:

- health-check endpoint;
- staging slot or separate staging service;
- Application Insights or retention policy;
- database migration rollback;
- authentication or authorization;
- production-scale reliability or disaster recovery;
- a cost estimate beyond the selected App Service SKU and Azure billing alerts.

- [ ] **Step 3: Define the next hardening change**

The next deployment change should add health checks, structured diagnostics, budget alerts, and a staging path before Argus receives authentication, market-data integrations, or AI provider credentials.

## Acceptance Criteria

- [ ] Azure CLI authenticates to the intended tenant and subscription.
- [ ] The selected region and available App Service runtime are recorded before provisioning.
- [ ] The production Resource Group, App Service Plan, and Web App use the approved names, region, runtime, and SKU.
- [ ] The manual ZIP deployment succeeds before the GitHub workflow is enabled.
- [ ] `https://<WEB_APP_NAME>.azurewebsites.net/weatherforecast` returns HTTP 200 JSON.
- [ ] The HTTP endpoint redirects to HTTPS.
- [ ] The production OpenAPI document is not exposed.
- [ ] GitHub Actions authenticates with OIDC and no client secret or publish profile.
- [ ] Only pushes to `master` trigger deployment.
- [ ] The automated deployment succeeds and passes the same smoke test.
- [ ] A known-good ZIP artifact and redeploy command are recorded.
- [ ] The scaffold/demo limitations are explicit and not represented as full production readiness.
