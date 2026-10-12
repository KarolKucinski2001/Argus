# Argus Landing Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the minimal public landing page with a responsive dark analytics presentation that explains Argus's educational value and leads users to the account flow without implying investment advice.

**Architecture:** Keep the page as a Razor Page. Put all visual tokens and reusable landing-page primitives in one static stylesheet, link that stylesheet from the shared layout, and keep `Pages/Index.cshtml` responsible for semantic structure and copy only. Use a static illustrative ranking card; do not add data calls or new endpoints.

**Tech Stack:** ASP.NET Core Razor Pages on .NET 10, semantic HTML, CSS custom properties, existing xUnit/integration test project, manual browser visual gate.

## Global Constraints

- Change only the public landing page and shared HTML shell; do not implement market analysis, persistence, new API endpoints, or a frontend framework.
- Use `wwwroot/css/site.css` as the single source for colors, spacing, typography, borders, focus treatment, buttons, cards, and responsive breakpoints.
- Keep the headline in the discovery direction: “Zrozum rynek, zanim wybierzesz aktywa”.
- Use “Poznaj sposób działania” as the primary CTA, linking to the workflow section.
- Mark example scores and ranking data as illustrative; they must not look like live recommendations.
- Preserve `/account/signin` for the account link and `/` for the Argus logo.
- Support desktop and narrow mobile layouts, keyboard focus, readable contrast, hover, and reduced-motion behavior.
- Do not add Tailwind, shadcn, JavaScript, a database, or a new UI test framework.

---

## File Map

- Create `wwwroot/css/site.css`: semantic design tokens, page layout, typography, cards, buttons, responsive rules, focus styles, and reduced-motion behavior.
- Modify `Pages/Shared/_Layout.cshtml`: load the stylesheet and retain the existing Razor layout and routing.
- Modify `Pages/Index.cshtml`: add the semantic landing-page sections, static example ranking, workflow explanation, disclaimer, and valid links.
- Verify `tests/Argus.Authentication.Tests/`: existing contract tests continue to pass; no new test project is needed.
- Verify `docs/superpowers/specs/2026-10-12-landing-page-design.md`: implementation remains within the approved scope.

## Task 1: Add the shared visual token and component stylesheet

**Files:**
- Create: `wwwroot/css/site.css`

**Interfaces:**
- Produces semantic classes consumed by `Pages/Shared/_Layout.cshtml` and `Pages/Index.cshtml`.
- Exposes tokens through `:root` custom properties, including `--color-bg`, `--color-surface`, `--color-surface-raised`, `--color-text`, `--color-muted`, `--color-accent`, `--color-border`, `--space-*`, and `--radius-*`.

- [ ] **Step 1: Create the stylesheet with tokens and base rules**

Create `wwwroot/css/site.css` with:

```css
:root {
    --color-bg: #0d1726;
    --color-surface: #14243a;
    --color-surface-raised: #1b304c;
    --color-text: #eef6ff;
    --color-muted: #a9bdd7;
    --color-accent: #6ee7d8;
    --color-accent-strong: #38c7b5;
    --color-border: #2b4665;
    --color-warning-surface: #1d2b3d;
    --space-1: 0.5rem;
    --space-2: 0.75rem;
    --space-3: 1rem;
    --space-4: 1.5rem;
    --space-5: 2rem;
    --space-6: 3rem;
    --space-7: 5rem;
    --radius-sm: 0.5rem;
    --radius-md: 0.875rem;
    --radius-lg: 1.25rem;
    --content-width: 72rem;
    color-scheme: dark;
    font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
}

* {
    box-sizing: border-box;
}

html {
    scroll-behavior: smooth;
}

body {
    min-width: 20rem;
    margin: 0;
    background: var(--color-bg);
    color: var(--color-text);
    line-height: 1.5;
}

a {
    color: inherit;
}

a:focus-visible,
button:focus-visible {
    outline: 0.1875rem solid var(--color-accent);
    outline-offset: 0.25rem;
}

.site-header,
.landing-main {
    width: min(100% - 2rem, var(--content-width));
    margin-inline: auto;
}

.site-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding-block: var(--space-4);
}

.brand {
    color: var(--color-text);
    font-size: 1.125rem;
    font-weight: 800;
    letter-spacing: 0.08em;
    text-decoration: none;
    text-transform: uppercase;
}

.site-nav {
    display: flex;
    align-items: center;
    gap: var(--space-4);
}

.text-link {
    color: var(--color-muted);
    text-decoration: none;
}

.text-link:hover {
    color: var(--color-text);
}

.landing-hero {
    display: grid;
    align-items: center;
    gap: var(--space-6);
    min-height: 34rem;
    padding-block: var(--space-7);
}

.landing-hero__copy,
.landing-hero__preview {
    min-width: 0;
}

.eyebrow {
    margin: 0 0 var(--space-2);
    color: var(--color-accent);
    font-size: 0.75rem;
    font-weight: 800;
    letter-spacing: 0.14em;
    text-transform: uppercase;
}

.landing-hero h1 {
    max-width: 12ch;
    margin: 0;
    font-size: clamp(2.75rem, 7vw, 5.5rem);
    line-height: 0.98;
    letter-spacing: -0.06em;
}

.hero-lede {
    max-width: 36rem;
    margin: var(--space-4) 0 0;
    color: var(--color-muted);
    font-size: clamp(1.05rem, 2vw, 1.25rem);
}

.hero-actions {
    display: flex;
    flex-wrap: wrap;
    gap: var(--space-3);
    align-items: center;
    margin-top: var(--space-5);
}

.button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-height: 2.875rem;
    padding: 0.75rem 1.125rem;
    border: 0.0625rem solid transparent;
    border-radius: 999px;
    background: var(--color-accent);
    color: #09201d;
    font-weight: 800;
    text-decoration: none;
    transition: background-color 160ms ease, transform 160ms ease;
}

.button:hover {
    background: var(--color-accent-strong);
    transform: translateY(-0.125rem);
}

.button--secondary {
    border-color: var(--color-border);
    background: transparent;
    color: var(--color-text);
}

.button--secondary:hover {
    background: var(--color-surface);
}

.analysis-card,
.workflow-card,
.boundary-card {
    border: 0.0625rem solid var(--color-border);
    border-radius: var(--radius-lg);
    background: linear-gradient(145deg, var(--color-surface-raised), var(--color-surface));
    box-shadow: 0 1.5rem 4rem rgb(0 0 0 / 22%);
}

.analysis-card {
    padding: var(--space-4);
}

.analysis-card__header,
.analysis-row,
.factor-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: var(--space-3);
}

.analysis-card__header {
    padding-bottom: var(--space-3);
    border-bottom: 0.0625rem solid var(--color-border);
}

.analysis-card__label,
.analysis-card__note {
    color: var(--color-muted);
    font-size: 0.8125rem;
}

.analysis-card__label {
    font-weight: 800;
    letter-spacing: 0.08em;
    text-transform: uppercase;
}

.analysis-row {
    padding-block: var(--space-3);
    border-bottom: 0.0625rem solid rgb(169 189 215 / 16%);
}

.analysis-row__asset {
    display: flex;
    gap: var(--space-3);
    align-items: center;
}

.asset-symbol {
    display: grid;
    width: 2.25rem;
    height: 2.25rem;
    place-items: center;
    border-radius: 50%;
    background: var(--color-bg);
    color: var(--color-accent);
    font-size: 0.75rem;
    font-weight: 800;
}

.analysis-row__asset small {
    display: block;
    color: var(--color-muted);
}

.score {
    color: var(--color-accent);
    font-size: 1.375rem;
    font-weight: 800;
}

.analysis-card__factors {
    display: grid;
    gap: var(--space-2);
    margin-top: var(--space-4);
}

.factor-row {
    color: var(--color-muted);
    font-size: 0.875rem;
}

.factor-row strong {
    color: var(--color-text);
}

.section {
    padding-block: var(--space-7);
}

.section-heading {
    max-width: 38rem;
    margin-bottom: var(--space-5);
}

.section-heading h2 {
    margin: 0;
    font-size: clamp(2rem, 4vw, 3.5rem);
    line-height: 1.05;
}

.section-heading p {
    color: var(--color-muted);
}

.workflow-grid {
    display: grid;
    gap: var(--space-3);
}

.workflow-card {
    padding: var(--space-4);
}

.workflow-card__number {
    color: var(--color-accent);
    font-size: 0.8125rem;
    font-weight: 800;
}

.workflow-card h3 {
    margin: var(--space-3) 0 var(--space-1);
}

.workflow-card p {
    margin: 0;
    color: var(--color-muted);
}

.boundary-card {
    padding: var(--space-4);
    background: var(--color-warning-surface);
}

.boundary-card h2 {
    margin-top: 0;
}

.boundary-card p {
    max-width: 48rem;
    margin-bottom: 0;
    color: var(--color-muted);
}

@media (min-width: 48rem) {
    .landing-hero {
        grid-template-columns: minmax(0, 1fr) minmax(22rem, 0.85fr);
    }

    .workflow-grid {
        grid-template-columns: repeat(3, 1fr);
    }
}

@media (prefers-reduced-motion: reduce) {
    html {
        scroll-behavior: auto;
    }

    *,
    *::before,
    *::after {
        transition-duration: 0.01ms;
        animation-duration: 0.01ms;
    }
}
```

- [ ] **Step 2: Check the stylesheet for forbidden one-off values**

Run:

```powershell
rg -n "#[0-9a-fA-F]{3,8}|rgb\\(|hsl\\(|!important" wwwroot\css\site.css
```

Expected: color values appear only in the token declarations and the intentional
focus/reduced-motion rules; no `!important` is present in authored rules.

- [ ] **Step 3: Commit the stylesheet**

```powershell
git add wwwroot\css\site.css
git commit -m "style: add Argus landing page design tokens"
```

## Task 2: Wire the shared layout shell

**Files:**
- Modify: `Pages/Shared/_Layout.cshtml`

**Interfaces:**
- Consumes: `wwwroot/css/site.css`.
- Produces: a page shell with `.site-header`, `.brand`, `.site-nav`, and `.landing-main` hooks used by the landing page.

- [ ] **Step 1: Replace the unstyled shell with the semantic layout**

Update the layout to:

```cshtml
<!DOCTYPE html>
<html lang="pl">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Argus</title>
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
    <header class="site-header">
        <a class="brand" asp-page="/Index">Argus</a>
        <nav class="site-nav" aria-label="Nawigacja główna">
            <a class="text-link" href="#how-it-works">Jak to działa</a>
            <a class="text-link" asp-page="/Account/SignIn">Zaloguj się</a>
        </nav>
    </header>
    <main class="landing-main">
        @RenderBody()
    </main>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

Use the existing route helper for the sign-in page rather than hard-coding a
controller route. The `href="#how-it-works"` link is valid on the landing page
and will be covered by the visual/manual check.

- [ ] **Step 2: Build the project**

Run:

```powershell
dotnet build .bootstrap-scaffold.csproj --no-restore
```

Expected: build succeeds with zero errors and zero warnings.

- [ ] **Step 3: Commit the shell wiring**

```powershell
git add Pages\Shared\_Layout.cshtml
git commit -m "feat: wire Argus landing page shell"
```

## Task 3: Build the semantic landing page

**Files:**
- Modify: `Pages/Index.cshtml`

**Interfaces:**
- Consumes: the classes from `wwwroot/css/site.css` and links from the shared shell.
- Produces: a static public page with a hero, illustrative ranking, workflow section, disclaimer, and account CTA.

- [ ] **Step 1: Replace the starter content with the landing-page structure**

Replace `Pages/Index.cshtml` with:

```cshtml
@page
@model _bootstrap_scaffold.Pages.IndexModel
@{
    Layout = "_Layout";
    ViewData["Title"] = "Zrozum rynek";
}

<div class="landing-page">
    <section class="landing-hero" aria-labelledby="hero-title">
        <div class="landing-hero__copy">
            <p class="eyebrow">Edukacyjna analiza rynku</p>
            <h1 id="hero-title">Zrozum rynek, zanim wybierzesz aktywa.</h1>
            <p class="hero-lede">
                Argus łączy kontekst rynkowy, wyjaśnione czynniki i dane historyczne,
                aby początkujący mogli lepiej zrozumieć, co wpływa na wynik aktywa.
            </p>
            <div class="hero-actions">
                <a class="button" href="#how-it-works">Poznaj sposób działania</a>
                <a class="button button--secondary" asp-page="/Account/SignIn">Zaloguj się lub utwórz konto</a>
            </div>
        </div>

        <div class="landing-hero__preview">
            <article class="analysis-card" aria-labelledby="analysis-preview-title">
                <div class="analysis-card__header">
                    <div>
                        <p class="analysis-card__label">Przykładowy podgląd</p>
                        <h2 id="analysis-preview-title">Opportunity Score</h2>
                    </div>
                        <span class="analysis-card__note">Dane ilustracyjne</span>
                </div>

                <div class="analysis-row">
                    <div class="analysis-row__asset">
                        <span class="asset-symbol" aria-hidden="true">BTC</span>
                        <span>Bitcoin<small>BTC/USD</small></span>
                    </div>
                    <span class="score">82</span>
                </div>
                <div class="analysis-row">
                    <div class="analysis-row__asset">
                        <span class="asset-symbol" aria-hidden="true">ETH</span>
                        <span>Ethereum<small>ETH/USD</small></span>
                    </div>
                    <span class="score">74</span>
                </div>
                <div class="analysis-row">
                    <div class="analysis-row__asset">
                        <span class="asset-symbol" aria-hidden="true">MS</span>
                        <span>Microsoft<small>MSFT</small></span>
                    </div>
                    <span class="score">68</span>
                </div>

                <div class="analysis-card__factors" aria-label="Przykładowe czynniki wpływające na wynik">
                    <div class="factor-row"><span>Niedawny impet</span><strong>Pozytywny</strong></div>
                    <div class="factor-row"><span>Zakres historyczny</span><strong>Mieszany</strong></div>
                    <div class="factor-row"><span>Aktualność danych</span><strong>Bieżąca</strong></div>
                </div>
            </article>
        </div>
    </section>

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

    <section class="section" aria-labelledby="boundary-title">
        <div class="boundary-card">
            <p class="eyebrow">Ważne ograniczenie</p>
            <h2 id="boundary-title">Ucz się z danych. Podejmuj własne decyzje.</h2>
            <p>
                Argus udostępnia analizę edukacyjną i nie oferuje spersonalizowanych
                porad inwestycyjnych ani gwarancji przyszłych wyników. Oceny i przykłady
                na tej stronie są ilustracyjne.
            </p>
        </div>
    </section>
</div>
```

Keep the copy explicit about educational use. Do not describe an asset as a
recommendation, winner, guarantee, or expected return.

- [ ] **Step 2: Build and run the focused authentication tests**

Run:

```powershell
dotnet build .bootstrap-scaffold.csproj --no-restore
dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore
```

Expected: build succeeds with zero errors and the existing authentication,
account-access, and market-data contract tests pass.

- [ ] **Step 3: Commit the landing page**

```powershell
git add Pages\Index.cshtml
git commit -m "feat: redesign Argus landing page"
```

## Task 4: Perform the visual and accessibility gate

**Files:**
- Verify: `Pages/Index.cshtml`
- Verify: `Pages/Shared/_Layout.cshtml`
- Verify: `wwwroot/css/site.css`
- Verify: `docs/superpowers/specs/2026-10-12-landing-page-design.md`

**Interfaces:**
- Consumes: the completed public page.
- Produces: verified desktop/mobile behavior and documented UI findings for the M2L5 checklist.

- [ ] **Step 1: Start the local application**

Run:

```powershell
dotnet run --project .bootstrap-scaffold.csproj
```

Open the development URL from `Properties\launchSettings.json`.

- [ ] **Step 2: Check the required UI states and routes**

Verify manually:

1. At desktop width, the hero copy and illustrative ranking render side by side.
2. At a narrow mobile width, the hero stacks without horizontal scrolling.
3. The “See how Argus works” link scrolls to `#how-it-works`.
4. The “Sign in or create an account” link opens `/account/signin`.
5. The Argus logo returns to `/`.
6. Tab navigation exposes a visible focus ring on every link.
7. Hover states do not hide text or reduce contrast.
8. The page clearly labels the ranking as illustrative and educational.

- [ ] **Step 3: Run the M2L5 hardcoded-value and scope checks**

Run:

```powershell
rg -n "#[0-9a-fA-F]{3,8}|rgb\\(|hsl\\(" Pages wwwroot\css
rg -n "tailwind|shadcn|Opportunity Score|not investment advice" Pages wwwroot\css
```

Expected: literal colors are confined to `wwwroot/css/site.css` token/base
definitions, no new UI framework is present, and the view contains the
illustrative-score and educational-boundary language.

- [ ] **Step 4: Stop at the implementation review gate**

Record any visual findings in the change's review notes and run
`/10x-impl-review` before treating the landing page as complete. Do not expand
the change into dashboard, scoring, AI, or provider work.
