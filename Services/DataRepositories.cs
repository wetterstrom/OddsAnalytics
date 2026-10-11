using System.Collections.Concurrent;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public interface IUpcomingMatchRepository
{
    Task<DataResult<IReadOnlyList<MatchOdds>>> GetUpcomingMatchesAsync(
        LeagueDefinition league,
        CancellationToken cancellationToken = default);
}

public interface ILeagueStatisticsRepository
{
    Task<DataResult<IReadOnlyList<Team>>> GetTeamsAsync(
        LeagueDefinition league,
        CancellationToken cancellationToken = default);
}

public interface IDemoSportsDataFactory
{
    IReadOnlyList<MatchOdds> CreateMatches(LeagueDefinition league);
    IReadOnlyList<Team> CreateTeams(LeagueDefinition league);
}

public sealed class CachedUpcomingMatchRepository(IUpcomingMatchRepository inner) : IUpcomingMatchRepository
{
    private readonly AsyncResultCache<IReadOnlyList<MatchOdds>> _cache = new(TimeSpan.FromMinutes(3));

    public Task<DataResult<IReadOnlyList<MatchOdds>>> GetUpcomingMatchesAsync(
        LeagueDefinition league,
        CancellationToken cancellationToken = default) =>
        _cache.GetAsync(league.Key, () => inner.GetUpcomingMatchesAsync(league, cancellationToken), cancellationToken);
}

public sealed class CachedLeagueStatisticsRepository(ILeagueStatisticsRepository inner) : ILeagueStatisticsRepository
{
    private readonly AsyncResultCache<IReadOnlyList<Team>> _cache = new(TimeSpan.FromMinutes(30));

    public Task<DataResult<IReadOnlyList<Team>>> GetTeamsAsync(
        LeagueDefinition league,
        CancellationToken cancellationToken = default) =>
        _cache.GetAsync($"{league.Key}:{league.Season}", () => inner.GetTeamsAsync(league, cancellationToken), cancellationToken);
}

public sealed class DevelopmentFallbackMatchRepository(
    IUpcomingMatchRepository inner,
    IDemoSportsDataFactory demoData,
    bool useDevelopmentMocks) : IUpcomingMatchRepository
{
    public async Task<DataResult<IReadOnlyList<MatchOdds>>> GetUpcomingMatchesAsync(
        LeagueDefinition league,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetUpcomingMatchesAsync(league, cancellationToken);
        if (result.IsAvailable && result.Data is { Count: > 0 })
            return result;

        return useDevelopmentMocks
            ? DataResult<IReadOnlyList<MatchOdds>>.Demo(
                demoData.CreateMatches(league),
                $"DEMO DATA — no live odds feed is available for {league.DisplayName}.")
            : DataResult<IReadOnlyList<MatchOdds>>.Unavailable(
                result.Message ?? $"No live odds feed is available for {league.DisplayName}.");
    }
}

public sealed class DevelopmentFallbackStatisticsRepository(
    ILeagueStatisticsRepository inner,
    IDemoSportsDataFactory demoData,
    bool useDevelopmentMocks) : ILeagueStatisticsRepository
{
    public async Task<DataResult<IReadOnlyList<Team>>> GetTeamsAsync(
        LeagueDefinition league,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetTeamsAsync(league, cancellationToken);
        if (result.IsAvailable && result.Data is { Count: > 0 })
            return result;

        return useDevelopmentMocks
            ? DataResult<IReadOnlyList<Team>>.Demo(
                demoData.CreateTeams(league),
                $"DEMO DATA — no live standings feed is available for {league.DisplayName}.")
            : DataResult<IReadOnlyList<Team>>.Unavailable(
                result.Message ?? $"No live standings feed is available for {league.DisplayName}.");
    }
}

internal sealed class AsyncResultCache<T>(TimeSpan lifetime)
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<DataResult<T>> GetAsync(
        string key,
        Func<Task<DataResult<T>>> factory,
        CancellationToken cancellationToken)
    {
        if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
            return entry.Result;

        var gate = _locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (_entries.TryGetValue(key, out entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
                return entry.Result;

            var result = await factory();
            if (result.IsAvailable)
                _entries[key] = new CacheEntry(result, DateTimeOffset.UtcNow.Add(lifetime));
            else
                _entries.TryRemove(key, out _);

            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed record CacheEntry(DataResult<T> Result, DateTimeOffset ExpiresAt);
}
