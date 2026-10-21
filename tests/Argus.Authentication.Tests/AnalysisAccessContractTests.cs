using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;
using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;

namespace Argus.Authentication.Tests;

public sealed class AnalysisAccessContractTests
{
    [Fact]
    public async Task Testing_ui_state_fixture_renders_the_required_states()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/analysis/ui-state-fixture");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Default", html);
        Assert.Contains("Hover", html);
        Assert.Contains("Focus-visible", html);
        Assert.Contains("Disabled", html);
        Assert.Contains("Error", html);
        Assert.Contains("Empty", html);
        Assert.Contains("Loading", html);
        Assert.Contains("ui-state-fixture__grid", html);
        Assert.Contains("analysis-workspace", html);
        Assert.Contains("analysis-workspace__sidebar", html);
        Assert.Contains("analysis-workspace__content", html);
        Assert.Contains("analysis-result-card__metrics", html);
        Assert.Contains("role=\"status\"", html);
        Assert.Contains("data-focus-visible-example", html);
        Assert.Contains("class=\"asset-option ui-state-fixture__focus-target\"", html);
        Assert.Contains("tabindex=\"0\"", html);
        Assert.Contains("data-analysis-form", html);
        Assert.Contains("data-submit-button", html);
        Assert.Contains("data-pending-status", html);
        Assert.Contains("ui-state-fixture__loading-card", html);
        Assert.Contains("Analysis result states", html);
        Assert.Contains("Completed", html);
        Assert.Contains("Partial result", html);
        Assert.Contains("Stale data", html);
        Assert.Contains("Insufficient evidence", html);
        Assert.Contains("analysis-result-card analysis-result-card--success analysis-result-card--stale", html);
        Assert.Contains("analysis-result-card analysis-result-card--insufficient-evidence", html);
        Assert.Contains("Częściowe wyniki", html);
        Assert.Contains("Dane nieaktualne", html);
        Assert.Contains("Niewystarczające dane do wyliczenia score.", html);
    }

    [Fact]
    public async Task Ui_state_fixture_is_not_available_outside_testing()
    {
        await using var factory = new DevelopmentUiStateFixtureFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/analysis/ui-state-fixture");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_analysis_page_uses_the_explicit_provider_challenge()
    {
        await using var factory = new UnauthenticatedAnalysisFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/analysis");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("test.invalid", response.Headers.Location?.ToString());
    }

    internal sealed class DevelopmentUiStateFixtureFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            TestEntraConfiguration.Apply(builder);
        }
    }

    [Fact]
    public async Task Authenticated_analysis_page_renders_the_configured_assets_and_limit()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/analysis");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AAPL", html);
        Assert.Contains("MSFT", html);
        Assert.Contains("BTC-USD", html);
        Assert.Contains("ETH-USD", html);
        Assert.Contains("maksymalnie 5", html);
        Assert.Contains(">Analiza</a>", html);
        Assert.Contains(">Konto</a>", html);
        Assert.Contains("/account/signout", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Zaloguj się", html);
        Assert.Contains("class=\"analysis-workspace\"", html);
        Assert.Contains("class=\"analysis-workspace__sidebar\"", html);
        Assert.Contains("class=\"analysis-workspace__content\"", html);
        Assert.Contains("class=\"analysis-results__list\"", html);
    }

    [Fact]
    public async Task Empty_asset_configuration_renders_an_explanatory_disabled_action()
    {
        await using var factory = new AnalysisProviderFactory(
            new FakeMarketDataProvider(),
            options => options.AllowedAssets.Clear());
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/analysis");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Brak skonfigurowanych aktywów", html);
        Assert.Contains("data-submit-button", html);
        Assert.Contains("disabled=\"disabled\"", html);
        Assert.Contains("data-pending-status", html);
    }

    [Fact]
    public async Task Valid_analysis_submission_renders_an_illustrative_started_state()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync(
            "/analysis",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("SelectedAssetIds", "AAPL"),
                new KeyValuePair<string, string>("SelectedAssetIds", "BTC-USD")
            ]));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Analiza rozpoczęta", html);
        Assert.Contains("ilustracyjnych danych", html);
        Assert.Contains("AAPL", html);
        Assert.Contains("BTC-USD", html);
    }

    [Fact]
    public async Task Valid_analysis_submission_renders_ranked_scores_and_metrics()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(
            CreateScoredResult("AAPL", MarketAssetClass.Equity, 120m, 120m, 120m, 120m),
            CreateScoredResult("MSFT", MarketAssetClass.Equity, 120m, 80m, 80m, 80m));
        await using var factory = new AnalysisProviderFactory(provider, _ => { });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostAnalysisAsync(client, token, "AAPL", "MSFT");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ranking analizy", html);
        var rankingHtml = html[html.IndexOf("Ranking analizy", StringComparison.Ordinal)..];
        Assert.True(rankingHtml.IndexOf("MSFT", StringComparison.Ordinal) < rankingHtml.IndexOf("AAPL", StringComparison.Ordinal));
        Assert.Contains("Miejsce 1", html);
        Assert.Contains("100/100", html);
        Assert.Contains("50/100", html);
        Assert.Contains("Cena aktualna", html);
        Assert.Contains("24 godziny", html);
        Assert.Contains("7 dni", html);
        Assert.Contains("30 dni", html);
    }

    [Fact]
    public async Task Analysis_results_render_factors_summary_and_chart_data()
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
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Czynniki", html);
        Assert.Contains("24-hour momentum", html);
        Assert.Contains("7-day momentum", html);
        Assert.Contains("30-day trend", html);
        Assert.Contains("contribution", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("weight 25%", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("weight 35%", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("weight 40%", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Score ", html);
        Assert.Contains("analysis-result-card__header", html);
        Assert.Contains("analysis-result-card__score", html);
        Assert.Contains("analysis-result-card__metrics", html);
        Assert.Contains("analysis-result-card__chart", html);
        Assert.Contains("analysis-result-card__summary", html);
        Assert.Contains("Historia cenowa", html);
        Assert.Contains("data-chart-asset=\"AAPL\"", html);
        Assert.Contains("data-chart-close=\"80", html);
    }

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

    [Fact]
    public async Task Analysis_results_render_accessible_svg_geometry_and_text_fallback()
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

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"analysis-chart-AAPL\"", html);
        Assert.Contains("data-chart-container", html);
        Assert.Contains("data-chart-svg", html);
        Assert.Contains("class=\"analysis-result-card__chart-axis\"", html);
        Assert.Contains("class=\"analysis-result-card__chart-line\"", html);
        Assert.Contains("aria-label=\"AAPL — obserwacja historyczna", html);
        Assert.Contains("tabindex=\"0\"", html);
        Assert.Equal(3, Regex.Matches(html, "data-chart-point").Count);
        Assert.Equal(3, Regex.Matches(html, "data-chart-point[^>]*data-chart-timestamp=\"[^\"]+\"").Count);
        Assert.Equal(3, Regex.Matches(html, "data-chart-point[^>]*data-chart-close=\"[^\"]+\"").Count);
        Assert.Contains("class=\"analysis-result-card__chart-point\"", html);
        Assert.Contains("data-chart-tooltip", html);
        Assert.Contains("role=\"status\"", html);
        Assert.Contains("<time datetime=", html);
        Assert.Contains("Historyczne obserwacje ceny zamknięcia", html);
        Assert.DoesNotContain("prognoz", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-chart-range", html);
        Assert.Contains("aria-label=\"Zakres historii cenowej\"", html);
        Assert.Equal(5, Regex.Matches(html, "data-chart-range-option").Count);
        Assert.Contains("data-chart-range-option=\"1M\"", html);
        Assert.Contains("data-chart-range-option=\"3M\"", html);
        Assert.Contains("data-chart-range-option=\"6M\"", html);
        Assert.Contains("data-chart-range-option=\"1Y\"", html);
        Assert.Contains("data-chart-range-option=\"Max\"", html);
        Assert.Matches("data-chart-range-option=\"1Y\"[^>]*aria-pressed=\"true\"", html);
        Assert.Matches("data-chart-range-option=\"1M\"[^>]*aria-pressed=\"false\"", html);
    }

    [Fact]
    public async Task Analysis_results_render_safe_no_history_chart_state_without_geometry()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateThinResult("AAPL", MarketAssetClass.Equity));
        await using var factory = new AnalysisProviderFactory(provider, _ => { });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostAnalysisAsync(client, token, "AAPL");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Historia cenowa", html);
        Assert.Contains("Brak historycznych obserwacji do wyświetlenia.", html);
        Assert.DoesNotContain("data-chart-svg", html);
        Assert.DoesNotContain("data-chart-point", html);
        Assert.DoesNotContain("data-chart-timestamp", html);
        Assert.DoesNotContain("data-chart-close", html);
    }


    [Fact]
    public async Task Analysis_results_surface_delayed_incomplete_and_warning_states_without_raw_messages()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(
            CreateScoredResult("AAPL", MarketAssetClass.Equity, 120m, 100m, 96m, 80m) with
            {
                Quality = MarketDataQuality.Incomplete,
                Warnings =
                [
                    new MarketDataWarning(
                        MarketDataWarningCategory.DelayedData,
                        "RAW provider says delayed by 15 minutes.",
                        true),
                    new MarketDataWarning(
                        MarketDataWarningCategory.IncompleteHistory,
                        "RAW incomplete provider payload.",
                        false)
                ]
            });
        await using var factory = new AnalysisProviderFactory(provider, _ => { });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostAnalysisAsync(client, token, "AAPL");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Dane opóźnione", html);
        Assert.Contains("Dane niepełne", html);
        Assert.Contains("Historia niepełna", html);
        Assert.DoesNotContain("RAW provider", html);
        Assert.DoesNotContain("RAW incomplete", html);
    }

    [Fact]
    public async Task Analysis_results_render_the_responsive_styling_class_contract()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(
            CreateScoredResult("AAPL", MarketAssetClass.Equity, 120m, 100m, 96m, 80m, isStale: true),
            CreateThinResult("BTC-USD", MarketAssetClass.Crypto),
            CreateErrorResult(
                "MSFT",
                MarketAssetClass.Equity,
                new MarketDataError(
                    MarketDataErrorCategory.UpstreamUnavailable,
                    "RAW provider outage ticket provider-789",
                    true)));
        await using var factory = new AnalysisProviderFactory(provider, _ => { });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostAnalysisAsync(client, token, "AAPL", "BTC-USD", "MSFT");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("class=\"analysis-results__list\"", html);
        Assert.Contains("class=\"analysis-result-card analysis-result-card--success analysis-result-card--stale\"", html);
        Assert.Contains("class=\"analysis-result-card analysis-result-card--insufficient-evidence\"", html);
        Assert.Contains("class=\"analysis-result-card analysis-result-card--error\"", html);
        Assert.Contains("class=\"analysis-result-card__header\"", html);
        Assert.Contains("class=\"analysis-result-card__rank\"", html);
        Assert.Contains("class=\"analysis-result-card__score\"", html);
        Assert.Contains("class=\"analysis-result-card__metrics\"", html);
        Assert.Contains("class=\"analysis-result-card__metric\"", html);
        Assert.Contains("class=\"analysis-result-card__factors\"", html);
        Assert.Contains("class=\"analysis-result-card__factor\"", html);
        Assert.Contains("class=\"analysis-result-card__factor-contribution\"", html);
        Assert.Contains("class=\"analysis-result-card__chart\"", html);
        Assert.Contains("class=\"analysis-result-card__chart-point\"", html);
        Assert.Contains("class=\"analysis-result-card__summary\"", html);
        Assert.Contains("nie stanowią porady inwestycyjnej", html);
    }

    [Fact]
    public async Task Generated_summary_renders_per_asset_without_provider_details()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(
            CreateScoredResult("AAPL", MarketAssetClass.Equity, 120m, 100m, 96m, 80m),
            CreateScoredResult("MSFT", MarketAssetClass.Equity, 120m, 100m, 96m, 80m));
        await using var factory = new AnalysisProviderFactory(
            provider,
            _ => { },
            new FixedSummaryGenerator("Explains the displayed evidence."));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostAnalysisAsync(client, token, "AAPL", "MSFT");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, Regex.Matches(html, "Explains the displayed evidence\\.").Count);
        Assert.DoesNotContain("provider-id", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw response", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deterministic_summary_keeps_analysis_evidence_when_generator_is_unavailable()
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

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Podsumowanie", html);
        Assert.Contains("Score 100/100", html);
        Assert.Contains("100/100", html);
        Assert.Contains("Czynniki", html);
        Assert.DoesNotContain("provider", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Analysis_results_keep_the_educational_boundary_and_avoid_advice_language()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateScoredResult("AAPL", MarketAssetClass.Equity, 120m, 100m, 96m, 80m));
        await using var factory = new AnalysisProviderFactory(
            provider,
            _ => { },
            new FixedSummaryGenerator("Displayed evidence only."));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostAnalysisAsync(client, token, "AAPL");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Wyniki mają charakter edukacyjny i nie stanowią porady inwestycyjnej.", html);
        Assert.Contains("Dane w środowisku deweloperskim są ilustracyjne", html);
        Assert.Contains("nie stanowią porady inwestycyjnej.", html);
        Assert.DoesNotContain("best investment", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("buy", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("guaranteed", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("top opportunity", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Partial_analysis_renders_successful_asset_and_bounded_failed_asset()
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
        using var response = await PostAnalysisAsync(client, token, "AAPL", "MSFT");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AAPL", html);
        Assert.Contains("MSFT", html);
        Assert.Contains("Częściowe wyniki", html);
        Assert.Contains("Nie udało się przeanalizować tego aktywa", html);
        Assert.Contains("Spróbuj ponownie później", html);
        Assert.Contains("Historia cenowa", html);
        Assert.Contains("data-chart-range-option=\"1Y\"", html);
        Assert.Contains("data-chart-range-option=\"Max\"", html);
        Assert.Contains("data-chart-svg", html);
        Assert.Contains("analysis-result-card__factor-list", html);
        Assert.DoesNotContain("The market data provider returned no result", html);
    }

    [Fact]
    public async Task Insufficient_evidence_renders_bounded_state_without_zero_score()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateThinResult("AAPL", MarketAssetClass.Equity));
        await using var factory = new AnalysisProviderFactory(provider, _ => { });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostAnalysisAsync(client, token, "AAPL");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AAPL", html);
        Assert.Contains("Niewystarczające dane", html);
        Assert.Contains("Insufficient evidence", html);
        Assert.DoesNotContain("0/100", html);
        Assert.DoesNotContain("Score 0", html);
    }

    [Fact]
    public async Task Analysis_results_do_not_render_provider_identifiers_or_raw_errors()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(
            CreateScoredResult("AAPL", MarketAssetClass.Equity, 120m, 100m, 96m, 80m) with
            {
                Source = new MarketDataSource(
                    "secret-provider-id",
                    "RAW secret attribution",
                    false,
                    null)
            },
            CreateErrorResult(
                "MSFT",
                MarketAssetClass.Equity,
                new MarketDataError(
                    MarketDataErrorCategory.UpstreamUnavailable,
                    "RAW provider outage ticket provider-789",
                    true)));
        await using var factory = new AnalysisProviderFactory(provider, _ => { });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await PostAnalysisAsync(client, token, "AAPL", "MSFT");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ranking analizy", html);
        Assert.DoesNotContain("secret-provider-id", html);
        Assert.DoesNotContain("RAW secret attribution", html);
        Assert.DoesNotContain("RAW provider outage", html);
        Assert.DoesNotContain("provider-789", html);
    }

    [Fact]
    public async Task Valid_analysis_submission_sends_the_canonical_daily_history_request()
    {
        var provider = new FakeMarketDataProvider();
        provider.SetResults(CreateResultForAnalysis("AAPL"));
        await using var factory = new AnalysisProviderFactory(provider, _ => { });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync(
            "/analysis",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("SelectedAssetIds", "AAPL")
            ]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(provider.LastRequest);
        Assert.Equal(["AAPL"], provider.LastRequest!.Assets.Select(asset => asset.Id));
        Assert.Equal(
            new MarketDataHistoryRequest(
                DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-12),
                DateOnly.FromDateTime(DateTime.UtcNow),
                MarketDataGranularity.Daily),
            provider.LastRequest.History);
    }

    [Fact]
    public async Task Invalid_analysis_submission_renders_bounded_inline_validation()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync(
            "/analysis",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("SelectedAssetIds", "AAPL"),
                new KeyValuePair<string, string>("SelectedAssetIds", "aapl")
            ]));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("tylko raz", html);
        Assert.DoesNotContain("illustrative-development", html);
    }

    [Fact]
    public async Task Empty_analysis_submission_renders_bounded_inline_validation()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync(
            "/analysis",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token)
            ]));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Wybierz co najmniej jedno aktywo", html);
        Assert.DoesNotContain("Analiza rozpoczęta", html);
    }

    [Fact]
    public async Task Over_limit_analysis_submission_renders_bounded_inline_validation()
    {
        await using var factory = new AuthenticationTestFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync(
            "/analysis",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("SelectedAssetIds", "AAPL"),
                new KeyValuePair<string, string>("SelectedAssetIds", "MSFT"),
                new KeyValuePair<string, string>("SelectedAssetIds", "BTC-USD"),
                new KeyValuePair<string, string>("SelectedAssetIds", "ETH-USD"),
                new KeyValuePair<string, string>("SelectedAssetIds", "AAPL"),
                new KeyValuePair<string, string>("SelectedAssetIds", "MSFT")
            ]));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("maksymalnie 5", html);
        Assert.DoesNotContain("Analiza rozpoczęta", html);
    }

    [Fact]
    public async Task Unavailable_provider_renders_bounded_error_without_raw_provider_details()
    {
        await using var factory = new AnalysisProviderFactory(
            new UnavailableMarketDataProvider(),
            options =>
            {
                options.ProviderId = "approved-test-provider";
                options.PublicDisplayApproved = true;
            });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync(
            "/analysis",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("SelectedAssetIds", "AAPL")
            ]));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Nie udało się rozpocząć analizy", html);
        Assert.Contains("Analiza jest", html);
        Assert.DoesNotContain("approved-test-provider", html);
        Assert.DoesNotContain("An approved provider is not registered", html);
    }

    [Fact]
    public async Task Provider_error_preserves_the_selected_assets_in_the_form()
    {
        await using var factory = new AnalysisProviderFactory(
            new UnavailableMarketDataProvider(),
            options =>
            {
                options.ProviderId = "approved-test-provider";
                options.PublicDisplayApproved = true;
            });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var token = await GetAntiforgeryTokenAsync(client);
        using var response = await client.PostAsync(
            "/analysis",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("__RequestVerificationToken", token),
                new KeyValuePair<string, string>("SelectedAssetIds", "AAPL"),
                new KeyValuePair<string, string>("SelectedAssetIds", "BTC-USD")
            ]));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Matches(
            "<input[^>]*(?:value=\"AAPL\"[^>]*checked=\"checked\"|checked=\"checked\"[^>]*value=\"AAPL\")",
            html);
        Assert.Matches(
            "<input[^>]*(?:value=\"BTC-USD\"[^>]*checked=\"checked\"|checked=\"checked\"[^>]*value=\"BTC-USD\")",
            html);
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/analysis");
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]+value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant);

        Assert.True(match.Success, "The analysis page did not render an antiforgery token.");
        return match.Groups[1].Value;
    }

    private static Task<HttpResponseMessage> PostAnalysisAsync(
        HttpClient client,
        string token,
        params string[] assetIds) =>
        client.PostAsync(
            "/analysis",
            new FormUrlEncodedContent(
                new[] { new KeyValuePair<string, string>("__RequestVerificationToken", token) }
                    .Concat(assetIds.Select(assetId =>
                        new KeyValuePair<string, string>("SelectedAssetIds", assetId)))));

    private static MarketDataResult CreateResultForAnalysis(string id)
    {
        var now = DateTimeOffset.UtcNow;
        var history = new MarketDataHistoryRequest(
            DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-12),
            DateOnly.FromDateTime(DateTime.UtcNow),
            MarketDataGranularity.Daily);
        var freshness = new MarketDataFreshness(now.AddMinutes(-1), now, now.AddMinutes(5), false);
        return new MarketDataResult(
            new AssetReference(id, id, MarketAssetClass.Equity),
            history,
            new MarketQuote(215.50m, "USD", now, freshness, MarketDataQuality.Complete),
            [],
            new MarketDataSource("fake-provider", "Fake provider attribution", true, TimeSpan.FromMinutes(15)),
            freshness,
            MarketDataQuality.Complete,
            []);
    }

    private static MarketDataResult CreateScoredResult(
        string id,
        MarketAssetClass assetClass,
        decimal currentQuote,
        decimal close24Hours,
        decimal close7Days,
        decimal close30Days,
        bool isStale = false)
    {
        var now = DateTimeOffset.UtcNow;
        var history = new MarketDataHistoryRequest(
            DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-12),
            DateOnly.FromDateTime(DateTime.UtcNow),
            MarketDataGranularity.Daily);
        var freshness = isStale
            ? new MarketDataFreshness(now.AddHours(-8), now, null, true)
            : new MarketDataFreshness(now.AddMinutes(-1), now, now.AddMinutes(5), false);
        return new MarketDataResult(
            new AssetReference(id, id, assetClass),
            history,
            new MarketQuote(currentQuote, "USD", now, freshness, MarketDataQuality.Complete),
            [
                Point(now.AddDays(-30), close30Days),
                Point(now.AddDays(-7), close7Days),
                Point(now.AddHours(-24), close24Hours)
            ],
            new MarketDataSource("fake-provider", "Fake provider attribution", true, TimeSpan.FromMinutes(15)),
            freshness,
            MarketDataQuality.Complete,
            []);
    }

    private static MarketDataResult CreateThinResult(string id, MarketAssetClass assetClass)
    {
        var now = DateTimeOffset.UtcNow;
        var history = new MarketDataHistoryRequest(
            DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-12),
            DateOnly.FromDateTime(DateTime.UtcNow),
            MarketDataGranularity.Daily);
        var freshness = new MarketDataFreshness(now.AddMinutes(-1), now, now.AddMinutes(5), false);
        return new MarketDataResult(
            new AssetReference(id, id, assetClass),
            history,
            new MarketQuote(120m, "USD", now, freshness, MarketDataQuality.Incomplete),
            [],
            new MarketDataSource("fake-provider", "Fake provider attribution", true, TimeSpan.FromMinutes(15)),
            freshness,
            MarketDataQuality.Incomplete,
            [new MarketDataWarning(MarketDataWarningCategory.IncompleteHistory, "Only limited history is available.", false)]);
    }

    private static MarketDataResult CreateErrorResult(
        string id,
        MarketAssetClass assetClass,
        MarketDataError error)
    {
        var now = DateTimeOffset.UtcNow;
        var history = new MarketDataHistoryRequest(
            DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-12),
            DateOnly.FromDateTime(DateTime.UtcNow),
            MarketDataGranularity.Daily);
        var freshness = new MarketDataFreshness(now.AddMinutes(-30), now, null, true);
        return new MarketDataResult(
            new AssetReference(id, id, assetClass),
            history,
            null,
            [],
            new MarketDataSource("secret-provider-id", "RAW secret attribution", false, null),
            freshness,
            MarketDataQuality.Unavailable,
            [],
            error);
    }

    private static HistoricalPricePoint Point(DateTimeOffset timestamp, decimal close) =>
        new(timestamp, null, null, null, close, null, "USD", true);

}

internal sealed class UnauthenticatedAnalysisFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        TestEntraConfiguration.Apply(builder);
        builder.ConfigureServices(services =>
        {
            TestAuthenticationService.Reset();
            services.Replace(ServiceDescriptor.Singleton<IAuthenticationService, TestAuthenticationService>());
        });
    }
}

internal sealed class AnalysisProviderFactory(
    IMarketDataProvider provider,
    Action<MarketDataOptions> configureOptions,
    IAnalysisSummaryGenerator? summaryGenerator = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        TestEntraConfiguration.Apply(builder);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MarketData:ProviderId"] = "approved-test-provider",
                ["MarketData:PublicDisplayApproved"] = "true"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultForbidScheme = "Test";
                options.DefaultSignInScheme = "Test";
                options.DefaultSignOutScheme = "Test";
            });
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Test")
                    .RequireAuthenticatedUser()
                    .Build();
            });
            services.RemoveAll<IMarketDataProvider>();
            services.AddSingleton(provider);
            if (summaryGenerator is not null)
            {
                services.RemoveAll<IAnalysisSummaryGenerator>();
                services.AddSingleton(summaryGenerator);
            }
            services.PostConfigure<MarketDataOptions>(configureOptions);
        });
    }
}

internal sealed class FixedSummaryGenerator(string text) : IAnalysisSummaryGenerator
{
    public Task<AnalysisSummaryResult> GenerateAsync(
        AnalysisSummaryRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(AnalysisSummaryResult.Generated(text));
    }
}
