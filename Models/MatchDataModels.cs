namespace OddsAnalytics.Models;

public enum DataAvailability
{
    Live,
    Demo,
    Unavailable
}

public sealed record DataResult<T>(
    T? Data,
    DataAvailability Availability,
    string? Message = null)
{
    public bool IsAvailable => Availability != DataAvailability.Unavailable;

    public static DataResult<T> Live(T data) => new(data, DataAvailability.Live);

    public static DataResult<T> Demo(T data, string message) => new(data, DataAvailability.Demo, message);

    public static DataResult<T> Unavailable(string message) => new(default, DataAvailability.Unavailable, message);
}

public sealed record MatchOdds(
    string Id,
    string HomeTeam,
    string AwayTeam,
    DateTime Kickoff,
    string BookmakerName,
    double OddsHome,
    double? OddsDraw,
    double OddsAway,
    bool IsDemo = false);
