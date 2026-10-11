using System.ComponentModel;
using System.Runtime.CompilerServices;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public sealed class DailyBestBetsViewModel(
    IUpcomingMatchRepository matchRepository,
    ILeagueStatisticsRepository statisticsRepository,
    MatchAnalyzerService analyzer) : INotifyPropertyChanged, IDisposable
{
    private readonly SemaphoreSlim _leagueLoadLimit = new(4, 4);
    private CancellationTokenSource? _loadCancellation;
    private long _loadVersion;
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Today);
    private IReadOnlyList<ScannedValueBet> _bets = [];
    private bool _isLoading;
    private string _statusMessage = "Select a date to load matches.";
    private DataAvailability _availability = DataAvailability.Unavailable;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DateOnly SelectedDate
    {
        get => _selectedDate;
        set => SetField(ref _selectedDate, value);
    }

    public IReadOnlyList<ScannedValueBet> Bets
    {
        get => _bets;
        private set => SetField(ref _bets, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public DataAvailability Availability
    {
        get => _availability;
        private set => SetField(ref _availability, value);
    }

    public async Task LoadAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);
        _loadCancellation?.Cancel();
        var loadCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loadCancellation = loadCancellation;
        var token = loadCancellation.Token;
        SelectedDate = date;
        IsLoading = true;
        StatusMessage = "Loading match and league data…";

        try
        {
            var results = await Task.WhenAll(LeagueCatalog.AllLeagues.Select(league => LoadLeagueAsync(league, date, token)));
            token.ThrowIfCancellationRequested();

            Bets = results.SelectMany(result => result.Bets)
                .OrderByDescending(bet => bet.ExpectedValue)
                .ToArray();

            var demoLeagueCount = results.Count(result => result.IsDemo);
            var availableLeagueCount = results.Count(result => result.IsAvailable);
            Availability = demoLeagueCount > 0
                ? DataAvailability.Demo
                : availableLeagueCount > 0
                    ? DataAvailability.Live
                    : DataAvailability.Unavailable;

            StatusMessage = Availability switch
            {
                DataAvailability.Demo => $"DEMO DATA — simulated data used for {demoLeagueCount} league(s).",
                DataAvailability.Live => $"Live data loaded for {availableLeagueCount} league(s).",
                _ => "Data unavailable for this date. Check back-end feed coverage or choose another date."
            };
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_loadCancellation, loadCancellation))
            {
                IsLoading = false;
                _loadCancellation = null;
            }

            loadCancellation.Dispose();
        }
    }

    private async Task<LeagueLoadResult> LoadLeagueAsync(
        LeagueDefinition league,
        DateOnly date,
        CancellationToken token)
    {
        await _leagueLoadLimit.WaitAsync(token);
        try
        {
            var matchesTask = matchRepository.GetUpcomingMatchesAsync(league, token);
            var teamsTask = statisticsRepository.GetTeamsAsync(league, token);
            await Task.WhenAll(matchesTask, teamsTask);
            token.ThrowIfCancellationRequested();

            var matches = await matchesTask;
            var teams = await teamsTask;
            var isAvailable = matches.IsAvailable && teams.IsAvailable;
            var isDemo = matches.Availability == DataAvailability.Demo || teams.Availability == DataAvailability.Demo;

            if (!isAvailable || matches.Data is null || teams.Data is null || teams.Data.Count == 0)
                return new LeagueLoadResult([], isAvailable, isDemo);

            var bets = new List<ScannedValueBet>();
            foreach (var match in matches.Data.Where(match => DateOnly.FromDateTime(match.Kickoff.ToLocalTime()) == date))
            {
                var home = FindTeam(match.HomeTeam, teams.Data);
                var away = FindTeam(match.AwayTeam, teams.Data);
                if (home is null || away is null)
                    continue;

                var leagueConfig = new LeagueConfig(
                    league.Key,
                    league.DisplayName,
                    league.Sport,
                    teams.Data.Average(team => team.HomeGoalsScoredAvg),
                    teams.Data.Average(team => team.AwayGoalsScoredAvg));
                var probabilities = analyzer.Analyze(home, away, leagueConfig);

                AddBet(bets, match, $"1 ({match.HomeTeam})", probabilities.HomeProb, match.OddsHome, league);
                if (match.OddsDraw is double drawOdds && probabilities.DrawProb > 0)
                    AddBet(bets, match, "X (Draw)", probabilities.DrawProb, drawOdds, league);
                AddBet(bets, match, $"2 ({match.AwayTeam})", probabilities.AwayProb, match.OddsAway, league);
            }

            return new LeagueLoadResult(bets, isAvailable, isDemo);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return new LeagueLoadResult([], false, false);
        }
        finally
        {
            _leagueLoadLimit.Release();
        }
    }

    private static Team? FindTeam(string name, IReadOnlyList<Team> teams) =>
        teams.FirstOrDefault(team => team.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? teams.FirstOrDefault(team => team.Name.Contains(name, StringComparison.OrdinalIgnoreCase)
            || name.Contains(team.Name, StringComparison.OrdinalIgnoreCase));

    private static void AddBet(
        ICollection<ScannedValueBet> bets,
        MatchOdds match,
        string outcome,
        double probability,
        double bookmakerOdds,
        LeagueDefinition league)
    {
        if (probability <= 0 || bookmakerOdds <= 1)
            return;

        var result = PoissonEngine.EvaluateBet(outcome, probability, bookmakerOdds);
        if (result.ExpectedValue <= 0)
            return;

        bets.Add(new ScannedValueBet(
            match.Id,
            match.HomeTeam,
            match.AwayTeam,
            match.Kickoff,
            outcome,
            result.TrueProbability,
            result.FairOdds,
            result.BookmakerOdds,
            match.BookmakerName,
            result.ExpectedValue,
            result.KellyStakePercent,
            league.Key,
            league.DisplayName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public void Dispose()
    {
        _loadCancellation?.Cancel();
    }

    private sealed record LeagueLoadResult(IReadOnlyList<ScannedValueBet> Bets, bool IsAvailable, bool IsDemo);
}
