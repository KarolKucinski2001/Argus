# Analysis Insight-First Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn `/analysis` result cards into insight-first dashboard cards (identity, score, 7-day change, per-asset sparkline, factor bars, one-line data insight, details collapsed) and give each development asset its own price shape.

**Architecture:** A pure presenter (`Analysis/AnalysisInsightPresenter.cs`) converts an `AssetAnalysis` into an `AssetInsightView` (identity, tone, factor bars, insight sentence, sparkline geometry). A Razor partial `_InsightCard.cshtml` renders that view; the existing detailed chart moves unchanged into `_PriceHistoryChart.cshtml` inside a collapsed `<details>`. The Testing fixture renders the same partial from sample data, so fixture and production cannot drift.

**Tech Stack:** ASP.NET Core Razor Pages (.NET 10), xUnit + `WebApplicationFactory`, plain CSS tokens in `wwwroot/css/site.css`.

**Spec:** `docs/superpowers/specs/2026-10-21-analysis-insight-dashboard-design.md`

## Global Constraints

- No predictions or forecast cards; never render "prognoz", "buy", "guaranteed", "best investment", "top opportunity".
- No new scoring factors. `Analysis/OpportunityScorer.cs`, `Analysis/AnalysisContracts.cs`, `Analysis/AnalysisService.cs` stay unchanged.
- No new production market-data providers; `DevelopmentMarketDataProvider` remains Development/Testing only.
- No client-side chart library; sparklines are server-rendered SVG.
- Views contain no literal colors or arbitrary layout values; new colors, sizes and spacing are tokens/classes in `wwwroot/css/site.css`.
- UI copy is Polish, matching the existing page. Labels used by this plan: "Wniosek z danych", "Szczegóły i metodologia", "Brak danych do wykresu", "n/d".
- The text "chwilowo niedostępne" must never render on `/analysis`.
- Keep every existing CSS class asserted by `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs` (they move inside `<details>`, they are not deleted).
- Never stage `.superpowers/**`, `.github/.10x-cli-manifest.json`, or `.github/skills/10x-ui/`.
- Every commit message ends with the trailer `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`.
- Test command: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`. Build command: `dotnet build .bootstrap-scaffold.csproj --no-restore` (0 warnings expected).

### Clarification of the spec (decided while planning)

The spec says the insight line shows "the AI summary when available, otherwise the presenter insight". The current contract cannot distinguish an AI summary from the deterministic fallback (`AnalysisService` returns both as `AnalysisSummaryStatus.Generated`), and no AI generator is registered. To keep contracts unchanged, the card's insight line **always** shows the presenter insight labeled "Wniosek z danych"; the summary text (AI or deterministic) renders inside `<details>` under "Podsumowanie".

---

## File Structure

| File | Responsibility |
| --- | --- |
| `Analysis/AssetIdentityCatalog.cs` (create) | Ticker → display name + monogram. |
| `Analysis/AnalysisInsightPresenter.cs` (create) | Pure mapping `AssetAnalysis` → `AssetInsightView`. |
| `MarketData/DevelopmentMarketDataProvider.cs` (modify) | Per-asset deterministic price series. |
| `Pages/Analysis/AnalysisFormatting.cs` (create) | Shared invariant formatting helpers for analysis views. |
| `Pages/Analysis/InsightCardModel.cs` (create) | Model passed to `_InsightCard`. |
| `Pages/Analysis/_InsightCard.cshtml` (create) | One insight card (`<li>`). |
| `Pages/Analysis/_PriceHistoryChart.cshtml` (create) | Existing detailed chart, moved verbatim. |
| `Pages/Analysis/Index.cshtml` (modify) | Compact sidebar, card loop via partial, shorter copy. |
| `Pages/Analysis/Index.cshtml.cs` (modify) | Make the two message-mapping methods `static`. |
| `Pages/Analysis/UiStateFixture.cshtml(.cs)` (modify) | Render result states through `_InsightCard`. |
| `wwwroot/css/site.css` (modify) | Insight tokens and styles. |
| `tests/Argus.Authentication.Tests/AnalysisInsightPresenterTests.cs` (create) | Presenter unit tests. |
| `tests/Argus.Authentication.Tests/MarketDataProviderEnvironmentTests.cs` (modify) | Dev provider shape tests. |
| `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs` (modify) | Rendered-HTML contract tests. |

---

### Task 1: Asset identity and insight presenter

**Files:**
- Create: `Analysis/AssetIdentityCatalog.cs`
- Create: `Analysis/AnalysisInsightPresenter.cs`
- Test: `tests/Argus.Authentication.Tests/AnalysisInsightPresenterTests.cs`

**Interfaces:**
- Consumes: `AssetAnalysis`, `AnalysisFactor`, `AnalysisChartPoint`, `PerformanceMetric` from `Analysis/AnalysisContracts.cs` (unchanged).
- Produces (namespace `_bootstrap_scaffold.Analysis`):
  - `public sealed record AssetIdentity(string Id, string Name, string Monogram)`
  - `public static class AssetIdentityCatalog { public static AssetIdentity For(string id); }`
  - `public enum InsightTone { Positive, Negative, Neutral }`
  - `public sealed record FactorBarView(string Key, string Label, decimal? Percentage, decimal? Value, bool IsAvailable)`
  - `public sealed record SparklineView(string ViewBox, string LinePoints, string AreaPath)`
  - `public sealed record AssetInsightView(AssetIdentity Identity, InsightTone Tone, decimal? SevenDayChange, IReadOnlyList<FactorBarView> FactorBars, string Insight, SparklineView? Sparkline)`
  - `public static class AnalysisInsightPresenter { public static AssetInsightView Present(AssetAnalysis analysis); public static decimal Normalize(decimal percentage); }`

- [ ] **Step 1: Write the failing tests**

Create `tests/Argus.Authentication.Tests/AnalysisInsightPresenterTests.cs`:

```csharp
using Xunit;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace Argus.Authentication.Tests;

public sealed class AnalysisInsightPresenterTests
{
    [Theory]
    [InlineData("AAPL", "Apple Inc.", "A")]
    [InlineData("MSFT", "Microsoft", "M")]
    [InlineData("BTC-USD", "Bitcoin", "₿")]
    [InlineData("eth-usd", "Ethereum", "Ξ")]
    public void Known_tickers_map_to_name_and_monogram(string id, string name, string monogram)
    {
        var identity = AssetIdentityCatalog.For(id);

        Assert.Equal(name, identity.Name);
        Assert.Equal(monogram, identity.Monogram);
    }

    [Fact]
    public void Unknown_ticker_falls_back_to_ticker_and_first_letter()
    {
        var identity = AssetIdentityCatalog.For("xyz");

        Assert.Equal("xyz", identity.Id);
        Assert.Equal("xyz", identity.Name);
        Assert.Equal("X", identity.Monogram);
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

    [Fact]
    public void Insight_names_strongest_and_weakest_available_factor()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", -1.1m, 3m, 8.2m, 10m, 20m, 15m));

        Assert.Equal(
            "Najsilniejszy sygnał: Trend 30d (+8.2%); najsłabszy: Momentum 24h (-1.1%).",
            view.Insight);
    }

    [Fact]
    public void Insight_with_one_factor_names_only_that_factor()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, null, 5m, 10m, 11m));

        Assert.Equal("Trend 30d: +5%.", view.Insight);
    }

    [Fact]
    public void Insight_without_factors_says_there_is_not_enough_data()
    {
        var view = AnalysisInsightPresenter.Present(Analysis("AAPL", null, null, null));

        Assert.Equal("Za mało danych rynkowych, aby wskazać sygnał.", view.Insight);
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
Expected: build FAIL with `CS0103`/`CS0246` for `AssetIdentityCatalog`, `AnalysisInsightPresenter`, `InsightTone`.

- [ ] **Step 3: Create `Analysis/AssetIdentityCatalog.cs`**

```csharp
namespace _bootstrap_scaffold.Analysis;

public sealed record AssetIdentity(string Id, string Name, string Monogram);

public static class AssetIdentityCatalog
{
    private static readonly IReadOnlyDictionary<string, AssetIdentity> Known =
        new Dictionary<string, AssetIdentity>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new("AAPL", "Apple Inc.", "A"),
            ["MSFT"] = new("MSFT", "Microsoft", "M"),
            ["BTC-USD"] = new("BTC-USD", "Bitcoin", "₿"),
            ["ETH-USD"] = new("ETH-USD", "Ethereum", "Ξ")
        };

    public static AssetIdentity For(string id) =>
        Known.TryGetValue(id, out var identity)
            ? identity
            : new AssetIdentity(
                id,
                id,
                string.IsNullOrEmpty(id) ? "?" : id[..1].ToUpperInvariant());
}
```

- [ ] **Step 4: Create `Analysis/AnalysisInsightPresenter.cs`**

```csharp
using System.Globalization;

namespace _bootstrap_scaffold.Analysis;

public enum InsightTone
{
    Positive,
    Negative,
    Neutral
}

public sealed record FactorBarView(
    string Key,
    string Label,
    decimal? Percentage,
    decimal? Value,
    bool IsAvailable);

public sealed record SparklineView(
    string ViewBox,
    string LinePoints,
    string AreaPath);

public sealed record AssetInsightView(
    AssetIdentity Identity,
    InsightTone Tone,
    decimal? SevenDayChange,
    IReadOnlyList<FactorBarView> FactorBars,
    string Insight,
    SparklineView? Sparkline);

public static class AnalysisInsightPresenter
{
    private const decimal SparklineWidth = 100m;
    private const decimal SparklineHeight = 32m;
    private const decimal SparklinePadding = 2m;

    private static readonly (string Key, string Label)[] FactorOrder =
    [
        ("momentum-24h", "Momentum 24h"),
        ("momentum-7d", "Momentum 7d"),
        ("trend-30d", "Trend 30d")
    ];

    public static AssetInsightView Present(AssetAnalysis analysis)
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
            BuildInsight(bars),
            BuildSparkline(analysis.ChartPoints));
    }

    // Same 0–100 scale as OpportunityScorer.Normalize, rounded for display.
    public static decimal Normalize(decimal percentage) =>
        Math.Round(
            Math.Clamp((percentage + 20m) / 40m * 100m, 0m, 100m),
            0,
            MidpointRounding.AwayFromZero);

    private static FactorBarView CreateBar(string key, string label, IReadOnlyList<AnalysisFactor> factors)
    {
        var factor = factors.FirstOrDefault(item => item.Key == key);
        var isAvailable = factor is { IsAvailable: true, Percentage: not null };

        return new FactorBarView(
            key,
            label,
            factor?.Percentage,
            isAvailable ? Normalize(factor!.Percentage!.Value) : null,
            isAvailable);
    }

    private static InsightTone ToneFor(decimal? sevenDay) =>
        sevenDay switch
        {
            > 0m => InsightTone.Positive,
            < 0m => InsightTone.Negative,
            _ => InsightTone.Neutral
        };

    private static string BuildInsight(IReadOnlyList<FactorBarView> bars)
    {
        var available = bars
            .Where(bar => bar.IsAvailable)
            .OrderByDescending(bar => bar.Percentage!.Value)
            .ToArray();

        return available.Length switch
        {
            0 => "Za mało danych rynkowych, aby wskazać sygnał.",
            1 => $"{available[0].Label}: {FormatSignedPercent(available[0].Percentage!.Value)}.",
            _ => $"Najsilniejszy sygnał: {available[0].Label} ({FormatSignedPercent(available[0].Percentage!.Value)}); " +
                 $"najsłabszy: {available[^1].Label} ({FormatSignedPercent(available[^1].Percentage!.Value)})."
        };
    }

    private static SparklineView? BuildSparkline(IReadOnlyList<AnalysisChartPoint> chartPoints)
    {
        var closes = chartPoints
            .OrderBy(point => point.Timestamp)
            .Select(point => point.Close)
            .ToArray();
        if (closes.Length < 2)
        {
            return null;
        }

        var minimum = closes.Min();
        var range = closes.Max() - minimum;
        var plotHeight = SparklineHeight - 2m * SparklinePadding;
        var coordinates = closes
            .Select((close, index) =>
            {
                var x = index * SparklineWidth / (closes.Length - 1);
                var normalized = range == 0m ? 0.5m : (close - minimum) / range;
                var y = SparklinePadding + (1m - normalized) * plotHeight;
                return $"{Format(x)},{Format(y)}";
            })
            .ToArray();
        var baseline = Format(SparklineHeight);

        return new SparklineView(
            $"0 0 {Format(SparklineWidth)} {baseline}",
            string.Join(" ", coordinates),
            $"M0,{baseline} L{string.Join(" L", coordinates)} L{Format(SparklineWidth)},{baseline} Z");
    }

    private static string FormatSignedPercent(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:+0.##;-0.##;0}%");

    private static string Format(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter "FullyQualifiedName~AnalysisInsightPresenterTests"`
Expected: PASS (all presenter tests).

- [ ] **Step 6: Commit**

```powershell
git add Analysis/AssetIdentityCatalog.cs Analysis/AnalysisInsightPresenter.cs tests/Argus.Authentication.Tests/AnalysisInsightPresenterTests.cs
git commit -m "feat: add analysis insight presenter" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 2: Per-asset development price shapes

**Files:**
- Modify: `MarketData/DevelopmentMarketDataProvider.cs` (whole file)
- Test: `tests/Argus.Authentication.Tests/MarketDataProviderEnvironmentTests.cs`

**Interfaces:**
- Consumes: `IMarketDataProvider`, `MarketDataRequest`, `MarketDataResult`, `HistoricalPricePoint`, `MarketQuote` (unchanged contracts).
- Produces: unchanged public surface (`DevelopmentMarketDataProvider.GetAsync`). Behavior change: each known asset gets a distinct deterministic series; the quote equals the last close.

- [ ] **Step 1: Write the failing tests**

Add these two tests inside `MarketDataProviderEnvironmentTests` (after `Illustrative_provider_returns_only_requested_assets_and_history`):

```csharp
    [Fact]
    public async Task Illustrative_provider_gives_each_known_asset_a_distinct_price_shape()
    {
        var results = await new DevelopmentMarketDataProvider().GetAsync(FourAssetRequest());

        var shapes = results
            .Select(result =>
            {
                var closes = result.HistoricalPoints.Select(point => point.Close).ToArray();
                var minimum = closes.Min();
                var range = closes.Max() - minimum;
                return closes.Select(close => Math.Round((close - minimum) / range, 2)).ToArray();
            })
            .ToArray();

        Assert.Equal(4, shapes.Length);
        for (var left = 0; left < shapes.Length; left++)
        {
            for (var right = left + 1; right < shapes.Length; right++)
            {
                var differingPoints = shapes[left].Zip(shapes[right]).Count(pair => pair.First != pair.Second);
                Assert.True(
                    differingPoints > shapes[left].Length * 0.3,
                    $"Assets {left} and {right} share {shapes[left].Length - differingPoints} of {shapes[left].Length} normalized points.");
            }
        }
    }

    [Fact]
    public async Task Illustrative_provider_is_deterministic_and_quotes_the_last_close()
    {
        var provider = new DevelopmentMarketDataProvider();

        var first = await provider.GetAsync(FourAssetRequest());
        var second = await provider.GetAsync(FourAssetRequest());

        for (var index = 0; index < first.Count; index++)
        {
            var firstCloses = first[index].HistoricalPoints.Select(point => point.Close);
            Assert.Equal(firstCloses, second[index].HistoricalPoints.Select(point => point.Close));
            Assert.All(first[index].HistoricalPoints, point => Assert.True(point.Close > 0m));
            Assert.Equal(first[index].HistoricalPoints[^1].Close, first[index].CurrentQuote?.Value);
        }
    }

    private static MarketDataRequest FourAssetRequest() =>
        new(
            [
                new AssetReference("AAPL", "AAPL", MarketAssetClass.Equity),
                new AssetReference("MSFT", "MSFT", MarketAssetClass.Equity),
                new AssetReference("BTC-USD", "BTC-USD", MarketAssetClass.Crypto),
                new AssetReference("ETH-USD", "ETH-USD", MarketAssetClass.Crypto)
            ],
            new MarketDataHistoryRequest(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 3, 31),
                MarketDataGranularity.Daily));
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketDataProviderEnvironmentTests"`
Expected: `Illustrative_provider_gives_each_known_asset_a_distinct_price_shape` FAILS (the current saw-tooth makes every normalized series identical) and `..._quotes_the_last_close` FAILS (quote is the base value).

- [ ] **Step 3: Replace `MarketData/DevelopmentMarketDataProvider.cs`**

```csharp
namespace _bootstrap_scaffold.MarketData;

public sealed class DevelopmentMarketDataProvider : IMarketDataProvider
{
    private static readonly SeriesShape DefaultShape = new(100m, 0.0002, 0.02, 30, 0.0, 7);

    // Illustrative, deterministic shapes so each development asset looks different.
    private static readonly IReadOnlyDictionary<string, SeriesShape> Shapes =
        new Dictionary<string, SeriesShape>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new(215.50m, 0.0006, 0.03, 45, 0.0, 11),
            ["MSFT"] = new(420.25m, 0.0003, 0.02, 30, 1.2, 23),
            ["BTC-USD"] = new(68000m, 0.0012, 0.08, 60, 2.4, 37),
            ["ETH-USD"] = new(3500m, -0.0004, 0.10, 21, 0.6, 53)
        };

    public Task<IReadOnlyList<MarketDataResult>> GetAsync(
        MarketDataRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = request.Assets.Select(asset => CreateResult(asset, request.History)).ToArray();
        return Task.FromResult<IReadOnlyList<MarketDataResult>>(results);
    }

    private static MarketDataResult CreateResult(
        AssetReference asset,
        MarketDataHistoryRequest history)
    {
        var observedAt = new DateTimeOffset(history.End.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var retrievedAt = observedAt.AddMinutes(1);
        var freshness = new MarketDataFreshness(
            observedAt,
            retrievedAt,
            retrievedAt.AddMinutes(5),
            false);
        var shape = Shapes.GetValueOrDefault(asset.Id, DefaultShape);
        var dayCount = Math.Max(history.End.DayNumber - history.Start.DayNumber + 1, 0);
        var closes = GenerateCloses(shape, dayCount);
        var points = closes
            .Select((close, offset) =>
            {
                var date = history.Start.AddDays(offset);
                var timestamp = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                return new HistoricalPricePoint(
                    timestamp,
                    close,
                    Round(close * 1.01m),
                    Round(close * 0.99m),
                    close,
                    null,
                    "USD",
                    true);
            })
            .ToArray();
        var quoteValue = closes.Length > 0 ? closes[^1] : shape.BaseValue;

        return new MarketDataResult(
            asset,
            history,
            new MarketQuote(quoteValue, "USD", observedAt, freshness, MarketDataQuality.Complete),
            points,
            new MarketDataSource(
                "illustrative-development",
                "Illustrative data for local development and testing only.",
                false,
                null),
            freshness,
            MarketDataQuality.Complete,
            []);
    }

    private static decimal[] GenerateCloses(SeriesShape shape, int count)
    {
        var random = new Random(shape.Seed);
        var closes = new decimal[count];
        for (var offset = 0; offset < count; offset++)
        {
            var wave = shape.Amplitude * Math.Sin(2 * Math.PI * offset / shape.PeriodDays + shape.Phase);
            var noise = (random.NextDouble() - 0.5) * shape.Amplitude * 0.4;
            var factor = 1 + shape.TrendPerDay * offset + wave + noise;
            closes[offset] = Round(shape.BaseValue * (decimal)factor);
        }

        return closes;
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed record SeriesShape(
        decimal BaseValue,
        double TrendPerDay,
        double Amplitude,
        double PeriodDays,
        double Phase,
        int Seed);
}
```

- [ ] **Step 4: Run the provider tests, then the full suite**

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter "FullyQualifiedName~MarketDataProviderEnvironmentTests"`
Expected: PASS.

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
Expected: PASS (no other test depends on the old saw-tooth values).

- [ ] **Step 5: Commit**

```powershell
git add MarketData/DevelopmentMarketDataProvider.cs tests/Argus.Authentication.Tests/MarketDataProviderEnvironmentTests.cs
git commit -m "feat: give development assets distinct price shapes" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 3: Insight card partial and compact `/analysis` page

**Files:**
- Create: `Pages/Analysis/AnalysisFormatting.cs`
- Create: `Pages/Analysis/InsightCardModel.cs`
- Create: `Pages/Analysis/_InsightCard.cshtml`
- Create: `Pages/Analysis/_PriceHistoryChart.cshtml`
- Modify: `Pages/Analysis/Index.cshtml` (sidebar, header, results loop, `@functions` removed)
- Modify: `Pages/Analysis/Index.cshtml.cs:139-161` (`MapAssetErrorMessage`, `MapSummaryStatusMessage` become `public static`)
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs`

**Interfaces:**
- Consumes (Task 1): `AnalysisInsightPresenter.Present(AssetAnalysis) : AssetInsightView`, `AssetIdentityCatalog.For(string) : AssetIdentity`, `InsightTone`, `FactorBarView`, `SparklineView`.
- Produces (namespace `_bootstrap_scaffold.Pages.Analysis`, used by Task 4):
  - `public sealed record InsightCardModel(AssetAnalysis Result, int Index, MarketQuote? Quote, string? ErrorMessage, string SummaryStatusMessage)`
  - Partial `_InsightCard` (model `InsightCardModel`) renders one `<li class="analysis-result-card ...">`; must be placed inside `<ol class="analysis-results__list">`.
  - `public static string IndexModel.MapAssetErrorMessage(MarketDataErrorCategory? category)`, `public static string IndexModel.MapSummaryStatusMessage(AnalysisSummaryStatus status)`.
  - `public static class AnalysisFormatting` with `Metric`, `NullablePercent`, `SignedPercent`, `NullableDecimal`, `Weight`, `Quality`, `Warning`, `Decimal`.
  - New CSS class hooks (styled in Task 4): `insight-card`, `insight-card--positive|negative|neutral`, `insight-card__monogram`, `insight-card__identity`, `insight-card__ticker`, `insight-card__chips`, `insight-card__chip`, `insight-card__chip--warning`, `insight-card__error`, `insight-card__headline`, `insight-card__change`, `insight-card__change-label`, `insight-card__sparkline`, `insight-card__sparkline-area`, `insight-card__sparkline-line`, `insight-card__sparkline-empty`, `insight-card__bars`, `insight-card__bar`, `insight-card__bar--unavailable`, `insight-card__bar-label`, `insight-card__bar-meter`, `insight-card__bar-value`, `insight-card__insight`, `insight-card__insight-label`, `insight-card__details`, `asset-option__monogram`, `asset-option__rank`.

- [ ] **Step 1: Write the failing rendered-HTML tests**

Add to `AnalysisAccessContractTests` (after `Analysis_results_render_factors_summary_and_chart_data`):

```csharp
    [Fact]
    public async Task Insight_card_puts_identity_change_sparkline_bars_and_insight_first()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateScoredResult("AAPL", MarketAssetClass.Equity, 120m, 100m, 96m, 80m));
        await using var factory = new AnalysisProviderFactory(provider, _ => { });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostAnalysisAsync(client, token, "AAPL");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        var card = html[html.IndexOf("class=\"analysis-results__list\"", StringComparison.Ordinal)..];
        var details = card.IndexOf("<details class=\"insight-card__details\"", StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(details > 0, "The card must contain a collapsed details element.");
        Assert.DoesNotContain("<details class=\"insight-card__details\" open", card);
        Assert.Contains("Apple Inc.", card[..details]);
        Assert.Contains("class=\"insight-card__monogram\"", card[..details]);
        Assert.Contains("+25%", card[..details]);
        Assert.Contains("data-sparkline", card[..details]);
        Assert.Equal(3, Regex.Matches(card[..details], "data-factor-bar=").Count);
        Assert.Contains("Wniosek z danych", card[..details]);
        Assert.Contains("Najsilniejszy sygnał: Trend 30d (+50%); najsłabszy: Momentum 24h (+20%).", card[..details]);
        Assert.Contains("analysis-result-card__metrics", card[details..]);
        Assert.Contains("analysis-result-card__factor-list", card[details..]);
        Assert.Contains("data-chart-svg", card[details..]);
        Assert.Contains("Podsumowanie", card[details..]);
        Assert.DoesNotContain("chwilowo niedostępne", html);
    }

    [Fact]
    public async Task Sidebar_shows_rank_and_score_and_failed_cards_have_no_sparkline()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(
            CreateScoredResult("AAPL", MarketAssetClass.Equity, 120m, 120m, 120m, 120m),
            CreateScoredResult("MSFT", MarketAssetClass.Equity, 120m, 80m, 80m, 80m),
            CreateErrorResult(
                "BTC-USD",
                MarketAssetClass.Crypto,
                new MarketDataError(MarketDataErrorCategory.UpstreamUnavailable, "RAW outage", true)));
        await using var factory = new AnalysisProviderFactory(provider, _ => { });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostAnalysisAsync(client, token, "AAPL", "MSFT", "BTC-USD");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("class=\"asset-option__monogram\"", html);
        Assert.Contains("#1 · 100", html);
        Assert.Contains("#2 · 50", html);
        Assert.Equal(2, Regex.Matches(html, "data-sparkline").Count);
        Assert.Contains("Nie udało się przeanalizować tego aktywa", html);
        Assert.DoesNotContain("RAW outage", html);
    }

    [Fact]
    public async Task Results_boundary_is_a_single_educational_line()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateScoredResult("AAPL", MarketAssetClass.Equity, 120m, 100m, 96m, 80m));
        await using var factory = new AnalysisProviderFactory(provider, _ => { });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostAnalysisAsync(client, token, "AAPL");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Contains("Wyniki mają charakter edukacyjny i nie stanowią porady inwestycyjnej.", html);
        Assert.DoesNotContain("spersonalizowanych obietnic", html);
        Assert.DoesNotContain("Rankingi, kontekst rynkowy i czynniki wyjaśniające", html);
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter "FullyQualifiedName~Insight_card|FullyQualifiedName~Sidebar_shows|FullyQualifiedName~Results_boundary"`
Expected: 3 FAIL (no `insight-card__details`, no monogram, long boundary still present).

- [ ] **Step 3: Create `Pages/Analysis/AnalysisFormatting.cs`**

Move the helpers from the `@functions` block of `Index.cshtml` into this class (same behavior):

```csharp
using System.Globalization;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Pages.Analysis;

public static class AnalysisFormatting
{
    public static string Metric(PerformanceMetric metric) =>
        metric.IsAvailable && metric.Percentage.HasValue
            ? SignedPercent(metric.Percentage.Value)
            : "niedostępne";

    public static string NullablePercent(decimal? value) =>
        value.HasValue ? SignedPercent(value.Value) : "niedostępne";

    public static string SignedPercent(decimal value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value:+0.##;-0.##;0}%");

    public static string NullableDecimal(decimal? value) =>
        value.HasValue ? Decimal(value.Value) : "niedostępne";

    public static string Weight(decimal value) =>
        $"{(Math.Clamp(value, 0m, 1m) * 100m).ToString("0.##", CultureInfo.InvariantCulture)}%";

    public static string Quality(MarketDataQuality quality) =>
        quality switch
        {
            MarketDataQuality.Incomplete => "Dane niepełne",
            MarketDataQuality.Stale => "Dane nieaktualne",
            MarketDataQuality.Unavailable => "Dane niedostępne",
            _ => "Dane kompletne"
        };

    public static string Warning(MarketDataWarningCategory category) =>
        category switch
        {
            MarketDataWarningCategory.MissingCurrentQuote => "Brak bieżącej ceny",
            MarketDataWarningCategory.MissingHistory => "Brak historii",
            MarketDataWarningCategory.IncompleteHistory => "Historia niepełna",
            MarketDataWarningCategory.StaleData => "Dane nieaktualne",
            MarketDataWarningCategory.DelayedData => "Dane opóźnione",
            MarketDataWarningCategory.ProviderQuota => "Ograniczona dostępność danych",
            MarketDataWarningCategory.UpstreamUnavailable => "Źródło danych chwilowo niedostępne",
            _ => "Ograniczona jakość danych"
        };

    public static string Decimal(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
```

- [ ] **Step 4: Create `Pages/Analysis/InsightCardModel.cs`**

```csharp
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Pages.Analysis;

public sealed record InsightCardModel(
    AssetAnalysis Result,
    int Index,
    MarketQuote? Quote,
    string? ErrorMessage,
    string SummaryStatusMessage);
```

- [ ] **Step 5: Make the two message mappers static**

In `Pages/Analysis/Index.cshtml.cs`, change only the signatures:

```csharp
    public static string MapAssetErrorMessage(MarketDataErrorCategory? category) =>
```

```csharp
    public static string MapSummaryStatusMessage(AnalysisSummaryStatus status) =>
```

- [ ] **Step 6: Create `Pages/Analysis/_PriceHistoryChart.cshtml`**

This is a mechanical move of the existing chart. Create the file with this header:

```cshtml
@using System.Globalization
@using _bootstrap_scaffold.Analysis
@using _bootstrap_scaffold.Pages.Analysis
@model AssetAnalysis
@{
    var result = Model;
    var chartPoints = result.ChartPoints.OrderBy(point => point.Timestamp).ToArray();
```

Then copy `Pages/Analysis/Index.cshtml` lines 239–266 (from `var chartId = $"analysis-chart-{result.Asset.Id}";` through the closing `}));` of `pathData`) directly below, close the code block with `}`, and copy lines 268–381 (from `<section id="@chartId"` through its closing `</section>`) below that. In the copied text, replace every `FormatDecimal(` with `AnalysisFormatting.Decimal(`. Make no other changes; the geometry constants intentionally stay as they are (accepted by the user; this chart is replaced by the sparkline in the card face).

- [ ] **Step 7: Create `Pages/Analysis/_InsightCard.cshtml`**

```cshtml
@using _bootstrap_scaffold.Analysis
@using _bootstrap_scaffold.MarketData
@using _bootstrap_scaffold.Pages.Analysis
@model InsightCardModel
@{
    var result = Model.Result;
    var insight = AnalysisInsightPresenter.Present(result);
    var toneClass = $"insight-card--{insight.Tone.ToString().ToLowerInvariant()}";
    var cardStateClass = result.Error is not null
        ? "analysis-result-card--error"
        : result.Score.HasValue
            ? result.Freshness.IsStale
                ? "analysis-result-card--success analysis-result-card--stale"
                : "analysis-result-card--success"
            : "analysis-result-card--insufficient-evidence";
    var isDelayed = result.IsDelayed || result.Warnings.Any(warning =>
        warning.Category == MarketDataWarningCategory.DelayedData);
}

<li class="analysis-result-card @cardStateClass">
    <article class="analysis-result-card__body insight-card @toneClass"
             aria-labelledby="analysis-result-@result.Asset.Id"
             tabindex="0">
        <header class="analysis-result-card__header">
            <span class="insight-card__monogram" aria-hidden="true">@insight.Identity.Monogram</span>
            <div class="insight-card__identity">
                <h3 id="analysis-result-@result.Asset.Id">@insight.Identity.Name</h3>
                <p class="insight-card__ticker">@result.Asset.Id</p>
            </div>
            <p class="analysis-result-card__rank">@(result.Score.HasValue ? $"Miejsce {Model.Index + 1}" : "Wynik nierankingowy")</p>
            @if (result.Score.HasValue)
            {
                <p class="analysis-result-card__score">Score @AnalysisFormatting.Decimal(result.Score.Value)/100</p>
            }
            else if (result.Error is null)
            {
                <p class="analysis-result-card__status">Niewystarczające dane do wyliczenia score.</p>
            }
        </header>

        @if (result.Error is not null)
        {
            <p class="analysis-result-card__status insight-card__error" role="status">@Model.ErrorMessage</p>
        }
        else
        {
            <div class="insight-card__chips">
                @if (isDelayed)
                {
                    <span class="insight-card__chip insight-card__chip--warning" role="status">Dane opóźnione</span>
                }
                @if (result.Freshness.IsStale)
                {
                    <span class="insight-card__chip insight-card__chip--warning" role="status">Dane nieaktualne</span>
                }
                @if (result.Quality != MarketDataQuality.Complete)
                {
                    <span class="insight-card__chip insight-card__chip--warning" role="status">@AnalysisFormatting.Quality(result.Quality)</span>
                }
            </div>

            <div class="insight-card__headline">
                <p class="insight-card__change">@AnalysisFormatting.NullablePercent(insight.SevenDayChange)</p>
                <p class="insight-card__change-label">7 dni</p>
            </div>

            @if (insight.Sparkline is { } sparkline)
            {
                <svg class="insight-card__sparkline"
                     viewBox="@sparkline.ViewBox"
                     preserveAspectRatio="none"
                     role="img"
                     aria-label="Trend ceny: @insight.Identity.Name"
                     data-sparkline>
                    <path class="insight-card__sparkline-area" d="@sparkline.AreaPath" />
                    <polyline class="insight-card__sparkline-line" points="@sparkline.LinePoints" />
                </svg>
            }
            else
            {
                <p class="insight-card__sparkline-empty" role="status">Brak danych do wykresu</p>
            }

            <ul class="insight-card__bars" aria-label="Czynniki rankingu dla @result.Asset.Id">
                @foreach (var bar in insight.FactorBars)
                {
                    <li class="insight-card__bar @(bar.IsAvailable ? null : "insight-card__bar--unavailable")"
                        data-factor-bar="@bar.Key">
                        <span class="insight-card__bar-label">@bar.Label</span>
                        <meter class="insight-card__bar-meter"
                               min="0"
                               max="100"
                               value="@AnalysisFormatting.Decimal(bar.Value ?? 0m)"
                               aria-label="@bar.Label">@AnalysisFormatting.Decimal(bar.Value ?? 0m)</meter>
                        <span class="insight-card__bar-value">@(bar.IsAvailable ? AnalysisFormatting.NullablePercent(bar.Percentage) : "n/d")</span>
                    </li>
                }
            </ul>

            <p class="insight-card__insight">
                <span class="insight-card__insight-label">Wniosek z danych</span>
                @insight.Insight
            </p>

            <details class="insight-card__details">
                <summary>Szczegóły i metodologia</summary>

                <dl class="analysis-result-card__metrics">
                    <div class="analysis-result-card__metric">
                        <dt>Cena aktualna</dt>
                        <dd>@(Model.Quote is { } quote ? $"{AnalysisFormatting.Decimal(quote.Value)} {quote.Currency}" : "niedostępna")</dd>
                    </div>
                    <div class="analysis-result-card__metric">
                        <dt>24 godziny</dt>
                        <dd>@AnalysisFormatting.Metric(result.Performance.Change24Hours)</dd>
                    </div>
                    <div class="analysis-result-card__metric">
                        <dt>7 dni</dt>
                        <dd>@AnalysisFormatting.Metric(result.Performance.Change7Days)</dd>
                    </div>
                    <div class="analysis-result-card__metric">
                        <dt>30 dni</dt>
                        <dd>@AnalysisFormatting.Metric(result.Performance.Change30Days)</dd>
                    </div>
                    <div class="analysis-result-card__metric">
                        <dt>Jakość danych</dt>
                        <dd>@result.Quality</dd>
                    </div>
                    <div class="analysis-result-card__metric">
                        <dt>Świeżość danych</dt>
                        <dd>@(result.Freshness.IsStale ? "stale" : "current")</dd>
                    </div>
                </dl>

                @if (result.Warnings.Count > 0)
                {
                    <section class="analysis-result-card__warnings" aria-label="Ostrzeżenia dla @result.Asset.Id">
                        <h4>Ostrzeżenia</h4>
                        <ul>
                            @foreach (var warning in result.Warnings.Select(warning => AnalysisFormatting.Warning(warning.Category)).Distinct())
                            {
                                <li>@warning</li>
                            }
                        </ul>
                    </section>
                }

                <section class="analysis-result-card__factors" aria-label="Czynniki dla @result.Asset.Id">
                    <h4>Czynniki</h4>
                    <ul class="analysis-result-card__factor-list">
                        @foreach (var factor in result.Factors)
                        {
                            <li class="analysis-result-card__factor">
                                <strong>@factor.Label</strong>:
                                @if (factor.IsAvailable)
                                {
                                    <span>@AnalysisFormatting.NullablePercent(factor.Percentage)</span>
                                    <span class="analysis-result-card__factor-weight">weight @AnalysisFormatting.Weight(factor.Weight)</span>
                                    <span class="analysis-result-card__factor-contribution">contribution @AnalysisFormatting.NullableDecimal(factor.Contribution)</span>
                                }
                                else
                                {
                                    <span>niedostępny</span>
                                    <span class="analysis-result-card__factor-weight">weight @AnalysisFormatting.Weight(factor.Weight)</span>
                                }
                                <span>@factor.Explanation</span>
                            </li>
                        }
                    </ul>
                </section>

                <partial name="_PriceHistoryChart" model="result" />

                <section class="analysis-result-card__summary"
                         aria-label="Podsumowanie analizy dla @result.Asset.Id">
                    @if (result.SummaryResult is { Status: AnalysisSummaryStatus.Generated, Text: not null } generatedSummary)
                    {
                        <h4>Podsumowanie</h4>
                        <p>@generatedSummary.Text</p>
                    }
                    else if (result.SummaryResult is { Status: var summaryStatus } &&
                             summaryStatus != AnalysisSummaryStatus.Unavailable)
                    {
                        <p class="analysis-result-card__summary--status" role="status">
                            <strong>Podsumowanie:</strong>
                            @Model.SummaryStatusMessage
                            @if (summaryStatus == AnalysisSummaryStatus.InsufficientEvidence)
                            {
                                <span class="visually-hidden">Insufficient evidence</span>
                            }
                        </p>
                    }
                    else if (result.Summary is not null)
                    {
                        <h4>Podsumowanie</h4>
                        <p>@result.Summary</p>
                    }
                </section>
            </details>
        }
    </article>
</li>
```

- [ ] **Step 8: Rework `Pages/Analysis/Index.cshtml`**

Make these exact edits (line numbers refer to the file after Step 6 and before this step; locate blocks by their content if they shifted):

1. Add `@using _bootstrap_scaffold.Pages.Analysis` below the existing `@using` lines, and add to the top `@{ }` block (after `ViewData["Title"] = ...;`):

```cshtml
    var rankById = new Dictionary<string, (int Rank, decimal? Score)>(StringComparer.OrdinalIgnoreCase);
    if (Model.HasResults)
    {
        for (var rankIndex = 0; rankIndex < Model.AnalysisResults!.Results.Count; rankIndex++)
        {
            var ranked = Model.AnalysisResults.Results[rankIndex];
            rankById[ranked.Asset.Id] = (rankIndex + 1, ranked.Score);
        }
    }
```

2. Delete the `<p class="analysis-page__lede">…</p>` paragraph (lines 16–20).

3. Replace the asset `<label class="asset-option">…</label>` block (lines 40–50) with:

```cshtml
                var identity = AssetIdentityCatalog.For(asset.Id);
                <label class="asset-option">
                    <input class="asset-option__input"
                           type="checkbox"
                           name="SelectedAssetIds"
                           value="@asset.Id"
                           checked="@(isSelected ? "checked" : null)" />
                    <span class="asset-option__monogram" aria-hidden="true">@identity.Monogram</span>
                    <span class="asset-option__copy">
                        <strong>@asset.Id</strong>
                        <small>@identity.Name</small>
                    </span>
                    @if (rankById.TryGetValue(asset.Id, out var rank) && rank.Score is { } rankScore)
                    {
                        <span class="asset-option__rank">#@rank.Rank · @AnalysisFormatting.Decimal(rankScore)</span>
                    }
                </label>
```

4. In `<header class="analysis-workspace__header">`, delete `<p>Rankingi, kontekst rynkowy i czynniki wyjaśniające dla wybranych aktywów.</p>`.

5. Replace the results boundary paragraph with exactly:

```cshtml
                <p class="analysis-results__boundary">
                    Wyniki mają charakter edukacyjny i nie stanowią porady inwestycyjnej.
                </p>
```

6. Replace everything inside `<ol class="analysis-results__list">…</ol>` with:

```cshtml
                @if (Model.HasResults)
                {
                    @for (var index = 0; index < Model.AnalysisResults!.Results.Count; index++)
                    {
                        var result = Model.AnalysisResults.Results[index];
                        var quote = Model.CurrentQuotes.TryGetValue(result.Asset.Id, out var currentQuote)
                            ? currentQuote
                            : null;
                        var card = new InsightCardModel(
                            result,
                            index,
                            quote,
                            result.Error is null ? null : IndexModel.MapAssetErrorMessage(result.Error.Category),
                            IndexModel.MapSummaryStatusMessage(result.SummaryResult?.Status ?? AnalysisSummaryStatus.Unavailable));
                        <partial name="_InsightCard" model="card" />
                    }
                }
```

7. Delete the whole `@functions { … }` block at the end of the file (now in `AnalysisFormatting`).

- [ ] **Step 9: Build and run the full suite**

Run: `dotnet build .bootstrap-scaffold.csproj --no-restore`
Expected: `0 Warning(s)`, `0 Error(s)`.

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
Expected: PASS, including the 3 new tests and all pre-existing `/analysis` tests (their classes and texts now live inside `<details>`). If a pre-existing assertion fails, fix the markup to satisfy it — do not weaken the existing assertion — unless it asserts text this plan deliberately removes (the long boundary sentence or the header description), in which case report it.

- [ ] **Step 10: Commit**

```powershell
git add Pages/Analysis/AnalysisFormatting.cs Pages/Analysis/InsightCardModel.cs Pages/Analysis/_InsightCard.cshtml Pages/Analysis/_PriceHistoryChart.cshtml Pages/Analysis/Index.cshtml Pages/Analysis/Index.cshtml.cs tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs
git commit -m "feat: render analysis results as insight cards" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```

---

### Task 4: Insight styling and fixture alignment

**Files:**
- Modify: `wwwroot/css/site.css` (tokens in `:root`; new rules after the existing `.analysis-result-card` rules; responsive rules in the existing mobile media query)
- Modify: `Pages/Analysis/UiStateFixture.cshtml.cs`
- Modify: `Pages/Analysis/UiStateFixture.cshtml` (the `Analysis result states` section, lines 250–313)
- Test: `tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs` (`Testing_ui_state_fixture_renders_the_required_states`)

**Interfaces:**
- Consumes (Task 3): `InsightCardModel`, partial `_InsightCard`, `IndexModel.MapSummaryStatusMessage`, class hooks listed in Task 3.
- Produces: `UiStateFixtureModel.ResultStates : IReadOnlyList<FixtureResultState>`; `public sealed record FixtureResultState(string Heading, string? Note, InsightCardModel Card)`.

- [ ] **Step 1: Extend the fixture test (failing)**

In `Testing_ui_state_fixture_renders_the_required_states`, add after the last existing assertion:

```csharp
        Assert.Contains("class=\"insight-card__monogram\"", html);
        Assert.Contains("data-sparkline", html);
        Assert.Contains("data-factor-bar=\"trend-30d\"", html);
        Assert.Contains("insight-card__bar insight-card__bar--unavailable", html);
        Assert.Contains("Wniosek z danych", html);
        Assert.Contains("<details class=\"insight-card__details\"", html);
        Assert.DoesNotContain("chwilowo niedostępne", html);
```

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter "FullyQualifiedName~Testing_ui_state_fixture_renders_the_required_states"`
Expected: FAIL (fixture still uses static result markup).

- [ ] **Step 2: Add sample result states to `Pages/Analysis/UiStateFixture.cshtml.cs`**

Replace the file with:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace _bootstrap_scaffold.Pages.Analysis;

public sealed record FixtureResultState(string Heading, string? Note, InsightCardModel Card);

[AllowAnonymous]
public sealed class UiStateFixtureModel(IWebHostEnvironment environment) : PageModel
{
    public IReadOnlyList<FixtureResultState> ResultStates { get; } = BuildResultStates();

    public IActionResult OnGet() =>
        string.Equals(environment.EnvironmentName, "Testing", StringComparison.Ordinal)
            ? Page()
            : NotFound();

    private static IReadOnlyList<FixtureResultState> BuildResultStates()
    {
        var now = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        var current = new MarketDataFreshness(now, now, now.AddMinutes(5), false);
        var stale = new MarketDataFreshness(now.AddHours(-8), now, null, true);
        var generated = AnalysisSummaryResult.Generated("Wynik oparty na wyświetlonych danych.");

        return
        [
            new("Completed", null, Card(
                Sample("AAPL", 82m, 1.2m, 4.8m, 9.6m, [210m, 212m, 209m, 214m, 216m, 215.5m], MarketDataQuality.Complete, current, generated),
                0)),
            new("Partial result", "Częściowe wyniki — część aktywów nie ma pełnych danych.", Card(
                Sample("MSFT", 58m, null, 2.1m, 3.4m, [418m, 421m, 419m, 420.25m], MarketDataQuality.Incomplete, current, generated),
                1)),
            new("Stale data", null, Card(
                Sample("BTC-USD", 64m, -0.8m, 3.5m, 12.2m, [64000m, 66500m, 65200m, 68000m], MarketDataQuality.Complete, stale, generated),
                2)),
            new("Insufficient evidence", null, Card(
                Sample("ETH-USD", null, null, null, -2.4m, [3500m], MarketDataQuality.Incomplete, current, AnalysisSummaryResult.InsufficientEvidence()),
                3))
        ];
    }

    private static InsightCardModel Card(AssetAnalysis analysis, int index) =>
        new(
            analysis,
            index,
            null,
            null,
            IndexModel.MapSummaryStatusMessage(analysis.SummaryResult?.Status ?? AnalysisSummaryStatus.Unavailable));

    private static AssetAnalysis Sample(
        string id,
        decimal? score,
        decimal? change24Hours,
        decimal? change7Days,
        decimal? change30Days,
        decimal[] closes,
        MarketDataQuality quality,
        MarketDataFreshness freshness,
        AnalysisSummaryResult summary)
    {
        var points = closes
            .Select((close, index) => new AnalysisChartPoint(freshness.ObservedAt.AddDays(index - closes.Length), close))
            .ToArray();
        var performance = new AssetPerformance(
            Metric("24h", change24Hours),
            Metric("7d", change7Days),
            Metric("30d", change30Days),
            points);
        AnalysisFactor[] factors =
        [
            Factor("momentum-24h", "24-hour momentum", change24Hours),
            Factor("momentum-7d", "7-day momentum", change7Days),
            Factor("trend-30d", "30-day trend", change30Days)
        ];

        return new AssetAnalysis(
            new AssetReference(id, id, id.EndsWith("-USD", StringComparison.Ordinal) ? MarketAssetClass.Crypto : MarketAssetClass.Equity),
            score,
            performance,
            factors,
            points,
            summary.Text,
            summary,
            quality,
            freshness,
            false,
            [],
            null);
    }

    private static PerformanceMetric Metric(string period, decimal? percentage) =>
        new(period, percentage, percentage.HasValue);

    private static AnalysisFactor Factor(string key, string label, decimal? percentage) =>
        new(
            key,
            label,
            percentage,
            percentage.HasValue ? 0.33m : 0m,
            percentage.HasValue ? 20m : null,
            percentage.HasValue,
            $"{label} (fixture).");
}
```

- [ ] **Step 3: Render the fixture result states through the partial**

In `Pages/Analysis/UiStateFixture.cshtml`, replace the whole `<section class="ui-state-fixture__state" aria-labelledby="result-surface-states">…</section>` block (lines 250–313) with:

```cshtml
    <section class="ui-state-fixture__state" aria-labelledby="result-surface-states">
        <h2 id="result-surface-states">Analysis result states</h2>
        <div class="ui-state-fixture__grid">
            @foreach (var state in Model.ResultStates)
            {
                <div class="ui-state-fixture__result-state">
                    <h3>@state.Heading</h3>
                    @if (state.Note is not null)
                    {
                        <p role="status">@state.Note</p>
                    }
                    <ol class="analysis-results__list">
                        <partial name="_InsightCard" model="state.Card" />
                    </ol>
                </div>
            }
        </div>
    </section>
```

- [ ] **Step 4: Run the fixture test**

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore --filter "FullyQualifiedName~Testing_ui_state_fixture"`
Expected: PASS (the existing assertions such as `analysis-result-card analysis-result-card--success analysis-result-card--stale`, `Dane nieaktualne`, `Niewystarczające dane do wyliczenia score.` are now produced by the partial).

- [ ] **Step 5: Add tokens to `:root` in `wwwroot/css/site.css`**

Add after `--result-metric-min: 12rem;`:

```css
    --color-positive: #4ade80;
    --color-negative: #f87171;
    --color-warning: #fbbf24;
    --insight-monogram-size: 2.5rem;
    --insight-change-size: 2rem;
    --insight-sparkline-height: 4.5rem;
    --insight-bar-height: 0.375rem;
    --insight-bar-label-width: 7rem;
    --insight-area-opacity: 18%;
```

- [ ] **Step 6: Add the insight styles**

Append after the last `.analysis-result-card…` rule block (before the responsive media queries):

```css
.insight-card {
    --insight-tone: var(--color-muted);
    display: grid;
    gap: var(--space-3);
}

.insight-card--positive {
    --insight-tone: var(--color-positive);
}

.insight-card--negative {
    --insight-tone: var(--color-negative);
}

.insight-card .analysis-result-card__header {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr) auto;
    align-items: center;
    column-gap: var(--space-2);
    row-gap: var(--space-1);
}

.insight-card .analysis-result-card__rank,
.insight-card .analysis-result-card__status {
    grid-column: 1 / -1;
    margin: 0;
    color: var(--color-muted);
}

.insight-card__monogram,
.asset-option__monogram {
    display: inline-grid;
    place-items: center;
    inline-size: var(--insight-monogram-size);
    block-size: var(--insight-monogram-size);
    border-radius: var(--radius-md);
    background: var(--color-surface-raised);
    color: var(--color-accent);
    font-weight: 700;
}

.insight-card__identity h3,
.insight-card__ticker {
    margin: 0;
}

.insight-card__ticker {
    color: var(--color-muted);
}

.insight-card .analysis-result-card__score {
    margin: 0;
    padding: var(--space-1) var(--space-2);
    border-radius: var(--radius-sm);
    background: var(--color-surface-raised);
    color: var(--color-accent);
    font-weight: 700;
}

.insight-card__chips {
    display: flex;
    flex-wrap: wrap;
    gap: var(--space-1);
}

.insight-card__chips:empty {
    display: none;
}

.insight-card__chip {
    padding: 0 var(--space-1);
    border-radius: var(--radius-sm);
    border: 1px solid var(--color-border);
    color: var(--color-muted);
}

.insight-card__chip--warning {
    border-color: var(--color-warning);
    color: var(--color-warning);
}

.insight-card__headline {
    display: flex;
    align-items: baseline;
    gap: var(--space-1);
}

.insight-card__change {
    margin: 0;
    font-size: var(--insight-change-size);
    font-weight: 700;
    color: var(--insight-tone);
}

.insight-card__change-label {
    margin: 0;
    color: var(--color-muted);
}

.insight-card__sparkline {
    display: block;
    inline-size: 100%;
    block-size: var(--insight-sparkline-height);
    color: var(--insight-tone);
}

.insight-card__sparkline-line {
    fill: none;
    stroke: currentColor;
    stroke-width: 2;
    vector-effect: non-scaling-stroke;
}

.insight-card__sparkline-area {
    fill: color-mix(in srgb, currentColor var(--insight-area-opacity), transparent);
    stroke: none;
}

.insight-card__sparkline-empty {
    margin: 0;
    color: var(--color-muted);
}

.insight-card__bars {
    display: grid;
    gap: var(--space-1);
    margin: 0;
    padding: 0;
    list-style: none;
}

.insight-card__bar {
    display: grid;
    grid-template-columns: var(--insight-bar-label-width) minmax(0, 1fr) auto;
    align-items: center;
    gap: var(--space-2);
}

.insight-card__bar-label,
.insight-card__bar-value {
    color: var(--color-muted);
}

.insight-card__bar-meter {
    appearance: none;
    inline-size: 100%;
    block-size: var(--insight-bar-height);
    border: 0;
    border-radius: var(--radius-sm);
    background: var(--color-surface-raised);
}

.insight-card__bar-meter::-webkit-meter-bar {
    block-size: var(--insight-bar-height);
    border: 0;
    border-radius: var(--radius-sm);
    background: var(--color-surface-raised);
}

.insight-card__bar-meter::-webkit-meter-optimum-value {
    border-radius: var(--radius-sm);
    background: var(--color-accent);
}

.insight-card__bar-meter::-moz-meter-bar {
    border-radius: var(--radius-sm);
    background: var(--color-accent);
}

.insight-card__bar--unavailable {
    opacity: 0.5;
}

.insight-card__insight {
    margin: 0;
}

.insight-card__insight-label {
    display: block;
    color: var(--color-accent);
    font-weight: 600;
}

.insight-card__details {
    border-top: 1px solid var(--color-divider);
    padding-top: var(--space-2);
}

.insight-card__details > summary {
    cursor: pointer;
    color: var(--color-muted);
}

.insight-card__details > summary:focus-visible {
    outline: 2px solid var(--color-focus);
    outline-offset: 2px;
}

.insight-card__details[open] > :not(summary) {
    margin-top: var(--space-2);
}

.asset-option__rank {
    margin-inline-start: auto;
    color: var(--color-accent);
    font-weight: 600;
}

.ui-state-fixture__result-state {
    display: grid;
    gap: var(--space-2);
}
```

Then, inside the existing mobile `@media` block that stacks the workspace (search for `analysis-workspace` within a `@media (max-width:` rule), add:

```css
    .insight-card__bar {
        grid-template-columns: minmax(0, 1fr) auto;
    }

    .insight-card__bar-meter {
        grid-column: 1 / -1;
        grid-row: 2;
    }
```

- [ ] **Step 7: Verify the hardcoded-value boundary and the suite**

Run: `Select-String -Path Pages\Analysis\*.cshtml -Pattern '#[0-9a-fA-F]{3,8}\b|rgb\(|style='`
Expected: no matches (`#@rank.Rank` is not matched because `@` is not a hex digit).

Run: `dotnet build .bootstrap-scaffold.csproj --no-restore`
Expected: `0 Warning(s)`, `0 Error(s)`.

Run: `dotnet test tests\Argus.Authentication.Tests\Argus.Authentication.Tests.csproj --no-restore`
Expected: PASS (all tests).

- [ ] **Step 8: Visual check against the mockup**

Run the app in the Testing environment and open the fixture:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Testing"; dotnet run --project .bootstrap-scaffold.csproj --no-build --launch-profile https
```

Open `https://localhost:<port>/analysis/ui-state-fixture` and compare the "Analysis result states" cards with `.superpowers/brainstorm/640-1792563901/content/dashboard-insight-first-v1.html`: monogram + name, score badge, large colored 7-day change, filled sparkline, three bars, one insight line, collapsed details. Stop the app by its PID afterwards. If the Testing environment needs Entra settings to start, report that instead of adding secrets.

- [ ] **Step 9: Commit**

```powershell
git add wwwroot/css/site.css Pages/Analysis/UiStateFixture.cshtml Pages/Analysis/UiStateFixture.cshtml.cs tests/Argus.Authentication.Tests/AnalysisAccessContractTests.cs
git commit -m "feat: style insight cards and align the UI fixture" -m "Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
```
