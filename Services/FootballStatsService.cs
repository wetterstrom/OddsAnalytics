using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public class FootballStatsService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private static readonly Dictionary<int, List<Team>> _cache = new();

    public FootballStatsService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["ApiKeys:FootballApi"] ?? "";
    }

    public async Task<List<Team>> FetchLeagueStatsAsync(int leagueId, string leagueName, int season = 2024)
    {
        if (_cache.TryGetValue(leagueId, out var cached) && cached.Count > 0)
        {
            return cached;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://v3.football.api-sports.io/standings?league={leagueId}&season={season}");
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            request.Headers.Add("x-apisports-key", _apiKey);
        }

        try
        {
            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return GetDefaultTeams(leagueName);

            var data = await response.Content.ReadFromJsonAsync<StandingsApiResponse>();
            var standingsList = data?.Response.FirstOrDefault()?.League.Standings.FirstOrDefault();

            if (standingsList == null || standingsList.Count == 0)
                return GetDefaultTeams(leagueName);

            var teams = new List<Team>();
            foreach (var item in standingsList)
            {
                double homePlayed = Math.Max(1, item.Home.Played);
                double awayPlayed = Math.Max(1, item.Away.Played);

                teams.Add(new Team(
                    Name: item.Team.Name,
                    League: leagueName,
                    HomeGoalsScoredAvg: Math.Round((double)item.Home.Goals.For / homePlayed, 2),
                    HomeGoalsConcededAvg: Math.Round((double)item.Home.Goals.Against / homePlayed, 2),
                    AwayGoalsScoredAvg: Math.Round((double)item.Away.Goals.For / awayPlayed, 2),
                    AwayGoalsConcededAvg: Math.Round((double)item.Away.Goals.Against / awayPlayed, 2)
                ));
            }

            _cache[leagueId] = teams;
            return teams;
        }
        catch
        {
            return GetDefaultTeams(leagueName);
        }
    }

    private List<Team> GetDefaultTeams(string leagueName)
    {
        var fallback = new TeamDataService();
        return fallback.Teams.Where(t => t.League == leagueName).ToList();
    }
}
