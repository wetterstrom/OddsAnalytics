using System;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services.Strategies;

public interface ILeagueAnalysisStrategy
{
    SportCategory Sport { get; }
    MatchProbabilities Analyze(Team home, Team away, LeagueConfig config);
    double[,] GenerateScoreMatrix(Team home, Team away, LeagueConfig config, int maxGoals = 7);
}

public class FootballPredictionStrategy : ILeagueAnalysisStrategy
{
    public SportCategory Sport => SportCategory.Football;

    public MatchProbabilities Analyze(Team home, Team away, LeagueConfig config)
    {
        var (lambdaHome, lambdaAway) = CalculateLambdas(home, away, config);
        return PoissonEngine.CalculateMatchProbabilities(lambdaHome, lambdaAway, maxGoals: 8);
    }

    public double[,] GenerateScoreMatrix(Team home, Team away, LeagueConfig config, int maxGoals = 7)
    {
        var (lambdaHome, lambdaAway) = CalculateLambdas(home, away, config);
        return PoissonEngine.GenerateScoreMatrix(lambdaHome, lambdaAway, maxGoals);
    }

    private static (double Home, double Away) CalculateLambdas(Team home, Team away, LeagueConfig config)
    {
        double homeAvg = Math.Max(0.5, config.HomeGoalsAvg);
        double awayAvg = Math.Max(0.5, config.AwayGoalsAvg);

        double lambdaHome = (home.HomeGoalsScoredAvg / homeAvg) * (away.AwayGoalsConcededAvg / homeAvg) * homeAvg;
        double lambdaAway = (away.AwayGoalsScoredAvg / awayAvg) * (home.HomeGoalsConcededAvg / awayAvg) * awayAvg;

        return (Math.Clamp(lambdaHome, 0.2, 5.0), Math.Clamp(lambdaAway, 0.2, 5.0));
    }
}

public class HockeyPredictionStrategy : ILeagueAnalysisStrategy
{
    public SportCategory Sport => SportCategory.IceHockey;

    public MatchProbabilities Analyze(Team home, Team away, LeagueConfig config)
    {
        var (lambdaHome, lambdaAway) = CalculateLambdas(home, away, config);
        var baseProbs = PoissonEngine.CalculateMatchProbabilities(lambdaHome, lambdaAway, maxGoals: 12);

        bool isMoneylineOnly = config.Id.Equals("nhl", StringComparison.OrdinalIgnoreCase);

        if (isMoneylineOnly)
        {
            double totalGoals = baseProbs.ExpectedHomeGoals + baseProbs.ExpectedAwayGoals;
            double overtimeHomeShare = totalGoals > 0 ? baseProbs.ExpectedHomeGoals / totalGoals : 0.5;

            double newHomeProb = baseProbs.HomeProb + (baseProbs.DrawProb * overtimeHomeShare);
            double newAwayProb = baseProbs.AwayProb + (baseProbs.DrawProb * (1.0 - overtimeHomeShare));

            return baseProbs with
            {
                HomeProb = newHomeProb,
                DrawProb = 0,
                AwayProb = newAwayProb,
                FairOddsHome = newHomeProb > 0 ? Math.Round(1.0 / newHomeProb, 2) : 0,
                FairOddsDraw = 0,
                FairOddsAway = newAwayProb > 0 ? Math.Round(1.0 / newAwayProb, 2) : 0
            };
        }

        return baseProbs;
    }

    public double[,] GenerateScoreMatrix(Team home, Team away, LeagueConfig config, int maxGoals = 7)
    {
        var (lambdaHome, lambdaAway) = CalculateLambdas(home, away, config);
        return PoissonEngine.GenerateScoreMatrix(lambdaHome, lambdaAway, maxGoals);
    }

    private static (double Home, double Away) CalculateLambdas(Team home, Team away, LeagueConfig config)
    {
        double homeAvg = Math.Max(1.5, config.HomeGoalsAvg);
        double awayAvg = Math.Max(1.5, config.AwayGoalsAvg);

        double lambdaHome = (home.HomeGoalsScoredAvg / homeAvg) * (away.AwayGoalsConcededAvg / homeAvg) * homeAvg;
        double lambdaAway = (away.AwayGoalsScoredAvg / awayAvg) * (home.HomeGoalsConcededAvg / awayAvg) * awayAvg;

        return (Math.Clamp(lambdaHome, 0.8, 6.5), Math.Clamp(lambdaAway, 0.8, 6.5));
    }
}