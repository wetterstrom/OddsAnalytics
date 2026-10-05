namespace OddsAnalytics.Models;

public record LeagueConfig(
    string Key,
    string DisplayName,
    string Country,
    int ApiFootballId,
    string OddsApiSportKey,
    int Season = 2024
);

public static class LeagueRepository
{
    public static readonly List<LeagueConfig> AllLeagues = new()
    {
        // 🏴󠁧󠁢󠁥󠁮󠁧󠁿 England
        new("epl", "Premier League", "🏴󠁧󠁢󠁥󠁮󠁧󠁿 England", 39, "soccer_epl"),
        new("championship", "Championship", "🏴󠁧󠁢󠁥󠁮󠁧󠁿 England", 40, "soccer_efl_champ"),
        new("league_one", "League One", "🏴󠁧󠁢󠁥󠁮󠁧󠁿 England", 41, "soccer_england_league1"),
        new("league_two", "League Two", "🏴󠁧󠁢󠁥󠁮󠁧󠁿 England", 42, "soccer_england_league2"),

        // 🇪🇸 Spanien
        new("la_liga", "La Liga", "🇪🇸 Spanien", 140, "soccer_spain_la_liga"),
        new("la_liga_2", "La Liga 2 (Segunda)", "🇪🇸 Spanien", 141, "soccer_spain_segunda_division"),

        // 🇩🇪 Tyskland
        new("bundesliga", "Bundesliga", "🇩🇪 Tyskland", 78, "soccer_germany_bundesliga"),
        new("bundesliga_2", "2. Bundesliga", "🇩🇪 Tyskland", 79, "soccer_germany_bundesliga2"),
        new("3_liga", "3. Liga", "🇩🇪 Tyskland", 80, "soccer_germany_liga3"),

        // 🇮🇹 Italien
        new("serie_a", "Serie A", "🇮🇹 Italien", 135, "soccer_italy_serie_a"),

        // 🇫🇷 Frankrike
        new("ligue_1", "Ligue 1", "🇫🇷 Frankrike", 61, "soccer_france_ligue_one"),
        new("ligue_2", "Ligue 2", "🇫🇷 Frankrike", 62, "soccer_france_ligue_two"),

        // 🇸🇪 Sverige
        new("allsvenskan", "Allsvenskan", "🇸🇪 Sverige", 113, "soccer_sweden_allsvenskan"),
        new("superettan", "Superettan", "🇸🇪 Sverige", 114, "soccer_sweden_superettan")
    };
}
