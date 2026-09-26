using System.Text.Json.Serialization;

namespace OddsAnalytics.Models;

public class StandingsApiResponse
{
    [JsonPropertyName("response")]
    public List<LeagueResponse> Response { get; set; } = new();
}

public class LeagueResponse
{
    [JsonPropertyName("league")]
    public LeagueDetail League { get; set; } = new();
}

public class LeagueDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("standings")]
    public List<List<StandingItem>> Standings { get; set; } = new();
}

public class StandingItem
{
    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("team")]
    public TeamInfo Team { get; set; } = new();

    [JsonPropertyName("all")]
    public StatDetail All { get; set; } = new();

    [JsonPropertyName("home")]
    public StatDetail Home { get; set; } = new();

    [JsonPropertyName("away")]
    public StatDetail Away { get; set; } = new();
}

public class TeamInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

public class StatDetail
{
    [JsonPropertyName("played")]
    public int Played { get; set; }

    [JsonPropertyName("goals")]
    public GoalsDetail Goals { get; set; } = new();
}

public class GoalsDetail
{
    [JsonPropertyName("for")]
    public int For { get; set; }

    [JsonPropertyName("against")]
    public int Against { get; set; }
}
