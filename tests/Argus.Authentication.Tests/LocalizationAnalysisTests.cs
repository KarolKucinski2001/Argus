using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.Localization;
using _bootstrap_scaffold.MarketData;
using _bootstrap_scaffold.Pages.Analysis;

namespace Argus.Authentication.Tests;

[Collection(TestAuthenticationServiceCollection.Name)]
public sealed class LocalizationAnalysisTests
{
    [Fact]
    public void Insight_presenter_describes_the_data_in_english()
    {
        using (Lang.Use(UiLanguage.English))
        {
            var view = AnalysisInsightPresenter.Present(Analysis(1.2m, 4.8m, 9.6m), rank: 1, averageScore: 50m);

            Assert.Equal("Why position #1?", view.WhyHeading);
            Assert.Equal("Price rose 9.6% over 30 days — the highest-scored asset in this analysis.", view.Takeaway);
            Assert.Equal("Upward trend", view.TrendLabel);
            Assert.Equal("Stocks", view.AssetClassLabel);
            Assert.Equal(["24h momentum", "7-day momentum", "30-day trend"], view.FactorBars.Select(bar => bar.Label));
            Assert.Equal("Rising", view.FactorBars[0].Word);
            Assert.Equal("Score 60 — 10 pts above the analysis average", view.WhyBullets[0]);
            Assert.Contains("7-day momentum is rising (+4.8%)", view.WhyBullets);
        }

        Assert.Equal("Akcje", AnalysisInsightPresenter.AssetClassLabel(MarketAssetClass.Equity));
    }

    [Fact]
    public void Summary_strip_and_page_messages_switch_to_english()
    {
        using (Lang.Use(UiLanguage.English))
        {
            Assert.Equal("1 asset", AnalysisSummaryStripPresenter.AssetCountLabel(1));
            Assert.Equal("3 assets", AnalysisSummaryStripPresenter.AssetCountLabel(3));
            Assert.Equal("5 assets", AnalysisSummaryStripPresenter.AssetCountLabel(5));
            Assert.Equal(
                "This asset could not be analyzed. Please try again later.",
                IndexModel.MapAssetErrorMessage(null));
            Assert.Equal("Stale data", AnalysisFormatting.Quality(MarketDataQuality.Stale));
        }

        Assert.Equal("5 aktywów", AnalysisSummaryStripPresenter.AssetCountLabel(5));
    }

    [Fact]
    public async Task Ranked_analysis_fixture_renders_in_english_with_the_language_cookie()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/analysis/ui-state-fixture?view=ranked");
        request.Headers.Add("Cookie", $"{Lang.CookieName}=en");

        using var response = await client.SendAsync(request);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Analysis dashboard", html);
        Assert.Contains("You can select any number of assets.", html);
        Assert.Contains("Ranking summary", html);
        Assert.Contains("Why position #1?", html);
        Assert.Contains("Data takeaway", html);
        Assert.Contains("Score based on the displayed data.", html);
        Assert.DoesNotContain("Pulpit analizy", html);
        Assert.DoesNotContain("Wniosek z danych", html);
    }

    private static AssetAnalysis Analysis(decimal change24Hours, decimal change7Days, decimal change30Days)
    {
        var now = new DateTimeOffset(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);
        AnalysisChartPoint[] points =
        [
            new(now.AddDays(-2), 10m),
            new(now.AddDays(-1), 11m)
        ];
        var performance = new AssetPerformance(
            new PerformanceMetric("24h", change24Hours, true),
            new PerformanceMetric("7d", change7Days, true),
            new PerformanceMetric("30d", change30Days, true),
            points);
        AnalysisFactor[] factors =
        [
            new("momentum-24h", "momentum-24h", change24Hours, 0.33m, 10m, true, "explanation"),
            new("momentum-7d", "momentum-7d", change7Days, 0.33m, 20m, true, "explanation"),
            new("trend-30d", "trend-30d", change30Days, 0.33m, 30m, true, "explanation")
        ];

        return new AssetAnalysis(
            new AssetReference("AAPL", "AAPL", MarketAssetClass.Equity),
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
}
