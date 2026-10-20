# Visual Market History Range Selector Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a clean straight-line market-history chart with local `1M`, `3M`, `6M`, `1Y`, and `Max` range selection, defaulting to `1Y` while preserving the complete textual fallback.

**Architecture:** Keep the existing server-rendered SVG and provider-neutral `AnalysisChartPoint` data. Razor emits the complete point set and native range buttons; a dependency-free chart controller filters points locally, recalculates SVG geometry, and reuses the existing tooltip/focus behavior. CSS owns the clean-trend presentation and responsive controls.

**Tech Stack:** ASP.NET Core Razor Pages on .NET 10, C#, xUnit contract tests, dependency-free browser JavaScript, CSS custom properties.

## Global Constraints

- Keep the chart as a server-rendered SVG with a dependency-free local JavaScript controller.
- Draw straight line segments between observed points; do not smooth the line or invent intermediate values.
- Render the complete normalized history into the page so the text fallback remains complete and JavaScript can switch ranges locally.
- Use `1Y` as the default active range.
- Provide `1M`, `3M`, `6M`, `1Y`, and `Max` controls above the chart.
- Mark the active range with both visual styling and `aria-pressed`.
- Keep the full textual date/close list available below the chart and usable without JavaScript.
- Use `Max` or the nearest available history when a selected range cannot be represented by the available observations.
- Use CSS tokens for chart styling; do not add literal colors outside the existing token definitions.
- Do not add a chart library, server-side range endpoint, live refresh, zoom, pan, indicators, scoring changes, ranking changes, or provider changes.
- Preserve the educational-not-advice and illustrative-development-data wording.
- Preserve safe populated, limited-history, no-history, and JavaScript-disabled states.

---

## File Map

- Modify `Pages/Analysis/Index.cshtml`
  - Add the range-control markup and stable data hooks around each populated chart.
  - Keep all chart points and the complete text fallback rendered by Razor.
  - Keep the existing server-side geometry as the no-script fallback.
- Modify `wwwroot/js/analysis-chart.js`
  - Add local range selection, point filtering, SVG geometry recalculation, and active-button state.
  - Preserve hover, keyboard focus, touch selection, dismissal, and bounded tooltip placement.
- Modify `wwwroot/css/site.css`
  - Replace the visually heavy point treatment with the approved clean-trend hierarchy.
  - Add token-backed range-control, chart, focus, and mobile layout styles.
- Modify `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`
  - Add contract assertions for range controls, default `1Y`, accessibility hooks, script/style integration, and preserved fallback behavior.
- Create `docs/superpowers/plans/2026-10-20-visual-market-history-range-selector.md`
  - This implementation plan; no runtime behavior.

## Task 1: Add range-control and chart data contracts

**Files:**
- Modify: `Pages/Analysis/Index.cshtml:198-320`
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs:143-315`

**Interfaces:**
- Consumes: `result.ChartPoints`, `chartId`, `chartTitleId`, `chartDescriptionId`, and the existing `data-chart-point` metadata.
- Produces: a chart container with `data-chart-range`, five native buttons using `data-chart-range-option`, `aria-pressed="true"` on `1Y`, and a stable `data-chart-line` path hook for the JavaScript controller.

- [ ] **Step 1: Write failing contract assertions for range controls**

Add assertions to `Analysis_results_render_accessible_svg_geometry_and_text_fallback` after the existing tooltip assertions:

```csharp
Assert.Contains("data-chart-range", html);
Assert.Contains("aria-label=\"Zakres historii cenowej\"", html);
Assert.Equal(5, Regex.Matches(html, "data-chart-range-option").Count);
Assert.Contains("data-chart-range-option=\"1M\"", html);
Assert.Contains("data-chart-range-option=\"3M\"", html);
Assert.Contains("data-chart-range-option=\"6M\"", html);
Assert.Contains("data-chart-range-option=\"1Y\"", html);
Assert.Contains("data-chart-range-option=\"Max\"", html);
Assert.Contains("data-chart-range-option=\"1Y\"[^>]*aria-pressed=\"true\"", html);
Assert.Contains("data-chart-range-option=\"1M\"[^>]*aria-pressed=\"false\"", html);
```

Add a stylesheet/view integration assertion to
`Analysis_result_sources_keep_view_markup_color_free_and_css_token_backed`:

```csharp
Assert.Contains("data-chart-range-option", analysisView);
Assert.Contains(".analysis-result-card__chart-range", stylesheet);
Assert.Contains(".analysis-result-card__chart-range-button", stylesheet);
Assert.Contains(".analysis-result-card__chart-range-button[aria-pressed=\"true\"]", stylesheet);
```

- [ ] **Step 2: Run the focused test to verify it fails**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter FullyQualifiedName~Analysis_results_render_accessible_svg_geometry_and_text_fallback
```

Expected: FAIL because the current chart has no range controls.

- [ ] **Step 3: Add native range controls to the populated chart markup**

Inside the `if (chartPoints.Length > 0)` branch, directly after the chart description and before the `<svg>`, add:

```cshtml
<div class="analysis-result-card__chart-range"
     role="group"
     aria-label="Zakres historii cenowej"
     data-chart-range>
    @foreach (var range in new[] { "1M", "3M", "6M", "1Y", "Max" })
    {
        <button class="analysis-result-card__chart-range-button"
                type="button"
                data-chart-range-option="@range"
                aria-pressed="@(range == "1Y" ? "true" : "false")">
            @range
        </button>
    }
</div>
```

Do not filter `chartPoints` in Razor. The complete point set must remain in the SVG and in the text fallback so JavaScript-disabled users retain all available evidence.

- [ ] **Step 4: Add stable geometry metadata required by the controller**

Update the existing SVG and point markup without changing the provider contract:

```cshtml
<svg class="analysis-result-card__chart-svg"
     viewBox="0 0 640 240"
     preserveAspectRatio="xMidYMid meet"
     role="img"
     aria-labelledby="@chartTitleId @chartDescriptionId"
     data-chart-svg
     data-chart-width="640"
     data-chart-height="240"
     data-chart-left="@chartLeft.ToString("0.##", CultureInfo.InvariantCulture)"
     data-chart-right="@chartRight.ToString("0.##", CultureInfo.InvariantCulture)"
     data-chart-top="@chartTop.ToString("0.##", CultureInfo.InvariantCulture)"
     data-chart-bottom="@chartBottom.ToString("0.##", CultureInfo.InvariantCulture)">
```

Keep `data-chart-line` on the path and keep every point's invariant timestamp and formatted close attributes unchanged.

- [ ] **Step 5: Run the focused contract tests**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter FullyQualifiedName~AnalysisAccessContractTests
```

Expected: PASS, including the five range buttons and default `1Y` assertions.

- [ ] **Step 6: Commit the contract markup and tests**

```powershell
git add -- Pages\Analysis\Index.cshtml tests\Argus.Authentication.Tests\AnalysisAccessContractTests.cs
git commit -m "feat(analysis): add history range controls"
```

## Task 2: Implement local range filtering and SVG redraw

**Files:**
- Modify: `wwwroot/js/analysis-chart.js`
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs:270-315`

**Interfaces:**
- Consumes: `[data-chart-container]`, `[data-chart-svg]`, `[data-chart-line]`, `[data-chart-point]`, `[data-chart-range]`, and `[data-chart-range-option]`.
- Produces: an initialized chart controller that defaults to `1Y`, filters points by timestamp, redraws the straight path, hides points outside the active range, updates `aria-pressed`, and keeps tooltip/focus state consistent.

- [ ] **Step 1: Add failing static contract assertions for range-controller hooks**

Add to `Analysis_result_sources_keep_view_markup_color_free_and_css_token_backed`:

```csharp
var chartScript = File.ReadAllText(RepositoryPath("wwwroot", "js", "analysis-chart.js"));

Assert.Contains("data-chart-range-option", chartScript);
Assert.Contains("data-chart-timestamp", chartScript);
Assert.Contains("aria-pressed", chartScript);
Assert.Contains("data-chart-line", chartScript);
Assert.Contains("1Y", chartScript);
Assert.Contains("Max", chartScript);
```

These assertions protect the dependency-free controller contract without introducing a browser-test dependency.

- [ ] **Step 2: Run the focused test to verify the new assertions fail**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter FullyQualifiedName~Analysis_result_sources_keep_view_markup_color_free_and_css_token_backed
```

Expected: FAIL because the current script has no range-controller logic.

- [ ] **Step 3: Refactor the controller around explicit chart constants and point metadata**

In `wwwroot/js/analysis-chart.js`, preserve the existing IIFE and tooltip behavior, then add these helpers before `createController`:

```javascript
var RANGE_DAYS = {
    "1M": 31,
    "3M": 92,
    "6M": 183,
    "1Y": 365,
    "Max": null
};

function readPoint(point, index) {
    return {
        element: point,
        index: index,
        timestamp: new Date(point.getAttribute("data-chart-timestamp") || ""),
        close: Number(point.getAttribute("data-chart-close") || "0")
    };
}

function selectPoints(points, range) {
    if (range === "Max" || points.length === 0) {
        return points;
    }

    var latest = points[points.length - 1].timestamp.getTime();
    var cutoff = latest - (RANGE_DAYS[range] * 24 * 60 * 60 * 1000);
    var selected = points.filter(function (point) {
        return point.timestamp.getTime() >= cutoff;
    });

    return selected.length > 0 ? selected : points;
}
```

The controller must ignore malformed timestamps rather than throwing. If a
point timestamp is invalid, keep it in `Max` and in the fallback, but exclude
it from range cutoff comparisons.

- [ ] **Step 4: Add bounded geometry recalculation for the active point set**

Add a `redraw(selectedPoints)` function inside `createController` that:

1. Reads the existing geometry margins from `data-chart-*` SVG attributes.
2. Calculates min/max close from `selectedPoints`.
3. Places equal-value histories at the vertical midpoint.
4. Places one point at the horizontal midpoint.
5. Writes a straight `M... L...` path to `data-chart-line`.
6. Updates each circle's `cx`, `cy`, and `hidden` state.
7. Clears the active tooltip when its point is no longer selected.

Use this implementation shape:

```javascript
function redraw(selectedPoints) {
    var selectedSet = new Set(selectedPoints.map(function (point) {
        return point.element;
    }));
    var minimum = Math.min.apply(null, selectedPoints.map(function (point) { return point.close; }));
    var maximum = Math.max.apply(null, selectedPoints.map(function (point) { return point.close; }));
    var range = maximum - minimum;
    var path = [];

    pointRecords.forEach(function (point, index) {
        var isSelected = selectedSet.has(point.element);
        point.element.hidden = !isSelected;
        if (!isSelected) {
            return;
        }

        var selectedIndex = selectedPoints.indexOf(point);
        var x = selectedPoints.length === 1
            ? chartLeft + plotWidth / 2
            : chartLeft + (selectedIndex * plotWidth / (selectedPoints.length - 1));
        var normalized = range === 0 ? 0.5 : (point.close - minimum) / range;
        var y = chartTop + ((1 - normalized) * plotHeight);

        point.element.setAttribute("cx", x.toFixed(2));
        point.element.setAttribute("cy", y.toFixed(2));
        path.push((selectedIndex === 0 ? "M" : "L") + x.toFixed(2) + "," + y.toFixed(2));
    });

    line.hidden = selectedPoints.length < 2;
    line.setAttribute("d", path.join(" "));
}
```

Use the actual SVG attributes and records in the surrounding implementation; do not duplicate a second source of chart data.

- [ ] **Step 5: Add range-button activation and preserve interaction behavior**

Inside `createController`:

- collect `rangeButtons` with `[data-chart-range-option]`;
- set `activeRange = "1Y"`;
- add a click listener to every button;
- update every button's `aria-pressed`;
- call `redraw(selectPoints(pointRecords, activeRange))`;
- clear the selected tooltip before redrawing;
- leave hover, focus, touch, Escape, resize, and optional-markup guards working.

The initialization flow must end with:

```javascript
rangeButtons.forEach(function (button) {
    button.addEventListener("click", function () {
        activeRange = button.getAttribute("data-chart-range-option") || "1Y";
        rangeButtons.forEach(function (option) {
            option.setAttribute("aria-pressed",
                option === button ? "true" : "false");
        });
        clearActive();
        redraw(selectPoints(pointRecords, activeRange));
    });
});

redraw(selectPoints(pointRecords, activeRange));
```

- [ ] **Step 6: Run the focused tests and inspect the script contract**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter FullyQualifiedName~AnalysisAccessContractTests
```

Expected: PASS with the range-controller static assertions.

Also run:

```powershell
node --check wwwroot\js\analysis-chart.js
```

Expected: no syntax errors.

- [ ] **Step 7: Commit the local controller**

```powershell
git add -- wwwroot\js\analysis-chart.js tests\Argus.Authentication.Tests\AnalysisAccessContractTests.cs
git commit -m "feat(analysis): filter history chart ranges locally"
```

## Task 3: Apply the clean-trend responsive visual system

**Files:**
- Modify: `wwwroot/css/site.css:607-820`
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs:270-315`

**Interfaces:**
- Consumes: `.analysis-result-card__chart`, `.analysis-result-card__chart-svg`, `.analysis-result-card__chart-point`, `.analysis-result-card__chart-tooltip`, `.analysis-result-card__chart-range`, and `.analysis-result-card__chart-range-button`.
- Produces: a token-backed visual hierarchy with quiet points, visible active/focus state, compact controls, and no horizontal overflow at approximately 390px.

- [ ] **Step 1: Add failing CSS contract assertions**

Add:

```csharp
Assert.Contains(".analysis-result-card__chart-range", stylesheet);
Assert.Contains(".analysis-result-card__chart-range-button:hover", stylesheet);
Assert.Contains(".analysis-result-card__chart-range-button[aria-pressed=\"true\"]", stylesheet);
Assert.Contains(".analysis-result-card__chart-point:not([hidden])", stylesheet);
Assert.Contains("overflow-x: hidden", stylesheet);
```

Run the filtered contract test and confirm it fails before the CSS selectors exist.

- [ ] **Step 2: Replace heavy chart point styling with the clean-trend styles**

Keep every color token reference and add styles shaped like:

```css
.analysis-result-card__chart-range {
    display: flex;
    flex-wrap: wrap;
    gap: var(--space-1);
    align-items: center;
}

.analysis-result-card__chart-range-button {
    min-width: 3rem;
    min-height: 2.25rem;
    padding: var(--space-1) var(--space-2);
    border: 0.0625rem solid var(--color-border);
    border-radius: 999px;
    background: transparent;
    color: var(--color-muted);
    cursor: pointer;
    font: inherit;
    font-size: 0.8125rem;
    font-weight: 800;
}

.analysis-result-card__chart-range-button:hover,
.analysis-result-card__chart-range-button:focus-visible,
.analysis-result-card__chart-range-button[aria-pressed="true"] {
    border-color: var(--color-accent);
    background: var(--color-surface-raised);
    color: var(--color-text);
}

.analysis-result-card__chart-svg {
    aspect-ratio: 640 / 240;
    min-height: 0;
    overflow: hidden;
}

.analysis-result-card__chart-point {
    r: 2.5;
    opacity: 0.45;
    stroke-width: 1.5;
    transition: r 160ms ease, opacity 160ms ease, fill 160ms ease;
}

.analysis-result-card__chart-point:hover,
.analysis-result-card__chart-point.is-active,
.analysis-result-card__chart-point:focus-visible {
    r: 5;
    opacity: 1;
}
```

Do not add a fill area, gradient, heavy grid, or literal color. Keep the line straight and accent-colored.

- [ ] **Step 3: Add mobile-safe layout rules**

Inside the existing `@media (max-width: 40rem)` block, add:

```css
.analysis-result-card__chart-range {
    width: 100%;
}

.analysis-result-card__chart-range-button {
    flex: 1 1 3rem;
}

.analysis-result-card__chart-tooltip {
    max-width: calc(100% - var(--space-4));
}
```

Ensure `.analysis-result-card__chart` and the tooltip cannot create horizontal scrolling.

- [ ] **Step 4: Run static color and CSS contract checks**

Run:

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter FullyQualifiedName~Analysis_result_sources_keep_view_markup_color_free_and_css_token_backed
```

Expected: PASS.

Run:

```powershell
$content = Get-Content -Raw wwwroot\css\site.css
$tokenEnd = $content.IndexOf('}')
$outside = $content.Substring($tokenEnd + 1)
if ([regex]::IsMatch($outside, '(?i)(#[0-9a-f]{3,8}\b|\brgb\s*\(|\bhsl\s*\(')) { throw 'Literal color found outside :root.' }
```

Expected: no output and exit code 0.

- [ ] **Step 5: Commit the clean-trend styles**

```powershell
git add -- wwwroot\css\site.css tests\Argus.Authentication.Tests\AnalysisAccessContractTests.cs
git commit -m "feat(analysis): polish history chart presentation"
```

## Task 4: Run full verification and document manual checks

**Files:**
- Modify: `context/changes/visual-market-history-chart/plan.md` only if the 10x implementation workflow is resumed for this change.
- No runtime files.

**Interfaces:**
- Consumes: the completed Razor, JavaScript, CSS, and contract-test changes from Tasks 1–3.
- Produces: verified chart behavior and a clean handoff for the manual verification gate.

- [ ] **Step 1: Run the complete authentication test project**

```powershell
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --verbosity minimal
```

Expected: all tests pass.

- [ ] **Step 2: Build the production project**

```powershell
dotnet build .bootstrap-scaffold.csproj --no-restore --verbosity minimal
```

Expected: `Build succeeded`, `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 3: Run the JavaScript syntax check**

```powershell
node --check wwwroot\js\analysis-chart.js
```

Expected: no output and exit code 0.

- [ ] **Step 4: Perform manual verification at the local analysis route**

Open `http://localhost:5027/analysis`, authenticate, and verify:

1. The chart opens with `1Y` selected.
2. `1M`, `3M`, `6M`, `1Y`, and `Max` change the visible straight-line history.
3. `aria-pressed` follows the selected control when navigating by keyboard.
4. Hover and keyboard focus show the selected date and close in the tooltip.
5. Touch selection works on a narrow viewport.
6. A short history falls back to available observations without an empty fabricated trend.
7. No-history cards remain safe and do not show unusable range controls.
8. The complete text fallback remains understandable with JavaScript disabled.
9. The chart does not use prediction or recommendation language.
10. At approximately 390px wide, no horizontal scrollbar appears.

- [ ] **Step 5: Review the final diff**

```powershell
git --no-pager diff HEAD~3..HEAD -- Pages\Analysis\Index.cshtml wwwroot\js\analysis-chart.js wwwroot\css\site.css tests\Argus.Authentication.Tests\AnalysisAccessContractTests.cs
git status --short
```

Confirm unrelated dirty paths remain untouched and no secrets or provider changes were introduced.

