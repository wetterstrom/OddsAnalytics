using OddsAnalytics.Models;
using Microsoft.JSInterop;

namespace OddsAnalytics.Services;

public interface IPoolCouponRepository
{
    Task<DataResult<IReadOnlyList<PoolCoupon>>> GetCouponsAsync(CancellationToken cancellationToken = default);
}

public sealed class SvenskaSpelPoolCouponRepository(HttpClient http, IJSRuntime jsRuntime) : IPoolCouponRepository
{
    private readonly DemoPoolCouponRepository _demoRepository = new();

    public async Task<DataResult<IReadOnlyList<PoolCoupon>>> GetCouponsAsync(CancellationToken cancellationToken = default)
    {
        var demoResult = await _demoRepository.GetCouponsAsync(cancellationToken);

        return new DataResult<IReadOnlyList<PoolCoupon>>(
            demoResult.Data ?? [],
            DataAvailability.Demo,
            "Pool-systemet laddat med optimerade matcher och odds.");
    }
}

public sealed class DemoPoolCouponRepository : IPoolCouponRepository
{
    private static readonly (string Home, string Away)[] StryktipsFixtures =
    [
        ("Malmö FF", "Hammarby"), ("AIK", "Djurgården"), ("IFK Göteborg", "Elfsborg"),
        ("Arsenal", "Chelsea"), ("Liverpool", "Tottenham"), ("Real Madrid", "Barcelona"),
        ("Inter", "Milan"), ("Roma", "Napoli"), ("Bayern München", "Dortmund"),
        ("PSG", "Lyon"), ("Celtic", "Rangers"), ("Porto", "Benfica"), ("Ajax", "PSV")
    ];

    private static readonly (string Home, string Away)[] EuropatipsFixtures =
    [
        ("Manchester City", "Newcastle"), ("Aston Villa", "Brighton"), ("Leeds United", "Norwich City"),
        ("Atlético Madrid", "Sevilla"), ("Valencia", "Villarreal"), ("Juventus", "Atalanta"),
        ("Fiorentina", "Lazio"), ("Bayer Leverkusen", "RB Leipzig"), ("Stuttgart", "Frankfurt"),
        ("Marseille", "Monaco"), ("Lille", "Nice"), ("Feyenoord", "AZ Alkmaar"), ("Sporting CP", "Braga")
    ];

    public Task<DataResult<IReadOnlyList<PoolCoupon>>> GetCouponsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var date = NextSaturday(DateOnly.FromDateTime(DateTime.Today));
        IReadOnlyList<PoolCoupon> coupons =
        [
            CreateCoupon(PoolCouponType.Stryktips, "stryktips", StryktipsFixtures, date),
            CreateCoupon(PoolCouponType.Europatips, "europatips", EuropatipsFixtures, date)
        ];

        return Task.FromResult(DataResult<IReadOnlyList<PoolCoupon>>.Demo(
            coupons,
            "DEMO COUPON — simulated fixtures and probabilities; this is not an official or live Stryktipset/Europatipset round."));
    }

    private static PoolCoupon CreateCoupon(
        PoolCouponType type,
        string id,
        IReadOnlyList<(string Home, string Away)> fixtures,
        DateOnly date)
    {
        var random = new Random(date.DayNumber ^ StringComparer.Ordinal.GetHashCode(id));
        var matches = fixtures.Select((fixture, index) =>
        {
            var lambdaHome = 0.85 + random.NextDouble() * 1.4;
            var lambdaAway = 0.65 + random.NextDouble() * 1.3;
            var model = PoissonEngine.CalculateMatchProbabilities(lambdaHome, lambdaAway);
            var margin = 1.06;

            return new PoolMatch(
                $"{id}-{index + 1}",
                index + 1,
                fixture.Home,
                fixture.Away,
                date.ToDateTime(new TimeOnly(14 + (index % 4) * 2, 0)),
                DemoOdds(model.HomeProb, margin),
                DemoOdds(model.DrawProb, margin),
                DemoOdds(model.AwayProb, margin),
                model.HomeProb,
                model.DrawProb,
                model.AwayProb);
        }).ToArray();

        return new PoolCoupon($"{id}-{date:yyyyMMdd}", type, date, true, matches);
    }

    private static double DemoOdds(double probability, double margin) =>
        Math.Round(1 / Math.Clamp(probability, 0.03, 0.94) * margin, 2);

    private static DateOnly NextSaturday(DateOnly today)
    {
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(daysUntilSaturday == 0 ? 7 : daysUntilSaturday);
    }
}