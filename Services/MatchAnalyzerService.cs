using System;
using System.Collections.Generic;
using System.Linq;
using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public class MatchAnalyzerService
{
    // Injectar automatiskt ALLA registrerade strategier från Program.cs
    private readonly IEnumerable<ILeagueAnalysisStrategy> _strategies;

    public MatchAnalyzerService(IEnumerable<ILeagueAnalysisStrategy> strategies)
    {
        _strategies = strategies;
    }

    public MatchProbabilities Analyze(Team home, Team away, LeagueConfig config)
    {
        var strategy = GetStrategy(config.Sport);
        return strategy.Analyze(home, away, config);
    }

    public double[,] GenerateScoreMatrix(Team home, Team away, LeagueConfig config, int maxGoals = 5)
    {
        var strategy = GetStrategy(config.Sport);
        // Öka maxmål för hockey-heatmaps
        int goals = config.Sport == SportCategory.IceHockey ? 7 : maxGoals;
        return strategy.GenerateScoreMatrix(home, away, config, goals);
    }

    private ILeagueAnalysisStrategy GetStrategy(SportCategory sport)
    {
        var strategy = _strategies.FirstOrDefault(s => s.Sport == sport);
        if (strategy == null)
        {
            throw new InvalidOperationException($"Ingen analysstrategi hittades för sporten: {sport}");
        }
        return strategy;
    }
}