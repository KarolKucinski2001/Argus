using System.Globalization;
using _bootstrap_scaffold.Localization;

namespace _bootstrap_scaffold.Strategies;

public sealed record ProfileCardView(
    string Id,
    string Name,
    string Description,
    string Monogram,
    int AccentIndex,
    string HoldingsCount,
    string AsOfText,
    string SourceLabel);

public sealed record ProfileHoldingRow(
    string AssetId,
    string Name,
    string WeightText,
    int WeightMeter,
    string Sector,
    string AssetClass);

public sealed record ProfileShareRow(string Label, string ShareText, int ShareMeter);

public sealed record ProfileDetailView(
    string Id,
    string Name,
    string Description,
    string AsOfText,
    string SourceLabel,
    IReadOnlyList<ProfileHoldingRow> Holdings,
    IReadOnlyList<ProfileShareRow> SectorShares,
    IReadOnlyList<ProfileShareRow> AssetClassShares,
    IReadOnlyList<string> Factors);

public sealed record SimulatedHolding(string AssetId, string Name, decimal Weight, string Sector, string AssetClass);

public sealed record SimulatedPortfolioRow(
    string Id,
    string Name,
    string ProfileName,
    string AsOfText,
    string CreatedText,
    string HoldingsSummary,
    IReadOnlyList<ProfileHoldingRow> Holdings);

public static class StrategyProfilePresenter
{
    private const string SimulationPrefix = "Symulacja: ";

    public static ProfileCardView BuildCard(CuratedProfile profile) => new(
        profile.Id,
        Lang.T(profile.Name),
        Lang.T(profile.Description),
        profile.Monogram,
        profile.AccentIndex,
        HoldingsCount(profile.Holdings.Count),
        AsOf(profile.AsOf),
        Lang.T(profile.SourceLabel));

    public static ProfileDetailView BuildDetail(CuratedProfile profile)
    {
        var holdings = BuildHoldings(profile.Holdings);
        var sectors = Shares(profile.Holdings, holding => holding.Sector);
        var classes = Shares(profile.Holdings, holding => holding.AssetClass);
        return new ProfileDetailView(
            profile.Id,
            Lang.T(profile.Name),
            Lang.T(profile.Description),
            AsOf(profile.AsOf),
            Lang.T(profile.SourceLabel),
            holdings,
            sectors,
            classes,
            BuildFactors(profile.Holdings, sectors, classes));
    }

    public static IReadOnlyList<ProfileHoldingRow> BuildHoldings(IEnumerable<CuratedHolding> holdings) =>
        holdings
            .OrderByDescending(holding => holding.Weight)
            .ThenBy(holding => holding.AssetId, StringComparer.Ordinal)
            .Select(holding => new ProfileHoldingRow(
                holding.AssetId,
                holding.Name,
                Percent(holding.Weight),
                Meter(holding.Weight),
                CompositionLabel(holding.Sector),
                CompositionLabel(holding.AssetClass)))
            .ToArray();

    public static IReadOnlyList<ProfileShareRow> Shares(
        IEnumerable<CuratedHolding> holdings,
        Func<CuratedHolding, string> key) =>
        holdings
            .GroupBy(key, StringComparer.Ordinal)
            .Select(group => (Label: group.Key, Share: group.Sum(holding => holding.Weight)))
            .OrderByDescending(item => item.Share)
            .ThenBy(item => item.Label, StringComparer.Ordinal)
            .Select(item => new ProfileShareRow(CompositionLabel(item.Label), Percent(item.Share), Meter(item.Share)))
            .ToArray();

    public static IReadOnlyList<string> BuildFactors(
        IReadOnlyList<CuratedHolding> holdings,
        IReadOnlyList<ProfileShareRow> sectors,
        IReadOnlyList<ProfileShareRow> classes)
    {
        var ordered = holdings.OrderByDescending(holding => holding.Weight).ToArray();
        var factors = new List<string>();
        if (ordered.Length == 0)
        {
            return factors;
        }

        var top = ordered[0];
        factors.Add(Lang.T("Największa pozycja to {0} ({1}) z wagą {2} wyświetlanych pozycji.", top.Name, top.AssetId, Percent(top.Weight)));

        var topThree = ordered.Take(3).Sum(holding => holding.Weight);
        factors.Add(Lang.T("Trzy największe pozycje stanowią łącznie {0} wyświetlanych pozycji.", Percent(topThree)));

        if (sectors.Count > 0)
        {
            factors.Add(Lang.T("Największy udział ma sektor: {0} ({1}).", sectors[0].Label, sectors[0].ShareText));
        }

        if (classes.Count == 1)
        {
            factors.Add(Lang.T("Wszystkie wyświetlane pozycje należą do jednej klasy aktywów: {0}.", classes[0].Label));
        }
        else if (classes.Count > 1)
        {
            var split = string.Join(", ", classes.Select(item => $"{item.Label} {item.ShareText}"));
            factors.Add(Lang.T("Podział na klasy aktywów: {0}.", split));
        }

        factors.Add(Lang.T("Profil zawiera {0}. Opis dotyczy składu portfela i nie jest oceną ani zaleceniem.", HoldingsCount(ordered.Length)));
        return factors;
    }

    public static SimulatedPortfolioRow BuildPortfolioRow(SimulatedPortfolio portfolio)
    {
        var holdings = BuildHoldings(portfolio.Holdings.Select(holding => new CuratedHolding(
            holding.AssetId,
            holding.Name,
            holding.Weight,
            holding.Sector,
            holding.AssetClass)));
        return new SimulatedPortfolioRow(
            portfolio.Id,
            DisplayName(portfolio),
            Lang.T(portfolio.ProfileName),
            AsOf(portfolio.AsOf),
            Lang.T("Utworzono: {0}", portfolio.CreatedAtUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            HoldingsCount(holdings.Count),
            holdings);
    }

    public static SimulatedPortfolio CreateCopy(CuratedProfile profile, string id, DateTimeOffset nowUtc) => new(
        id,
        profile.Id,
        profile.Name,
        SimulationPrefix + profile.Name,
        profile.AsOf,
        nowUtc,
        profile.Holdings
            .Select(holding => new SimulatedHolding(
                holding.AssetId,
                holding.Name,
                holding.Weight,
                holding.Sector,
                holding.AssetClass))
            .ToArray());

    // Stored portfolio names keep the Polish copy prefix; translate it only for display.
    public static string DisplayName(SimulatedPortfolio portfolio) =>
        portfolio.Name.StartsWith(SimulationPrefix, StringComparison.Ordinal)
            ? Lang.T("Symulacja: {0}", Lang.T(portfolio.Name[SimulationPrefix.Length..]))
            : Lang.T(portfolio.Name);

    // "Akcje" is also the shared "Actions" column key, so the asset-class meaning is resolved here.
    public static string CompositionLabel(string polish) =>
        Lang.IsEnglish && polish == "Akcje" ? "Stocks" : Lang.T(polish);

    public static string AsOf(DateOnly date) =>
        Lang.T("Dane na dzień: {0}", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

    public static string Percent(decimal value) =>
        value.ToString("0.#", Lang.Culture) + "%";

    public static string HoldingsCount(int count) => count switch
    {
        1 => Lang.T("1 pozycja"),
        >= 2 and <= 4 => Lang.T("{0} pozycje", count),
        _ => Lang.T("{0} pozycji", count)
    };

    private static int Meter(decimal value) => (int)Math.Clamp(Math.Round(value), 0m, 100m);
}
