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

            builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped<FootballStatsService>();
            builder.Services.AddScoped<OddsApiService>();
            builder.Services.AddScoped<IPredictionStrategy, FootballPredictionStrategy>();
            builder.Services.AddScoped<IPredictionStrategy, HockeyPredictionStrategy>();
            builder.Services.AddScoped<MatchAnalyzerService>();

            await builder.Build().RunAsync();
        }
    }
}
