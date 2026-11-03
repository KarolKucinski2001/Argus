using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using _bootstrap_scaffold.Strategies;

namespace Argus.Authentication.Tests;

public sealed class SimulatedPortfolioAnalysisPresenterTests
{
    private static readonly string[] Supported = ["AAPL", "MSFT", "NVDA", "BTC-USD", "ETH-USD"];

    private static SimulatedPortfolio Copy(string profileId) =>
        StrategyProfilePresenter.CreateCopy(
            CuratedProfileCatalog.Find(profileId)!,
            "a".PadRight(32, 'b'),
            DateTimeOffset.UnixEpoch);

    [Fact]
    public void Effective_positions_is_inverse_sum_of_squared_weights()
    {
        Assert.Equal(4m, SimulatedPortfolioAnalysisPresenter.EffectivePositions([25m, 25m, 25m, 25m]));
        Assert.Equal(1m, SimulatedPortfolioAnalysisPresenter.EffectivePositions([100m]));
        Assert.Equal(0m, SimulatedPortfolioAnalysisPresenter.EffectivePositions([]));
        Assert.Equal(3m, Math.Round(SimulatedPortfolioAnalysisPresenter.EffectivePositions([33m, 33m, 33m]), 6));
    }

    [Theory]
    [InlineData("all-weather-style", "Wysoka koncentracja")]
    [InlineData("berkshire-style", "Umiarkowana koncentracja")]
    [InlineData("sp500-top-holdings", "Umiarkowana koncentracja")]
    public void Concentration_label_follows_effective_positions(string profileId, string expected)
    {
        var view = SimulatedPortfolioAnalysisPresenter.Build(Copy(profileId), Supported, 5);

        Assert.Equal(expected, view.ConcentrationLabel);
        Assert.Equal("Rozproszony skład", SimulatedPortfolioAnalysisPresenter.ConcentrationLabel(7m));
    }

    [Fact]
    public void Analysis_lists_metrics_shares_and_keeps_as_of_date()
    {
        var view = SimulatedPortfolioAnalysisPresenter.Build(Copy("berkshire-style"), Supported, 5);

        Assert.Equal("Dane na dzień: 2025-06-30", view.AsOfText);
        Assert.Contains(view.Metrics, metric => metric.Label == "Największa pozycja" && metric.Value == "40%");
        Assert.Contains(view.Metrics, metric => metric.Label == "Trzy największe pozycje" && metric.Value == "71%");
        Assert.Contains(view.Metrics, metric => metric.Label == "Efektywna liczba pozycji" && metric.Value == "4,3");
        Assert.Equal("Technologia", view.SectorShares[0].Label);
        Assert.Single(view.AssetClassShares);
        Assert.Contains(view.Factors, factor => factor.Contains("efektywna liczba pozycji", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Coverage_lists_only_supported_assets_up_to_the_selection_limit()
    {
        var berkshire = SimulatedPortfolioAnalysisPresenter.Build(Copy("berkshire-style"), Supported, 5);
        var sAndP = SimulatedPortfolioAnalysisPresenter.Build(Copy("sp500-top-holdings"), Supported, 2);
        var allWeather = SimulatedPortfolioAnalysisPresenter.Build(Copy("all-weather-style"), Supported, 5);

        Assert.Equal(["AAPL"], berkshire.AnalyzableAssetIds);
        Assert.Contains("1 z 7", berkshire.CoverageText);
        Assert.Equal(["NVDA", "MSFT"], sAndP.AnalyzableAssetIds);
        Assert.Empty(allWeather.AnalyzableAssetIds);
        Assert.Contains("Żadna", allWeather.CoverageText);
    }

    [Fact]
    public void Analysis_copy_stays_descriptive_without_advice_or_forecast_language()
    {
        var banned = new[] { "kup", "sprzedaj", "polecam", "zalecamy", "warto", "prognoz", "zysk", "gwarant", "najlepsz", "ryzykown", "bezpieczn" };
        foreach (var profile in CuratedProfileCatalog.Profiles)
        {
            var view = SimulatedPortfolioAnalysisPresenter.Build(Copy(profile.Id), Supported, 5);
            var text = string.Join(
                " ",
                view.Factors
                    .Concat(view.Metrics.Select(metric => metric.Label + " " + metric.Hint))
                    .Append(view.ConcentrationLabel)
                    .Append(view.ConcentrationExplanation)
                    .Append(view.CoverageText)).ToLowerInvariant();
            foreach (var word in banned)
            {
                Assert.DoesNotContain(word, text);
            }
        }
    }
}

public sealed class SimulatedPortfolioAnalysisPageTests
{
    private static HttpClient NewClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    private static SimulatedPortfolio NewCopy(string profileId = "berkshire-style") =>
        StrategyProfilePresenter.CreateCopy(
            CuratedProfileCatalog.Find(profileId)!,
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow);

    [Fact]
    public async Task Analysis_requires_authentication()
    {
        await using var factory = new UnauthenticatedWatchlistFactory();
        using var client = NewClient(factory);

        using var response = await client.GetAsync($"/strategies/portfolios/{Guid.NewGuid():N}/analysis");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("test.invalid/authorize", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Own_portfolio_analysis_shows_explanation_disclaimer_and_as_of_date()
    {
        await using var factory = new StrategyTestFactory();
        var portfolio = NewCopy();
        await factory.Portfolios.CreateAsync("test-subject", portfolio);
        using var client = NewClient(factory);

        var html = WebUtility.HtmlDecode(
            await client.GetStringAsync($"/strategies/portfolios/{portfolio.Id}/analysis"));

        Assert.Contains("Analiza portfela symulowanego", html);
        Assert.Contains("Umiarkowana koncentracja", html);
        Assert.Contains("Efektywna liczba pozycji", html);
        Assert.Contains("Dane na dzień: 2025-06-30", html);
        Assert.Contains(CuratedProfileCatalog.Disclaimer, html);
        Assert.Contains("name=\"SelectedAssetIds\" value=\"AAPL\"", html);
    }

    [Fact]
    public async Task Portfolio_list_links_to_analysis()
    {
        await using var factory = new StrategyTestFactory();
        var portfolio = NewCopy();
        await factory.Portfolios.CreateAsync("test-subject", portfolio);
        using var client = NewClient(factory);

        var html = await client.GetStringAsync("/strategies/portfolios");

        Assert.Contains($"href=\"/strategies/portfolios/{portfolio.Id}/analysis\"", html);
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("unknown")]
    [InlineData("invalid")]
    public async Task Foreign_unknown_or_invalid_portfolio_returns_not_found(string kind)
    {
        await using var factory = new StrategyTestFactory();
        var foreign = NewCopy();
        await factory.Portfolios.CreateAsync("someone-else", foreign);
        var id = kind switch
        {
            "foreign" => foreign.Id,
            "unknown" => Guid.NewGuid().ToString("N"),
            _ => "not-a-valid-id"
        };
        using var client = NewClient(factory);

        using var response = await client.GetAsync($"/strategies/portfolios/{id}/analysis");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Storage_failure_renders_error_without_throwing()
    {
        await using var factory = new StrategyTestFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISimulatedPortfolioStore>();
                services.AddSingleton<ISimulatedPortfolioStore, FailingSimulatedPortfolioStore>();
            }));
        using var client = NewClient(factory);

        using var response = await client.GetAsync($"/strategies/portfolios/{Guid.NewGuid():N}/analysis");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Nie udało się pobrać portfela symulowanego", html);
    }
}

internal sealed class FailingSimulatedPortfolioStore : ISimulatedPortfolioStore
{
    public Task<SimulatedPortfolioListResult> ListAsync(string subjectId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new SimulatedPortfolioListResult(SimulatedPortfolioStatus.StorageFailure, []));

    public Task<SimulatedPortfolioStatus> CreateAsync(string subjectId, SimulatedPortfolio portfolio, CancellationToken cancellationToken = default) =>
        Task.FromResult(SimulatedPortfolioStatus.StorageFailure);

    public Task<SimulatedPortfolioStatus> DeleteAsync(string subjectId, string portfolioId, CancellationToken cancellationToken = default) =>
        Task.FromResult(SimulatedPortfolioStatus.StorageFailure);
}
