using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using _bootstrap_scaffold.Localization;
using _bootstrap_scaffold.Strategies;

namespace Argus.Authentication.Tests;

[Collection(TestAuthenticationServiceCollection.Name)]
public sealed class LocalizationStrategiesTests
{
    [Fact]
    public void Profile_detail_is_presented_in_english_with_english_number_format()
    {
        var profile = CuratedProfileCatalog.Find("all-weather-style")!;

        using (Lang.Use(UiLanguage.English))
        {
            var detail = StrategyProfilePresenter.BuildDetail(profile);

            Assert.Equal("All Weather-style portfolio (example)", detail.Name);
            Assert.Equal("Data as of: 2025-06-30", detail.AsOfText);
            Assert.Contains(detail.Holdings, row => row.AssetId == "GLD" && row.WeightText == "7.5%" && row.Sector == "Gold" && row.AssetClass == "Commodities");
            Assert.Contains(detail.AssetClassShares, share => share.Label == "Stocks");
            Assert.Contains("The profile contains 5 positions.", detail.Factors[^1]);
        }

        var polish = StrategyProfilePresenter.BuildDetail(profile);
        Assert.Contains(polish.Holdings, row => row.AssetId == "GLD" && row.WeightText == "7,5%" && row.AssetClass == "Surowce");
        Assert.Contains(polish.AssetClassShares, share => share.Label == "Akcje");
    }

    [Fact]
    public void Simulated_portfolio_analysis_translates_stored_polish_names_and_coverage()
    {
        var curated = CuratedProfileCatalog.Find("berkshire-style")!;
        var portfolio = StrategyProfilePresenter.CreateCopy(curated, new string('a', 32), DateTimeOffset.UnixEpoch);

        using (Lang.Use(UiLanguage.English))
        {
            var analysis = SimulatedPortfolioAnalysisPresenter.Build(portfolio, ["AAPL"]);

            Assert.Equal("Simulation: Berkshire Hathaway-style portfolio", analysis.Name);
            Assert.Equal("Berkshire Hathaway-style portfolio", analysis.ProfileName);
            Assert.Equal("Moderate concentration", analysis.ConcentrationLabel);
            Assert.StartsWith("Argus can show market analysis for 1 of 7 positions: AAPL.", analysis.CoverageText);
            Assert.Contains(analysis.Metrics, metric => metric.Label == "Largest position" && metric.Value == "40%");
        }

        Assert.Equal("Symulacja: Portfel w stylu Berkshire Hathaway", portfolio.Name);
    }

    [Fact]
    public async Task Public_profiles_page_renders_in_english_when_cookie_is_set()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/strategies/profiles");
        request.Headers.Add("Cookie", "argus-lang=en");

        using var response = await client.SendAsync(request);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Public profiles", html);
        Assert.Contains("Berkshire Hathaway-style portfolio", html);
        Assert.Contains("Sample, approximate data", html);
        Assert.Contains("Results are educational and do not constitute investment advice.", html);
        Assert.DoesNotContain("Profile publiczne", html);
        Assert.DoesNotContain("Zobacz skład", html);
    }
}
