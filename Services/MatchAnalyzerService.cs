using System;
using System.Collections.Generic;
using System.Linq;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public class MatchAnalyzerService
{
    private readonly IEnumerable<OddsAnalytics.Services.Strategies.ILeagueAnalysisStrategy> _strategies;

    public MatchAnalyzerService(IEnumerable<OddsAnalytics.Services.Strategies.ILeagueAnalysisStrategy> strategies)
    {
        _strategies = strategies;
    }

    public MatchProbabilities Analyze(Team home, Team away, LeagueConfig config) =>
        GetStrategy(config.Sport).Analyze(home, away, config);

    public double[,] GenerateScoreMatrix(Team home, Team away, LeagueConfig config, int maxGoals = 5) =>
        GetStrategy(config.Sport).GenerateScoreMatrix(home, away, config, maxGoals);

    private OddsAnalytics.Services.Strategies.ILeagueAnalysisStrategy GetStrategy(SportCategory sport)
    {
        var strategy = _strategies.FirstOrDefault(s => s.Sport == sport);

        if (strategy == null)
        {
            throw new InvalidOperationException($"Ingen analysstrategi hittades för sporten: {sport}");
        }

        return strategy;
    }
}