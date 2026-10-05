namespace OddsAnalytics.Models;

public record MatchProbabilities(
    double HomeProb,
    double DrawProb,
    double AwayProb,
    double DoubleChance1XProb,
    double DoubleChanceX2Prob,
    double DnbHomeProb,
    double DnbAwayProb,
    double Over25Prob,
    double Under25Prob,
    double BttsProb,
    double FairOddsHome,
    double FairOddsDraw,
    double FairOddsAway,
    double FairOdds1X,
    double FairOddsX2,
    double FairOddsDnbHome,
    double FairOddsDnbAway,
    double FairOddsOver25,
    double ExpectedHomeGoals,
    double ExpectedAwayGoals
)
{
    public double OverUnderLine { get; init; } = 2.5;
    public double OverProbability => Over25Prob;
    public double UnderProbability => Under25Prob;
    public double FairOddsOver => FairOddsOver25;
}

public record ValueBetResult(
    string Outcome,
    double TrueProbability,
    double FairOdds,
    double BookmakerOdds,
    double ExpectedValue,
    double KellyStakePercent
);

public record Team(
    string Name,
    string League,
    double HomeGoalsScoredAvg,
    double HomeGoalsConcededAvg,
    double AwayGoalsScoredAvg,
    double AwayGoalsConcededAvg
);

public record ScannedValueBet(
    string MatchId,
    string HomeTeam,
    string AwayTeam,
    DateTime Kickoff,
    string Outcome,
    double TrueProbability,
    double FairOdds,
    double BookmakerOdds,
    string BookmakerName,
    double ExpectedValue,
    double KellyStakePercent,
    string LeagueKey = "",
    string LeagueName = ""
);
