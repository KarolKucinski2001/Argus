# Analysis Dashboard Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform the authenticated `/analysis` page into a dark fintech-style split dashboard with a persistent desktop asset picker, responsive result-card grid, and preserved analysis behavior.

**Architecture:** Keep `Pages/Analysis/Index.cshtml.cs` and all analysis services unchanged. Restructure the existing Razor markup into an `analysis-workspace` sidebar/content shell, extend the semantic CSS token layer in `wwwroot/css/site.css`, and preserve the current chart/form JavaScript controllers. Add focused HTML contract assertions for the new structure and retain the Testing-only seven-state fixture as the visual gate.

**Tech Stack:** ASP.NET Core Razor Pages on .NET 10, server-rendered HTML, semantic CSS variables in `wwwroot/css/site.css`, existing vanilla JavaScript controllers, xUnit integration tests with `WebApplicationFactory<Program>`.

## Global Constraints

- Preserve current authentication, provider, scoring, ranking, chart, summary, validation, partial-result, stale-data, insufficient-evidence, and educational-boundary contracts.
- Do not add a frontend build pipeline, UI framework, screenshot dependency, watchlist, prediction behavior, drag-and-drop selection, or new market-data provider.
- Keep `/analysis` as one Razor Page and keep `IndexModel`, `OnGet`, and `OnPostAsync` as the behavior source.
- Use semantic roles from `wwwroot/css/site.css`; do not add literal colors or arbitrary layout values to Razor views.
- Desktop uses a persistent asset-selection sidebar beside the results; mobile stacks the expanded selector above the results.
- Result cards remain fully visible without tabs; use only data already exposed by `AnalysisResponse.Results`.
- Keep the Testing-only fixture unavailable outside the Testing environment.
- Do not stage or modify unrelated `.superpowers` artifacts or `.github/.10x-cli-manifest.json`.

---

### Task 1: Add the split workspace structure

**Files:**
- Modify: `Pages/Analysis/Index.cshtml`
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`
- Reference: `Pages/Analysis/Index.cshtml.cs`

**Interfaces:**
- Consumes: `IndexModel.Assets`, `IndexModel.SelectedAssetIds`, `IndexModel.HasResults`, `IndexModel.AnalysisResults`, and existing status properties.
- Produces: Stable `.analysis-workspace`, `.analysis-workspace__sidebar`, `.analysis-workspace__content`, and `.analysis-results__list` hooks for CSS and contract tests.

- [ ] **Step 1: Add a failing structure assertion**

Extend `Authenticated_analysis_page_renders_the_configured_assets_and_limit` after its existing assertions:

```csharp
Assert.Contains("class=\"analysis-workspace\"", html);
Assert.Contains("class=\"analysis-workspace__sidebar\"", html);
Assert.Contains("class=\"analysis-workspace__content\"", html);
Assert.Contains("class=\"analysis-results__list\"", html);
```

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --verbosity minimal
```

Expected: the test fails because the workspace classes do not yet exist.

- [ ] **Step 2: Wrap the existing analysis regions**

In `Pages/Analysis/Index.cshtml`, replace the current single `.analysis-page` content structure with this server-rendered shape while retaining the existing loops and conditionals:

```cshtml
<section class="analysis-workspace" aria-labelledby="analysis-title">
    <aside class="analysis-workspace__sidebar">
        <div class="analysis-page__header">
            <p class="eyebrow">Nowa analiza</p>
            <h1 id="analysis-title">Wybierz aktywa do sprawdzenia</h1>
            <p class="analysis-page__lede">
                Wybierz od jednego do @Model.MaximumSelectedAssets aktywów.
                Argus pobierze dzienną historię z ostatnich 12 miesięcy.
            </p>
        </div>

        <form method="post" class="analysis-form" data-analysis-form>
            <div class="asset-list">
                <!-- Keep the existing asset loop, validation, status cards, pending status, and submit button here. -->
            </div>
        </form>
    </aside>

    <div class="analysis-workspace__content">
        <header class="analysis-workspace__header">
            <p class="eyebrow">Market intelligence</p>
            <h2>Dashboard analizy</h2>
            <p>Rankingi, kontekst rynkowy i czynniki wyjaśniające dla wybranych aktywów.</p>
        </header>

        <section class="analysis-results" aria-labelledby="analysis-results-title">
            <h2 id="analysis-results-title">Ranking analizy</h2>
            <ol class="analysis-results__list">
                <!-- Keep the existing result loop and educational boundary here. -->
            </ol>
        </section>
    </div>
</section>
```

The existing form must stay intact inside the sidebar. Move only result-related markup into the content region. Do not change `name`, `value`, `checked`, antiforgery, status conditions, result loops, or chart data attributes.

- [ ] **Step 3: Run the structure and regression tests**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --verbosity minimal
```

Expected: all focused tests pass, including the new workspace assertions and existing result/chart contracts.

- [ ] **Step 4: Commit the workspace structure**

```powershell
git add Pages/Analysis/Index.cshtml tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs
git commit -m "feat: add analysis dashboard workspace structure"
```

### Task 2: Implement the dashboard token and grid system

**Files:**
- Modify: `wwwroot/css/site.css`
- Modify: `AGENTS.md`
- Reference: `docs/superpowers/specs/2026-10-20-analysis-dashboard-redesign-design.md`

**Interfaces:**
- Consumes: Existing `--color-*`, `--space-*`, `--radius-*`, `--content-width`, and analysis class conventions.
- Produces: Token-backed workspace/sidebar/content/card grid styles that later state and responsive work can reuse.

- [ ] **Step 1: Add semantic dashboard sizing tokens**

Add these roles to the existing `:root` block without replacing existing values:

```css
--workspace-sidebar-width: 20rem;
--workspace-content-min: 0;
--workspace-card-min: 28rem;
--workspace-gap: var(--space-4);
```

- [ ] **Step 2: Add desktop workspace styles**

Add styles using only semantic variables:

```css
.analysis-workspace {
    display: grid;
    grid-template-columns: minmax(16rem, var(--workspace-sidebar-width)) minmax(var(--workspace-content-min), 1fr);
    gap: var(--workspace-gap);
    width: min(100%, var(--content-width));
    margin-inline: auto;
    padding-block: var(--space-5);
}

.analysis-workspace__sidebar {
    align-self: start;
    position: sticky;
    top: var(--space-4);
}

.analysis-workspace__content {
    display: grid;
    gap: var(--space-4);
    min-width: 0;
}

.analysis-workspace__header {
    display: grid;
    gap: var(--space-2);
}

.analysis-workspace__header h2,
.analysis-workspace__header p {
    margin: 0;
}

.analysis-workspace__header p {
    color: var(--color-muted);
}

.analysis-results__list {
    display: grid;
    grid-template-columns: repeat(2, minmax(var(--workspace-card-min), 1fr));
    gap: var(--space-4);
}
```

- [ ] **Step 3: Add the persistent UI rule**

Keep the existing analysis UI rule in `AGENTS.md` and extend it with the split-dashboard contract:

```markdown
- Keep `/analysis` structured as a token-backed split workspace: the asset picker belongs in the sidebar and result cards belong in the content grid; keep mobile stacking in CSS rather than duplicating views.
```

- [ ] **Step 4: Run the view scan and build**

Run:

```powershell
$pattern = '#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(|oklch\(|-\[[0-9.]+(px|rem)\]|\b(bg|text|border|ring|outline|from|via|to|fill|stroke|shadow|divide)-(slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose|white|black)\b'; Select-String -Path 'Pages\Analysis\Index.cshtml' -Pattern $pattern -AllMatches
dotnet build .bootstrap-scaffold.csproj --no-restore
```

Expected: no hardcoded-value matches and a build with zero warnings and zero errors.

- [ ] **Step 5: Commit the token and grid system**

```powershell
git add wwwroot/css/site.css AGENTS.md
git commit -m "feat: add analysis dashboard grid tokens"
```

### Task 3: Refine result cards and responsive behavior

**Files:**
- Modify: `wwwroot/css/site.css`
- Modify: `Pages/Analysis/Index.cshtml`
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

**Interfaces:**
- Consumes: `.analysis-result-card`, `.analysis-result-card__header`, `.analysis-result-card__metrics`, chart classes, and existing `AnalysisResponse.Results` markup.
- Produces: A two-column desktop result grid, one-column narrow layout, stronger asset/rank/score hierarchy, and no horizontal overflow.

- [ ] **Step 1: Add result hierarchy assertions**

Extend the existing ranked-result test with:

```csharp
Assert.Contains("analysis-result-card__header", html);
Assert.Contains("analysis-result-card__score", html);
Assert.Contains("analysis-result-card__metrics", html);
Assert.Contains("analysis-result-card__chart", html);
Assert.Contains("analysis-result-card__summary", html);
```

- [ ] **Step 2: Style the result header and secondary evidence**

Update the existing selectors so the score uses the action token, the asset heading is visually dominant, metrics use the secondary surface, and factors/summary use dividers instead of competing standalone cards:

```css
.analysis-result-card__header h3 {
    font-size: clamp(1.25rem, 2vw, 1.75rem);
}

.analysis-result-card__score {
    background: var(--color-action);
    color: var(--color-on-accent);
}

.analysis-result-card__summary,
.analysis-result-card__chart-points {
    border-color: var(--color-secondary-border);
    background: var(--color-bg);
}
```

Retain accessible text contrast and existing chart SVG behavior.

- [ ] **Step 3: Add responsive grid rules**

Use the existing narrow breakpoint to collapse the result grid and workspace:

```css
@media (max-width: 64rem) {
    .analysis-workspace {
        grid-template-columns: 1fr;
    }

    .analysis-workspace__sidebar {
        position: static;
    }

    .analysis-results__list {
        grid-template-columns: 1fr;
    }
}
```

Keep the existing two-column metric rule at narrow widths and ensure chart-range controls wrap.

- [ ] **Step 4: Run focused tests and scan**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --verbosity minimal
$pattern = '#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(|oklch\(|-\[[0-9.]+(px|rem)\]|\b(bg|text|border|ring|outline|from|via|to|fill|stroke|shadow|divide)-(slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose|white|black)\b'; Select-String -Path 'Pages\Analysis\Index.cshtml' -Pattern $pattern -AllMatches
```

Expected: all tests pass and the scan returns no matches.

- [ ] **Step 5: Commit result hierarchy and responsive rules**

```powershell
git add Pages/Analysis/Index.cshtml wwwroot/css/site.css tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs
git commit -m "feat: refine analysis result dashboard cards"
```

### Task 4: Align fixture states and final dashboard contracts

**Files:**
- Modify: `Pages/Analysis/UiStateFixture.cshtml`
- Modify: `Pages/Analysis/UiStateFixture.cshtml.cs`
- Modify: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`
- Modify: `wwwroot/css/site.css`
- Reference: `wwwroot/js/analysis-form.js`

**Interfaces:**
- Consumes: Existing Testing-only fixture guard, `data-analysis-form`, `data-submit-button`, and `data-pending-status` hooks.
- Produces: A fixture that visually exercises the redesigned workspace at all seven states and contracts that protect its availability and state labels.

- [ ] **Step 1: Update the fixture to use dashboard wrappers**

Wrap the fixture’s state cards in the same classes used by production:

```cshtml
<section class="ui-state-fixture" aria-labelledby="ui-state-fixture-title">
    <div class="ui-state-fixture__grid">
        <section class="ui-state-fixture__state">
            <div class="analysis-workspace">
                <aside class="analysis-workspace__sidebar">
                    <h2>Wybierz aktywa</h2>
                    <div class="asset-option">AAPL</div>
                </aside>
                <div class="analysis-workspace__content">
                    <h2>Dashboard analizy</h2>
                    <article class="analysis-result-card">
                        <div class="analysis-result-card__metrics">
                            <div class="analysis-result-card__metric">
                                <dt>Score</dt>
                                <dd>82/100</dd>
                            </div>
                        </div>
                    </article>
                </div>
            </div>
        </section>
    </div>
</section>
```

Keep all seven labels and keep the fixture Testing-only.

- [ ] **Step 2: Strengthen fixture contract assertions**

Add assertions:

```csharp
Assert.Contains("analysis-workspace", html);
Assert.Contains("analysis-workspace__sidebar", html);
Assert.Contains("analysis-workspace__content", html);
Assert.Contains("analysis-result-card__metrics", html);
Assert.Contains("role=\"status\"", html);
```

- [ ] **Step 3: Run the final automated checks**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --verbosity minimal
dotnet build .bootstrap-scaffold.csproj --no-restore
$pattern = '#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(|oklch\(|-\[[0-9.]+(px|rem)\]|\b(bg|text|border|ring|outline|from|via|to|fill|stroke|shadow|divide)-(slate|gray|zinc|neutral|stone|red|orange|amber|yellow|lime|green|emerald|teal|cyan|sky|blue|indigo|violet|purple|fuchsia|pink|rose|white|black)\b'; Select-String -Path 'Pages\Analysis\Index.cshtml','Pages\Analysis\UiStateFixture.cshtml' -Pattern $pattern -AllMatches
```

Expected: 99 or more focused tests pass, the build has zero warnings/errors,
and the scan returns no matches.

- [ ] **Step 4: Perform the visual gate**

Inspect the Testing-only fixture and the authenticated analysis flow at:

- desktop width: sidebar beside a two-column result grid;
- narrow mobile width: expanded selector above single-column results;
- keyboard focus across navigation, assets, submit, and chart controls;
- loading, empty, error, partial, stale, insufficient-evidence, and completed
  result states.

- [ ] **Step 5: Commit the final state-contract changes**

```powershell
git add Pages/Analysis/UiStateFixture.cshtml Pages/Analysis/UiStateFixture.cshtml.cs wwwroot/css/site.css tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs
git commit -m "test: cover analysis dashboard visual states"
```

## Final verification

After all tasks:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --verbosity minimal
dotnet build .bootstrap-scaffold.csproj --no-restore
git diff --check
```

Expected: focused tests pass, production build has zero warnings and errors,
and no whitespace errors remain.
