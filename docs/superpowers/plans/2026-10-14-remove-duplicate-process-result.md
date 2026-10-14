# Remove Duplicate Process Result Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the duplicated process-result ranking so the hero remains the landing page's single illustrative result and the process section stays focused on its three workflow cards.

**Architecture:** Keep the existing Razor Pages and BEM-style CSS structure. The process section will render only the semantic workflow grid; the existing hero `analysis-card` remains unchanged as the sole illustrative ranking. Contract coverage will assert the stable workflow copy and the existing educational boundary.

**Tech Stack:** ASP.NET Core Razor Pages on .NET 10, ordinary CSS tokens in `wwwroot/css/site.css`, and xUnit integration contract tests using `WebApplicationFactory`.

## Global Constraints

- Keep the existing `#how-it-works` anchor and three workflow steps.
- Keep the hero ranking, educational disclaimer, authentication links, and static-page behavior unchanged.
- Remove only the duplicate `process-result` markup and its dedicated CSS/token rules.
- Use existing semantic CSS tokens and BEM naming; add no dependencies or screenshot runner.
- Preserve the responsive workflow grid at `1440x900` and `390x844` without horizontal overflow.

---

### Task 1: Remove the duplicate process-result markup

**Files:**
- Modify: `Pages/Index.cshtml:64-112`

**Interfaces:**
- Consumes: Existing `section#how-it-works`, `.workflow-grid`, and three `.workflow-card` elements.
- Produces: A process section whose direct content after `.section-heading` is only `.workflow-grid`.

- [ ] **Step 1: Confirm the current duplicate contract**

Run:

```powershell
rg -n "process-proof|process-result|workflow-grid|Wybierz aktywa|Sprawdź dane|Zrozum czynniki" Pages\Index.cshtml
```

Expected: `process-proof` and `process-result` appear in the process section, alongside all three workflow-card labels.

- [ ] **Step 2: Remove only the duplicate result article**

Keep the process section and workflow grid in this shape:

```cshtml
<section class="section" id="how-it-works" aria-labelledby="how-it-works-title">
    <div class="section-heading">
        <p class="eyebrow">Przejrzysty proces</p>
        <h2 id="how-it-works-title">Od pytania do lepiej poinformowanego spojrzenia.</h2>
        <p>Argus pokazuje dowody zamiast ukrywać sposób rozumowania za jednym sygnałem.</p>
    </div>
    <div class="workflow-grid">
        <article class="workflow-card">
            <span class="workflow-card__number">01</span>
            <h3>Wybierz aktywa</h3>
            <p>Wybierz aktywa, które chcesz zrozumieć i porównać.</p>
        </article>
        <article class="workflow-card">
            <span class="workflow-card__number">02</span>
            <h3>Sprawdź dane</h3>
            <p>Zobacz bieżący i historyczny kontekst w jednym miejscu.</p>
        </article>
        <article class="workflow-card">
            <span class="workflow-card__number">03</span>
            <h3>Zrozum czynniki</h3>
            <p>Poznaj sygnały i dowody, które wpływają na każdy wynik.</p>
        </article>
    </div>
</section>
```

- [ ] **Step 3: Verify the view has one illustrative ranking**

Run:

```powershell
$html = Get-Content -Raw Pages\Index.cshtml
if ($html -match "process-result|process-proof") { throw "Duplicate process result remains" }
if (($html | Select-String -Pattern "analysis-card" -AllMatches).Matches.Count -ne 1) { throw "Hero analysis card contract changed" }
Write-Output "PASS: process result removed and hero analysis card retained"
```

- [ ] **Step 4: Commit the markup change**

```powershell
git add -- Pages\Index.cshtml
git commit -m "refactor(landing-page-ui-refinement): remove duplicate process result markup"
```

Expected: A new commit containing only the view change.

### Task 2: Remove unused process-result styling

**Files:**
- Modify: `wwwroot/css/site.css:25-27,302-460`

**Interfaces:**
- Consumes: Existing `workflow-grid`, `.workflow-card`, responsive breakpoint, and mobile row rules.
- Produces: No unused `process-proof`, `process-result`, or process-result token selectors; the existing workflow grid remains token-backed.

- [ ] **Step 1: Confirm CSS selectors are now unused**

Run:

```powershell
rg -n "process-proof|process-result|process-result-column-min" Pages wwwroot tests
```

Expected after Task 1: matches exist only in `wwwroot/css/site.css` and the phase test until the cleanup is applied.

- [ ] **Step 2: Remove the dedicated process-result CSS**

Delete only:

```css
--process-proof-gap: var(--space-5);
--process-result-column-min: 18rem;
```

and the `.process-proof`, `.process-result`, `.process-result__header`,
`.process-result__label`, `.process-result__note`, `.process-result h3`,
`.process-result__ranking`, `.process-result__factors`, and related breakpoint
or mobile selector additions. Keep `.workflow-grid` and `.workflow-card`
rules unchanged.

- [ ] **Step 3: Verify the stylesheet has no orphaned selectors**

Run:

```powershell
$css = Get-Content -Raw wwwroot\css\site.css
if ($css -match "process-proof|process-result|process-result-column-min") { throw "Unused process-result CSS remains" }
Write-Output "PASS: duplicate process-result CSS removed"
```

- [ ] **Step 4: Commit the CSS cleanup**

```powershell
git add -- wwwroot/css/site.css
git commit -m "refactor(landing-page-ui-refinement): remove duplicate process result styles"
```

Expected: A new commit containing only the stylesheet cleanup.

### Task 3: Align landing contract tests with the simplified process section

**Files:**
- Modify: `tests/Argus.Authentication.Tests/AccountAccessContractTests.cs:20-45`

**Interfaces:**
- Consumes: The public `/` response from `WebApplicationFactory<Program>`.
- Produces: Stable assertions for the three workflow labels, hero illustrative data, and educational boundary without requiring the removed panel.

- [ ] **Step 1: Replace implementation-detail assertions with stable workflow assertions**

Keep the existing boundary and hero assertions, remove assertions for
`process-result` and `Ranking z wyjaśnieniem`, and add these content assertions:

```csharp
Assert.Contains("Wybierz aktywa", html);
Assert.Contains("Sprawdź dane", html);
Assert.Contains("Zrozum czynniki", html);
Assert.Contains("Dane ilustracyjne", html);
```

- [ ] **Step 2: Run the targeted contract tests**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore
```

Expected: 40 tests pass with 0 failures.

- [ ] **Step 3: Confirm the landing response has no duplicate result marker**

The existing public-entry test should additionally assert the removed marker is
absent:

```csharp
Assert.DoesNotContain("process-result", html);
```

This protects the intended simplification without asserting CSS structure.

- [ ] **Step 4: Run the targeted tests again**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore
```

Expected: 40 tests pass with 0 failures.

- [ ] **Step 5: Commit the contract update**

```powershell
git add -- tests/Argus.Authentication.Tests/AccountAccessContractTests.cs
git commit -m "test(landing-page-ui-refinement): cover simplified process section"
```

Expected: A new commit containing only the landing contract test update.

### Task 4: Perform visual and repository validation

**Files:**
- Verify: `Pages/Index.cshtml`
- Verify: `wwwroot/css/site.css`
- Verify: `tests/Argus.Authentication.Tests/AccountAccessContractTests.cs`

**Interfaces:**
- Consumes: The completed markup, CSS, and contract test changes from Tasks 1–3.
- Produces: Verified landing page behavior ready for the parent `landing-page-ui-refinement` phase review.

- [ ] **Step 1: Build the application**

Run:

```powershell
dotnet build .bootstrap-scaffold.csproj --no-restore
```

Expected: Build succeeds with 0 warnings and 0 errors.

- [ ] **Step 2: Start the Development server**

Run in PowerShell:

```powershell
dotnet build .bootstrap-scaffold.csproj --no-restore; $env:ASPNETCORE_ENVIRONMENT="Development"; dotnet .\bin\Debug\net10.0\.bootstrap-scaffold.dll --urls http://localhost:5027
```

Expected: The server listens on `http://localhost:5027`.

- [ ] **Step 3: Check the landing page at both agreed viewports**

Open `http://localhost:5027/` and verify:

- At `1440x900`, the hero ranking is the only result visualization and the three process cards remain visually balanced.
- At `390x844`, the process cards and hero remain readable without horizontal scrolling or clipped Polish labels.
- The `Jak to działa` CTA still reaches `#how-it-works`.
- The educational disclaimer and `Dane ilustracyjne` label remain visible.

- [ ] **Step 4: Stop the local server**

Press `Ctrl+C` in the server terminal after the manual checks.

- [ ] **Step 5: Inspect the final diff**

Run:

```powershell
git --no-pager diff HEAD~3..HEAD --stat
git --no-pager status --short
```

Expected: Only the intended landing view, stylesheet, contract test, and their
implementation-plan/spec commits are present; unrelated `.github` and
`.superpowers` paths remain outside the commits.

