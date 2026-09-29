# Deployment Phase 0 Design

## Scope

Phase 0 executes Task 1 from `context/changes/deployment/deployment-plan.md`: verify the local tools, Azure context, App Service runtime availability, and GitHub deployment branch.

The supplied deployment inputs are:

- Subscription: `0ed38a37-bb2c-4424-b5e4-9452cd8ea351`
- Tenant: `074e3485-8e25-4d93-be5f-a5777052131c`
- Resource group: `argus-prod-rg`
- Region: `Poland Central`
- App Service plan: `argus-prod-plan`
- Web App: `argus-api-kucink01`
- Repository: `KarolKucinski2001/Argus`
- Deployment branch: `master`
- Approved plan: Basic B1

## Verification flow

Run read-only checks from the repository root:

1. Verify the installed .NET SDK and Azure CLI.
2. Confirm the active Azure account uses the supplied subscription and tenant.
3. Confirm the Microsoft.Web provider is registered.
4. Inspect the Linux App Service runtime catalog and identify the .NET 10 runtime string.
5. Confirm the Git remote points to `KarolKucinski2001/Argus`.
6. Confirm the local branch and remote `master` branch.

The checks will use explicit resource and subscription values. They will not change Azure resources, CLI defaults, repository files, credentials, or application settings.

## Success criteria

Phase 0 succeeds only if:

- the .NET 10 SDK is installed and usable;
- Azure CLI is installed and usable;
- the active Azure subscription and tenant match the supplied values;
- Microsoft.Web is registered;
- a Linux App Service runtime corresponding to .NET 10 is available;
- the GitHub remote is reachable;
- the `master` branch exists remotely.

If authentication is missing, the subscription or tenant differs, the runtime is unavailable, or the remote branch is missing, stop and report the specific blocker. Do not guess values or provision resources.

## Evidence

Record command results in the task handoff or deployment run record without storing access tokens, credentials, or other secrets. The next phase may proceed only after all success criteria pass.
