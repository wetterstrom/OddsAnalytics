using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OddsAnalytics;
using OddsAnalytics.Services;

namespace OddsAnalytics
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            var useDevelopmentMocks = builder.HostEnvironment.IsDevelopment();
            // Läs från appsettings, men fall tillbaka på din Cloudflare Worker om GitHub Pages missar json-filen
            var configuredWorkerUrl = builder.Configuration["Cloudflare:WorkerBaseUrl"];

            Uri workerBaseUri;

            if (!string.IsNullOrWhiteSpace(configuredWorkerUrl) &&
                Uri.TryCreate(configuredWorkerUrl, UriKind.Absolute, out var parsedWorkerUri))
            {
                workerBaseUri = parsedWorkerUri;
            }
            else
            {
                workerBaseUri = new Uri(
                    "https://football-proxy.robinwetterstrom.workers.dev/",
                    UriKind.Absolute);
            }

            builder.Services.AddScoped(_ => new HttpClient
            {
                BaseAddress = workerBaseUri
            });

            builder.Services.AddScoped<IDemoSportsDataFactory, DemoSportsDataFactory>();
            builder.Services.AddScoped<OddsApiService>();
            builder.Services.AddScoped<FootballStatsService>();
            builder.Services.AddScoped<IUpcomingMatchRepository>(services =>
                new DevelopmentFallbackMatchRepository(
                    new CachedUpcomingMatchRepository(services.GetRequiredService<OddsApiService>()),
                    services.GetRequiredService<IDemoSportsDataFactory>(),
                    useDevelopmentMocks));
            builder.Services.AddScoped<ILeagueStatisticsRepository>(services =>
                new DevelopmentFallbackStatisticsRepository(
                    new CachedLeagueStatisticsRepository(services.GetRequiredService<FootballStatsService>()),
                    services.GetRequiredService<IDemoSportsDataFactory>(),
                    useDevelopmentMocks));
            builder.Services.AddScoped<ILeagueAnalysisStrategy, FootballPredictionStrategy>();
            builder.Services.AddScoped<ILeagueAnalysisStrategy, HockeyPredictionStrategy>();
            builder.Services.AddScoped<MatchAnalyzerService>();
            builder.Services.AddScoped<DailyBestBetsViewModel>();
            builder.Services.AddScoped<IStryktipsSystemFactory, StryktipsSystemFactory>();
            builder.Services.AddScoped<IPoolCouponRepository, SvenskaSpelPoolCouponRepository>();
            builder.Services.AddScoped<PoolCouponViewModel>();

            await builder.Build().RunAsync();
        }
    }
}