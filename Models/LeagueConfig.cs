namespace OddsAnalytics.Models;

public enum SportCategory
{
    Football,
    IceHockey
}

public sealed record LeagueConfig(
    string Id,
    string Name,
    SportCategory Sport,
    double HomeGoalsAvg,
    double AwayGoalsAvg);

public sealed record LeagueDefinition(
    string Key,
    string DisplayName,
    string Country,
    SportCategory Sport,
    int? ApiFootballId,
    string? OddsApiSportKey,
    int Season);

public static class LeagueCatalog
{
    private static readonly int CurrentSeason = DateTime.UtcNow.Month >= 7
        ? DateTime.UtcNow.Year
        : DateTime.UtcNow.Year - 1;

    public static IReadOnlyList<LeagueDefinition> AllLeagues { get; } = new List<LeagueDefinition>
    {
        new("epl", "Premier League", "England", SportCategory.Football, 39, "soccer_epl", CurrentSeason),
        new("championship", "Championship", "England", SportCategory.Football, 40, "soccer_efl_champ", CurrentSeason),
        new("league_one", "League One", "England", SportCategory.Football, 41, "soccer_england_league1", CurrentSeason),
        new("league_two", "League Two", "England", SportCategory.Football, 42, "soccer_england_league2", CurrentSeason),
        new("la_liga", "La Liga", "Spain", SportCategory.Football, 140, "soccer_spain_la_liga", CurrentSeason),
        new("la_liga_2", "La Liga 2 (Segunda)", "Spain", SportCategory.Football, 141, "soccer_spain_segunda_division", CurrentSeason),
        new("bundesliga", "Bundesliga", "Germany", SportCategory.Football, 78, "soccer_germany_bundesliga", CurrentSeason),
        new("bundesliga_2", "2. Bundesliga", "Germany", SportCategory.Football, 79, "soccer_germany_bundesliga2", CurrentSeason),
        new("3_liga", "3. Liga", "Germany", SportCategory.Football, 80, "soccer_germany_liga3", CurrentSeason),
        new("serie_a", "Serie A", "Italy", SportCategory.Football, 135, "soccer_italy_serie_a", CurrentSeason),
        new("serie_b", "Serie B", "Italy", SportCategory.Football, 136, "soccer_italy_serie_b", CurrentSeason),
        new("ligue_1", "Ligue 1", "France", SportCategory.Football, 61, "soccer_france_ligue_one", CurrentSeason),
        new("ligue_2", "Ligue 2", "France", SportCategory.Football, 62, "soccer_france_ligue_two", CurrentSeason),
        new("allsvenskan", "Allsvenskan", "Sweden", SportCategory.Football, 113, "soccer_sweden_allsvenskan", CurrentSeason),
        new("superettan", "Superettan", "Sweden", SportCategory.Football, 114, "soccer_sweden_superettan", CurrentSeason),
        new("division_1", "Division 1", "Sweden", SportCategory.Football, null, null, CurrentSeason),
        new("mls", "MLS", "United States", SportCategory.Football, 253, "soccer_usa_mls", CurrentSeason),
        new("nhl", "NHL", "North America", SportCategory.IceHockey, null, "icehockey_nhl", CurrentSeason),
        new("shl", "SHL", "Sweden", SportCategory.IceHockey, null, "icehockey_sweden_hockey_league", CurrentSeason),
        new("hockeyallsvenskan", "HockeyAllsvenskan", "Sweden", SportCategory.IceHockey, null, "icehockey_sweden_allsvenskan", CurrentSeason)
    };
}

