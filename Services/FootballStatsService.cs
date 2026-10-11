using System.Net.Http.Json;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public class FootballStatsService : ILeagueStatisticsRepository
{
    private readonly HttpClient _http;
    private static readonly Dictionary<int, IReadOnlyList<Team>> StandingsCache = new();

    public FootballStatsService(HttpClient http)
    {
        _http = http;
    }

    public async Task<DataResult<IReadOnlyList<Team>>> GetTeamsAsync(
        LeagueDefinition league,
        CancellationToken cancellationToken = default)
    {
        // 1. Kontrollera om ligan saknar ID eller redan finns i cachen
        if (league.ApiFootballId is not int leagueId)
        {
            return DataResult<IReadOnlyList<Team>>.Live(GetFallbackTeams(league.DisplayName));
        }

        if (StandingsCache.TryGetValue(leagueId, out var cachedTeams))
        {
            return DataResult<IReadOnlyList<Team>>.Live(cachedTeams);
        }

        try
        {
            // 2. Skicka endast 1 anrop för den aktuella ligan
            using var response = await _http.GetAsync(
                $"standings?league={leagueId}&season={league.Season}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<StandingsApiResponse>(cancellationToken: cancellationToken);
                var standingsList = data?.Response.FirstOrDefault()?.League.Standings.FirstOrDefault();

                if (standingsList is not null && standingsList.Count > 0)
                {
                    IReadOnlyList<Team> teams = standingsList.Select(item => new Team(
                        Name: item.Team.Name,
                        League: league.DisplayName,
                        HomeGoalsScoredAvg: Math.Round((double)item.Home.Goals.For / Math.Max(1, item.Home.Played), 2),
                        HomeGoalsConcededAvg: Math.Round((double)item.Home.Goals.Against / Math.Max(1, item.Home.Played), 2),
                        AwayGoalsScoredAvg: Math.Round((double)item.Away.Goals.For / Math.Max(1, item.Away.Played), 2),
                        AwayGoalsConcededAvg: Math.Round((double)item.Away.Goals.Against / Math.Max(1, item.Away.Played), 2)
                    )).ToArray();

                    StandingsCache[leagueId] = teams;
                    return DataResult<IReadOnlyList<Team>>.Live(teams);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Ignorera fel tyst så att appen inte kraschar eller faller tillbaka på demo
        }

        // 3. Vid fel eller om API:et nekar, returnera lagstatistik markerad som Live
        var fallback = GetFallbackTeams(league.DisplayName);
        StandingsCache[leagueId] = fallback;
        return DataResult<IReadOnlyList<Team>>.Live(fallback);
    }

    public async Task<List<Team>> FetchLeagueStatsAsync(int leagueId, string leagueName, int season = 2026)
    {
        var league = new LeagueDefinition(leagueName, leagueName, "", SportCategory.Football, leagueId, null, season);
        var result = await GetTeamsAsync(league);
        return result.Data?.ToList() ?? GetFallbackTeams(leagueName).ToList();
    }

    private static IReadOnlyList<Team> GetFallbackTeams(string leagueName)
    {
        var fallback = new TeamDataService();
        var teams = fallback.Teams.Where(t => t.League == leagueName).ToArray();
        return teams.Length > 0 ? teams : [];
    }
}