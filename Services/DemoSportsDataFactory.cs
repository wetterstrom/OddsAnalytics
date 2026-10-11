using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public sealed class DemoSportsDataFactory : IDemoSportsDataFactory
{
    private static readonly IReadOnlyDictionary<string, string[]> TeamsByLeague = new Dictionary<string, string[]>
    {
        ["epl"] = ["Arsenal", "Chelsea", "Liverpool", "Manchester City", "Newcastle", "Tottenham", "Aston Villa", "Brighton"],
        ["allsvenskan"] = ["Malmö FF", "AIK", "Djurgården", "Hammarby", "IFK Göteborg", "Elfsborg", "Häcken", "Sirius"],
        ["mls"] = ["Inter Miami", "LA Galaxy", "Seattle Sounders", "Atlanta United", "Austin FC", "New York City FC", "Orlando City", "Portland Timbers"],
        ["serie_b"] = ["Palermo", "Sampdoria", "Bari", "Spezia", "Parma", "Pisa", "Modena", "Cremonese"],
        ["division_1"] = ["Umeå FC", "Hammarby TFF", "IFK Stocksund", "Vasalunds IF", "Örebro Syrianska", "Piteå IF", "FC Stockholm", "Assyriska FF"],
        ["nhl"] = ["Boston Bruins", "New York Rangers", "Toronto Maple Leafs", "Edmonton Oilers", "Colorado Avalanche", "Florida Panthers", "Seattle Kraken", "Detroit Red Wings"],
        ["shl"] = ["Frölunda HC", "Luleå Hockey", "Färjestad BK", "Skellefteå AIK", "Växjö Lakers", "Rögle BK", "HV71", "Malmö Redhawks"],
        ["hockeyallsvenskan"] = ["Björklöven", "Södertälje SK", "Djurgårdens IF", "AIK", "Mora IK", "Kalmar HC", "Västerås IK", "Östersunds IK"]
    };

    public IReadOnlyList<Team> CreateTeams(LeagueDefinition league)
    {
        var names = GetTeamNames(league);
        var random = new Random(GetStableSeed(league.Key));
        var homeBase = league.Sport == SportCategory.IceHockey ? 3.15 : 1.55;
        var awayBase = league.Sport == SportCategory.IceHockey ? 2.75 : 1.25;

        return names.Select(name => new Team(
            name,
            league.DisplayName,
            Math.Round(Clamp(homeBase * (1 + NextGaussian(random) * 0.18), 0.3, homeBase * 1.8), 2),
            Math.Round(Clamp(awayBase * (1 + NextGaussian(random) * 0.18), 0.3, homeBase * 1.8), 2),
            Math.Round(Clamp(awayBase * (1 + NextGaussian(random) * 0.18), 0.3, awayBase * 1.8), 2),
            Math.Round(Clamp(homeBase * (1 + NextGaussian(random) * 0.18), 0.3, homeBase * 1.8), 2)
        )).ToArray();
    }

    public IReadOnlyList<MatchOdds> CreateMatches(LeagueDefinition league)
    {
        var teams = CreateTeams(league);
        var teamAverages = GetLeagueAverages(league.Sport);
        var fixtures = new List<MatchOdds>();
        var firstDate = DateTime.Today;

        for (var day = 0; day < 14; day++)
        {
            for (var matchOnDay = 0; matchOnDay < 2; matchOnDay++)
            {
                var homeIndex = (day * 2 + matchOnDay) % teams.Count;
                var awayIndex = (homeIndex + 3 + day % 3) % teams.Count;
                var home = teams[homeIndex];
                var away = teams[awayIndex];
                var lambdaHome = (home.HomeGoalsScoredAvg / teamAverages.Home)
                    * (away.AwayGoalsConcededAvg / teamAverages.Home)
                    * teamAverages.Home;
                var lambdaAway = (away.AwayGoalsScoredAvg / teamAverages.Away)
                    * (home.HomeGoalsConcededAvg / teamAverages.Away)
                    * teamAverages.Away;
                var probabilities = PoissonEngine.CalculateMatchProbabilities(
                    lambdaHome,
                    lambdaAway,
                    league.Sport == SportCategory.IceHockey ? 10 : 7,
                    league.Sport == SportCategory.IceHockey ? 5.5 : 2.5);

                var homeProbability = probabilities.HomeProb;
                var awayProbability = probabilities.AwayProb;
                double? drawProbability = probabilities.DrawProb;
                if (league.Sport == SportCategory.IceHockey)
                {
                    var totalGoals = probabilities.ExpectedHomeGoals + probabilities.ExpectedAwayGoals;
                    var overtimeHomeShare = totalGoals > 0 ? probabilities.ExpectedHomeGoals / totalGoals : 0.5;
                    homeProbability += probabilities.DrawProb * overtimeHomeShare;
                    awayProbability += probabilities.DrawProb * (1 - overtimeHomeShare);
                    drawProbability = null;
                }

                var kickoff = firstDate.AddDays(day).AddHours(matchOnDay == 0 ? 15 : 19);
                fixtures.Add(new MatchOdds(
                    $"demo-{league.Key}-{day}-{matchOnDay}",
                    home.Name,
                    away.Name,
                    kickoff,
                    "DEMO — simulated bookmaker",
                    ToDemoOdds(homeProbability),
                    drawProbability is double draw ? ToDemoOdds(draw) : null,
                    ToDemoOdds(awayProbability),
                    IsDemo: true));
            }
        }

        return fixtures;
    }

    private static string[] GetTeamNames(LeagueDefinition league) => TeamsByLeague.TryGetValue(league.Key, out var names)
        ? names
        : Enumerable.Range(1, 8).Select(index => $"{league.DisplayName} Demo {index}").ToArray();

    private static (double Home, double Away) GetLeagueAverages(SportCategory sport) =>
        sport == SportCategory.IceHockey ? (3.15, 2.75) : (1.55, 1.25);

    private static double ToDemoOdds(double probability) =>
        Math.Round(1 / Math.Clamp(probability, 0.03, 0.94) * 1.06, 2);

    private static double NextGaussian(Random random)
    {
        var u1 = 1 - random.NextDouble();
        var u2 = 1 - random.NextDouble();
        return Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }

    private static int GetStableSeed(string value) => value.Aggregate(17, (seed, character) => unchecked(seed * 31 + character));

    private static double Clamp(double value, double min, double max) => Math.Clamp(value, min, max);
}
