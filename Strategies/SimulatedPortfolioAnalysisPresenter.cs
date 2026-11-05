using _bootstrap_scaffold.Localization;

namespace _bootstrap_scaffold.Strategies;

public sealed record PortfolioMetricView(string Label, string Value, string Hint);

public sealed record SimulatedPortfolioAnalysisView(
    string Id,
    string Name,
    string ProfileName,
    string AsOfText,
    string ConcentrationLabel,
    string ConcentrationExplanation,
    IReadOnlyList<PortfolioMetricView> Metrics,
    IReadOnlyList<ProfileHoldingRow> Holdings,
    IReadOnlyList<ProfileShareRow> SectorShares,
    IReadOnlyList<ProfileShareRow> AssetClassShares,
    IReadOnlyList<string> Factors,
    IReadOnlyList<string> AnalyzableAssetIds,
    string CoverageText);

/// <summary>
/// Composition-based explanation of a simulated portfolio (PRD v2 FR-005). It describes how
/// weights are distributed and never scores, forecasts, or recommends the portfolio.
/// </summary>
public static class SimulatedPortfolioAnalysisPresenter
{
    public const decimal HighConcentrationBelow = 4m;
    public const decimal ModerateConcentrationBelow = 7m;

    public const string ConcentrationExplanation =
        "Etykieta opisuje rozkład wag na podstawie efektywnej liczby pozycji: poniżej 4 — wysoka koncentracja, od 4 do poniżej 7 — umiarkowana koncentracja, od 7 wzwyż — rozproszony skład. To opis składu, a nie ocena portfela.";

    public static SimulatedPortfolioAnalysisView Build(
        SimulatedPortfolio portfolio,
        IEnumerable<string> supportedAssetIds)
    {
        var holdings = portfolio.Holdings
            .Select(holding => new CuratedHolding(
                holding.AssetId,
                holding.Name,
                holding.Weight,
                holding.Sector,
                holding.AssetClass))
            .ToArray();
        var ordered = holdings
            .OrderByDescending(holding => holding.Weight)
            .ThenBy(holding => holding.AssetId, StringComparer.Ordinal)
            .ToArray();
        var sectors = StrategyProfilePresenter.Shares(holdings, holding => holding.Sector);
        var classes = StrategyProfilePresenter.Shares(holdings, holding => holding.AssetClass);
        var effective = EffectivePositions(holdings.Select(holding => holding.Weight));
        var label = Lang.T(ConcentrationLabel(effective));

        var factors = StrategyProfilePresenter.BuildFactors(holdings, sectors, classes).ToList();
        if (holdings.Length > 0)
        {
            factors.Insert(
                factors.Count - 1,
                Lang.T("Efektywna liczba pozycji wynosi {0}, co odpowiada etykiecie: {1}.", Number(effective), label));
        }

        var supported = new HashSet<string>(supportedAssetIds, StringComparer.OrdinalIgnoreCase);
        var analyzable = ordered
            .Where(holding => supported.Contains(holding.AssetId))
            .Select(holding => holding.AssetId)
            .ToArray();
        var coveredCount = ordered.Count(holding => supported.Contains(holding.AssetId));

        return new SimulatedPortfolioAnalysisView(
            portfolio.Id,
            StrategyProfilePresenter.DisplayName(portfolio),
            Lang.T(portfolio.ProfileName),
            StrategyProfilePresenter.AsOf(portfolio.AsOf),
            label,
            Lang.T(ConcentrationExplanation),
            BuildMetrics(ordered, effective, sectors.Count, classes.Count),
            StrategyProfilePresenter.BuildHoldings(holdings),
            sectors,
            classes,
            factors,
            analyzable,
            Coverage(coveredCount, ordered.Length, analyzable));
    }

    public static decimal EffectivePositions(IEnumerable<decimal> weights)
    {
        var values = weights.ToArray();
        var total = values.Sum();
        if (total <= 0m)
        {
            return 0m;
        }

        var sumOfSquares = values.Sum(weight => (weight / total) * (weight / total));
        return 1m / sumOfSquares;
    }

    public static string ConcentrationLabel(decimal effectivePositions) => effectivePositions switch
    {
        < HighConcentrationBelow => "Wysoka koncentracja",
        < ModerateConcentrationBelow => "Umiarkowana koncentracja",
        _ => "Rozproszony skład"
    };

    private static IReadOnlyList<PortfolioMetricView> BuildMetrics(
        IReadOnlyList<CuratedHolding> ordered,
        decimal effective,
        int sectorCount,
        int classCount)
    {
        var largest = ordered.Count == 0 ? 0m : ordered[0].Weight;
        var topThree = ordered.Take(3).Sum(holding => holding.Weight);
        return
        [
            new(Lang.T("Największa pozycja"), StrategyProfilePresenter.Percent(largest), Lang.T("Udział pozycji o największej wadze.")),
            new(Lang.T("Trzy największe pozycje"), StrategyProfilePresenter.Percent(topThree), Lang.T("Łączny udział trzech pozycji o największej wadze.")),
            new(Lang.T("Efektywna liczba pozycji"), Number(effective), Lang.T("Liczba równych pozycji o podobnym rozkładzie wag (1 / suma kwadratów wag).")),
            new(Lang.T("Sektory"), sectorCount.ToString(Lang.Culture), Lang.T("Liczba różnych sektorów w składzie.")),
            new(Lang.T("Klasy aktywów"), classCount.ToString(Lang.Culture), Lang.T("Liczba różnych klas aktywów w składzie."))
        ];
    }

    private static string Coverage(int covered, int total, IReadOnlyList<string> analyzable)
    {
        if (covered == 0)
        {
            return Lang.T("Żadna pozycja tego portfela nie jest obecnie obsługiwana przez źródło danych rynkowych Argus, więc analiza dotyczy tylko składu.");
        }

        return Lang.T(
            "Argus może pokazać analizę rynkową dla {0} z {1} pozycji: {2}. Pozostałe pozycje opisuje tylko analiza składu.",
            covered,
            total,
            string.Join(", ", analyzable));
    }

    private static string Number(decimal value) => value.ToString("0.0", Lang.Culture);
}
