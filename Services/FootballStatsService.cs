using System.Text.Json;
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
        if (league.ApiFootballId is not int leagueId)
        {
            return CreateFallbackResult(league.DisplayName, $"No API-Football league ID is configured for {league.DisplayName}.");
        }

        if (StandingsCache.TryGetValue(leagueId, out var cachedTeams))
        {
            return DataResult<IReadOnlyList<Team>>.Live(cachedTeams);
        }

        try
        {
            var requestUri = $"standings?league={leagueId}&season={league.Season}";
            Console.WriteLine($"[FootballStats] GET {_http.BaseAddress}{requestUri}");
            using var response = await _http.GetAsync(requestUri, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[FootballStats] HTTP {(int)response.StatusCode} {response.StatusCode}; response: {rawJson}");

            if (!response.IsSuccessStatusCode)
            {
                return CreateFallbackResult(league.DisplayName, $"Standings request returned HTTP {(int)response.StatusCode} ({response.StatusCode}).");
            }

            var data = JsonSerializer.Deserialize<StandingsApiResponse>(rawJson);
            var standingsList = data?.Response.FirstOrDefault()?.League.Standings.FirstOrDefault();
            Console.WriteLine($"[FootballStats] Parsed standings rows: {standingsList?.Count ?? 0}");

            if (standingsList is not { Count: > 0 })
            {
                return CreateFallbackResult(league.DisplayName, "The standings response contained no teams.");
            }

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[FootballStats] Request/deserialization failed for league={leagueId}, season={league.Season}: {exception}");
            return CreateFallbackResult(league.DisplayName, exception.Message);
        }
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

    private static DataResult<IReadOnlyList<Team>> CreateFallbackResult(string leagueName, string reason)
    {
        var fallback = GetFallbackTeams(leagueName);
        return fallback.Count > 0
            ? DataResult<IReadOnlyList<Team>>.Demo(fallback, $"Live standings unavailable; using fallback data. {reason}")
            : DataResult<IReadOnlyList<Team>>.Unavailable(reason);
    }
}