using System;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OddsAnalytics;
using OddsAnalytics.Services;
using OddsAnalytics.Services.Strategies;
using OddsAnalytics.Models;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. Säker konfiguration av Worker-URL (kraschar aldrig om appsettings saknas)
var configuredUrl = builder.Configuration["Cloudflare:WorkerBaseUrl"];
const string defaultWorkerUrl = "https://football-proxy.robinwetterstrom.workers.dev/";

var validWorkerUrl = (!string.IsNullOrWhiteSpace(configuredUrl) &&
                      Uri.TryCreate(configuredUrl, UriKind.Absolute, out var parsedUri) &&
                      parsedUri.Scheme == Uri.UriSchemeHttps)
    ? configuredUrl
    : defaultWorkerUrl;

if (!validWorkerUrl.EndsWith("/"))
{
    validWorkerUrl += "/";
}

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(validWorkerUrl)
});

// 2. Registrera analysstrategier (Football & Hockey)
builder.Services.AddSingleton<OddsAnalytics.Services.Strategies.ILeagueAnalysisStrategy, FootballPredictionStrategy>();
builder.Services.AddSingleton<OddsAnalytics.Services.Strategies.ILeagueAnalysisStrategy, HockeyPredictionStrategy>();

// 3. Registrera domän- och applikationstjänster
builder.Services.AddSingleton<TeamDataService>();
builder.Services.AddSingleton<TeamNameNormalizer>();

// 4. Registrera repositories och ViewModels som Home.razor kräver
builder.Services.AddScoped<OddsAnalytics.Services.ILeagueStatisticsRepository, FootballStatsService>();
builder.Services.AddScoped<OddsAnalytics.Services.IUpcomingMatchRepository, OddsApiService>();
builder.Services.AddScoped<MatchAnalyzerService>();
builder.Services.AddScoped<DailyBestBetsViewModel>();

await builder.Build().RunAsync();