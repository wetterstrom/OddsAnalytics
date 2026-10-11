using OddsAnalytics.Models;

namespace OddsAnalytics.Services;

public interface IStryktipsSystemFactory
{
    CouponSystem Create(PoolCoupon coupon, CouponSystemTier tier);
}

public sealed class StryktipsSystemFactory : IStryktipsSystemFactory
{
    private static readonly MatchOutcome[] AllOutcomes =
    [
        MatchOutcome.Home,
        MatchOutcome.Draw,
        MatchOutcome.Away
    ];

    public CouponSystem Create(PoolCoupon coupon, CouponSystemTier tier)
    {
        ArgumentNullException.ThrowIfNull(coupon);
        if (coupon.Matches.Count != 13)
            throw new ArgumentException("A Swedish pool coupon must contain exactly 13 matches.", nameof(coupon));
        if (coupon.Matches.Select(match => match.MatchNumber).Distinct().Count() != 13
            || coupon.Matches.Select(match => match.Id).Distinct(StringComparer.Ordinal).Count() != 13)
            throw new ArgumentException("Coupon match numbers and identifiers must be unique.", nameof(coupon));

        var doubleCount = (int)Math.Log2((int)tier);
        if (tier is not (CouponSystemTier.Kr32 or CouponSystemTier.Kr64 or CouponSystemTier.Kr128)
            || 1 << doubleCount != (int)tier)
            throw new ArgumentOutOfRangeException(nameof(tier));

        var ranked = coupon.Matches
            .Select(match => CreateCandidate(match))
            .OrderByDescending(candidate => candidate.RankScore)
            .ThenBy(candidate => candidate.Match.MatchNumber)
            .Take(doubleCount)
            .Select(candidate => candidate.Match.Id)
            .ToHashSet(StringComparer.Ordinal);

        var selections = coupon.Matches
            .OrderBy(match => match.MatchNumber)
            .Select(match => CreateSelection(match, ranked.Contains(match.Id)))
            .ToArray();

        return new CouponSystem(
            coupon.Type,
            tier,
            (int)tier,
            (int)tier,
            coupon.IsDemo,
            selections);
    }

    private static Candidate CreateCandidate(PoolMatch match)
    {
        var probabilities = NormalizeProbabilities(match);
        var entropy = -probabilities
            .Where(probability => probability > 0)
            .Sum(probability => probability * Math.Log(probability)) / Math.Log(3);
        var bestExpectedValue = AllOutcomes
            .Select((outcome, index) => probabilities[index] * Odds(match, outcome) - 1)
            .Max();
        var valueScore = Math.Clamp(bestExpectedValue, 0, 1);
        var rankScore = (0.7 * entropy) + (0.3 * valueScore);
        return new Candidate(match, probabilities, entropy, valueScore, rankScore);
    }

    private static CouponMatchSelection CreateSelection(PoolMatch match, bool isDouble)
    {
        var probabilities = NormalizeProbabilities(match);
        var selected = AllOutcomes
            .OrderByDescending(outcome => Probability(match, outcome))
            .ThenBy(outcome => outcome)
            .Take(isDouble ? 2 : 1)
            .ToArray();

        var bestExpectedValue = selected.Max(outcome =>
            probabilities[(int)outcome] * Odds(match, outcome) - 1);
        var entropy = -probabilities
            .Where(probability => probability > 0)
            .Sum(probability => probability * Math.Log(probability)) / Math.Log(3);

        return new CouponMatchSelection(
            match.Id,
            match.MatchNumber,
            match.HomeTeam,
            match.AwayTeam,
            selected,
            entropy,
            Math.Max(0, bestExpectedValue));
    }

    private static double[] NormalizeProbabilities(PoolMatch match)
    {
        var probabilities = new[] { match.ProbabilityHome, match.ProbabilityDraw, match.ProbabilityAway };
        if (probabilities.Any(probability => !double.IsFinite(probability) || probability < 0))
            throw new ArgumentException($"Match {match.MatchNumber} has invalid probabilities.", nameof(match));

        var total = probabilities.Sum();
        if (total <= 0)
            throw new ArgumentException($"Match {match.MatchNumber} probabilities must sum to more than zero.", nameof(match));
        if (new[] { match.OddsHome, match.OddsDraw, match.OddsAway }
            .Any(odds => !double.IsFinite(odds) || odds <= 1))
            throw new ArgumentException($"Match {match.MatchNumber} has invalid odds.", nameof(match));

        return probabilities.Select(probability => probability / total).ToArray();
    }

    private static double Probability(PoolMatch match, MatchOutcome outcome) => outcome switch
    {
        MatchOutcome.Home => match.ProbabilityHome,
        MatchOutcome.Draw => match.ProbabilityDraw,
        MatchOutcome.Away => match.ProbabilityAway,
        _ => 0
    };

    private static double Odds(PoolMatch match, MatchOutcome outcome) => outcome switch
    {
        MatchOutcome.Home => match.OddsHome,
        MatchOutcome.Draw => match.OddsDraw,
        MatchOutcome.Away => match.OddsAway,
        _ => 0
    };

    private sealed record Candidate(PoolMatch Match, double[] Probabilities, double Entropy, double ValueScore, double RankScore);
}
