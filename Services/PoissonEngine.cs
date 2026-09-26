using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public static class PoissonEngine
{
    public static double PoissonProbability(int k, double lambda)
    {
        if (lambda <= 0) return k == 0 ? 1.0 : 0.0;
        return (Math.Pow(lambda, k) * Math.Exp(-lambda)) / Factorial(k);
    }

    private static double Factorial(int n)
    {
        if (n <= 1) return 1.0;
        double result = 1.0;
        for (int i = 2; i <= n; i++) result *= i;
        return result;
    }

    public static MatchProbabilities CalculateMatchProbabilities(double lambdaHome, double lambdaAway, int maxGoals = 7)
    {
        double homeWinProb = 0;
        double drawProb = 0;
        double awayWinProb = 0;
        double over25Prob = 0;
        double under25Prob = 0;
        double bttsProb = 0;

        for (int homeGoals = 0; homeGoals <= maxGoals; homeGoals++)
        {
            double pHome = PoissonProbability(homeGoals, lambdaHome);

            for (int awayGoals = 0; awayGoals <= maxGoals; awayGoals++)
            {
                double pAway = PoissonProbability(awayGoals, lambdaAway);
                double jointProb = pHome * pAway;

                if (homeGoals > awayGoals) homeWinProb += jointProb;
                else if (homeGoals == awayGoals) drawProb += jointProb;
                else awayWinProb += jointProb;

                if (homeGoals + awayGoals > 2.5) over25Prob += jointProb;
                else under25Prob += jointProb;

                if (homeGoals > 0 && awayGoals > 0) bttsProb += jointProb;
            }
        }

        double total1x2 = homeWinProb + drawProb + awayWinProb;
        if (total1x2 > 0)
        {
            homeWinProb /= total1x2;
            drawProb /= total1x2;
            awayWinProb /= total1x2;
        }

        // Dubbelchans
        double dc1X = Math.Min(1.0, homeWinProb + drawProb);
        double dcX2 = Math.Min(1.0, drawProb + awayWinProb);

        // Draw No Bet (villkorad sannolikhet vid avgjort resultat)
        double decidedTotal = homeWinProb + awayWinProb;
        double dnbHome = decidedTotal > 0 ? homeWinProb / decidedTotal : 0.5;
        double dnbAway = decidedTotal > 0 ? awayWinProb / decidedTotal : 0.5;

        return new MatchProbabilities(
            HomeProb: homeWinProb,
            DrawProb: drawProb,
            AwayProb: awayWinProb,
            DoubleChance1XProb: dc1X,
            DoubleChanceX2Prob: dcX2,
            DnbHomeProb: dnbHome,
            DnbAwayProb: dnbAway,
            Over25Prob: over25Prob,
            Under25Prob: under25Prob,
            BttsProb: bttsProb,
            FairOddsHome: homeWinProb > 0 ? Math.Round(1.0 / homeWinProb, 2) : 0,
            FairOddsDraw: drawProb > 0 ? Math.Round(1.0 / drawProb, 2) : 0,
            FairOddsAway: awayWinProb > 0 ? Math.Round(1.0 / awayWinProb, 2) : 0,
            FairOdds1X: dc1X > 0 ? Math.Round(1.0 / dc1X, 2) : 0,
            FairOddsX2: dcX2 > 0 ? Math.Round(1.0 / dcX2, 2) : 0,
            FairOddsDnbHome: dnbHome > 0 ? Math.Round(1.0 / dnbHome, 2) : 0,
            FairOddsDnbAway: dnbAway > 0 ? Math.Round(1.0 / dnbAway, 2) : 0,
            FairOddsOver25: over25Prob > 0 ? Math.Round(1.0 / over25Prob, 2) : 0,
            ExpectedHomeGoals: Math.Round(lambdaHome, 2),
            ExpectedAwayGoals: Math.Round(lambdaAway, 2)
        );
    }

    // Beräknar spelbolagens syntetiska odds för Dubbelchans från 1X2
    public static double CalculateSyntheticDoubleChanceOdds(double oddsA, double oddsB)
    {
        if (oddsA <= 1.0 || oddsB <= 1.0) return 1.0;
        return Math.Round((oddsA * oddsB) / (oddsA + oddsB), 2);
    }

    // Beräknar spelbolagens syntetiska odds för Draw No Bet (DNB) från 1X2
    public static double CalculateSyntheticDnbOdds(double oddsTarget, double oddsDraw)
    {
        if (oddsTarget <= 1.0 || oddsDraw <= 1.0) return oddsTarget;
        return Math.Round(oddsTarget * ((oddsDraw - 1.0) / oddsDraw), 2);
    }

    public static double[,] GenerateScoreMatrix(double lambdaHome, double lambdaAway, int maxGoals = 5)
    {
        var matrix = new double[maxGoals + 1, maxGoals + 1];
        for (int h = 0; h <= maxGoals; h++)
        {
            for (int a = 0; a <= maxGoals; a++)
            {
                matrix[h, a] = PoissonProbability(h, lambdaHome) * PoissonProbability(a, lambdaAway);
            }
        }
        return matrix;
    }

    public static ValueBetResult EvaluateBet(string outcome, double trueProb, double bookmakerOdds)
    {
        double ev = (trueProb * bookmakerOdds) - 1.0;
        double evPercent = Math.Round(ev * 100, 2);

        double b = bookmakerOdds - 1.0;
        double q = 1.0 - trueProb;
        double fullKelly = b > 0 ? (b * trueProb - q) / b : 0;
        double quarterKelly = Math.Max(0, (fullKelly * 0.25) * 100);

        return new ValueBetResult(
            Outcome: outcome,
            TrueProbability: Math.Round(trueProb * 100, 1),
            FairOdds: trueProb > 0 ? Math.Round(1.0 / trueProb, 2) : 0,
            BookmakerOdds: Math.Round(bookmakerOdds, 2),
            ExpectedValue: evPercent,
            KellyStakePercent: Math.Round(quarterKelly, 1)
        );
    }
}
