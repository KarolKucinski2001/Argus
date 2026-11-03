# Argus UI and Content Clarity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Implement the approved UI/content clarity spec
(`docs/superpowers/specs/2026-11-02-argus-ui-content-clarity-design.md`).

**Architecture:** Surgical Razor and copy changes. Profile explanation copy
lives as constants in `CuratedProfileCatalog` (presenter/catalog-derived copy
rule); views only bind. Contract tests use the existing
`WebApplicationFactory` fixtures.

**Tech Stack:** ASP.NET Core Razor Pages (.NET 10), xUnit, `Microsoft.AspNetCore.Mvc.Testing`.

## Global Constraints

- Use semantic tokens/existing classes from `wwwroot/css/site.css`; no literal colors or arbitrary values in views.
- Copy is Polish, descriptive, never advice (`kup|sprzedaj|buy|sell` forbidden).
- Do not claim any market-data provider is approved for public production display.
- No change to routes, authentication, persistence, or calculations.
- Test command: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj -c Release`.

---

### Task 1: Landing page — signed-in CTA and "Źródła danych"

**Files:**
- Modify: `Pages/Index.cshtml` (hero actions; new section before boundary section)
- Test: `tests/Argus.Authentication.Tests/AccountAccessContractTests.cs`

- [x] **Step 1: Write failing tests** — add to `AccountAccessContractTests`:

```csharp
[Fact]
public async Task Public_entry_lists_data_sources_before_boundary()
{
    await using var factory = new AccountAccessFactory();
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });

    var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

    Assert.Contains("Źródła danych", html);
    Assert.Contains("data-data-source=\"market\"", html);
    Assert.Contains("data-data-source=\"profiles\"", html);
    Assert.Contains("data-data-source=\"ai\"", html);
    Assert.Contains("SEC 13F", html);
    Assert.True(
        html.IndexOf("Źródła danych", StringComparison.Ordinal) <
        html.IndexOf("Ważne ograniczenie", StringComparison.Ordinal));
}

[Fact]
public async Task Signed_in_entry_links_to_dashboard_instead_of_sign_in()
{
    await using var factory = new WatchlistTestFactory();
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });

    var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));

    Assert.Contains("href=\"/dashboard\">Przejdź do Dashboardu", html);
    Assert.DoesNotContain("Zaloguj się lub utwórz konto", html);
}
```

- [x] **Step 2: Run** `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj -c Release --filter "FullyQualifiedName~AccountAccessContractTests"` — expect the two new tests to FAIL.

- [x] **Step 3: Implement** — replace the secondary hero link:

```cshtml
@if (User.Identity?.IsAuthenticated == true)
{
    <a class="button button--secondary" asp-page="/Dashboard/Index">Przejdź do Dashboardu</a>
}
else
{
    <a class="button button--secondary" asp-page="/Account/SignIn">Zaloguj się lub utwórz konto</a>
}
```

Insert before the `boundary-title` section:

```cshtml
<section class="section" id="data-sources" aria-labelledby="data-sources-title">
    <div class="section-heading">
        <p class="eyebrow">Źródła danych</p>
        <h2 id="data-sources-title">Skąd pochodzą dane i co z nich bierzemy.</h2>
        <p>Każdy widok pokazuje, na jakich danych się opiera. Nie ukrywamy przybliżeń.</p>
    </div>
    <div class="workflow-grid">
        <article class="workflow-card source-card" data-data-source="market">
            <h3>Dane rynkowe</h3>
            <p>Ceny, zmiany 24h, 7 i 30 dni oraz historia cen. Dane są ujednolicane przed analizą. W tej wersji publicznej pokazujemy dane ilustracyjne — produkcyjny dostawca danych czeka na zatwierdzenie licencji.</p>
        </article>
        <article class="workflow-card source-card" data-data-source="profiles">
            <h3>Profile publiczne</h3>
            <p>Przybliżone składy portfeli z publicznych zgłoszeń SEC 13F oraz edukacyjne ilustracje oparte na publicznych opisach strategii. Wagi są zaokrąglone i nie zastępują oficjalnych dokumentów.</p>
        </article>
        <article class="workflow-card source-card" data-data-source="ai">
            <h3>Podsumowania AI</h3>
            <p>Podsumowania powstają wyłącznie z danych widocznych na stronie wyników. Nie dodają informacji spoza nich.</p>
        </article>
    </div>
</section>
```

(`source-card` is a hook only; the existing assertion counting exact `class="workflow-card"` stays at 3.)

- [x] **Step 4: Run** the same filter — expect PASS (including the existing `Public_entry_renders_purpose_and_educational_boundary`).

- [x] **Step 5: Commit** — `git add Pages/Index.cshtml tests/Argus.Authentication.Tests/AccountAccessContractTests.cs; git commit -m "feat: add data sources and signed-in CTA to landing"`

### Task 2: Dashboard — remove duplicate analysis actions

**Files:**
- Modify: `Pages/Dashboard/Index.cshtml` (watchlist table "Analiza" column; insights header link)
- Test: `tests/Argus.Authentication.Tests/DashboardAndStrategiesContractTests.cs`

Decision: keep both "Zobacz wszystkie" links, keep the empty-state "Przejdź do analizy" (the only action when the list is empty), keep asset-name links in insights. Remove the per-row "Analiza" column and the insights header "Przejdź do analizy".

- [x] **Step 1: Write failing test** — in `Dashboard_renders_watchlist_rows_and_insights_for_saved_assets` append:

```csharp
Assert.DoesNotContain(">Analiza</th>", html);
Assert.DoesNotContain(">Analiza</a>", html);
Assert.DoesNotContain("Przejdź do analizy", html);
Assert.Contains("Zobacz wszystkie", html);
```

- [x] **Step 2: Run** with `--filter "FullyQualifiedName~DashboardAndStrategiesContractTests"` — expect FAIL.

- [x] **Step 3: Implement** — delete `<th scope="col" class="watchlist-table__number">Analiza</th>`, the matching `<td class="watchlist-table__number"><a asp-page="/Analysis/Index">Analiza</a></td>`, and `<a asp-page="/Analysis/Index">Przejdź do analizy</a>` inside the `dashboard-insights-title` header.

- [x] **Step 4: Run** the filter — expect PASS (empty-state test still finds its analysis link).

- [x] **Step 5: Commit** — `git commit -am "feat: remove duplicate dashboard analysis actions"`

### Task 3: Strategy profiles — explain provenance and approximate data

**Files:**
- Modify: `Strategies/CuratedProfileCatalog.cs` (two new constants after `DataNote`)
- Modify: `Pages/Strategies/Profiles/Index.cshtml`, `Pages/Strategies/Profiles/Detail.cshtml`
- Test: `tests/Argus.Authentication.Tests/StrategyProfileTests.cs`

**Interfaces — Produces:** `CuratedProfileCatalog.ProvenanceNote`, `CuratedProfileCatalog.ApproximateExplanation` (`public const string`).

- [x] **Step 1: Write failing test** — in `Profiles_list_and_detail_show_disclaimer_and_as_of_date`, inside the `foreach`:

```csharp
Assert.Contains(CuratedProfileCatalog.ProvenanceNote, html);
Assert.Contains(CuratedProfileCatalog.ApproximateExplanation, html);
```

- [x] **Step 2: Run** `--filter "FullyQualifiedName~StrategyPortfolioContractTests"` — expect compile FAIL.

- [x] **Step 3: Implement** constants:

```csharp
public const string ProvenanceNote =
    "Profile publiczne to edukacyjne przykłady składu portfeli opisanych w publicznych źródłach, takich jak zgłoszenia SEC 13F lub publiczne opisy strategii. Argus nie jest powiązany z ich autorami ani zarządzającymi.";

public const string ApproximateExplanation =
    "„Przykładowe, przybliżone dane” oznacza, że wagi są zaokrąglone i przeliczone tylko na pokazane pozycje. Służą do nauki i porównań, a nie do odtworzenia oficjalnego zgłoszenia.";
```

List view: after the `Disclaimer` note add
`<p class="account-note" role="note">@CuratedProfileCatalog.ProvenanceNote @CuratedProfileCatalog.ApproximateExplanation</p>`.
Detail view: after the `Disclaimer/DataNote` note add the same paragraph.

- [x] **Step 4: Run** the filter plus `~StrategyProfileTests` — expect PASS; advice-wording tests unaffected.

- [x] **Step 5: Commit** — `git commit -am "feat: explain public profile provenance and approximate data"`

### Task 4: Watchlist — rename movers heading

**Files:**
- Modify: `Pages/Watchlist/Index.cshtml:77`
- Test: `tests/Argus.Authentication.Tests/WatchlistContractTests.cs`

- [x] **Step 1: Write failing test**:

```csharp
[Fact]
public async Task Watchlist_movers_heading_uses_clear_polish_label()
{
    await using var factory = new WatchlistTestFactory();
    await factory.Store.SaveAsync("test-subject", ["AAPL"]);
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });

    var html = WebUtility.HtmlDecode(await client.GetStringAsync("/watchlist"));

    Assert.Contains(">Największe zmiany</h2>", html);
    Assert.DoesNotContain("ruszyciele", html);
}
```

- [x] **Step 2: Run** `--filter "FullyQualifiedName~WatchlistContractTests"` — expect FAIL.
- [x] **Step 3: Implement** — change heading text to `Największe zmiany`.
- [x] **Step 4: Run** — expect PASS.
- [x] **Step 5: Commit** — `git commit -am "fix: rename watchlist movers heading"`

### Task 5: Full verification and review

- [x] Run full test project in Release; expect 0 failures.
- [x] Run the app and inspect `/` (anonymous and signed-in), `/dashboard`, `/watchlist`, `/strategies/profiles`, `/strategies/profiles/berkshire-style`.
- [x] Review the diff against the spec and report to the user.
