using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public interface ILeagueAnalysisStrategy
{
    SportCategory Sport { get; }
    MatchProbabilities Analyze(Team home, Team away, LeagueConfig config);
    double[,] GenerateScoreMatrix(Team home, Team away, LeagueConfig config, int maxGoals = 5);
}