using System.Net.Http.Json;
using System.Text.Json;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public class NhlStatsService
{
    private readonly HttpClient _http;
    private readonly IDemoSportsDataFactory _demoData;

    public NhlStatsService(HttpClient http, IDemoSportsDataFactory demoData)
    {
        _http = http;
        _demoData = demoData;
    }

    public Task<List<Team>> FetchNhlStandingsAsync() => FetchStandingsAsync("nhl");

    public async Task<List<Team>> FetchStandingsAsync(string leagueKey)
    {
        // Endast NHL har ett gratis standings-API via Workern.
        // SHL/HockeyAllsvenskan saknar gratis API och faller tillbaka på demodata.
        if (!leagueKey.Equals("nhl", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[NHL] League '{leagueKey}' has no live standings API - using demo data.");
            var league = LeagueCatalog.AllLeagues.FirstOrDefault(l => l.Key.Equals(leagueKey, StringComparison.OrdinalIgnoreCase));
            return league is null ? new List<Team>() : _demoData.CreateTeams(league).ToList();
        }

        var teams = new List<Team>();

        try
        {
            var url = "nhl/standings";
            Console.WriteLine($"[NHL] GET {_http.BaseAddress}{url}");
            var response = await _http.GetFromJsonAsync<JsonElement>(url);
            if (!response.TryGetProperty("standings", out var standingsList))
            {
                Console.WriteLine("[NHL] Response JSON saknar 'standings'-property.");
                return teams;
            }

            foreach (var s in standingsList.EnumerateArray())
            {
                try
                {
                    string place = "";
                    if (s.TryGetProperty("teamPlaceName", out var tp) && tp.TryGetProperty("default", out var tpd))
                        place = tpd.GetString() ?? "";

                    string common = "";
                    if (s.TryGetProperty("teamCommonName", out var tc) && tc.TryGetProperty("default", out var tcd))
                        common = tcd.GetString() ?? "";

                    string fullName = $"{place} {common}".Trim();

                    if (string.IsNullOrWhiteSpace(fullName) && s.TryGetProperty("teamName", out var tn) && tn.TryGetProperty("default", out var tnd))
                    {
                        fullName = tnd.GetString() ?? "";
                    }

                    int homeGames = s.TryGetProperty("homeGamesPlayed", out var hg) && hg.ValueKind == JsonValueKind.Number ? hg.GetInt32() : 0;
                    int homeGf = s.TryGetProperty("homeGoalFor", out var hgf) && hgf.ValueKind == JsonValueKind.Number ? hgf.GetInt32() : 0;
                    int homeGa = s.TryGetProperty("homeGoalAgainst", out var hga) && hga.ValueKind == JsonValueKind.Number ? hga.GetInt32() : 0;

                    int awayGames = s.TryGetProperty("roadGamesPlayed", out var ag) && ag.ValueKind == JsonValueKind.Number ? ag.GetInt32() : 0;
                    int awayGf = s.TryGetProperty("roadGoalFor", out var agf) && agf.ValueKind == JsonValueKind.Number ? agf.GetInt32() : 0;
                    int awayGa = s.TryGetProperty("roadGoalAgainst", out var aga) && aga.ValueKind == JsonValueKind.Number ? aga.GetInt32() : 0;

                    if (homeGames == 0 && s.TryGetProperty("gamesPlayed", out var gp) && gp.ValueKind == JsonValueKind.Number)
                    {
                        int totalGames = gp.GetInt32();
                        int totalGf = s.TryGetProperty("goalFor", out var gf) && gf.ValueKind == JsonValueKind.Number ? gf.GetInt32() : 0;
                        int totalGa = s.TryGetProperty("goalAgainst", out var ga) && ga.ValueKind == JsonValueKind.Number ? ga.GetInt32() : 0;

                        if (totalGames > 0)
                        {
                            homeGames = totalGames / 2;
                            awayGames = totalGames - homeGames;
                            homeGf = totalGf / 2;
                            awayGf = totalGf - homeGf;
                            homeGa = totalGa / 2;
                            awayGa = totalGa - homeGa;
                        }
                    }

                    double homeScoredAvg = homeGames > 0 ? Math.Round((double)homeGf / homeGames, 2) : 3.10;
                    double homeConcededAvg = homeGames > 0 ? Math.Round((double)homeGa / homeGames, 2) : 2.80;
                    double awayScoredAvg = awayGames > 0 ? Math.Round((double)awayGf / awayGames, 2) : 2.70;
                    double awayConcededAvg = awayGames > 0 ? Math.Round((double)awayGa / awayGames, 2) : 3.10;

                    string form = "";
                    if (s.TryGetProperty("streakCode", out var streak) && streak.ValueKind == JsonValueKind.String)
                    {
                        form = streak.GetString() ?? "";
                    }

                    teams.Add(new Team(
                        Name: fullName,
                        League: "NHL",
                        HomeGoalsScoredAvg: homeScoredAvg,
                        HomeGoalsConcededAvg: homeConcededAvg,
                        AwayGoalsScoredAvg: awayScoredAvg,
                        AwayGoalsConcededAvg: awayConcededAvg
     
                    ));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NHL] Fel vid parsning av lagrad: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NHL] Standings-hämtning misslyckades: {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine($"[NHL] Parsade {teams.Count} lag.");
        return teams;
    }
}