using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public class FootballPredictionStrategy : ILeagueAnalysisStrategy
{
    public SportCategory Sport => SportCategory.Football;

    public MatchProbabilities Analyze(Team home, Team away, LeagueConfig config)
    {
        double lambdaHome = (home.HomeGoalsScoredAvg / config.HomeGoalsAvg) *
                            (away.AwayGoalsConcededAvg / config.HomeGoalsAvg) *
                            config.HomeGoalsAvg;

        double lambdaAway = (away.AwayGoalsScoredAvg / config.AwayGoalsAvg) *
                            (home.HomeGoalsConcededAvg / config.AwayGoalsAvg) *
                            config.AwayGoalsAvg;

        // Fotboll använder 2.5 som standardlina för Över/Under
        return PoissonEngine.CalculateMatchProbabilities(lambdaHome, lambdaAway, 7, 2.5);
    }

    public double[,] GenerateScoreMatrix(Team home, Team away, LeagueConfig config, int maxGoals = 5)
    {
        double lambdaHome = (home.HomeGoalsScoredAvg / config.HomeGoalsAvg) * (away.AwayGoalsConcededAvg / config.HomeGoalsAvg) * config.HomeGoalsAvg;
        double lambdaAway = (away.AwayGoalsScoredAvg / config.AwayGoalsAvg) * (home.HomeGoalsConcededAvg / config.AwayGoalsAvg) * config.AwayGoalsAvg;
        return PoissonEngine.GenerateScoreMatrix(lambdaHome, lambdaAway, maxGoals);
    }
}