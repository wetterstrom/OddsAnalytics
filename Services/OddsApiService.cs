using System.Net.Http.Json;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public class OddsApiService
{
    private readonly HttpClient _http;

    public OddsApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ExtractedMatchOdds>> FetchUpcomingOddsAsync(string sportKey)
    {
        try
        {
            var url = $"https://football-proxy.robinwetterstrom.workers.dev/odds/{Uri.EscapeDataString(sportKey)}";
            var matches = await _http.GetFromJsonAsync<List<OddsApiResponse>>(url);

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
        catch
        {
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
