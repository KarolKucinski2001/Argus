using System.Globalization;

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
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    public static ProfileCardView BuildCard(CuratedProfile profile) => new(
        profile.Id,
        profile.Name,
        profile.Description,
        profile.Monogram,
        profile.AccentIndex,
        HoldingsCount(profile.Holdings.Count),
        AsOf(profile.AsOf),
        profile.SourceLabel);

    public static ProfileDetailView BuildDetail(CuratedProfile profile)
    {
        var holdings = BuildHoldings(profile.Holdings);
        var sectors = Shares(profile.Holdings, holding => holding.Sector);
        var classes = Shares(profile.Holdings, holding => holding.AssetClass);
        return new ProfileDetailView(
            profile.Id,
            profile.Name,
            profile.Description,
            AsOf(profile.AsOf),
            profile.SourceLabel,
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
                holding.Sector,
                holding.AssetClass))
            .ToArray();

    public static IReadOnlyList<ProfileShareRow> Shares(
        IEnumerable<CuratedHolding> holdings,
        Func<CuratedHolding, string> key) =>
        holdings
            .GroupBy(key, StringComparer.Ordinal)
            .Select(group => (Label: group.Key, Share: group.Sum(holding => holding.Weight)))
            .OrderByDescending(item => item.Share)
            .ThenBy(item => item.Label, StringComparer.Ordinal)
            .Select(item => new ProfileShareRow(item.Label, Percent(item.Share), Meter(item.Share)))
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
        factors.Add($"Największa pozycja to {top.Name} ({top.AssetId}) z wagą {Percent(top.Weight)} wyświetlanych pozycji.");

        var topThree = ordered.Take(3).Sum(holding => holding.Weight);
        factors.Add($"Trzy największe pozycje stanowią łącznie {Percent(topThree)} wyświetlanych pozycji.");

        if (sectors.Count > 0)
        {
            factors.Add($"Największy udział ma sektor: {sectors[0].Label} ({sectors[0].ShareText}).");
        }

        if (classes.Count == 1)
        {
            factors.Add($"Wszystkie wyświetlane pozycje należą do jednej klasy aktywów: {classes[0].Label}.");
        }
        else if (classes.Count > 1)
        {
            var split = string.Join(", ", classes.Select(item => $"{item.Label} {item.ShareText}"));
            factors.Add($"Podział na klasy aktywów: {split}.");
        }

        factors.Add($"Profil zawiera {HoldingsCount(ordered.Length)}. Opis dotyczy składu portfela i nie jest oceną ani zaleceniem.");
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
            portfolio.Name,
            portfolio.ProfileName,
            AsOf(portfolio.AsOf),
            "Utworzono: " + portfolio.CreatedAtUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            HoldingsCount(holdings.Count),
            holdings);
    }

    public static SimulatedPortfolio CreateCopy(CuratedProfile profile, string id, DateTimeOffset nowUtc) => new(
        id,
        profile.Id,
        profile.Name,
        "Symulacja: " + profile.Name,
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

    public static string AsOf(DateOnly date) =>
        "Dane na dzień: " + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string Percent(decimal value) =>
        value.ToString("0.#", Polish) + "%";

    public static string HoldingsCount(int count) => count switch
    {
        1 => "1 pozycja",
        >= 2 and <= 4 => $"{count} pozycje",
        _ => $"{count} pozycji"
    };

    private static int Meter(decimal value) => (int)Math.Clamp(Math.Round(value), 0m, 100m);
}
