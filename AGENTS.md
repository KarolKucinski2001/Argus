# Repository Guidelines

Argus is an early ASP.NET Core Web API scaffold targeting .NET 10. The runnable application is currently the minimal API in `Program.cs`; the investment-analysis product described in `@context/foundation/prd.md` is not implemented yet.

## Critical repository rules

- Read `@context/foundation/prd.md` and `@context/foundation/tech-stack.md` before changing product behavior; these files are the source of truth for product and stack decisions.
- Never write resolved work into `context/archive/`; archived material is immutable. Put bootstrap audit output under `context/changes/bootstrap-verification/`.
- Preserve the 10x workflow and conflict rules in `@.github/copilot-instructions.md`; keep implementation guidance separate from generated scaffold instructions.
- Treat `/weatherforecast` in `@Program.cs` as starter code, not Argus domain architecture.

## Structure and architecture

- `@Program.cs` owns startup, dependency registration, middleware, and current route mappings using top-level statements.
- OpenAPI is registered with `AddOpenApi()` and mapped only inside the Development environment guard.
- HTTPS redirection is global. Development URLs are defined in `@Properties/launchSettings.json`.
- The project remains `.bootstrap-scaffold.csproj` with root namespace `_bootstrap_scaffold`; do not infer production namespaces or feature boundaries from it.
- Future domain work should follow the PRD flow: authenticated users select assets, market data is analyzed, scores are ranked with explaining factors, and AI summaries remain grounded in displayed data with an educational-not-advice boundary.

## Build, test, and run

- `dotnet restore .bootstrap-scaffold.csproj` — restore NuGet dependencies.
- `dotnet build .bootstrap-scaffold.csproj --no-restore` — compile the API.
- `dotnet run --project .bootstrap-scaffold.csproj` — run the Development API using launch profiles.
- `dotnet test .bootstrap-scaffold.csproj --no-restore` — current test check; no test project or cases exist yet.

There is no lint configuration, single-test command, or GitHub Actions workflow. Add and document each with the first corresponding tooling or test change. The bootstrap audit is recorded in `@context/changes/bootstrap-verification/verification.md`.

## Code and change conventions

Use nullable reference types and implicit usings as enabled by `@.bootstrap-scaffold.csproj`. Keep route names explicit with `.WithName(...)`, and keep environment-specific OpenAPI exposure inside the existing Development guard. No commit-message convention can be inferred because the repository has no commits; define one when repository history is established.
