using System.Text.Json.Serialization;

namespace OddsAnalytics.Models;

public class OddsApiResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("sport_key")]
    public string SportKey { get; set; } = "";

    [JsonPropertyName("commence_time")]
    public DateTime CommenceTime { get; set; }

    [JsonPropertyName("home_team")]
    public string HomeTeam { get; set; } = "";

    [JsonPropertyName("away_team")]
    public string AwayTeam { get; set; } = "";

    [JsonPropertyName("bookmakers")]
    public List<Bookmaker> Bookmakers { get; set; } = new();
}

public class Bookmaker
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("markets")]
    public List<Market> Markets { get; set; } = new();
}

public class Market
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("outcomes")]
    public List<Outcome> Outcomes { get; set; } = new();
}

public class Outcome
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("price")]
    public double Price { get; set; }
}

public record ExtractedMatchOdds(
    string MatchId,
    string HomeTeam,
    string AwayTeam,
    DateTime Kickoff,
    string BookmakerName,
    double OddsHome,
    double OddsDraw,
    double OddsAway
);
