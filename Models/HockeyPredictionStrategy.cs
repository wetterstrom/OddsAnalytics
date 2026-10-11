using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public class HockeyPredictionStrategy : ILeagueAnalysisStrategy
{
    public SportCategory Sport => SportCategory.IceHockey;

    public MatchProbabilities Analyze(Team home, Team away, LeagueConfig config)
    {
        double lambdaHome = (home.HomeGoalsScoredAvg / config.HomeGoalsAvg) *
                            (away.AwayGoalsConcededAvg / config.HomeGoalsAvg) *
                            config.HomeGoalsAvg;

        double lambdaAway = (away.AwayGoalsScoredAvg / config.AwayGoalsAvg) *
                            (home.HomeGoalsConcededAvg / config.AwayGoalsAvg) *
                            config.AwayGoalsAvg;

        // Hockey använder högre maxtak och 5.5 som mållina
        var baseProbs = PoissonEngine.CalculateMatchProbabilities(lambdaHome, lambdaAway, 10, 5.5);

        // NHL (Moneyline) har inget kryss, så vi fördelar om krysset till 1 och 2 baserat på lagstyrka.
        bool isMoneylineOnly = config.Id.Equals("nhl", System.StringComparison.OrdinalIgnoreCase);

        if (isMoneylineOnly)
        {
            double totalGoals = baseProbs.ExpectedHomeGoals + baseProbs.ExpectedAwayGoals;
            double overtimeHomeShare = totalGoals > 0 ? baseProbs.ExpectedHomeGoals / totalGoals : 0.5;

            double newHomeProb = baseProbs.HomeProb + (baseProbs.DrawProb * overtimeHomeShare);
            double newAwayProb = baseProbs.AwayProb + (baseProbs.DrawProb * (1 - overtimeHomeShare));

            return baseProbs with
            {
                HomeProb = newHomeProb,
                DrawProb = 0,
                AwayProb = newAwayProb,
                FairOddsHome = newHomeProb > 0 ? System.Math.Round(1.0 / newHomeProb, 2) : 0,
                FairOddsDraw = 0,
                FairOddsAway = newAwayProb > 0 ? System.Math.Round(1.0 / newAwayProb, 2) : 0
            };
        }

        // För SHL och HockeyAllsvenskan (1X2) returnerar vi den vanliga
        return baseProbs;
    }

    public double[,] GenerateScoreMatrix(Team home, Team away, LeagueConfig config, int maxGoals = 7)
    {
        double lambdaHome = (home.HomeGoalsScoredAvg / config.HomeGoalsAvg) * (away.AwayGoalsConcededAvg / config.HomeGoalsAvg) * config.HomeGoalsAvg;
        double lambdaAway = (away.AwayGoalsScoredAvg / config.AwayGoalsAvg) * (home.HomeGoalsConcededAvg / config.AwayGoalsAvg) * config.AwayGoalsAvg;
        return PoissonEngine.GenerateScoreMatrix(lambdaHome, lambdaAway, maxGoals);
    }
}