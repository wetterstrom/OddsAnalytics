using System.Text.Json;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public class OddsApiService : IUpcomingMatchRepository
{
    private readonly HttpClient _http;

    public OddsApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<DataResult<IReadOnlyList<MatchOdds>>> GetUpcomingMatchesAsync(
        LeagueDefinition league,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(league.OddsApiSportKey))
            return DataResult<IReadOnlyList<MatchOdds>>.Unavailable($"No odds feed is configured for {league.DisplayName}.");

        try
        {
            var sportKey = Uri.EscapeDataString(league.OddsApiSportKey);
            var requestUri = $"odds/{sportKey}";
            Console.WriteLine($"[OddsApi] GET {_http.BaseAddress}{requestUri}");
            using var response = await _http.GetAsync(requestUri, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[OddsApi] HTTP {(int)response.StatusCode} {response.StatusCode}; response: {rawJson}");
            if (!response.IsSuccessStatusCode)
                return DataResult<IReadOnlyList<MatchOdds>>.Unavailable($"The odds feed returned HTTP {(int)response.StatusCode}.");

            var data = JsonSerializer.Deserialize<List<OddsApiResponse>>(rawJson);
            var matches = new List<MatchOdds>();

            foreach (var match in data ?? [])
            {
                var bookmaker = match.Bookmakers.FirstOrDefault();
                var market = bookmaker?.Markets.FirstOrDefault(item => item.Key == "h2h");
                if (bookmaker is null || market is null)
                    continue;

                var home = market.Outcomes.FirstOrDefault(item => item.Name.Equals(match.HomeTeam, StringComparison.OrdinalIgnoreCase));
                var away = market.Outcomes.FirstOrDefault(item => item.Name.Equals(match.AwayTeam, StringComparison.OrdinalIgnoreCase));
                var draw = market.Outcomes.FirstOrDefault(item =>
                    item.Name.Equals("Draw", StringComparison.OrdinalIgnoreCase)
                    || item.Name.Equals("X", StringComparison.OrdinalIgnoreCase));

                if (home is null || away is null || home.Price <= 1 || away.Price <= 1)
                    continue;

                matches.Add(new MatchOdds(
                    match.Id,
                    match.HomeTeam,
                    match.AwayTeam,
                    match.CommenceTime,
                    bookmaker.Title,
                    home.Price,
                    draw?.Price > 1 ? draw.Price : null,
                    away.Price));
            }

            Console.WriteLine($"[OddsApi] Parsed events: {data?.Count ?? 0}; usable matches: {matches.Count}");
            return DataResult<IReadOnlyList<MatchOdds>>.Live(matches);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[OddsApi] Request/deserialization failed for sportKey={league.OddsApiSportKey}: {exception}");
            return DataResult<IReadOnlyList<MatchOdds>>.Unavailable($"Odds could not be loaded: {exception.Message}");
        }
    }

    public async Task<List<ExtractedMatchOdds>> FetchUpcomingOddsAsync(string sportKey)
    {
        try
        {
            var url = $"odds/{Uri.EscapeDataString(sportKey)}";
            Console.WriteLine($"[OddsApi] GET {_http.BaseAddress}{url}");
            using var response = await _http.GetAsync(url);
            var rawJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[OddsApi] HTTP {(int)response.StatusCode} {response.StatusCode}; response: {rawJson}");
            response.EnsureSuccessStatusCode();
            var matches = JsonSerializer.Deserialize<List<OddsApiResponse>>(rawJson);

            if (matches == null || matches.Count == 0)
                return GetDemoMatches(sportKey);

            var result = new List<ExtractedMatchOdds>();

            foreach (var match in matches)
            {
                var bookmaker = match.Bookmakers.FirstOrDefault();
                if (bookmaker == null) continue;

                var h2h = bookmaker.Markets.FirstOrDefault(m => m.Key == "h2h");
                if (h2h == null) continue;

                var homeOutcome = h2h.Outcomes.FirstOrDefault(o => o.Name.Equals(match.HomeTeam, StringComparison.OrdinalIgnoreCase));
                var awayOutcome = h2h.Outcomes.FirstOrDefault(o => o.Name.Equals(match.AwayTeam, StringComparison.OrdinalIgnoreCase));
                var drawOutcome = h2h.Outcomes.FirstOrDefault(o => o.Name.Equals("Draw", StringComparison.OrdinalIgnoreCase) || o.Name.Equals("X", StringComparison.OrdinalIgnoreCase));

                if (homeOutcome != null && awayOutcome != null && drawOutcome != null)
                {
                    result.Add(new ExtractedMatchOdds(
                        MatchId: match.Id,
                        HomeTeam: match.HomeTeam,
                        AwayTeam: match.AwayTeam,
                        Kickoff: match.CommenceTime,
                        BookmakerName: bookmaker.Title,
                        OddsHome: homeOutcome.Price,
                        OddsDraw: drawOutcome.Price,
                        OddsAway: awayOutcome.Price
                    ));
                }
            }

            return result.Count > 0 ? result : GetDemoMatches(sportKey);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[OddsApi] Legacy odds request failed for sportKey={sportKey}: {exception}");
            return GetDemoMatches(sportKey);
        }
    }

    private List<ExtractedMatchOdds> GetDemoMatches(string sportKey)
    {
        var now = DateTime.UtcNow;
        return new()
        {
            new("demo-1", "Arsenal", "Chelsea", now.AddDays(1).AddHours(17), "Demo Bookmaker", 1.95, 3.60, 4.10),
            new("demo-2", "Manchester City", "Liverpool", now.AddDays(2).AddHours(16), "Demo Bookmaker", 2.10, 3.50, 3.40)
        };
    }
}
