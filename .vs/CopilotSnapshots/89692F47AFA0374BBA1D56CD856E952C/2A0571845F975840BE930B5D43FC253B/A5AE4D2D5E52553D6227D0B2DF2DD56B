using System;
using System.Collections.Generic;
using System.Linq;
using OddsAnalytics.Models; // Förutsätter att dina records som Team och MatchProbabilities ligger här[cite: 12]

namespace OddsAnalytics.Services.Predictions
{
    // 1. Value Object för Ligan - Ersätter de hårdkodade konstanterna i TeamDataService
    public sealed record LeagueConfig(
        string Id,
        string Name,
        SportCategory Sport,
        double HomeGoalsAvg,
        double AwayGoalsAvg);

    public enum SportCategory { Football, IceHockey }

    // 2. Gränssnittet (Dependency Inversion & Strategy)
    public interface IPredictionStrategy
    {
        SportCategory Sport { get; }
        MatchProbabilities Calculate(Team home, Team away, LeagueConfig leagueConfig);
    }

    // 3. Konkret strategi för Ishockey
    public class HockeyPredictionStrategy : IPredictionStrategy
    {
        public SportCategory Sport => SportCategory.IceHockey;

        public MatchProbabilities Calculate(Team home, Team away, LeagueConfig config)
        {
            // Defensiv programmering: Undvik division med noll om datan är korrupt
            if (config.HomeGoalsAvg <= 0 || config.AwayGoalsAvg <= 0)
                throw new ArgumentException("Ogiltigt ligasnitt för hockey.");

            // Beräknar Expected Goals (xG) - Flyttat hit från TeamDataService
            double homeAttack = (home.HomeGoalsScoredAvg / config.HomeGoalsAvg);
            double homeDefense = (home.HomeGoalsConcededAvg / config.AwayGoalsAvg);
            double lambdaHome = homeAttack * homeDefense * config.HomeGoalsAvg;

            double awayAttack = (away.AwayGoalsScoredAvg / config.AwayGoalsAvg);
            double awayDefense = (away.AwayGoalsConcededAvg / config.HomeGoalsAvg);
            double lambdaAway = awayAttack * homeDefense * config.AwayGoalsAvg;

            // Kallar på den existerande Poisson-motorn (som du byggt) men med anpassningar.
            // För hockey sätter vi maxGoals till 10 istället för 7 eller 5[cite: 9, 10]
            var matrix = GenerateHockeyMatrix(lambdaHome, lambdaAway, 10);

            return ExtractProbabilities(matrix);
        }

        private double[,] GenerateHockeyMatrix(double lambdaHome, double lambdaAway, int maxGoals)
        {
            var matrix = new double[maxGoals + 1, maxGoals + 1];
            for (int h = 0; h <= maxGoals; h++)
            {
                for (int a = 0; a <= maxGoals; a++)
                {
                    // Använder din befintliga statiska metod
                    matrix[h, a] = PoissonEngine.PoissonProbability(h, lambdaHome) *
                                   PoissonEngine.PoissonProbability(a, lambdaAway);
                }
            }
            return matrix;
        }

        private MatchProbabilities ExtractProbabilities(double[,] matrix)
        {
            // Förenklad extrahering för exemplet.
            // Här anpassar du logiken: t.ex. beräknas Över 5.5 mål istället för Över 2.5[cite: 10]
            double homeWin = 0, draw = 0, awayWin = 0, over55 = 0;
            int maxGoals = matrix.GetLength(0) - 1;

            for (int home = 0; home <= maxGoals; home++)
            {
                for (int away = 0; away <= maxGoals; away++)
                {
                    double prob = matrix[home, away];
                    if (home > away) homeWin += prob;
                    else if (home == away) draw += prob;
                    else awayWin += prob;

                    if ((home + away) > 5.5) over55 += prob;
                }
            }

            // Returnera ditt befintliga record (du kan behöva uppdatera namnet på egenskapen till OverUnderLineProb i framtiden)
            return new MatchProbabilities(
                HomeProb: Math.Round(homeWin, 4),
                DrawProb: Math.Round(draw, 4),
                AwayProb: Math.Round(awayWin, 4),
                // Fyll i resten baserat på din modell i BetModels.cs[cite: 12]
                DoubleChance1XProb: 0, DoubleChanceX2Prob: 0, DnbHomeProb: 0, DnbAwayProb: 0,
                Over25Prob: Math.Round(over55, 4), // Används temporärt för över 5.5
                Under25Prob: Math.Round(1 - over55, 4), BttsProb: 0, FairOddsHome: 0, FairOddsDraw: 0,
                FairOddsAway: 0, FairOdds1X: 0, FairOddsX2: 0, FairOddsDnbHome: 0, FairOddsDnbAway: 0,
                FairOddsOver25: 0, ExpectedHomeGoals: 0, ExpectedAwayGoals: 0
            );
        }
    }

    // 4. Analysmotorn som orkestrerar rätt strategi
    public class MatchAnalyzerService
    {
        private readonly IEnumerable<IPredictionStrategy> _strategies;

        // DI injicerar alla registrerade strategier automatiskt
        public MatchAnalyzerService(IEnumerable<IPredictionStrategy> strategies)
        {
            _strategies = strategies;
        }

        public MatchProbabilities Analyze(Team home, Team away, LeagueConfig config)
        {
            var strategy = _strategies.FirstOrDefault(s => s.Sport == config.Sport);

            if (strategy == null)
                throw new NotSupportedException($"Sporten {config.Sport} stöds inte av analysmotorn.");

            return strategy.Calculate(home, away, config);
        }
    }
}