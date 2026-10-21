using _bootstrap_scaffold.Analysis;
using _bootstrap_scaffold.MarketData;
using Xunit;

namespace Argus.Authentication.Tests;

public sealed class AnalysisContractTests
{
    private static readonly DateTimeOffset AsOf = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Build_mentions_available_metrics_and_avoids_unshown_facts()
    {
        var builder = new AnalysisExplanationBuilder();
        var marketData = CreateResult(
            120m,
            Point(AsOf.AddHours(-24), 150m),
            Point(AsOf.AddDays(-7), 120m),
            Point(AsOf.AddDays(-30), 100m));
        var performance = CreatePerformance(-20m, 0m, 20m);
        var assessment = new OpportunityScorer().Assess(performance);

        var summary = builder.Build(marketData, performance, assessment);

        Assert.Contains("Wynik 57.5/100", summary);
        Assert.Contains("momentum 24 godziny", summary);
        Assert.Contains("-20", summary);
        Assert.Contains("momentum 7 dni", summary);
        Assert.Contains("0", summary);
        Assert.Contains("trend 30 dni", summary);
        Assert.Contains("20", summary);
        Assert.Contains("Jakość danych: kompletna", summary);
        Assert.DoesNotContain("test", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider", summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Apple", summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_acknowledges_insufficient_evidence()
    {
        var builder = new AnalysisExplanationBuilder();
        var marketData = CreateResult(
            120m,
            Point(AsOf.AddHours(-24), 100m));
        var performance = new AssetPerformance(
            new PerformanceMetric("24h", null, false),
            new PerformanceMetric("7d", 20m, true),
            new PerformanceMetric("30d", null, false),
            []);
        var assessment = new OpportunityScorer().Assess(performance);

        var summary = builder.Build(marketData, performance, assessment);

        Assert.Contains("Niewystarczające dane", summary);
        Assert.Contains("mniej niż wymagane dwa czynniki", summary);
        Assert.Contains("momentum 7 dni", summary);
        Assert.Contains("20", summary);
        Assert.DoesNotContain("test", summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_preserves_provider_errors_without_raw_provider_messages()
    {
        var error = new MarketDataError(
            MarketDataErrorCategory.UpstreamUnavailable,
            "RAW vendor outage id provider-123",
            true,
            503);
        var warning = new MarketDataWarning(
            MarketDataWarningCategory.UpstreamUnavailable,
            "Provider is temporarily unavailable.",
            true);
        var source = new MarketDataSource("secret-provider", "Raw Attribution", false, null);
        var marketDataResponse = new MarketDataResponse(
        [
            CreateErrorResult(
                new AssetReference("ERR", "Errored asset", MarketAssetClass.Equity),
                error,
                [warning],
                source)
        ]);
        var service = CreateAnalysisService();

        var response = service.Analyze(marketDataResponse, AsOf);

        var result = Assert.Single(response.Results);
        Assert.Equal("ERR", result.Asset.Id);
        Assert.Null(result.Score);
        Assert.Null(result.Summary);
        Assert.Same(error, result.Error);
        Assert.Same(warning, Assert.Single(result.Warnings));
        Assert.Equal(MarketDataQuality.Unavailable, result.Quality);
        Assert.True(result.Freshness.IsStale);
    }

    [Fact]
    public void Analyze_ranks_scored_assets_descending()
    {
        var marketDataResponse = new MarketDataResponse(
        [
            CreateResult(
                120m,
                new AssetReference("LOW", "Low score", MarketAssetClass.Equity),
                Point(AsOf.AddHours(-24), 120m),
                Point(AsOf.AddDays(-7), 120m),
                Point(AsOf.AddDays(-30), 120m)),
            CreateResult(
                120m,
                new AssetReference("HIGH", "High score", MarketAssetClass.Equity),
                Point(AsOf.AddHours(-24), 80m),
                Point(AsOf.AddDays(-7), 80m),
                Point(AsOf.AddDays(-30), 80m))
        ]);
        var service = CreateAnalysisService();

        var response = service.Analyze(marketDataResponse, AsOf);

        Assert.Collection(
            response.Results,
            result =>
            {
                Assert.Equal("HIGH", result.Asset.Id);
                Assert.Equal(100m, result.Score);
            },
            result =>
            {
                Assert.Equal("LOW", result.Asset.Id);
                Assert.Equal(50m, result.Score);
            });
    }

    [Fact]
    public void Analyze_preserves_submitted_order_for_score_ties()
    {
        var marketDataResponse = new MarketDataResponse(
        [
            CreateResult(
                120m,
                new AssetReference("FIRST", "First score", MarketAssetClass.Equity),
                Point(AsOf.AddHours(-24), 100m),
                Point(AsOf.AddDays(-7), 100m),
                Point(AsOf.AddDays(-30), 100m)),
            CreateResult(
                240m,
                new AssetReference("SECOND", "Second score", MarketAssetClass.Equity),
                Point(AsOf.AddHours(-24), 200m),
                Point(AsOf.AddDays(-7), 200m),
                Point(AsOf.AddDays(-30), 200m))
        ]);
        var service = CreateAnalysisService();

        var response = service.Analyze(marketDataResponse, AsOf);

        Assert.Collection(
            response.Results,
            result => Assert.Equal("FIRST", result.Asset.Id),
            result => Assert.Equal("SECOND", result.Asset.Id));
        Assert.Equal(response.Results[0].Score, response.Results[1].Score);
    }

    [Fact]
    public void Analyze_keeps_successful_results_when_another_asset_has_an_error()
    {
        var successful = CreateResult(
            120m,
            new AssetReference("OK", "Successful asset", MarketAssetClass.Equity),
            Point(AsOf.AddHours(-24), 100m),
            Point(AsOf.AddDays(-7), 96m),
            Point(AsOf.AddDays(-30), 80m));
        var insufficient = CreateResult(
            120m,
            new AssetReference("THIN", "Thin history", MarketAssetClass.Equity),
            Point(AsOf.AddHours(-24), 100m));
        var errored = CreateErrorResult(
            new AssetReference("ERR", "Errored asset", MarketAssetClass.Crypto),
            new MarketDataError(MarketDataErrorCategory.Timeout, "RAW timeout provider-789", true),
            [],
            null);
        var service = CreateAnalysisService();

        var response = service.Analyze(new MarketDataResponse([errored, insufficient, successful]), AsOf);

        Assert.Collection(
            response.Results,
            result =>
            {
                Assert.Equal("OK", result.Asset.Id);
                Assert.NotNull(result.Score);
                Assert.Null(result.Error);
                Assert.NotNull(result.Summary);
                Assert.NotEmpty(result.Factors);
                Assert.NotEmpty(result.ChartPoints);
            },
            result =>
            {
                Assert.Equal("ERR", result.Asset.Id);
                Assert.Null(result.Score);
                Assert.NotNull(result.Error);
                Assert.Empty(result.Factors);
                Assert.Empty(result.ChartPoints);
            },
            result =>
            {
                Assert.Equal("THIN", result.Asset.Id);
                Assert.Null(result.Score);
                Assert.Null(result.Error);
                Assert.Contains("Niewystarczające dane", result.Summary!);
                Assert.NotEmpty(result.Factors);
            });
    }

    [Fact]
    public async Task AnalyzeAsync_invokes_generator_with_displayed_evidence()
    {
            var generator = new RecordingSummaryGenerator(
                _ => AnalysisSummaryResult.Generated("Grounded summary."));
            var service = CreateAnalysisService(generator);
            var marketData = new MarketDataResponse(
            [
                CreateResult(
                    120m,
                    new AssetReference("AAPL", "Apple", MarketAssetClass.Equity),
                    Point(AsOf.AddHours(-24), 100m),
                    Point(AsOf.AddDays(-7), 96m),
                    Point(AsOf.AddDays(-30), 80m))
            ]);

            var response = await service.AnalyzeAsync(marketData, AsOf, CancellationToken.None);

            var result = Assert.Single(response.Results);
            Assert.Equal(AnalysisSummaryStatus.Generated, result.SummaryResult?.Status);
            Assert.Equal("Grounded summary.", result.Summary);
            Assert.NotNull(generator.LastRequest);
            var request = generator.LastRequest!;
            Assert.Equal(result.Asset, request.Asset);
            Assert.Equal(result.Score, request.Score);
            Assert.Equal(result.Performance, request.Performance);
            Assert.Equal(result.Factors, request.Factors);
            Assert.Equal(result.Quality, request.Quality);
            Assert.Equal(result.Freshness, request.Freshness);
            Assert.Equal(result.IsDelayed, request.IsDelayed);
            Assert.Equal(result.Warnings.Select(warning => warning.Category), request.WarningCategories);
    }

    [Fact]
    public async Task AnalyzeAsync_skips_generator_when_evidence_is_insufficient()
    {
            var generator = new RecordingSummaryGenerator(
                _ => AnalysisSummaryResult.Generated("Should not be used."));
            var service = CreateAnalysisService(
                generator,
                new StaticPerformanceCalculator(
                    new AssetPerformance(
                        new PerformanceMetric("24h", 20m, true),
                        new PerformanceMetric("7d", null, false),
                        new PerformanceMetric("30d", null, false),
                        [])));
            var marketData = new MarketDataResponse(
            [
                CreateResult(
                    120m,
                    new AssetReference("THIN", "Thin history", MarketAssetClass.Equity),
                    Point(AsOf.AddDays(-7), 100m))
            ]);

            var response = await service.AnalyzeAsync(marketData, AsOf, CancellationToken.None);

            var result = Assert.Single(response.Results);
            Assert.Null(generator.LastRequest);
            Assert.Null(result.Score);
            Assert.Equal(AnalysisSummaryStatus.InsufficientEvidence, result.SummaryResult?.Status);
            Assert.Null(result.Summary);
    }

    [Fact]
    public async Task AnalyzeAsync_propagates_unexpected_generator_failures()
    {
            var generator = new RecordingSummaryGenerator(
                _ => throw new InvalidOperationException("generator bug"));
            var service = CreateAnalysisService(generator);
            var marketData = new MarketDataResponse(
            [
                CreateResult(
                    120m,
                    new AssetReference("AAPL", "Apple", MarketAssetClass.Equity),
                    Point(AsOf.AddHours(-24), 100m),
                    Point(AsOf.AddDays(-7), 96m),
                    Point(AsOf.AddDays(-30), 80m))
            ]);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AnalyzeAsync(marketData, AsOf, CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeAsync_marks_summary_unavailable_when_generator_times_out()
    {
            var generator = new BehaviorSummaryGenerator(
                (_, _) => throw new OperationCanceledException("provider timeout"));
            var service = CreateAnalysisService(generator);
            var marketData = new MarketDataResponse(
            [
                CreateResult(
                    120m,
                    new AssetReference("AAPL", "Apple", MarketAssetClass.Equity),
                    Point(AsOf.AddHours(-24), 100m),
                    Point(AsOf.AddDays(-7), 96m),
                    Point(AsOf.AddDays(-30), 80m))
            ]);

            var response = await service.AnalyzeAsync(marketData, AsOf, CancellationToken.None);

            var result = Assert.Single(response.Results);
            Assert.Equal(AnalysisSummaryStatus.Generated, result.SummaryResult?.Status);
            Assert.NotNull(result.Summary);
            Assert.Contains("Wynik", result.Summary);
            Assert.NotNull(result.Score);
            Assert.NotEmpty(result.Factors);
    }

    [Fact]
    public async Task AnalyzeAsync_preserves_evidence_when_generator_has_http_failure()
    {
            var generator = new BehaviorSummaryGenerator(
                (_, _) => throw new HttpRequestException("provider unavailable"));
            var service = CreateAnalysisService(generator);
            var marketData = new MarketDataResponse(
            [
                CreateResult(
                    120m,
                    new AssetReference("AAPL", "Apple", MarketAssetClass.Equity),
                    Point(AsOf.AddHours(-24), 100m),
                    Point(AsOf.AddDays(-7), 96m),
                    Point(AsOf.AddDays(-30), 80m))
            ]);

            var response = await service.AnalyzeAsync(marketData, AsOf, CancellationToken.None);

            var result = Assert.Single(response.Results);
            Assert.Equal(AnalysisSummaryStatus.Generated, result.SummaryResult?.Status);
            Assert.NotNull(result.Summary);
            Assert.Contains("Wynik", result.Summary);
    }

    [Fact]
    public async Task AnalyzeAsync_propagates_request_cancellation_without_rendering_partial_summary()
    {
            using var cancellation = new CancellationTokenSource();
            var generator = new BehaviorSummaryGenerator(
                (_, token) =>
                {
                    cancellation.Cancel();
                    token.ThrowIfCancellationRequested();
                    return Task.FromResult(AnalysisSummaryResult.Generated("Should not be used."));
                });
            var service = CreateAnalysisService(generator);
            var marketData = new MarketDataResponse(
            [
                CreateResult(
                    120m,
                    new AssetReference("AAPL", "Apple", MarketAssetClass.Equity),
                    Point(AsOf.AddHours(-24), 100m),
                    Point(AsOf.AddDays(-7), 96m),
                    Point(AsOf.AddDays(-30), 80m))
            ]);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                service.AnalyzeAsync(marketData, AsOf, cancellation.Token));
    }

    [Fact]
    public async Task AnalyzeAsync_invokes_generator_once_per_sufficient_asset_and_keeps_mixed_results()
    {
            var calls = new List<string>();
            var generator = new BehaviorSummaryGenerator(
                (request, _) =>
                {
                    calls.Add(request.Asset.Id);
                    return request.Asset.Id == "AAPL"
                        ? Task.FromResult(AnalysisSummaryResult.Generated("AAPL grounded summary."))
                        : throw new HttpRequestException("second asset unavailable");
                });
            var service = CreateAnalysisService(generator);
            var marketData = new MarketDataResponse(
            [
                CreateResult(
                    120m,
                    new AssetReference("AAPL", "Apple", MarketAssetClass.Equity),
                    Point(AsOf.AddHours(-24), 100m),
                    Point(AsOf.AddDays(-7), 96m),
                    Point(AsOf.AddDays(-30), 80m)),
                CreateResult(
                    120m,
                    new AssetReference("MSFT", "Microsoft", MarketAssetClass.Equity),
                    Point(AsOf.AddHours(-24), 100m),
                    Point(AsOf.AddDays(-7), 96m),
                    Point(AsOf.AddDays(-30), 80m))
            ]);

            var response = await service.AnalyzeAsync(marketData, AsOf, CancellationToken.None);

            Assert.Equal(["AAPL", "MSFT"], calls);
            Assert.Collection(
                response.Results,
                result => Assert.Equal(AnalysisSummaryStatus.Generated, result.SummaryResult?.Status),
                result =>
                {
                    Assert.Equal(AnalysisSummaryStatus.Generated, result.SummaryResult?.Status);
                    Assert.NotNull(result.Summary);
                });
            Assert.All(response.Results, result =>
            {
                Assert.NotNull(result.Score);
                Assert.NotEmpty(result.Factors);
                Assert.NotEmpty(result.ChartPoints);
            });
    }

    [Fact]
    public async Task AnalyzeAsync_does_not_invoke_generator_for_market_data_errors()
    {
            var generator = new RecordingSummaryGenerator(
                _ => AnalysisSummaryResult.Generated("Should not be used."));
            var service = CreateAnalysisService(generator);
            var marketData = new MarketDataResponse(
            [
                CreateErrorResult(
                    new AssetReference("ERR", "Errored asset", MarketAssetClass.Equity),
                    new MarketDataError(MarketDataErrorCategory.Timeout, "raw provider detail", true),
                    [],
                    null)
            ]);

            var response = await service.AnalyzeAsync(marketData, AsOf, CancellationToken.None);

            var result = Assert.Single(response.Results);
            Assert.Null(generator.LastRequest);
            Assert.Null(result.SummaryResult);
            Assert.Null(result.Score);
            Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task AnalyzeAsync_rejects_empty_and_oversized_generator_output()
    {
            foreach (var generated in new[]
            {
                AnalysisSummaryResult.Generated(" "),
                AnalysisSummaryResult.Generated(new string('x', AnalysisSummaryResult.MaximumSummaryLength + 1))
            })
            {
                var generator = new RecordingSummaryGenerator(_ => generated);
                var service = CreateAnalysisService(generator);
                var marketData = new MarketDataResponse(
                [
                    CreateResult(
                        120m,
                        new AssetReference("AAPL", "Apple", MarketAssetClass.Equity),
                        Point(AsOf.AddHours(-24), 100m),
                        Point(AsOf.AddDays(-7), 96m),
                        Point(AsOf.AddDays(-30), 80m))
                ]);

                var response = await service.AnalyzeAsync(marketData, AsOf, CancellationToken.None);

                var result = Assert.Single(response.Results);
                Assert.Equal(AnalysisSummaryStatus.Generated, result.SummaryResult?.Status);
                Assert.NotNull(result.Summary);
                Assert.Contains("Wynik", result.Summary);
            }
    }

    [Fact]
    public async Task AnalyzeAsync_rejects_control_characters_from_generator_output()
    {
            var generator = new RecordingSummaryGenerator(
                _ => AnalysisSummaryResult.Generated("safe\u0000unsafe"));
            var service = CreateAnalysisService(generator);
            var marketData = new MarketDataResponse(
            [
                CreateResult(
                    120m,
                    new AssetReference("AAPL", "Apple", MarketAssetClass.Equity),
                    Point(AsOf.AddHours(-24), 100m),
                    Point(AsOf.AddDays(-7), 96m),
                    Point(AsOf.AddDays(-30), 80m))
            ]);

            var response = await service.AnalyzeAsync(marketData, AsOf, CancellationToken.None);

            var result = Assert.Single(response.Results);
            Assert.Equal(AnalysisSummaryStatus.Generated, result.SummaryResult?.Status);
            Assert.NotNull(result.Summary);
            Assert.Contains("Wynik", result.Summary);
    }

    [Fact]
    public void Assess_normalizes_each_factor_to_zero_to_one_hundred()
    {
        var scorer = new OpportunityScorer();
        var performance = CreatePerformance(-20m, 0m, 20m);

        var assessment = scorer.Assess(performance);

        Assert.Equal(57.50m, assessment.Score);
        Assert.Collection(
            assessment.Factors,
            factor => AssertFactor(factor, "momentum-24h", -20m, 0.25m, 0m, true),
            factor => AssertFactor(factor, "momentum-7d", 0m, 0.35m, 17.50m, true),
            factor => AssertFactor(factor, "trend-30d", 20m, 0.40m, 40.00m, true));
    }

    [Fact]
    public void Assess_applies_fixed_weights_and_returns_contributions()
    {
        var scorer = new OpportunityScorer();
        var performance = CreatePerformance(-10m, 0m, 10m);

        var assessment = scorer.Assess(performance);

        Assert.True(assessment.HasSufficientEvidence);
        Assert.Equal(53.75m, assessment.Score);
        Assert.Collection(
            assessment.Factors,
            factor => AssertFactor(factor, "momentum-24h", -10m, 0.25m, 6.25m, true),
            factor => AssertFactor(factor, "momentum-7d", 0m, 0.35m, 17.50m, true),
            factor => AssertFactor(factor, "trend-30d", 10m, 0.40m, 30.00m, true));
    }

    [Fact]
    public void Assess_clamps_performance_outside_the_normalization_range()
    {
        var scorer = new OpportunityScorer();
        var performance = CreatePerformance(-50m, 80m, 50m);

        var assessment = scorer.Assess(performance);

        Assert.Equal(75.00m, assessment.Score);
        Assert.Collection(
            assessment.Factors,
            factor => AssertFactor(factor, "momentum-24h", -50m, 0.25m, 0m, true),
            factor => AssertFactor(factor, "momentum-7d", 80m, 0.35m, 35.00m, true),
            factor => AssertFactor(factor, "trend-30d", 50m, 0.40m, 40.00m, true));
    }

    [Fact]
    public void Assess_renormalizes_weights_when_one_factor_is_missing()
    {
        var scorer = new OpportunityScorer();
        var performance = new AssetPerformance(
            new PerformanceMetric("24h", 0m, true),
            new PerformanceMetric("7d", null, false),
            new PerformanceMetric("30d", 20m, true),
            []);

        var assessment = scorer.Assess(performance);

        Assert.True(assessment.HasSufficientEvidence);
        Assert.Equal(80.77m, assessment.Score);
        Assert.Collection(
            assessment.Factors,
            factor => AssertFactor(factor, "momentum-24h", 0m, 0.38m, 19.23m, true),
            factor => AssertFactor(factor, "momentum-7d", null, 0m, null, false),
            factor => AssertFactor(factor, "trend-30d", 20m, 0.62m, 61.54m, true));
    }

    [Fact]
    public void Assess_returns_insufficient_evidence_when_fewer_than_two_factors_exist()
    {
        var scorer = new OpportunityScorer();
        var performance = new AssetPerformance(
            new PerformanceMetric("24h", null, false),
            new PerformanceMetric("7d", 20m, true),
            new PerformanceMetric("30d", null, false),
            []);

        var assessment = scorer.Assess(performance);

        Assert.False(assessment.HasSufficientEvidence);
        Assert.Null(assessment.Score);
        Assert.Collection(
            assessment.Factors,
            factor => AssertFactor(factor, "momentum-24h", null, 0m, null, false),
            factor => AssertFactor(factor, "momentum-7d", 20m, 1.00m, 100.00m, true),
            factor => AssertFactor(factor, "trend-30d", null, 0m, null, false));
    }

    [Fact]
    public void Assess_returns_zero_score_for_three_negative_boundary_factors()
    {
        var scorer = new OpportunityScorer();
        var performance = CreatePerformance(-20m, -20m, -20m);

        var assessment = scorer.Assess(performance);

        Assert.True(assessment.HasSufficientEvidence);
        Assert.Equal(0m, assessment.Score);
        Assert.All(assessment.Factors, factor => Assert.Equal(0m, factor.Contribution));
    }

    [Fact]
    public void Calculate_uses_latest_history_point_at_or_before_each_period_boundary()
    {
        var calculator = new PerformanceCalculator();
        var result = CreateResult(
            120m,
            Point(AsOf.AddHours(-24).AddHours(-2), 90m),
            Point(AsOf.AddHours(-24), 100m),
            Point(AsOf.AddHours(-24).AddMinutes(1), 110m),
            Point(AsOf.AddDays(-7).AddHours(-1), 80m),
            Point(AsOf.AddDays(-7), 96m),
            Point(AsOf.AddDays(-7).AddMinutes(1), 110m),
            Point(AsOf.AddDays(-30).AddHours(-1), 70m),
            Point(AsOf.AddDays(-30), 80m),
            Point(AsOf.AddDays(-30).AddMinutes(1), 110m));

        var performance = calculator.Calculate(result, AsOf);

        Assert.Equal(20m, performance.Change24Hours.Percentage);
        Assert.True(performance.Change24Hours.IsAvailable);
        Assert.Equal(25m, performance.Change7Days.Percentage);
        Assert.True(performance.Change7Days.IsAvailable);
        Assert.Equal(50m, performance.Change30Days.Percentage);
        Assert.True(performance.Change30Days.IsAvailable);
    }

    [Fact]
    public void Calculate_returns_percentage_change_from_historical_close_to_current_quote()
    {
        var calculator = new PerformanceCalculator();
        var result = CreateResult(
            120m,
            Point(AsOf.AddDays(-30), 80m));

        var performance = calculator.Calculate(result, AsOf);

        Assert.Equal(50m, performance.Change24Hours.Percentage);
        Assert.Equal(50m, performance.Change7Days.Percentage);
        Assert.Equal(50m, performance.Change30Days.Percentage);
    }

    [Fact]
    public void Calculate_marks_period_unavailable_when_no_eligible_point_exists()
    {
        var calculator = new PerformanceCalculator();
        var result = CreateResult(
            120m,
            Point(AsOf.AddDays(-30).AddMinutes(1), 80m));

        var performance = calculator.Calculate(result, AsOf);

        Assert.Null(performance.Change30Days.Percentage);
        Assert.False(performance.Change30Days.IsAvailable);
    }

    [Fact]
    public void Calculate_projects_chart_points_in_timestamp_order()
    {
        var calculator = new PerformanceCalculator();
        var latest = Point(AsOf.AddDays(-1), 110m);
        var earliest = Point(AsOf.AddDays(-3), 90m);
        var middle = Point(AsOf.AddDays(-2), 100m);
        var result = CreateResult(120m, latest, earliest, middle);

        var performance = calculator.Calculate(result, AsOf);

        Assert.Collection(
            performance.ChartPoints,
            point =>
            {
                Assert.Equal(earliest.Timestamp, point.Timestamp);
                Assert.Equal(90m, point.Close);
            },
            point =>
            {
                Assert.Equal(middle.Timestamp, point.Timestamp);
                Assert.Equal(100m, point.Close);
            },
            point =>
            {
                Assert.Equal(latest.Timestamp, point.Timestamp);
                Assert.Equal(110m, point.Close);
            });
    }

    [Fact]
    public void Calculate_does_not_use_points_after_a_period_boundary()
    {
        var calculator = new PerformanceCalculator();
        var result = CreateResult(
            120m,
            Point(AsOf.AddDays(-7).AddMinutes(-1), 100m),
            Point(AsOf.AddDays(-7).AddMinutes(1), 60m));

        var performance = calculator.Calculate(result, AsOf);

        Assert.Equal(20m, performance.Change7Days.Percentage);
    }

    private static MarketDataResult CreateResult(decimal currentQuote, params HistoricalPricePoint[] points)
    {
        return CreateResult(
            currentQuote,
            new AssetReference("AAPL", "Apple", MarketAssetClass.Equity),
            points);
    }

    private static MarketDataResult CreateResult(
        decimal currentQuote,
        AssetReference asset,
        params HistoricalPricePoint[] points)
    {
        var history = new MarketDataHistoryRequest(
            DateOnly.FromDateTime(AsOf.AddDays(-30).Date),
            DateOnly.FromDateTime(AsOf.Date),
            MarketDataGranularity.Daily);
        var freshness = new MarketDataFreshness(AsOf, AsOf, AsOf.AddMinutes(5), false);

        return new MarketDataResult(
            asset,
            history,
            new MarketQuote(currentQuote, "USD", AsOf, freshness, MarketDataQuality.Complete),
            points,
            new MarketDataSource("test", "Test provider", false, null),
            freshness,
            MarketDataQuality.Complete,
            []);
    }

    private static MarketDataResult CreateErrorResult(
        AssetReference asset,
        MarketDataError error,
        IReadOnlyList<MarketDataWarning> warnings,
        MarketDataSource? source)
    {
        var history = new MarketDataHistoryRequest(
            DateOnly.FromDateTime(AsOf.AddDays(-30).Date),
            DateOnly.FromDateTime(AsOf.Date),
            MarketDataGranularity.Daily);
        var freshness = new MarketDataFreshness(AsOf.AddHours(-2), AsOf, AsOf.AddMinutes(-1), true);

        return new MarketDataResult(
            asset,
            history,
            null,
            [],
            source,
            freshness,
            MarketDataQuality.Unavailable,
            warnings,
            error);
    }

    private static AnalysisService CreateAnalysisService(
        IAnalysisSummaryGenerator? summaryGenerator = null,
        IPerformanceCalculator? performanceCalculator = null) =>
        new(
            performanceCalculator ?? new PerformanceCalculator(),
            new OpportunityScorer(),
            new AnalysisExplanationBuilder(),
            summaryGenerator);

    private static HistoricalPricePoint Point(DateTimeOffset timestamp, decimal close) =>
        new(timestamp, null, null, null, close, null, "USD", true);

    private static AssetPerformance CreatePerformance(decimal change24Hours, decimal change7Days, decimal change30Days) =>
        new(
            new PerformanceMetric("24h", change24Hours, true),
            new PerformanceMetric("7d", change7Days, true),
            new PerformanceMetric("30d", change30Days, true),
            []);

    private static void AssertFactor(
        AnalysisFactor factor,
        string key,
        decimal? percentage,
        decimal weight,
        decimal? contribution,
        bool isAvailable)
    {
        Assert.Equal(key, factor.Key);
        Assert.Equal(percentage, factor.Percentage);
        Assert.Equal(weight, Math.Round(factor.Weight, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(contribution, factor.Contribution);
        Assert.Equal(isAvailable, factor.IsAvailable);
        Assert.False(string.IsNullOrWhiteSpace(factor.Label));
        Assert.False(string.IsNullOrWhiteSpace(factor.Explanation));
    }

    private sealed class RecordingSummaryGenerator(
        Func<AnalysisSummaryRequest, AnalysisSummaryResult> handler) : IAnalysisSummaryGenerator
    {
        public AnalysisSummaryRequest? LastRequest { get; private set; }

        public Task<AnalysisSummaryResult> GenerateAsync(
            AnalysisSummaryRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastRequest = request;
            return Task.FromResult(handler(request));
        }
    }

    private sealed class BehaviorSummaryGenerator(
        Func<AnalysisSummaryRequest, CancellationToken, Task<AnalysisSummaryResult>> handler)
        : IAnalysisSummaryGenerator
    {
        public Task<AnalysisSummaryResult> GenerateAsync(
            AnalysisSummaryRequest request,
            CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }

    private sealed class StaticPerformanceCalculator(AssetPerformance performance) : IPerformanceCalculator
    {
        public AssetPerformance Calculate(MarketDataResult result, DateTimeOffset asOf) => performance;
    }
}
