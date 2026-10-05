using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OddsAnalytics;
using OddsAnalytics.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<IPredictionStrategy, FootballPredictionStrategy>();
builder.Services.AddScoped<IPredictionStrategy, HockeyPredictionStrategy>();
builder.Services.AddScoped<MatchAnalyzerService>();

await builder.Build().RunAsync();
