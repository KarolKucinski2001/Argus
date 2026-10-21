# Analysis Insight Dashboard Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the user-approved polish round to `/analysis`:
- own asset glyphs and per-asset accents
- short names
- a big score block with a band
- factor words
- a past-tense takeaway sentence
- no price table
- a 2/4/1-column results grid
- a smaller sidebar with highlighted selection
- the "Generuj ranking" button
- a header summary strip
- denser cards (~30–40% shorter)

**Architecture:** All new derived data is computed by pure, unit-tested C#:
- `AssetIdentityCatalog` provides names, monograms and accents.
- `AnalysisInsightPresenter` provides bands, factor words and the takeaway.
- The new `AnalysisSummaryStripPresenter` provides the summary strip.

The Razor partials only render that data:
- `_AssetGlyph`, `_AssetOption` and `_SummaryStrip` are new.
- `_InsightCard` and `_PriceHistoryChart` are modified.

All colors and sizes are tokens in `wwwroot/css/site.css`. The Testing-only UI fixture gets a `?view=ranked` mode, which renders four ranked cards in the real results grid. This lets the 2×2 and 4-column layouts be screenshot.

**Tech Stack:** ASP.NET Core Razor Pages (.NET 10), xUnit + `WebApplicationFactory`, plain CSS tokens, Playwright (scratch screenshots only, untracked).

**Spec:** `docs/superpowers/specs/2026-10-21-analysis-insight-dashboard-polish.md` (addendum) on top of `docs/superpowers/specs/2026-10-21-analysis-insight-dashboard-design.md`.

**Baseline (measured before planning):** in `/analysis/ui-state-fixture` at a 1440×900 viewport, the "Completed" insight card is **545px** tall (606px wide). The density target is **≤ 382px** (−30%) for the same card.

## Global Constraints

- Do not change `Analysis/OpportunityScorer.cs`, `Analysis/AnalysisContracts.cs` or `Analysis/AnalysisService.cs`.
- No client-side chart library. Glyphs and sparklines are server-rendered inline SVG.
- Asset glyphs are original, simple geometric shapes (circles/rects/ellipses). Do not use third-party logo files or traced copies of trademarked logos.
- Views contain no literal colors and no arbitrary layout values. Colors, sizes and spacing are tokens/classes in `wwwroot/css/site.css`. SVG `viewBox` drawing coordinates are allowed, as for the existing sparkline. SVG shapes carry no `fill`/`stroke` attributes; CSS sets `fill: currentColor`.
- Keep every CSS class currently asserted by the tests unless this plan removes the element (only the price table `analysis-result-card__chart-points`). Exact-match assertions that must keep passing:
  - `class="insight-card__monogram"`
  - `class="asset-option__monogram"`
  - `class="asset-option ui-state-fixture__focus-target"`
  - `class="analysis-results__list"`
  - `class="analysis-result-card__chart-point"` (the SVG circles)
- In factor bars, available bars must contain no `aria-hidden="true"`. Unavailable bars keep `aria-hidden="true"` on the `<meter>`.
- UI copy is Polish. New labels used by this plan:
  - "Nowa analiza", "Generuj ranking"
  - "Silny", "Umiarkowany", "Słaby"
  - "Rosnący", "Stabilny", "Malejący", "n/d"
  - "Przeanalizowane aktywa", "Najlepszy wynik 7d", "Najsłabszy wynik 7d", "Średni score"
  - "Wniosek z danych" (label kept)
- Never render "chwilowo niedostępne", "prognoz", "buy", "guaranteed", "best investment" or "top opportunity". The contract tests check "buy", "provider" and "exception" case-insensitively over the whole page, so none of these substrings may appear in new copy, classes or attributes.
- Never stage `.superpowers/**`, `.github/.10x-cli-manifest.json` or `.github/skills/10x-ui/`. Stage only the files listed in each task's commit step.
- Every commit message ends with the trailer `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`.
- Build: `dotnet build .bootstrap-scaffold.csproj --no-restore` (0 warnings expected). Tests: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`.
- **Environment gotcha:** another user's process may run `bin\Debug\net10.0\.bootstrap-scaffold.dll` and lock it, causing MSB3027 "file is locked". Do **not** stop that process. If the build or tests fail with MSB3027, rerun the same command with `-c Release` appended. Release output goes to `bin\Release` and is not locked.

### Spec clarifications (decided while planning)

- **Takeaway wording:** always a past-tense sentence about the price, built from the strongest factor (largest absolute % change; ties go to the earlier factor in 24h → 7d → 30d order), plus a rank suffix. Exact formats are in Task 1. The spec example "Silny trend 30d — …" is illustrative only; "Silny" is reserved for the score band.
- **Takeaway position:** keeps its place as the last visible line above the collapsed "Szczegóły i metodologia" disclosure, inside the existing `insight-card__insight` block. The "Wniosek z danych" label is kept, rendered inline.
- **Rank/score accessibility:** the visible "#1" and big score number are `aria-hidden`. Visually hidden text "Miejsce N" and "Score X/100" keeps the screen-reader wording and the existing test assertions.
- **Glyph wrapping:** the glyph partial is wrapped in the existing `insight-card__monogram` / `asset-option__monogram` spans so the exact-class assertions keep passing.
- **Glyph coverage:** AAPL and MSFT get SVG glyphs; BTC-USD (₿) and ETH-USD (Ξ) render their monogram character on the accent background. NVDA and TSLA use the letter monogram on the equity accent. Unknown tickers use the letter monogram on a neutral surface.
- **Sidebar option:** glyph, short name, ticker beneath (spec change 7), and the score only, without rank.
- **Summary strip:**
  - The count includes every result, failed ones too.
  - Best and worst use results without an error and with an available 7-day change. Best is the first maximum; worst is the last minimum.
  - Worst is "n/d" when fewer than two 7-day changes exist.
  - The average uses scored results only, rounded to 1 decimal (away from zero), and is "n/d" when none are scored.
- **Grid:**
  - 1 column by default.
  - 2 columns when the results container is ≥ 46rem wide (container query).
  - 4 columns at viewport ≥ 112.5rem (1800px; media query). At that width the main column widens to `--workspace-wide-width` (120rem) so four cards fit.
- **Ranked fixture:** a separate `?view=ranked` mode of the fixture page, so element ids (`analysis-result-AAPL`, `analysis-chart-AAPL`) never collide with the state cells.

---

## File Structure

| File | Responsibility |
| --- | --- |
| `Analysis/AssetIdentityCatalog.cs` (modify) | Short names, monograms, `AssetAccent`, `AccentClass(...)`. |
| `Analysis/AnalysisInsightPresenter.cs` (modify) | Bands, factor directions/words, takeaway sentence, public `ToneFor`. |
| `Analysis/AnalysisSummaryStrip.cs` (create) | Pure summary-strip model + builder. |
| `Pages/Analysis/InsightCardModel.cs` (modify) | `Rank` derived property. |
| `Pages/Analysis/AssetOptionModel.cs` (create) | Model for one sidebar asset option. |
| `Pages/Analysis/_AssetGlyph.cshtml` (create) | Inline SVG glyph / monogram, keyed by ticker. |
| `Pages/Analysis/_AssetOption.cshtml` (create) | One sidebar asset option. |
| `Pages/Analysis/_SummaryStrip.cshtml` (create) | Header summary strip. |
| `Pages/Analysis/_InsightCard.cshtml` (modify) | Glyph, accent, score block, factor words, takeaway. |
| `Pages/Analysis/_PriceHistoryChart.cshtml` (modify) | Remove the price table. |
| `Pages/Analysis/Index.cshtml` (modify) | Sidebar title/options/button, summary strip placement. |
| `Pages/Analysis/UiStateFixture.cshtml(.cs)` (modify) | Button copy; `?view=ranked` ranked-grid view. |
| `wwwroot/css/site.css` (modify) | Tokens and styles for everything above. |
| `tests/Argus.Authentication.Tests/AnalysisInsightPresenterTests.cs` (modify) | Catalog/presenter unit tests. |
| `tests/Argus.Authentication.Tests/AnalysisSummaryStripTests.cs` (create) | Summary-strip unit tests. |
| `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs` (modify) | Rendered-HTML contract tests. |
| `.superpowers/sdd/shot.mjs` (modify, untracked, never staged) | Screenshot + layout/height probe script. |

---

### Task 1: Presenter — short names, accents, score bands, factor words, takeaway

**Files:**
- Modify: `Analysis/AssetIdentityCatalog.cs`
- Modify: `Analysis/AnalysisInsightPresenter.cs`
- Modify: `Pages/Analysis/InsightCardModel.cs`
- Modify: `Pages/Analysis/_InsightCard.cshtml` (two-line wiring only)
- Test: `tests/Argus.Authentication.Tests/AnalysisInsightPresenterTests.cs` (rewrite)
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs` (deliberate updates)

**Interfaces:**
- Consumes: `AssetAnalysis`, `AnalysisFactor`, `AnalysisChartPoint`, `PerformanceMetric` (unchanged contracts).
- Produces (namespace `_bootstrap_scaffold.Analysis`):
  - `public enum AssetAccent { Equity, Bitcoin, Ethereum, Neutral }`
  - `public sealed record AssetIdentity(string Id, string Name, string Monogram, AssetAccent Accent)`
  - `AssetIdentityCatalog.For(string id)`, `AssetIdentityCatalog.AccentClass(AssetAccent accent)` → `"asset-accent--equity" | "asset-accent--bitcoin" | "asset-accent--ethereum" | "asset-accent--neutral"`
  - `public enum ScoreBand { Strong, Moderate, Weak }`
  - `public enum FactorDirection { Rising, Stable, Falling, Unavailable }`
  - `public sealed record FactorBarView(string Key, string Label, decimal? Percentage, decimal? Value, bool IsAvailable, FactorDirection Direction, string Word)`
  - `public sealed record AssetInsightView(AssetIdentity Identity, InsightTone Tone, decimal? SevenDayChange, IReadOnlyList<FactorBarView> FactorBars, string Takeaway, SparklineView? Sparkline, ScoreBand? Band)`
  - `AnalysisInsightPresenter.Present(AssetAnalysis analysis, int? rank = null)`, `Normalize(decimal)`, `ToneFor(decimal?)` (now public), `BandFor(decimal)`, `BandLabel(ScoreBand)`, `DirectionFor(decimal?)`, `FactorWord(FactorDirection)`
  - `InsightCardModel.Rank` → `int?` (`Index + 1` when `Result.Score` has a value, else `null`)

**Takeaway formats.** `{abs}` uses invariant `0.##`. `{signed}` is the existing `FormatSignedPercent` output, which already includes `%`, e.g. "+0.9%" or "0%".

The movement part depends on the strongest factor's direction:

| Direction | Format |
| --- | --- |
| Rising | `Cena wzrosła o {abs}% w ciągu {period}` |
| Falling | `Cena spadła o {abs}% w ciągu {period}` |
| Stable | `Cena pozostała stabilna w ciągu {period} ({signed})` |

`{period}` is "24 godzin" (momentum-24h), "7 dni" (momentum-7d) or "30 dni" (trend-30d).

The suffix depends on the asset's score and rank:

| Case | Suffix |
| --- | --- |
| Score present, rank 1 | ` — najwyżej oceniane aktywo w tej analizie.` |
| Score present, rank n > 1 | ` — miejsce {n} w tej analizie.` |
| Score present, rank not supplied | `.` |
| Score absent (unranked) | ` — za mało danych, aby nadać miejsce w rankingu.` |

When no factor is available, the whole takeaway is `Za mało danych rynkowych, aby opisać zmianę ceny.` with no suffix.

- [ ] **Step 1: Rewrite the presenter tests (failing)**

Replace the whole content of `tests/Argus.Authentication.Tests/AnalysisInsightPresenterTests.cs` with:

```csharp
using Xunit;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace Argus.Authentication.Tests;

public sealed class AnalysisInsightPresenterTests
{
    [Theory]
    [InlineData("AAPL", "Apple", "A", AssetAccent.Equity)]
    [InlineData("MSFT", "Microsoft", "M", AssetAccent.Equity)]
    [InlineData("NVDA", "NVIDIA", "N", AssetAccent.Equity)]
    [InlineData("TSLA", "Tesla", "T", AssetAccent.Equity)]
    [InlineData("BTC-USD", "Bitcoin", "₿", AssetAccent.Bitcoin)]
    [InlineData("eth-usd", "Ethereum", "Ξ", AssetAccent.Ethereum)]
    public void Known_tickers_map_to_short_name_monogram_and_accent(
        string id, string name, string monogram, AssetAccent accent)
    {
        var identity = AssetIdentityCatalog.For(id);

        Assert.Equal(name, identity.Name);
        Assert.Equal(monogram, identity.Monogram);
        Assert.Equal(accent, identity.Accent);
    }

    [Fact]
    public void Unknown_ticker_falls_back_to_ticker_first_letter_and_neutral_accent()
    {
        var identity = AssetIdentityCatalog.For("xyz");

        Assert.Equal("xyz", identity.Id);
        Assert.Equal("xyz", identity.Name);
        Assert.Equal("X", identity.Monogram);
        Assert.Equal(AssetAccent.Neutral, identity.Accent);
    }

    [Theory]
    [InlineData(AssetAccent.Equity, "asset-accent--equity")]
    [InlineData(AssetAccent.Bitcoin, "asset-accent--bitcoin")]
    [InlineData(AssetAccent.Ethereum, "asset-accent--ethereum")]
    [InlineData(AssetAccent.Neutral, "asset-accent--neutral")]
    public void Accent_maps_to_a_css_class(AssetAccent accent, string expected)
    {
        Assert.Equal(expected, AssetIdentityCatalog.AccentClass(accent));
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(20, 100)]
    [InlineData(-25, 0)]
    [InlineData(30, 100)]
    [InlineData(8.2, 71)]
    [InlineData(3, 58)]
    public void Normalize_matches_the_scorer_scale(decimal percentage, decimal expected)
    {
        Assert.Equal(expected, AnalysisInsightPresenter.Normalize(percentage));
    }

    [Theory]
    [InlineData(100, ScoreBand.Strong)]
    [InlineData(70, ScoreBand.Strong)]
    [InlineData(69.99, ScoreBand.Moderate)]
    [InlineData(40, ScoreBand.Moderate)]
    [InlineData(39.99, ScoreBand.Weak)]
    [InlineData(0, ScoreBand.Weak)]
    public void Score_band_uses_70_and_40_thresholds(decimal score, ScoreBand expected)
    {
        Assert.Equal(expected, AnalysisInsightPresenter.BandFor(score));
    }

    [Theory]
    [InlineData(ScoreBand.Strong, "Silny")]
    [InlineData(ScoreBand.Moderate, "Umiarkowany")]
    [InlineData(ScoreBand.Weak, "Słaby")]
    public void Score_band_has_a_polish_label(ScoreBand band, string expected)
    {
        Assert.Equal(expected, AnalysisInsightPresenter.BandLabel(band));
    }

    [Theory]
    [InlineData(1.01, FactorDirection.Rising)]
    [InlineData(1, FactorDirection.Stable)]
    [InlineData(0, FactorDirection.Stable)]
    [InlineData(-1, FactorDirection.Stable)]
    [InlineData(-1.01, FactorDirection.Falling)]
    public void Factor_direction_uses_one_percent_thresholds(decimal percentage, FactorDirection expected)
    {
        Assert.Equal(expected, AnalysisInsightPresenter.DirectionFor(percentage));
    }

    [Fact]
    public void Missing_factor_percentage_is_unavailable()
    {
        Assert.Equal(FactorDirection.Unavailable, AnalysisInsightPresenter.DirectionFor(null));
    }

    [Theory]
    [InlineData(FactorDirection.Rising, "Rosnący")]
    [InlineData(FactorDirection.Stable, "Stabilny")]
    [InlineData(FactorDirection.Falling, "Malejący")]
    [InlineData(FactorDirection.Unavailable, "n/d")]
    public void Factor_direction_has_a_polish_word(FactorDirection direction, string expected)
    {
        Assert.Equal(expected, AnalysisInsightPresenter.FactorWord(direction));
    }

    [Fact]
    public void Factor_bars_carry_direction_and_word()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", -1.1m, 0.5m, 8.2m, 10m, 20m, 15m));

        Assert.Equal(
            new[] { FactorDirection.Falling, FactorDirection.Stable, FactorDirection.Rising },
            view.FactorBars.Select(bar => bar.Direction));
        Assert.Equal(new[] { "Malejący", "Stabilny", "Rosnący" }, view.FactorBars.Select(bar => bar.Word));
    }

    [Fact]
    public void Takeaway_for_the_top_ranked_asset_names_the_strongest_rise()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", -1.1m, 3m, 8.2m, 10m, 20m, 15m), 1);

        Assert.Equal("Cena wzrosła o 8.2% w ciągu 30 dni — najwyżej oceniane aktywo w tej analizie.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_for_a_lower_rank_names_the_strongest_fall_and_the_place()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", -1.1m, -3m, -8.2m, 10m, 20m, 15m), 3);

        Assert.Equal(InsightTone.Negative, view.Tone);
        Assert.Equal("Cena spadła o 8.2% w ciągu 30 dni — miejsce 3 w tej analizie.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_for_small_moves_says_the_price_was_stable()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 0.4m, -0.2m, 0.9m, 10m, 11m), 2);

        Assert.Equal("Cena pozostała stabilna w ciągu 30 dni (+0.9%) — miejsce 2 w tej analizie.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_ties_resolve_to_the_earlier_factor()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 5m, -5m, 5m, 10m, 11m), 1);

        Assert.Equal("Cena wzrosła o 5% w ciągu 24 godzin — najwyżej oceniane aktywo w tej analizie.", view.Takeaway);
    }

    [Fact]
    public void Available_factor_without_percentage_is_an_unavailable_bar_excluded_from_takeaway()
    {
        var analysis = Analysis("AAPL", -9m, 3m, 8.2m, 10m, 20m, 15m);
        var factors = analysis.Factors.ToArray();
        factors[0] = factors[0] with { IsAvailable = true, Percentage = null };

        var view = AnalysisInsightPresenter.Present(analysis with { Factors = factors }, 2);

        Assert.False(view.FactorBars[0].IsAvailable);
        Assert.Null(view.FactorBars[0].Value);
        Assert.Equal(FactorDirection.Unavailable, view.FactorBars[0].Direction);
        Assert.Equal("n/d", view.FactorBars[0].Word);
        Assert.Equal("Cena wzrosła o 8.2% w ciągu 30 dni — miejsce 2 w tej analizie.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_without_a_rank_ends_with_a_full_stop()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, null, 5m, 10m, 11m));

        Assert.Equal("Cena wzrosła o 5% w ciągu 30 dni.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_for_an_unscored_asset_says_it_cannot_be_ranked()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, null, -2.4m, 10m) with { Score = null }, 1);

        Assert.Null(view.Band);
        Assert.Equal("Cena spadła o 2.4% w ciągu 30 dni — za mało danych, aby nadać miejsce w rankingu.", view.Takeaway);
    }

    [Fact]
    public void Takeaway_without_factors_says_there_is_not_enough_data()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, null, null), 1);

        Assert.Equal("Za mało danych rynkowych, aby opisać zmianę ceny.", view.Takeaway);
    }

    [Fact]
    public void Scored_analysis_gets_a_band()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, 1m, 1m, 10m, 11m));

        Assert.Equal(ScoreBand.Moderate, view.Band);
    }

    [Fact]
    public void Factor_bars_follow_scorer_order_and_mark_unavailable_factors()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, 3m, 8.2m, 10m, 11m));

        Assert.Equal(new[] { "momentum-24h", "momentum-7d", "trend-30d" }, view.FactorBars.Select(bar => bar.Key));
        Assert.Equal(new[] { "Momentum 24h", "Momentum 7d", "Trend 30d" }, view.FactorBars.Select(bar => bar.Label));
        Assert.False(view.FactorBars[0].IsAvailable);
        Assert.Null(view.FactorBars[0].Value);
        Assert.Equal(58m, view.FactorBars[1].Value);
        Assert.Equal(71m, view.FactorBars[2].Value);
    }

    [Theory]
    [InlineData(3, InsightTone.Positive)]
    [InlineData(-2, InsightTone.Negative)]
    [InlineData(0, InsightTone.Neutral)]
    public void Tone_follows_the_seven_day_change(decimal sevenDay, InsightTone expected)
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, sevenDay, 1m, 10m, 11m));

        Assert.Equal(expected, view.Tone);
        Assert.Equal(sevenDay, view.SevenDayChange);
    }

    [Fact]
    public void Missing_seven_day_change_is_neutral()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, null, 1m, 10m, 11m));

        Assert.Equal(InsightTone.Neutral, view.Tone);
        Assert.Null(view.SevenDayChange);
    }

    [Fact]
    public void Sparkline_scales_closes_into_a_fixed_view_box()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, 1m, 1m, 10m, 20m, 15m));

        Assert.NotNull(view.Sparkline);
        Assert.Equal("0 0 100 32", view.Sparkline.ViewBox);
        Assert.Equal("0,30 50,2 100,16", view.Sparkline.LinePoints);
        Assert.Equal("M0,32 L0,30 L50,2 L100,16 L100,32 Z", view.Sparkline.AreaPath);
    }

    [Fact]
    public void Flat_sparkline_is_drawn_in_the_middle()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, 1m, 1m, 5m, 5m));

        Assert.Equal("0,16 100,16", view.Sparkline!.LinePoints);
    }

    [Fact]
    public void Fewer_than_two_points_produce_no_sparkline()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", 1m, 1m, 1m, 5m));

        Assert.Null(view.Sparkline);
    }

    private static AssetAnalysis Analysis(
        string id,
        decimal? change24Hours,
        decimal? change7Days,
        decimal? change30Days,
        params decimal[] closes)
    {
        var now = new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);
        var points = closes
            .Select((close, index) => new AnalysisChartPoint(now.AddDays(index - closes.Length), close))
            .ToArray();
        var performance = new AssetPerformance(
            Metric("24h", change24Hours),
            Metric("7d", change7Days),
            Metric("30d", change30Days),
            points);
        AnalysisFactor[] factors =
        [
            Factor("momentum-24h", change24Hours),
            Factor("momentum-7d", change7Days),
            Factor("trend-30d", change30Days)
        ];

        return new AssetAnalysis(
            new AssetReference(id, id, MarketAssetClass.Equity),
            60m,
            performance,
            factors,
            points,
            null,
            null,
            MarketDataQuality.Complete,
            new MarketDataFreshness(now, now, null, false),
            false,
            [],
            null);
    }

    private static PerformanceMetric Metric(string period, decimal? percentage) =>
        new(period, percentage, percentage.HasValue);

    private static AnalysisFactor Factor(string key, decimal? percentage) =>
        new(key, key, percentage, percentage.HasValue ? 0.33m : 0m, percentage.HasValue ? 10m : null, percentage.HasValue, "explanation");
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter "FullyQualifiedName~AnalysisInsightPresenterTests"`
Expected: build FAILS. `AssetAccent`, `ScoreBand`, `FactorDirection`, `Takeaway`, `Band`, `BandFor` and the other new members are not defined.

- [ ] **Step 3: Replace `Analysis/AssetIdentityCatalog.cs`**

```csharp
namespace _bootstrap_scaffold.Analysis;

public enum AssetAccent
{
    Equity,
    Bitcoin,
    Ethereum,
    Neutral
}

public sealed record AssetIdentity(string Id, string Name, string Monogram, AssetAccent Accent);

public static class AssetIdentityCatalog
{
    private static readonly IReadOnlyDictionary<string, AssetIdentity> Known =
        new Dictionary<string, AssetIdentity>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new("AAPL", "Apple", "A", AssetAccent.Equity),
            ["MSFT"] = new("MSFT", "Microsoft", "M", AssetAccent.Equity),
            ["NVDA"] = new("NVDA", "NVIDIA", "N", AssetAccent.Equity),
            ["TSLA"] = new("TSLA", "Tesla", "T", AssetAccent.Equity),
            ["BTC-USD"] = new("BTC-USD", "Bitcoin", "₿", AssetAccent.Bitcoin),
            ["ETH-USD"] = new("ETH-USD", "Ethereum", "Ξ", AssetAccent.Ethereum)
        };

    public static AssetIdentity For(string id) =>
        Known.TryGetValue(id, out var identity)
            ? identity
            : new AssetIdentity(
                id,
                id,
                string.IsNullOrEmpty(id) ? "?" : id[..1].ToUpperInvariant(),
                AssetAccent.Neutral);

    public static string AccentClass(AssetAccent accent) =>
        accent switch
        {
            AssetAccent.Equity => "asset-accent--equity",
            AssetAccent.Bitcoin => "asset-accent--bitcoin",
            AssetAccent.Ethereum => "asset-accent--ethereum",
            _ => "asset-accent--neutral"
        };
}
```

- [ ] **Step 4: Update `Analysis/AnalysisInsightPresenter.cs`**

4a. Replace the records `FactorBarView` and `AssetInsightView`. Add the two enums directly after `InsightTone`:

```csharp
public enum ScoreBand
{
    Strong,
    Moderate,
    Weak
}

public enum FactorDirection
{
    Rising,
    Stable,
    Falling,
    Unavailable
}

public sealed record FactorBarView(
    string Key,
    string Label,
    decimal? Percentage,
    decimal? Value,
    bool IsAvailable,
    FactorDirection Direction,
    string Word);
```

```csharp
public sealed record AssetInsightView(
    AssetIdentity Identity,
    InsightTone Tone,
    decimal? SevenDayChange,
    IReadOnlyList<FactorBarView> FactorBars,
    string Takeaway,
    SparklineView? Sparkline,
    ScoreBand? Band);
```

(`SparklineView` is unchanged.)

4b. Replace `FactorOrder`, `Present`, `CreateBar`, `ToneFor` and `BuildInsight` with the code below. Keep the constants, `Normalize`, `BuildSparkline`, `FormatSignedPercent` and `Format` unchanged.

```csharp
    private static readonly (string Key, string Label, string Period)[] FactorOrder =
    [
        ("momentum-24h", "Momentum 24h", "24 godzin"),
        ("momentum-7d", "Momentum 7d", "7 dni"),
        ("trend-30d", "Trend 30d", "30 dni")
    ];

    public static AssetInsightView Present(AssetAnalysis analysis, int? rank = null)
    {
        var sevenDay = analysis.Performance.Change7Days is { IsAvailable: true, Percentage: { } percentage }
            ? percentage
            : (decimal?)null;
        var bars = FactorOrder
            .Select(entry => CreateBar(entry.Key, entry.Label, analysis.Factors))
            .ToArray();

        return new AssetInsightView(
            AssetIdentityCatalog.For(analysis.Asset.Id),
            ToneFor(sevenDay),
            sevenDay,
            bars,
            BuildTakeaway(bars, analysis.Score, rank),
            BuildSparkline(analysis.ChartPoints),
            analysis.Score is { } score ? BandFor(score) : null);
    }

    public static InsightTone ToneFor(decimal? change) =>
        change switch
        {
            > 0m => InsightTone.Positive,
            < 0m => InsightTone.Negative,
            _ => InsightTone.Neutral
        };

    public static ScoreBand BandFor(decimal score) =>
        score switch
        {
            >= 70m => ScoreBand.Strong,
            >= 40m => ScoreBand.Moderate,
            _ => ScoreBand.Weak
        };

    public static string BandLabel(ScoreBand band) =>
        band switch
        {
            ScoreBand.Strong => "Silny",
            ScoreBand.Moderate => "Umiarkowany",
            _ => "Słaby"
        };

    public static FactorDirection DirectionFor(decimal? percentage) =>
        percentage switch
        {
            null => FactorDirection.Unavailable,
            > 1m => FactorDirection.Rising,
            < -1m => FactorDirection.Falling,
            _ => FactorDirection.Stable
        };

    public static string FactorWord(FactorDirection direction) =>
        direction switch
        {
            FactorDirection.Rising => "Rosnący",
            FactorDirection.Falling => "Malejący",
            FactorDirection.Stable => "Stabilny",
            _ => "n/d"
        };
```

```csharp
    private static FactorBarView CreateBar(string key, string label, IReadOnlyList<AnalysisFactor> factors)
    {
        var factor = factors.FirstOrDefault(item => item.Key == key);
        var isAvailable = factor is { IsAvailable: true, Percentage: not null };
        var direction = isAvailable ? DirectionFor(factor!.Percentage) : FactorDirection.Unavailable;

        return new FactorBarView(
            key,
            label,
            factor?.Percentage,
            isAvailable ? Normalize(factor!.Percentage!.Value) : null,
            isAvailable,
            direction,
            FactorWord(direction));
    }

    private static string BuildTakeaway(IReadOnlyList<FactorBarView> bars, decimal? score, int? rank)
    {
        var strongest = -1;
        for (var index = 0; index < bars.Count; index++)
        {
            if (!bars[index].IsAvailable)
            {
                continue;
            }

            if (strongest < 0 ||
                Math.Abs(bars[index].Percentage!.Value) > Math.Abs(bars[strongest].Percentage!.Value))
            {
                strongest = index;
            }
        }

        if (strongest < 0)
        {
            return "Za mało danych rynkowych, aby opisać zmianę ceny.";
        }

        var percentage = bars[strongest].Percentage!.Value;
        var period = FactorOrder[strongest].Period;
        var movement = bars[strongest].Direction switch
        {
            FactorDirection.Rising => $"Cena wzrosła o {Format(Math.Abs(percentage))}% w ciągu {period}",
            FactorDirection.Falling => $"Cena spadła o {Format(Math.Abs(percentage))}% w ciągu {period}",
            _ => $"Cena pozostała stabilna w ciągu {period} ({FormatSignedPercent(percentage)})"
        };

        return movement + RankSuffix(score, rank);
    }

    private static string RankSuffix(decimal? score, int? rank) =>
        (score, rank) switch
        {
            (null, _) => " — za mało danych, aby nadać miejsce w rankingu.",
            (_, 1) => " — najwyżej oceniane aktywo w tej analizie.",
            (_, { } place) => $" — miejsce {place} w tej analizie.",
            _ => "."
        };
```

Notes:
- `bars` is built in `FactorOrder` order, so `FactorOrder[strongest].Period` belongs to `bars[strongest]`.
- `FormatSignedPercent` already appends `%`, so the stable form renders `(+0.9%)`.
- `Format` is invariant (`0.##`). Do not interpolate decimals directly.

- [ ] **Step 5: Add `Rank` to `Pages/Analysis/InsightCardModel.cs`**

```csharp
public sealed record InsightCardModel(
    AssetAnalysis Result,
    int Index,
    MarketQuote? Quote,
    string? ErrorMessage,
    string SummaryStatusMessage)
{
    public int? Rank => Result.Score.HasValue ? Index + 1 : null;
}
```

- [ ] **Step 6: Wire the card to the new presenter API**

In `Pages/Analysis/_InsightCard.cshtml`:
- Change `var insight = AnalysisInsightPresenter.Present(result);` to `var insight = AnalysisInsightPresenter.Present(result, Model.Rank);`.
- Inside `<p class="insight-card__insight">`, replace `@insight.Insight` with `@insight.Takeaway`.

Leave all other markup alone; Task 3 reworks the header and bars.

- [ ] **Step 7: Deliberately update the contract tests for the new copy**

In `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`:

7a. In `Insight_card_puts_identity_change_sparkline_bars_and_insight_first`, the AAPL factors are 24h +20%, 7d +25% and 30d +50%, and the score is 100 at rank 1.
- Replace `Assert.Contains("Apple Inc.", card[..details]);` with `Assert.Contains(">Apple</h3>", card[..details]);`.
- Replace the `"Najsilniejszy sygnał: …"` assertion with:

```csharp
        Assert.Contains("Cena wzrosła o 50% w ciągu 30 dni — najwyżej oceniane aktywo w tej analizie.", card[..details]);
```

7b. In `Valid_analysis_submission_renders_ranked_scores_and_metrics`, MSFT is +50% on every factor with score 100; AAPL is 0% on every factor with score 50. Add after `Assert.Contains("50/100", html);`:

```csharp
        Assert.Contains("Cena wzrosła o 50% w ciągu 24 godzin — najwyżej oceniane aktywo w tej analizie.", html);
        Assert.Contains("Cena pozostała stabilna w ciągu 24 godzin (0%) — miejsce 2 w tej analizie.", html);
```

- [ ] **Step 8: Run all tests**

Run: `dotnet build .bootstrap-scaffold.csproj --no-restore` → 0 warnings, 0 errors.
Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
Expected: all tests PASS.

- [ ] **Step 9: Commit**

```powershell
git add Analysis/AssetIdentityCatalog.cs Analysis/AnalysisInsightPresenter.cs Pages/Analysis/InsightCardModel.cs Pages/Analysis/_InsightCard.cshtml tests/Argus.Authentication.Tests/AnalysisInsightPresenterTests.cs tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs
git commit -m "feat(analysis): add score bands, factor words and takeaway to insight presenter" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 2: Asset glyph partial and per-asset accent

**Files:**
- Create: `Pages/Analysis/_AssetGlyph.cshtml`
- Modify: `Pages/Analysis/_InsightCard.cshtml`
- Modify: `wwwroot/css/site.css`
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

**Interfaces:**
- Consumes:
  - `AssetIdentityCatalog.For(string)` and `AssetIdentityCatalog.AccentClass(AssetAccent)` (Task 1).
  - The existing wrapper spans `insight-card__monogram` / `asset-option__monogram`, which stay in place for the exact-class assertions.
- Produces:
  - Partial `_AssetGlyph` (`@model string`, the ticker). It renders `<span class="asset-glyph asset-accent--…" data-asset-glyph="{Id}">` containing either `<svg class="asset-glyph__svg" …>` (AAPL, MSFT) or `<span class="asset-glyph__monogram">{Monogram}</span>`.
  - CSS custom property `--asset-accent`, set by `.asset-accent--equity|bitcoin|ethereum` and read by `.asset-glyph` and `.insight-card`.
  - The card's `<article>` additionally carries the accent class.

- [ ] **Step 1: Write the failing contract test**

Add this test to `AnalysisAccessContractTests` directly after `Testing_ui_state_fixture_renders_the_required_states`. The fixture already renders AAPL, MSFT, BTC-USD, ETH-USD, NVDA and TSLA cards.

```csharp
    [Fact]
    public async Task Ui_state_fixture_renders_own_asset_glyphs_and_accents()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/analysis/ui-state-fixture");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Matches(
            "<span class=\"asset-glyph asset-accent--equity\" data-asset-glyph=\"AAPL\">\\s*<svg class=\"asset-glyph__svg\"[^>]*aria-hidden=\"true\"",
            html);
        Assert.Matches(
            "<span class=\"asset-glyph asset-accent--equity\" data-asset-glyph=\"MSFT\">\\s*<svg class=\"asset-glyph__svg\"",
            html);
        Assert.Matches(
            "<span class=\"asset-glyph asset-accent--bitcoin\" data-asset-glyph=\"BTC-USD\">\\s*<span class=\"asset-glyph__monogram\">₿</span>",
            html);
        Assert.Matches(
            "<span class=\"asset-glyph asset-accent--equity\" data-asset-glyph=\"NVDA\">\\s*<span class=\"asset-glyph__monogram\">N</span>",
            html);
        Assert.Matches(
            "<article class=\"analysis-result-card__body insight-card insight-card--[a-z]+ asset-accent--ethereum\"",
            html);
        Assert.Matches("<span class=\"insight-card__monogram\" aria-hidden=\"true\">\\s*<span class=\"asset-glyph", html);
        Assert.DoesNotMatch("<svg class=\"asset-glyph__svg\"[^>]*(fill|stroke)=", html);
    }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter "FullyQualifiedName~Ui_state_fixture_renders_own_asset_glyphs_and_accents"`
Expected: FAIL. No `asset-glyph` markup is rendered yet.

- [ ] **Step 3: Create `Pages/Analysis/_AssetGlyph.cshtml`**

The shapes are original geometric compositions: two overlapping circles with a stem and leaf, and a 2×2 square grid. Neither is a traced brand logo. The SVG has no `fill`/`stroke` attributes; CSS colors it with `currentColor`.

```cshtml
@using _bootstrap_scaffold.Analysis
@model string
@{
    var identity = AssetIdentityCatalog.For(Model);
    var accentClass = AssetIdentityCatalog.AccentClass(identity.Accent);
}
<span class="asset-glyph @accentClass" data-asset-glyph="@identity.Id">
    @switch (identity.Id.ToUpperInvariant())
    {
        case "AAPL":
            <svg class="asset-glyph__svg" viewBox="0 0 24 24" focusable="false" aria-hidden="true">
                <circle cx="9" cy="14" r="6" />
                <circle cx="15" cy="14" r="6" />
                <rect x="11.25" y="3" width="1.5" height="5" rx="0.75" />
                <ellipse cx="15.5" cy="5" rx="3" ry="1.5" transform="rotate(-30 15.5 5)" />
            </svg>
            break;
        case "MSFT":
            <svg class="asset-glyph__svg" viewBox="0 0 24 24" focusable="false" aria-hidden="true">
                <rect x="3" y="3" width="8.5" height="8.5" />
                <rect x="12.5" y="3" width="8.5" height="8.5" />
                <rect x="3" y="12.5" width="8.5" height="8.5" />
                <rect x="12.5" y="12.5" width="8.5" height="8.5" />
            </svg>
            break;
        default:
            <span class="asset-glyph__monogram">@identity.Monogram</span>
            break;
    }
</span>
```

- [ ] **Step 4: Use the glyph and the accent in `_InsightCard.cshtml`**

4a. In the top `@{ … }` block, add after the `toneClass` line:

```csharp
    var accentClass = AssetIdentityCatalog.AccentClass(insight.Identity.Accent);
```

4b. Change the article's opening tag class to:

```cshtml
    <article class="analysis-result-card__body insight-card @toneClass @accentClass"
```

4c. Replace the monogram line:

```cshtml
            <span class="insight-card__monogram" aria-hidden="true">@insight.Identity.Monogram</span>
```

with:

```cshtml
            <span class="insight-card__monogram" aria-hidden="true"><partial name="_AssetGlyph" model="result.Asset.Id" /></span>
```

- [ ] **Step 5: Add accent tokens and glyph styles to `wwwroot/css/site.css`**

5a. In `:root`, add after `--color-warning: #fbbf24;`:

```css
    --color-asset-equity: #60a5fa;
    --color-asset-bitcoin: #f7931a;
    --color-asset-ethereum: #a78bfa;
    --color-on-asset: var(--color-bg);
    --insight-accent-bar-size: 0.1875rem;
    --asset-glyph-svg-size: 62%;
```

5b. Change the `.insight-card` rule (currently `--insight-tone`, `display: grid`, `gap`, `container-type`) to:

```css
.insight-card {
    --insight-tone: var(--color-muted);
    display: grid;
    gap: var(--space-3);
    container-type: inline-size;
    box-shadow: inset 0 var(--insight-accent-bar-size) 0 var(--asset-accent, transparent);
}
```

5c. In the grouped `.insight-card__monogram, .asset-option__monogram` rule, add `overflow: hidden;` after `border-radius: var(--radius-md);`.

5d. Directly after that grouped rule, add:

```css
.asset-accent--equity {
    --asset-accent: var(--color-asset-equity);
}

.asset-accent--bitcoin {
    --asset-accent: var(--color-asset-bitcoin);
}

.asset-accent--ethereum {
    --asset-accent: var(--color-asset-ethereum);
}

.asset-glyph {
    display: grid;
    place-items: center;
    inline-size: 100%;
    block-size: 100%;
    background: var(--asset-accent, var(--color-surface-raised));
    color: var(--color-on-asset);
    line-height: 1;
}

.asset-glyph.asset-accent--neutral {
    background: var(--color-surface-raised);
    color: var(--color-accent);
}

.asset-glyph__svg {
    inline-size: var(--asset-glyph-svg-size);
    block-size: var(--asset-glyph-svg-size);
    fill: currentColor;
}
```

`.asset-accent--neutral` deliberately sets no `--asset-accent`, so neutral cards get a transparent accent bar.

- [ ] **Step 6: Build and run all tests**

Run: `dotnet build .bootstrap-scaffold.csproj --no-restore` → 0 warnings.
Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
Expected: all PASS. The existing `class="insight-card__monogram"` assertion still matches.

- [ ] **Step 7: Commit**

```powershell
git add Pages/Analysis/_AssetGlyph.cshtml Pages/Analysis/_InsightCard.cshtml wwwroot/css/site.css tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs
git commit -m "feat(analysis): add own asset glyphs and per-asset accent colors" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 3: Score block, factor words, price-table removal, responsive grid

**Files:**
- Modify: `Pages/Analysis/_InsightCard.cshtml`
- Modify: `Pages/Analysis/_PriceHistoryChart.cshtml`
- Modify: `wwwroot/css/site.css`
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

**Interfaces:**
- Consumes (from Task 1):
  - `InsightCardModel.Rank`
  - `AssetInsightView.Band`
  - `AnalysisInsightPresenter.BandLabel`
  - `FactorBarView.Direction` / `.Word`
- Produces markup:
  - `insight-card__score-block insight-card__score-block--{strong|moderate|weak}`, containing `analysis-result-card__rank`, `analysis-result-card__score`, `insight-card__score-value` and `insight-card__band`.
  - `insight-card__bar-reading` wrapping `insight-card__bar-word insight-card__bar-word--{rising|stable|falling|unavailable}` plus `insight-card__bar-value` (only for available bars).
  - The price table `analysis-result-card__chart-points` is gone. The SVG circles keep `class="analysis-result-card__chart-point"`, `data-chart-point`, `data-chart-asset`, `data-chart-timestamp` and `data-chart-close`.
- Produces CSS:
  - Container `analysis-results` (inline-size).
  - Tokens `--workspace-wide-width`, `--insight-rank-size`, `--insight-band-size` and `--insight-bar-value-size`; new value for `--insight-score-size`.

- [ ] **Step 1: Write the failing contract assertions**

1a. In `Insight_card_puts_identity_change_sparkline_bars_and_insight_first` (AAPL: score 100, rank 1, 24h +20%), add before `Assert.Contains("analysis-result-card__metrics", card[details..]);`:

```csharp
        Assert.Matches(
            "<div class=\"insight-card__score-block insight-card__score-block--strong\">\\s*" +
            "<p class=\"analysis-result-card__rank\"><span aria-hidden=\"true\">#1</span><span class=\"visually-hidden\">Miejsce 1</span></p>\\s*" +
            "<p class=\"analysis-result-card__score\"><span class=\"visually-hidden\">Score 100/100</span><span class=\"insight-card__score-value\" aria-hidden=\"true\">100</span></p>\\s*" +
            "<p class=\"insight-card__band\">Silny</p>",
            card[..details]);
        Assert.Contains("<span class=\"insight-card__bar-word insight-card__bar-word--rising\">Rosnący</span>", card[..details]);
        Assert.Contains("<span class=\"insight-card__bar-value\">+20%</span>", card[..details]);
```

1b. In `Valid_analysis_submission_renders_ranked_scores_and_metrics` (AAPL: score 50, every factor 0%), add after the takeaway assertions from Task 1:

```csharp
        Assert.Contains("insight-card__score-block insight-card__score-block--moderate", html);
        Assert.Contains("<p class=\"insight-card__band\">Umiarkowany</p>", html);
        Assert.Contains("<span class=\"insight-card__bar-word insight-card__bar-word--stable\">Stabilny</span>", html);
```

1c. In `Testing_ui_state_fixture_renders_the_required_states`, add after `Assert.Contains("insight-card__bar insight-card__bar--unavailable", html);`. ETH-USD has 30d −2.4%, so it is falling; its 24h/7d factors are missing.

```csharp
        Assert.Contains("<span class=\"insight-card__bar-word insight-card__bar-word--falling\">Malejący</span>", html);
        Assert.Contains("<span class=\"insight-card__bar-word insight-card__bar-word--unavailable\">n/d</span>", html);
        Assert.Contains("<p class=\"analysis-result-card__rank\">Wynik nierankingowy</p>", html);
```

1d. In `Analysis_results_render_accessible_svg_geometry_and_text_fallback`, replace `Assert.Contains("<time datetime=", html);` with:

```csharp
        Assert.DoesNotContain("analysis-result-card__chart-points", html);
        Assert.DoesNotContain("<time datetime=", html);
```

The `data-chart-point` counts (3), `data-chart-timestamp` / `data-chart-close` regexes and `class="analysis-result-card__chart-point"` keep passing via the SVG `<circle>` elements. `Analysis_results_render_factors_summary_and_chart_data` asserts `data-chart-asset="AAPL"` and `data-chart-close="80`, which the circles also carry.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter "FullyQualifiedName~AnalysisAccessContractTests"`
Expected: the four tests above FAIL (no score block, no bar words, `<time datetime=` still present).

- [ ] **Step 3: Replace the card header in `_InsightCard.cshtml`**

Replace everything from `<p class="analysis-result-card__rank">@(result.Score.HasValue ? …` through the closing `}` of the `else if (result.Error is null)` block, all inside `<header class="analysis-result-card__header">`, with:

```cshtml
            @if (result.Score is { } score && insight.Band is { } band)
            {
                <div class="insight-card__score-block insight-card__score-block--@band.ToString().ToLowerInvariant()">
                    <p class="analysis-result-card__rank"><span aria-hidden="true">#@Model.Rank</span><span class="visually-hidden">Miejsce @Model.Rank</span></p>
                    <p class="analysis-result-card__score"><span class="visually-hidden">Score @AnalysisFormatting.Decimal(score)/100</span><span class="insight-card__score-value" aria-hidden="true">@AnalysisFormatting.Decimal(score)</span></p>
                    <p class="insight-card__band">@AnalysisInsightPresenter.BandLabel(band)</p>
                </div>
            }
            else
            {
                <p class="analysis-result-card__rank">Wynik nierankingowy</p>
                if (result.Error is null)
                {
                    <p class="analysis-result-card__status">Niewystarczające dane do wyliczenia score.</p>
                }
            }
```

(The inner `if` has no `@` because it is already inside a Razor code block.)

The header now contains, in order: the monogram span (Task 2), the identity div, and either the score block or the unranked line(s).

- [ ] **Step 4: Replace the bar value with the word reading**

Inside the `@foreach (var bar in insight.FactorBars)` loop, replace:

```cshtml
                        <span class="insight-card__bar-value">@(bar.IsAvailable ? AnalysisFormatting.NullablePercent(bar.Percentage) : "n/d")</span>
```

with:

```cshtml
                        <span class="insight-card__bar-reading">
                            <span class="insight-card__bar-word insight-card__bar-word--@bar.Direction.ToString().ToLowerInvariant()">@bar.Word</span>
                            @if (bar.IsAvailable)
                            {
                                <span class="insight-card__bar-value">@AnalysisFormatting.NullablePercent(bar.Percentage)</span>
                            }
                        </span>
```

No `aria-hidden` is added inside bars; the fixture test asserts that available bars contain none.

- [ ] **Step 5: Remove the price table from `_PriceHistoryChart.cshtml`**

Delete the whole `<ul class="analysis-result-card__chart-points"> … </ul>` block: the `@foreach (var point in chartPoints)` loop rendering `<li class="analysis-result-card__chart-point" …><time …>…</time><span>…</span></li>`. It sits directly after the `@if (chartPoints.Length < 2) { … }` paragraph and before the closing `}` that precedes `else`. Keep everything else: the range buttons, SVG, circles, tooltip and the one-point/no-data status paragraphs.

Then check whether `CultureInfo` is still used in the file; it is, by the SVG attributes, so keep `@using System.Globalization`.

- [ ] **Step 6: Update `wwwroot/css/site.css` — tokens**

In `:root`:
- Delete `--result-chart-max-height: 18rem;`, `--insight-card-min-width: 28rem;` and `--insight-card-narrow-width: 22rem;`.
- Change `--insight-score-size: 0.875rem;` to `--insight-score-size: 1.75rem;`.
- Add after `--insight-score-size`:

```css
    --insight-rank-size: 0.875rem;
    --insight-band-size: 0.75rem;
    --insight-bar-value-size: 0.8125rem;
    --workspace-wide-width: 120rem;
    --results-two-column-min: 46rem;
```

`--results-two-column-min` documents the value used in the container query below. Container queries cannot read custom properties, so the query repeats the literal `46rem`.

- [ ] **Step 7: Update `wwwroot/css/site.css` — remove price-table styles**

7a. Change the grouped selector

```css
.analysis-result-card__factor-list,
.analysis-result-card__chart-points {
```

to just `.analysis-result-card__factor-list {` and keep its body.

7b. Delete these rules entirely:
- `.analysis-result-card__chart-points { max-block-size: …; … }`
- `.analysis-result-card__chart-points .analysis-result-card__chart-point { display: flex; … }`
- `.analysis-result-card__chart-points .analysis-result-card__chart-point:last-child { … }`
- The grouped `.analysis-result-card__chart-points .analysis-result-card__chart-point span, … time, .analysis-result-card__summary { min-width: 0; overflow-wrap: anywhere; }`

Then add `min-width: 0;` and `overflow-wrap: anywhere;` to the following `.analysis-result-card__summary { display: grid; … }` rule, so the summary keeps them.

7c. In `@media (max-width: 40rem)`:
- Change the grouped `.analysis-result-card__header, .analysis-result-card__chart-points .analysis-result-card__chart-point { grid-template-columns: 1fr; }` to `.analysis-result-card__header { grid-template-columns: 1fr; }`.
- Delete the `.analysis-result-card__chart-points .analysis-result-card__chart-point { display: grid; gap: var(--space-1); }` block.

7d. Verify with `Select-String -Path wwwroot\css\site.css -Pattern 'chart-points|result-chart-max-height|insight-card-min-width|insight-card-narrow-width'`. Expected: no matches.

- [ ] **Step 8: Update `wwwroot/css/site.css` — results grid**

8a. Replace the `.analysis-results { display: grid; gap: var(--space-4); }` rule with:

```css
.analysis-results {
    display: grid;
    gap: var(--space-4);
    container: analysis-results / inline-size;
}
```

8b. Replace the `.analysis-results__list { … }` rule (currently `repeat(auto-fit, minmax(min(100%, var(--insight-card-min-width)), 1fr))` plus an `inline-size` cap) with:

```css
.analysis-results__list {
    display: grid;
    grid-template-columns: minmax(0, 1fr);
    gap: var(--space-4);
    margin: 0;
    padding: 0;
    list-style: none;
}

/* Literal mirrors --results-two-column-min; container queries cannot read custom properties. */
@container analysis-results (min-width: 46rem) {
    .analysis-results__list {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
}
```

8c. Change `.analysis-results__list--single` to drop the now-unneeded `inline-size: 100%;`, keeping `grid-template-columns: minmax(0, 1fr);`.

8d. Directly before `@media (prefers-reduced-motion: reduce)`, add the wide-screen rule. 112.5rem = 1800px.

```css
@media (min-width: 112.5rem) {
    body:has(.analysis-results) {
        --content-width: var(--workspace-wide-width);
    }

    .analysis-results .analysis-results__list {
        grid-template-columns: repeat(4, minmax(0, 1fr));
    }
}
```

`body` sets `--content-width` for both `.site-header` and `.landing-main`, so header and content stay aligned. `.analysis-results .analysis-results__list` (0,2,0) beats the container-query rule (0,1,0).

- [ ] **Step 9: Update `wwwroot/css/site.css` — header score block and factor words**

9a. Replace the three rules `.insight-card .analysis-result-card__header { … }`, `.insight-card .analysis-result-card__rank, .insight-card .analysis-result-card__status { … }`, `.insight-card .analysis-result-card__rank { grid-row: 2; }` and `.insight-card .analysis-result-card__status { grid-row: 3; }` with:

```css
.insight-card .analysis-result-card__header {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    align-items: center;
    column-gap: var(--space-2);
    row-gap: var(--space-1);
}

.insight-card .analysis-result-card__header > .analysis-result-card__rank,
.insight-card .analysis-result-card__header > .analysis-result-card__status {
    grid-column: 1 / -1;
    margin: 0;
    color: var(--color-muted);
}

.insight-card .analysis-result-card__header > .analysis-result-card__rank {
    grid-row: 2;
}

.insight-card .analysis-result-card__header > .analysis-result-card__status {
    grid-row: 3;
}
```

9b. Replace the whole `@container (max-width: 22rem) { … }` block with:

```css
@container (max-width: 18rem) {
    .insight-card .analysis-result-card__header {
        grid-template-columns: auto minmax(0, 1fr);
    }

    .insight-card__score-block {
        grid-column: 1 / -1;
        grid-row: 2;
        justify-self: start;
        justify-items: start;
    }
}
```

9c. Replace the `.insight-card .analysis-result-card__score { grid-column: 3; … font-size: var(--insight-score-size); font-weight: 700; }` rule with:

```css
.insight-card__score-block {
    --insight-band-color: var(--color-muted);
    display: grid;
    grid-template-areas:
        "rank score"
        "band band";
    grid-column: 3;
    grid-row: 1;
    align-items: baseline;
    justify-items: end;
    column-gap: var(--space-1);
}

.insight-card__score-block--strong {
    --insight-band-color: var(--color-positive);
}

.insight-card__score-block--moderate {
    --insight-band-color: var(--color-warning);
}

.insight-card__score-block--weak {
    --insight-band-color: var(--color-negative);
}

.insight-card__score-block .analysis-result-card__rank {
    grid-area: rank;
    margin: 0;
    color: var(--color-muted);
    font-size: var(--insight-rank-size);
    letter-spacing: 0;
}

.insight-card__score-block .analysis-result-card__score {
    grid-area: score;
    margin: 0;
    padding: 0;
    border: 0;
    border-radius: 0;
    background: none;
    color: var(--color-text);
    font-size: var(--insight-score-size);
    font-weight: 800;
    line-height: 1;
}

.insight-card__band {
    grid-area: band;
    margin: 0;
    color: var(--insight-band-color);
    font-size: var(--insight-band-size);
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
}
```

9d. Directly after the `.insight-card__bar-label, .insight-card__bar-value { color: var(--color-muted); }` rule, add:

```css
.insight-card__bar-reading {
    display: inline-flex;
    align-items: baseline;
    gap: var(--space-1);
    white-space: nowrap;
}

.insight-card__bar-word {
    color: var(--color-text);
    font-weight: 600;
}

.insight-card__bar-word--rising {
    color: var(--color-positive);
}

.insight-card__bar-word--falling {
    color: var(--color-negative);
}

.insight-card__bar-word--unavailable {
    color: var(--color-muted);
    font-weight: 400;
}

.insight-card__bar-value {
    font-size: var(--insight-bar-value-size);
}
```

- [ ] **Step 10: Build and run all tests**

Run: `dotnet build .bootstrap-scaffold.csproj --no-restore` → 0 warnings.
Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
Expected: all PASS. The following existing assertions still pass through the visually hidden text:
- `"Miejsce 1"`, `"100/100"`, `"50/100"`, `"Score 100/100"`
- `class="analysis-result-card__rank"` / `class="analysis-result-card__score"`

The insufficient-evidence assertions (no `0/100`, no `Score 0`) still pass because unscored cards render no score block.

- [ ] **Step 11: Commit**

```powershell
git add Pages/Analysis/_InsightCard.cshtml Pages/Analysis/_PriceHistoryChart.cshtml wwwroot/css/site.css tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs
git commit -m "feat(analysis): show score band and factor words, drop price table, add 2/4-column grid" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 4: Compact sidebar, "Generuj ranking", header summary strip

**Files:**
- Create: `Analysis/AnalysisSummaryStrip.cs`
- Create: `Pages/Analysis/AssetOptionModel.cs`
- Create: `Pages/Analysis/_AssetOption.cshtml`
- Create: `Pages/Analysis/_SummaryStrip.cshtml`
- Modify: `Pages/Analysis/Index.cshtml`
- Modify: `Pages/Analysis/UiStateFixture.cshtml` (button copy only)
- Modify: `wwwroot/css/site.css`
- Test: `tests/Argus.Authentication.Tests/AnalysisSummaryStripTests.cs` (create)
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

**Interfaces:**
- Consumes:
  - `AssetIdentityCatalog.For` (short name)
  - `AnalysisInsightPresenter.ToneFor` (public since Task 1)
  - `AnalysisFormatting.SignedPercent` / `Decimal`
  - Partial `_AssetGlyph` (Task 2)
- Produces (namespace `_bootstrap_scaffold.Analysis`):
  - `public sealed record SummaryPerformer(string Name, decimal Change)`
  - `public sealed record AnalysisSummaryStripView(int AnalyzedCount, SummaryPerformer? Best, SummaryPerformer? Worst, decimal? AverageScore)`
  - `AnalysisSummaryStripPresenter.Build(IReadOnlyList<AssetAnalysis> results)`
- Produces (namespace `_bootstrap_scaffold.Pages.Analysis`):
  - `public sealed record AssetOptionModel(string AssetId, bool IsSelected, decimal? Score)`
  - Partials `_AssetOption` (`@model AssetOptionModel`) and `_SummaryStrip` (`@model AnalysisSummaryStripView`)
- Produces markup:
  - Classes `asset-option__score`, `analysis-workspace__title`, `analysis-summary-strip` with `__list`, `__item`, `__value`, `__value--muted`, `__name`, `__change` and `__change--{positive|negative|neutral}`.
  - `asset-option__rank` is renamed to `asset-option__score`.

**Summary-strip rules:**

| Field | Rule |
| --- | --- |
| `AnalyzedCount` | `results.Count`, including failed results. |
| Candidates for best/worst | Results with `Error is null` and an available 7-day percentage, named by `AssetIdentityCatalog.For(id).Name`. |
| `Best` | The first candidate with the maximum change; `null` when there are no candidates. |
| `Worst` | The last candidate with the minimum change; `null` when there are fewer than 2 candidates. |
| `AverageScore` | Mean of non-null scores, `Math.Round(…, 1, MidpointRounding.AwayFromZero)`; `null` when no result is scored. |

The view renders "n/d" (muted) for any `null` value.

- [ ] **Step 1: Write the failing summary-strip unit tests**

Create `tests/Argus.Authentication.Tests/AnalysisSummaryStripTests.cs`:

```csharp
using Xunit;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace Argus.Authentication.Tests;

public sealed class AnalysisSummaryStripTests
{
    [Fact]
    public void Empty_results_have_zero_count_and_no_values()
    {
        var view = AnalysisSummaryStripPresenter.Build([]);

        Assert.Equal(0, view.AnalyzedCount);
        Assert.Null(view.Best);
        Assert.Null(view.Worst);
        Assert.Null(view.AverageScore);
    }

    [Fact]
    public void Ranked_results_report_best_worst_and_average()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 78m, 4.8m),
            Result("MSFT", 66m, 1.6m),
            Result("BTC-USD", 52m, -0.6m),
            Result("ETH-USD", 31m, -3.9m)
        ]);

        Assert.Equal(4, view.AnalyzedCount);
        Assert.Equal(new SummaryPerformer("Apple", 4.8m), view.Best);
        Assert.Equal(new SummaryPerformer("Ethereum", -3.9m), view.Worst);
        Assert.Equal(56.8m, view.AverageScore);
    }

    [Fact]
    public void Failed_and_unscored_results_count_but_do_not_feed_values()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 80m, 2m),
            Result("MSFT", 60m, -1m),
            Result("ETH-USD", null, null),
            Result("TSLA", null, 9m, failed: true)
        ]);

        Assert.Equal(4, view.AnalyzedCount);
        Assert.Equal(new SummaryPerformer("Apple", 2m), view.Best);
        Assert.Equal(new SummaryPerformer("Microsoft", -1m), view.Worst);
        Assert.Equal(70m, view.AverageScore);
    }

    [Fact]
    public void Single_seven_day_change_has_no_worst()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 70m, 3m),
            Result("MSFT", null, null)
        ]);

        Assert.Equal(new SummaryPerformer("Apple", 3m), view.Best);
        Assert.Null(view.Worst);
        Assert.Equal(70m, view.AverageScore);
    }

    [Fact]
    public void Ties_pick_the_first_best_and_the_last_worst()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 50m, 2m),
            Result("MSFT", 50m, 2m)
        ]);

        Assert.Equal("Apple", view.Best!.Name);
        Assert.Equal("Microsoft", view.Worst!.Name);
    }

    [Fact]
    public void Average_score_is_rounded_to_one_decimal()
    {
        var view = AnalysisSummaryStripPresenter.Build(
        [
            Result("AAPL", 70m, 1m),
            Result("MSFT", 71m, 1m),
            Result("NVDA", 71m, 1m)
        ]);

        Assert.Equal(70.7m, view.AverageScore);
    }

    private static AssetAnalysis Result(string id, decimal? score, decimal? change7Days, bool failed = false)
    {
        var now = new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);
        var sevenDays = new PerformanceMetric("7d", change7Days, change7Days.HasValue);
        var missing = new PerformanceMetric("24h", null, false);

        return new AssetAnalysis(
            new AssetReference(id, id, MarketAssetClass.Equity),
            score,
            new AssetPerformance(missing, sevenDays, missing with { Period = "30d" }, []),
            [],
            [],
            null,
            null,
            MarketDataQuality.Complete,
            new MarketDataFreshness(now, now, null, false),
            false,
            [],
            failed ? new MarketDataError(MarketDataErrorCategory.Timeout, "Test timeout.", true) : null);
    }
}
```

- [ ] **Step 2: Write the failing contract assertions**

2a. In `Authenticated_analysis_page_renders_the_configured_assets_and_limit`, add at the end. That test reads raw HTML, so decode first.

```csharp
        var decoded = WebUtility.HtmlDecode(html);
        Assert.Contains("<h1 id=\"analysis-title\" class=\"analysis-workspace__title\">Nowa analiza</h1>", decoded);
        Assert.Contains("Generuj ranking", decoded);
        Assert.DoesNotContain("Rozpocznij analizę", decoded);
        Assert.DoesNotContain("Wybierz aktywa do sprawdzenia", decoded);
        Assert.Matches(
            "<span class=\"asset-option__copy\">\\s*<strong>Apple</strong>\\s*<small>AAPL</small>",
            decoded);
        Assert.Matches("<span class=\"asset-option__monogram\" aria-hidden=\"true\">\\s*<span class=\"asset-glyph", decoded);
        Assert.DoesNotContain("analysis-summary-strip", decoded);
        Assert.DoesNotContain("asset-option__score", decoded);
```

2b. In `Valid_analysis_submission_renders_ranked_scores_and_metrics` (MSFT: 100, 7d +50%; AAPL: 50, 7d 0%), add at the end:

```csharp
        Assert.Contains("<section class=\"analysis-summary-strip\" aria-label=\"Podsumowanie rankingu\">", html);
        Assert.Matches("<dt>Przeanalizowane aktywa</dt>\\s*<dd class=\"analysis-summary-strip__value\">2</dd>", html);
        Assert.Matches(
            "<dt>Najlepszy wynik 7d</dt>\\s*<dd class=\"analysis-summary-strip__value\">\\s*" +
            "<span class=\"analysis-summary-strip__name\">Microsoft</span>\\s*" +
            "<span class=\"analysis-summary-strip__change analysis-summary-strip__change--positive\">\\+50%</span>",
            html);
        Assert.Matches(
            "<dt>Najsłabszy wynik 7d</dt>\\s*<dd class=\"analysis-summary-strip__value\">\\s*" +
            "<span class=\"analysis-summary-strip__name\">Apple</span>\\s*" +
            "<span class=\"analysis-summary-strip__change analysis-summary-strip__change--neutral\">0%</span>",
            html);
        Assert.Matches("<dt>Średni score</dt>\\s*<dd class=\"analysis-summary-strip__value\">75</dd>", html);
```

2c. In `Sidebar_shows_rank_and_score_and_failed_cards_have_no_sparkline`, replace

```csharp
        Assert.Contains("#1 · 100", html);
        Assert.Contains("#2 · 50", html);
```

with:

```csharp
        Assert.Contains("<span class=\"asset-option__score\"><span class=\"visually-hidden\">Score </span>100</span>", html);
        Assert.Contains("<span class=\"asset-option__score\"><span class=\"visually-hidden\">Score </span>50</span>", html);
        Assert.Equal(2, Regex.Matches(html, "class=\"asset-option__score\"").Count);
        Assert.DoesNotContain("asset-option__rank", html);
```

BTC-USD is a failed result with no score, so only two score badges render. The test name stays as-is to keep the diff surgical.

2d. In `Testing_ui_state_fixture_renders_the_required_states`, add:

```csharp
        Assert.Contains("Generuj ranking", html);
        Assert.DoesNotContain("Rozpocznij analizę", html);
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
Expected: build FAILS (`AnalysisSummaryStripPresenter`, `SummaryPerformer` undefined). After Step 4 the unit tests compile, and the contract assertions keep failing until Steps 5–8.

- [ ] **Step 4: Create `Analysis/AnalysisSummaryStrip.cs`**

```csharp
namespace _bootstrap_scaffold.Analysis;

public sealed record SummaryPerformer(string Name, decimal Change);

public sealed record AnalysisSummaryStripView(
    int AnalyzedCount,
    SummaryPerformer? Best,
    SummaryPerformer? Worst,
    decimal? AverageScore);

public static class AnalysisSummaryStripPresenter
{
    public static AnalysisSummaryStripView Build(IReadOnlyList<AssetAnalysis> results)
    {
        var candidates = results
            .Where(result => result.Error is null &&
                result.Performance.Change7Days is { IsAvailable: true, Percentage: not null })
            .Select(result => new SummaryPerformer(
                AssetIdentityCatalog.For(result.Asset.Id).Name,
                result.Performance.Change7Days.Percentage!.Value))
            .ToArray();

        SummaryPerformer? best = null;
        SummaryPerformer? worst = null;
        foreach (var candidate in candidates)
        {
            if (best is null || candidate.Change > best.Change)
            {
                best = candidate;
            }

            if (worst is null || candidate.Change <= worst.Change)
            {
                worst = candidate;
            }
        }

        var scores = results
            .Where(result => result.Score.HasValue)
            .Select(result => result.Score!.Value)
            .ToArray();

        return new AnalysisSummaryStripView(
            results.Count,
            best,
            candidates.Length < 2 ? null : worst,
            scores.Length == 0
                ? null
                : Math.Round(scores.Average(), 1, MidpointRounding.AwayFromZero));
    }
}
```

- [ ] **Step 5: Create the sidebar option model and partial**

`Pages/Analysis/AssetOptionModel.cs`:

```csharp
namespace _bootstrap_scaffold.Pages.Analysis;

public sealed record AssetOptionModel(string AssetId, bool IsSelected, decimal? Score);
```

`Pages/Analysis/_AssetOption.cshtml`:

```cshtml
@using _bootstrap_scaffold.Analysis
@using _bootstrap_scaffold.Pages.Analysis
@model AssetOptionModel
@{
    var identity = AssetIdentityCatalog.For(Model.AssetId);
}
<label class="asset-option">
    <input class="asset-option__input"
           type="checkbox"
           name="SelectedAssetIds"
           value="@Model.AssetId"
           checked="@(Model.IsSelected ? "checked" : null)" />
    <span class="asset-option__monogram" aria-hidden="true"><partial name="_AssetGlyph" model="Model.AssetId" /></span>
    <span class="asset-option__copy">
        <strong>@identity.Name</strong>
        <small>@Model.AssetId</small>
    </span>
    @if (Model.Score is { } score)
    {
        <span class="asset-option__score"><span class="visually-hidden">Score </span>@AnalysisFormatting.Decimal(score)</span>
    }
</label>
```

- [ ] **Step 6: Create `Pages/Analysis/_SummaryStrip.cshtml`**

```cshtml
@using _bootstrap_scaffold.Analysis
@using _bootstrap_scaffold.Pages.Analysis
@model AnalysisSummaryStripView
@{
    var performers = new (string Label, SummaryPerformer? Performer)[]
    {
        ("Najlepszy wynik 7d", Model.Best),
        ("Najsłabszy wynik 7d", Model.Worst)
    };
}
<section class="analysis-summary-strip" aria-label="Podsumowanie rankingu">
    <dl class="analysis-summary-strip__list">
        <div class="analysis-summary-strip__item">
            <dt>Przeanalizowane aktywa</dt>
            <dd class="analysis-summary-strip__value">@Model.AnalyzedCount</dd>
        </div>
        @foreach (var (label, performer) in performers)
        {
            <div class="analysis-summary-strip__item">
                <dt>@label</dt>
                @if (performer is not null)
                {
                    <dd class="analysis-summary-strip__value">
                        <span class="analysis-summary-strip__name">@performer.Name</span>
                        <span class="analysis-summary-strip__change analysis-summary-strip__change--@AnalysisInsightPresenter.ToneFor(performer.Change).ToString().ToLowerInvariant()">@AnalysisFormatting.SignedPercent(performer.Change)</span>
                    </dd>
                }
                else
                {
                    <dd class="analysis-summary-strip__value analysis-summary-strip__value--muted">n/d</dd>
                }
            </div>
        }
        <div class="analysis-summary-strip__item">
            <dt>Średni score</dt>
            @if (Model.AverageScore is { } average)
            {
                <dd class="analysis-summary-strip__value">@AnalysisFormatting.Decimal(average)</dd>
            }
            else
            {
                <dd class="analysis-summary-strip__value analysis-summary-strip__value--muted">n/d</dd>
            }
        </div>
    </dl>
</section>
```

- [ ] **Step 7: Update `Pages/Analysis/Index.cshtml`**

7a. Replace the `rankById` declaration and loop in the top `@{ … }` block with:

```csharp
    var scoreById = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
    if (Model.HasResults)
    {
        foreach (var analyzed in Model.AnalysisResults!.Results)
        {
            scoreById[analyzed.Asset.Id] = analyzed.Score;
        }
    }
```

7b. Replace the sidebar header

```cshtml
        <div class="analysis-page__header">
            <p class="eyebrow">Nowa analiza</p>
            <h1 id="analysis-title">Wybierz aktywa do sprawdzenia</h1>
        </div>
```

with:

```cshtml
        <div class="analysis-page__header">
            <h1 id="analysis-title" class="analysis-workspace__title">Nowa analiza</h1>
        </div>
```

7c. Replace the body of `@foreach (var asset in Model.Assets) { … }` (the `isSelected` / `identity` variables and the whole `<label class="asset-option">…</label>`) with:

```cshtml
            @foreach (var asset in Model.Assets)
            {
                var isSelected = Model.SelectedAssetIds.Any(id =>
                    string.Equals(id, asset.Id, StringComparison.OrdinalIgnoreCase));
                var option = new AssetOptionModel(
                    asset.Id,
                    isSelected,
                    scoreById.TryGetValue(asset.Id, out var optionScore) ? optionScore : null);
                <partial name="_AssetOption" model="option" />
            }
```

7d. Change the submit button text `Rozpocznij analizę` to `Generuj ranking`. Keep `class="button"`, `type="submit"`, `data-submit-button` and the `disabled` expression.

7e. In the content header, render the strip after `<h2>Dashboard analizy</h2>`:

```cshtml
        <header class="analysis-workspace__header">
            <p class="eyebrow">Market intelligence</p>
            <h2>Dashboard analizy</h2>
            @if (Model.HasResults)
            {
                <partial name="_SummaryStrip" model="AnalysisSummaryStripPresenter.Build(Model.AnalysisResults!.Results)" />
            }
        </header>
```

7f. Check whether `@using System.Globalization` is still needed in `Index.cshtml` (search the file for `CultureInfo`). Remove the using only if no reference remains.

`wwwroot/js/analysis-form.js` was checked while planning: it never contains "Rozpocznij". It only sets the pending text "Przygotowywanie analizy...", so no JS change is needed.

- [ ] **Step 8: Update the fixture button copy**

In `Pages/Analysis/UiStateFixture.cshtml`, change both `<button class="button" type="button">Rozpocznij analizę</button>` (Default state) and `<button class="button" type="button" disabled>Rozpocznij analizę</button>` (Empty state) to use the text `Generuj ranking`. Nothing else changes.

- [ ] **Step 9: Update `wwwroot/css/site.css`**

9a. Add to `:root` (after `--workspace-wide-width`):

```css
    --workspace-title-size: 2rem;
    --color-selected-surface: color-mix(in srgb, var(--color-accent) 12%, var(--color-surface));
    --asset-option-selected-bar: 0.25rem;
    --asset-option-glyph-size: 2rem;
    --asset-option-ticker-size: 0.75rem;
    --summary-strip-item-min: 10rem;
    --summary-strip-label-size: 0.8125rem;
    --summary-strip-value-size: 1.125rem;
```

9b. After the `.analysis-page__header h1 { … }` rule, add:

```css
.analysis-page__header .analysis-workspace__title {
    max-width: none;
    font-size: var(--workspace-title-size);
}
```

9c. Make the asset list compact and the selection obvious. Change `.asset-list { gap: var(--space-2); }` to `gap: var(--space-1);`. In `.asset-option`, change `gap: var(--space-3);` to `gap: var(--space-2);` and `padding: var(--space-3);` to `padding: var(--space-2);`. Replace the `.asset-option:has(.asset-option__input:checked)` rule with:

```css
.asset-option:has(.asset-option__input:checked) {
    border-color: var(--color-action);
    background: var(--color-selected-surface);
    box-shadow: inset var(--asset-option-selected-bar) 0 0 var(--color-action);
}
```

Change `.asset-option__copy { display: grid; gap: var(--space-1); }` to:

```css
.asset-option__copy {
    display: grid;
    gap: 0;
    min-width: 0;
}
```

Replace `.asset-option__copy small { color: var(--color-muted); }` with:

```css
.asset-option__copy small {
    color: var(--color-muted);
    font-size: var(--asset-option-ticker-size);
}
```

9d. Directly after the `.asset-glyph__svg { … }` rule from Task 2, add:

```css
.asset-option__monogram {
    flex: none;
    inline-size: var(--asset-option-glyph-size);
    block-size: var(--asset-option-glyph-size);
}
```

9e. Rename the selector `.asset-option__rank` to `.asset-option__score` (same body: `margin-inline-start: auto; color: var(--color-accent); font-weight: 600;`).

9f. After the `.analysis-workspace__header p { color: var(--color-muted); }` rule, add:

```css
.analysis-summary-strip__list {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(var(--summary-strip-item-min), 1fr));
    gap: var(--space-2);
    margin: 0;
}

.analysis-summary-strip__item {
    display: grid;
    align-content: start;
    gap: var(--space-1);
    padding: var(--space-2) var(--space-3);
    border: 0.0625rem solid var(--color-border);
    border-radius: var(--radius-md);
    background: var(--color-surface);
}

.analysis-summary-strip__item dt {
    color: var(--color-muted);
    font-size: var(--summary-strip-label-size);
}

.analysis-summary-strip__value {
    display: flex;
    flex-wrap: wrap;
    align-items: baseline;
    gap: var(--space-1);
    margin: 0;
    color: var(--color-text);
    font-size: var(--summary-strip-value-size);
    font-weight: 700;
}

.analysis-summary-strip__value--muted {
    color: var(--color-muted);
    font-weight: 400;
}

.analysis-summary-strip__change--positive {
    color: var(--color-positive);
}

.analysis-summary-strip__change--negative {
    color: var(--color-negative);
}

.analysis-summary-strip__change--neutral {
    color: var(--color-muted);
}
```

- [ ] **Step 10: Build and run all tests**

Run: `dotnet build .bootstrap-scaffold.csproj --no-restore` → 0 warnings.
Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
Expected: all PASS. These existing assertions keep passing:
- `AccountAccessContractTests` "Wybierz aktywa" (landing page, unaffected)
- `class="asset-option__monogram"`
- the AAPL/MSFT/BTC-USD/ETH-USD presence asserts (tickers render in `<small>` and `value=`)

- [ ] **Step 11: Commit**

```powershell
git add Analysis/AnalysisSummaryStrip.cs Pages/Analysis/AssetOptionModel.cs Pages/Analysis/_AssetOption.cshtml Pages/Analysis/_SummaryStrip.cshtml Pages/Analysis/Index.cshtml Pages/Analysis/UiStateFixture.cshtml wwwroot/css/site.css tests/Argus.Authentication.Tests/AnalysisSummaryStripTests.cs tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs
git commit -m "feat(analysis): compact sidebar, Generuj ranking action and summary strip" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 5: Card density, ranked fixture view, visual check

**Files:**
- Modify: `wwwroot/css/site.css`
- Modify: `Pages/Analysis/UiStateFixture.cshtml.cs`
- Modify: `Pages/Analysis/UiStateFixture.cshtml`
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`
- Scratch (untracked, never staged): `.superpowers/sdd/shot.mjs`

**Interfaces:**
- Consumes:
  - `InsightCardModel` and `AssetOptionModel`
  - `AnalysisSummaryStripPresenter.Build`
  - Partials `_InsightCard`, `_AssetOption` and `_SummaryStrip`
  - The CSS from Tasks 2–4
- Produces:
  - `UiStateFixtureModel.OnGet(string? view)`
  - `ShowRankedGrid` (bool)
  - `RankedResults` (`IReadOnlyList<AssetAnalysis>`)
  - `RankedCards` (`IReadOnlyList<InsightCardModel>`)
  - `RankedSummary` (`AnalysisSummaryStripView`)
  - `RankedOptions` (`IReadOnlyList<AssetOptionModel>`)
- The URL `/analysis/ui-state-fixture?view=ranked` (Testing only) renders a full workspace with the real `analysis-results` grid. The default fixture view is unchanged.
- New tokens:
  - `--insight-card-padding`, `--insight-card-gap`, `--insight-header-padding`
  - `--insight-bar-row-gap`, `--insight-bar-label-size`, `--insight-name-size`

**Ranked fixture data.** Each asset has 6 closes, current freshness and a generated summary.

| Asset | Score | 24h | 7d | 30d | Band | Takeaway |
| --- | --- | --- | --- | --- | --- | --- |
| AAPL | 78 | +1.2 | +4.8 | +9.6 | Silny | Cena wzrosła o 9.6% w ciągu 30 dni — najwyżej oceniane aktywo w tej analizie. |
| MSFT | 66 | +0.4 | +1.6 | +3.4 | Umiarkowany | Cena wzrosła o 3.4% w ciągu 30 dni — miejsce 2 w tej analizie. |
| BTC-USD | 52 | −0.8 | −0.6 | +2.1 | Umiarkowany | Cena wzrosła o 2.1% w ciągu 30 dni — miejsce 3 w tej analizie. |
| ETH-USD | 31 | −1.5 | −3.9 | −6.2 | Słaby | Cena spadła o 6.2% w ciągu 30 dni — miejsce 4 w tej analizie. |

Summary strip: count 4; best Apple +4.8%; worst Ethereum −3.9%; average (78+66+52+31)/4 = 56.75, which rounds to **56.8**.

- [ ] **Step 1: Write the failing contract test**

Add after `Ui_state_fixture_renders_own_asset_glyphs_and_accents`:

```csharp
    [Fact]
    public async Task Ui_state_fixture_ranked_view_renders_four_cards_in_the_results_grid()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/analysis/ui-state-fixture?view=ranked");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Analysis result states", html);
        Assert.Contains("<section class=\"analysis-results\" aria-labelledby=\"analysis-results-title\">", html);
        var listStart = html.IndexOf("<ol class=\"analysis-results__list\">", StringComparison.Ordinal);
        Assert.True(listStart > 0, "The ranked view must render the real results list.");
        var list = html[listStart..];
        Assert.Equal(4, Regex.Matches(list, "<li class=\"analysis-result-card analysis-result-card--success\">").Count);
        var positions = new[] { ">Apple</h3>", ">Microsoft</h3>", ">Bitcoin</h3>", ">Ethereum</h3>" }
            .Select(name => list.IndexOf(name, StringComparison.Ordinal))
            .ToArray();
        Assert.All(positions, position => Assert.True(position >= 0));
        Assert.Equal(positions.OrderBy(position => position), positions);
        Assert.Single(Regex.Matches(list, "<p class=\"insight-card__band\">Silny</p>"));
        Assert.Equal(2, Regex.Matches(list, "<p class=\"insight-card__band\">Umiarkowany</p>").Count);
        Assert.Single(Regex.Matches(list, "<p class=\"insight-card__band\">Słaby</p>"));
        Assert.Contains("Cena spadła o 6.2% w ciągu 30 dni — miejsce 4 w tej analizie.", list);
        Assert.Matches("<dt>Przeanalizowane aktywa</dt>\\s*<dd class=\"analysis-summary-strip__value\">4</dd>", html);
        Assert.Matches(
            "<span class=\"analysis-summary-strip__name\">Apple</span>\\s*" +
            "<span class=\"analysis-summary-strip__change analysis-summary-strip__change--positive\">\\+4.8%</span>",
            html);
        Assert.Matches(
            "<span class=\"analysis-summary-strip__name\">Ethereum</span>\\s*" +
            "<span class=\"analysis-summary-strip__change analysis-summary-strip__change--negative\">-3.9%</span>",
            html);
        Assert.Matches("<dt>Średni score</dt>\\s*<dd class=\"analysis-summary-strip__value\">56.8</dd>", html);
        Assert.Equal(4, Regex.Matches(html, "class=\"asset-option__score\"").Count);
        Assert.Contains("Generuj ranking", html);
        Assert.DoesNotContain("chwilowo niedostępne", html);
    }
```

Also add `Assert.DoesNotContain("analysis-summary-strip", html);` at the end of `Testing_ui_state_fixture_renders_the_required_states`, so the default view stays state-cells-only.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter "FullyQualifiedName~Ui_state_fixture_ranked_view"`
Expected: FAIL. The page ignores `view`, so "Analysis result states" is present and there is no `analysis-results` section.

- [ ] **Step 3: Extend `Pages/Analysis/UiStateFixture.cshtml.cs`**

3a. Replace the `OnGet` method with:

```csharp
    public bool ShowRankedGrid { get; private set; }

    public IReadOnlyList<AssetAnalysis> RankedResults { get; } = BuildRankedResults();

    public IReadOnlyList<InsightCardModel> RankedCards =>
        RankedResults.Select((analysis, index) => Card(analysis, index)).ToArray();

    public AnalysisSummaryStripView RankedSummary => AnalysisSummaryStripPresenter.Build(RankedResults);

    public IReadOnlyList<AssetOptionModel> RankedOptions =>
        RankedResults
            .Select(analysis => new AssetOptionModel(analysis.Asset.Id, true, analysis.Score))
            .Concat(
            [
                new AssetOptionModel("NVDA", false, null),
                new AssetOptionModel("TSLA", false, null)
            ])
            .ToArray();

    public IActionResult OnGet(string? view)
    {
        if (!string.Equals(environment.EnvironmentName, "Testing", StringComparison.Ordinal))
        {
            return NotFound();
        }

        ShowRankedGrid = string.Equals(view, "ranked", StringComparison.OrdinalIgnoreCase);
        return Page();
    }
```

3b. Add this method after `BuildResultStates()`:

```csharp
    private static IReadOnlyList<AssetAnalysis> BuildRankedResults()
    {
        var now = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        var current = new MarketDataFreshness(now, now, now.AddMinutes(5), false);
        var generated = AnalysisSummaryResult.Generated("Wynik oparty na wyświetlonych danych.");

        return
        [
            Sample("AAPL", 78m, 1.2m, 4.8m, 9.6m, [210m, 212m, 209m, 214m, 216m, 215.5m], MarketDataQuality.Complete, current, generated),
            Sample("MSFT", 66m, 0.4m, 1.6m, 3.4m, [412m, 415m, 414m, 418m, 419m, 420.25m], MarketDataQuality.Complete, current, generated),
            Sample("BTC-USD", 52m, -0.8m, -0.6m, 2.1m, [66000m, 67200m, 66800m, 67500m, 66900m, 67100m], MarketDataQuality.Complete, current, generated),
            Sample("ETH-USD", 31m, -1.5m, -3.9m, -6.2m, [3720m, 3690m, 3650m, 3610m, 3580m, 3540m], MarketDataQuality.Complete, current, generated)
        ];
    }
```

`Sample`, `Card`, `Metric` and `Factor` already exist and are reused as-is. `AssetOptionModel` is in the same namespace (`_bootstrap_scaffold.Pages.Analysis`). `AnalysisSummaryStripView` / `AnalysisSummaryStripPresenter` come from the existing `using _bootstrap_scaffold.Analysis;`.

- [ ] **Step 4: Add the ranked view to `Pages/Analysis/UiStateFixture.cshtml`**

Wrap the existing top-level `<section class="ui-state-fixture" aria-labelledby="ui-state-fixture-title"> … </section>` (the whole current body after the `@{ … }` block) in the `else` branch below. Leave its inner lines unchanged; re-indenting is optional.

```cshtml
@if (Model.ShowRankedGrid)
{
    <section class="analysis-workspace" aria-labelledby="analysis-title">
        <aside class="analysis-workspace__sidebar">
            <div class="analysis-page__header">
                <h1 id="analysis-title" class="analysis-workspace__title">Nowa analiza</h1>
            </div>
            <div class="analysis-form">
                <div class="analysis-form__header">
                    <h2>Dostępne aktywa</h2>
                    <p>Możesz wybrać maksymalnie 5 aktywów.</p>
                </div>
                <div class="asset-list">
                    @foreach (var option in Model.RankedOptions)
                    {
                        <partial name="_AssetOption" model="option" />
                    }
                </div>
                <button class="button" type="button">Generuj ranking</button>
            </div>
        </aside>

        <div class="analysis-workspace__content">
            <header class="analysis-workspace__header">
                <p class="eyebrow">Market intelligence</p>
                <h2>Dashboard analizy</h2>
                <partial name="_SummaryStrip" model="Model.RankedSummary" />
            </header>

            <section class="analysis-results" aria-labelledby="analysis-results-title">
                <h2 id="analysis-results-title">Ranking analizy</h2>
                <p class="analysis-results__boundary">
                    Wyniki mają charakter edukacyjny i nie stanowią porady inwestycyjnej.
                </p>
                <ol class="analysis-results__list">
                    @foreach (var rankedCard in Model.RankedCards)
                    {
                        <partial name="_InsightCard" model="rankedCard" />
                    }
                </ol>
            </section>
        </div>
    </section>
}
else
{
    @* existing <section class="ui-state-fixture" …> … </section> goes here, unchanged *@
}
```

(The `@* … *@` line is a placement marker only. Replace it with the existing section markup; do not leave the comment in the file.)

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter "FullyQualifiedName~Ui_state_fixture"`
Expected: PASS for the ranked, default, glyph and not-outside-testing fixture tests.

- [ ] **Step 6: Density tokens and rules in `wwwroot/css/site.css`**

6a. In `:root`, change:
- `--insight-monogram-size: 2.5rem;` → `2.25rem`
- `--insight-change-size: 2rem;` → `1.5rem`
- `--insight-sparkline-height: 4.5rem;` → `3rem`

Add after `--insight-bar-value-size`:

```css
    --insight-card-padding: var(--space-3);
    --insight-card-gap: var(--space-2);
    --insight-header-padding: var(--space-2);
    --insight-bar-row-gap: 0.25rem;
    --insight-bar-label-size: 0.875rem;
    --insight-name-size: 1.125rem;
```

6b. In `.insight-card`, change `gap: var(--space-3);` to `gap: var(--insight-card-gap);`. Directly after the `.insight-card { … }` rule, add:

```css
.analysis-result-card__body.insight-card {
    padding: var(--insight-card-padding);
}
```

This compound selector (0,2,0) comes later than `.ui-state-fixture .analysis-result-card__body` and `.analysis-result-card__body`, so the fixture and the real page use the same padding.

6c. In `.insight-card .analysis-result-card__header { … }` (Task 3), add `padding-bottom: var(--insight-header-padding);`.

6d. In `.insight-card__identity h3 { … }`, add `font-size: var(--insight-name-size);`. It is declared after `.analysis-result-card__header h3` with equal specificity, so it wins.

6e. In `.insight-card__bars`, change `gap: var(--space-1);` to `gap: var(--insight-bar-row-gap);`. After the `.insight-card__bar-label, .insight-card__bar-value` rule, add:

```css
.insight-card__bar-label {
    overflow: hidden;
    font-size: var(--insight-bar-label-size);
    text-overflow: ellipsis;
    white-space: nowrap;
}
```

6f. Replace `.insight-card__insight-label { display: block; … }` with:

```css
.insight-card__insight-label {
    display: inline;
    margin-inline-end: var(--space-1);
    color: var(--color-accent);
    font-weight: 600;
}
```

- [ ] **Step 7: Build and run the full suite**

Run: `dotnet build .bootstrap-scaffold.csproj --no-restore` → 0 warnings.
Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
Expected: all PASS.

- [ ] **Step 8: Update the scratch screenshot script `.superpowers/sdd/shot.mjs`**

The file is untracked; never stage it. Replace its content with:

```js
import { chromium } from 'playwright';

const base = 'http://localhost:5099/analysis/ui-state-fixture';
const browser = await chromium.launch();

for (const [name, width] of [['desktop', 1440], ['mobile', 390]]) {
  const page = await browser.newPage({ viewport: { width, height: 900 } });
  await page.goto(base);
  const section = page.locator('[aria-labelledby="result-surface-states"]');
  await section.screenshot({ path: `insight-results-${name}.png` });
  const heights = await section.locator('.insight-card').evaluateAll(cards =>
    cards.map(card => Math.round(card.getBoundingClientRect().height)));
  console.log(`${name} state card heights: ${heights.join(', ')}`);
  await page.locator('details.insight-card__details').first().evaluate(d => d.open = true);
  await page.locator('.insight-card').first().screenshot({ path: `insight-card-open-${name}.png` });
  await page.close();
}

for (const [name, width] of [['desktop', 1440], ['wide', 1920], ['mobile', 390]]) {
  const page = await browser.newPage({ viewport: { width, height: 1000 } });
  await page.goto(`${base}?view=ranked`);
  await page.screenshot({ path: `insight-ranked-${name}.png`, fullPage: true });
  const boxes = await page.locator('.analysis-results__list > li').evaluateAll(items =>
    items.map(item => {
      const r = item.getBoundingClientRect();
      return `${Math.round(r.x)},${Math.round(r.y)} ${Math.round(r.width)}x${Math.round(r.height)}`;
    }));
  console.log(`${name} ranked cards: ${boxes.join(' | ')}`);
  await page.close();
}

await browser.close();
```

- [ ] **Step 9: Visual check**

Run everything in one PowerShell call from the repo root:

```powershell
dotnet publish .bootstrap-scaffold.csproj -c Release -o "$env:TEMP\argus-fixture" --nologo
$env:ASPNETCORE_ENVIRONMENT = 'Testing'
$env:Entra__Authority = 'https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0'
$env:Entra__TenantId = '00000000-0000-0000-0000-000000000000'
$env:Entra__ClientId = '00000000-0000-0000-0000-000000000001'
$env:Entra__ClientSecret = 'placeholder'
$env:Entra__CallbackPath = '/signin-oidc'
$server = Start-Process dotnet -ArgumentList '.\.bootstrap-scaffold.dll', '--urls', 'http://localhost:5099' -WorkingDirectory "$env:TEMP\argus-fixture" -PassThru -NoNewWindow
Start-Sleep -Seconds 8
try {
    Push-Location .superpowers\sdd
    node shot.mjs
}
finally {
    Pop-Location
    Stop-Process -Id $server.Id
}
```

Acceptance criteria. Check the console output and open the PNGs in `.superpowers\sdd` with the image viewer.

1. **Height.** The first `desktop state card heights` value (the "Completed" AAPL card) is **≤ 382** (baseline 545, −30%). If it is higher, tune tokens in this order and rerun Step 9 after each change:
   - `--insight-card-gap: var(--space-1);`
   - `--insight-sparkline-height: 2.5rem;`
   - `--insight-header-padding: var(--space-1);`

   Do not touch view markup for this.
2. **`desktop ranked cards`** (1440): exactly 2 distinct x values, and 2 cards per y value (a 2×2 grid).
3. **`wide ranked cards`** (1920): 4 distinct x values, all with the same y (1 row).
4. **`mobile ranked cards`** (390): 1 distinct x value (single column), with no horizontal overflow visible in `insight-ranked-mobile.png`.
5. **`insight-ranked-desktop.png`** shows:
   - the "Nowa analiza" title at roughly half the old size
   - compact options with glyphs and four highlighted selections
   - the "Generuj ranking" button
   - the summary strip (4 / Apple +4.8% / Ethereum −3.9% / 56.8)
   - cards with the apple glyph on blue, the four-squares glyph on blue, ₿ on orange and Ξ on purple
   - a colored top accent bar
   - the "#N" + big score + band block
   - factor words with small %
   - the takeaway as the last line before "Szczegóły i metodologia"
6. **`insight-card-open-desktop.png`** shows the open details with metrics, factors, the SVG chart and the summary, and **no** price list.

Then clean up: `Remove-Item -Recurse -Force "$env:TEMP\argus-fixture"`. The PNGs stay in `.superpowers\sdd` (untracked).

If token tuning was needed in item 1, rerun Step 7 before committing.

- [ ] **Step 10: Commit**

```powershell
git add wwwroot/css/site.css Pages/Analysis/UiStateFixture.cshtml.cs Pages/Analysis/UiStateFixture.cshtml tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs
git commit -m "feat(analysis): denser insight cards and ranked fixture view for layout checks" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

Run `git status --short` afterwards. `.superpowers/**` and `.github/.10x-cli-manifest.json` may show as modified or untracked; they must stay unstaged.

---

## Self-review

**Spec coverage (addendum changes → tasks):**

| # | Spec change | Task |
| --- | --- | --- |
| 1 | Sidebar headline "Nowa analiza", ≈40–50% smaller (2rem vs clamp up to 3.75rem) | 4 |
| 2 | Simplified sidebar options (glyph, name, score) + highlighted selection | 4 (glyph from 2) |
| 3 | "Generuj ranking" button (page + fixture) | 4 |
| 4 | Header summary strip with the four labels | 4 (pure model + tests), 5 (fixture/visual) |
| 5 | Score block: "#1", score number, Silny/Umiarkowany/Słaby with token colors | 1 (band), 3 (markup/CSS) |
| 6 | Per-asset accent tokens on glyph background + card accent bar | 1 (accent), 2 (CSS/markup) |
| 7 | Short names everywhere, ticker beneath | 1 (catalog), 3 (card keeps ticker line), 4 (sidebar) |
| 8 | Denser cards (target ≤ 382px vs 545px baseline) | 5 (+ smaller header in 3) |
| 9 | Factor words with ±1% thresholds; muted "n/d" | 1 (logic), 3 (markup/CSS) |
| 10 | Deterministic past-tense takeaway from strongest factor + rank | 1 |
| 11 | Price table removed from `_PriceHistoryChart` | 3 |
| — | Decision: own SVG glyphs, monogram fallback | 2 |
| — | Decision: 2 / 4 (≥ 1800px) / 1 column grid | 3 (CSS), 5 (visual proof) |

**Constraint checks:**
- `OpportunityScorer.cs`, `AnalysisContracts.cs` and `AnalysisService.cs` are never touched.
- There is no JS chart library and no JS change.
- Views have no literal colors or sizes. The only numeric values in views are SVG `viewBox` drawing coordinates.
- New strings contain none of the forbidden words. Checked case-insensitively for "buy", "provider" and "exception" across "Bitcoin", "Generuj ranking", "Podsumowanie rankingu", "Przeanalizowane aktywa" and the class names.

**Deliberate test updates** (all elements removed or renamed by the addendum):
- `"Apple Inc."` → `">Apple</h3>"`
- The old "Najsilniejszy sygnał…" sentence → the takeaway
- `"#1 · 100"` / `"#2 · 50"` → `asset-option__score` badges
- `<time datetime=` → `DoesNotContain`
- The presenter test file is rewritten for the new record shapes

**Type consistency:**
- `AssetIdentity(Id, Name, Monogram, Accent)` and `FactorBarView(…, IsAvailable, Direction, Word)`.
- `AssetInsightView(…, Takeaway, Sparkline, Band)` is used identically in Tasks 1, 3 and 5.
- `InsightCardModel.Rank` is used in Tasks 1 and 3.
- `AnalysisSummaryStripView` / `SummaryPerformer` are used in Tasks 4 and 5.
- `AssetOptionModel(AssetId, IsSelected, Score)` is used in Tasks 4 and 5.

