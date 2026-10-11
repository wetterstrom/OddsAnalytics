using System;
using System.Collections.Generic;
using System.Linq;
using OddsAnalytics.Models;
using OddsAnalytics.Services;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace OddsAnalytics.Models;

public enum PoolCouponType
{
    Stryktips,
    Europatips
}

public enum MatchOutcome
{
    Home,
    Draw,
    Away
}

public enum CouponSystemTier
{
    Kr32 = 32,
    Kr64 = 64,
    Kr128 = 128
}

public sealed record PoolMatch(
    string Id,
    int MatchNumber,
    string HomeTeam,
    string AwayTeam,
    DateTime Kickoff,
    double OddsHome,
    double OddsDraw,
    double OddsAway,
    double ProbabilityHome,
    double ProbabilityDraw,
    double ProbabilityAway);

public sealed record PoolCoupon(
    string Id,
    PoolCouponType Type,
    DateOnly MatchDate,
    bool IsDemo,
    IReadOnlyList<PoolMatch> Matches);

public sealed record CouponMatchSelection(
    string MatchId,
    int MatchNumber,
    string HomeTeam,
    string AwayTeam,
    IReadOnlyList<MatchOutcome> Outcomes,
    double UncertaintyScore,
    double ExpectedValueScore);

public sealed record CouponSystem(
    PoolCouponType CouponType,
    CouponSystemTier Tier,
    int RowCount,
    int CostKr,
    bool IsDemo,
    IReadOnlyList<CouponMatchSelection> Selections);
