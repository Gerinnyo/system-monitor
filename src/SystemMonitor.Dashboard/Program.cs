using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SystemMonitor.Dashboard;
using SystemMonitor.Dashboard.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var agentUrl = builder.Configuration["AgentUrl"] ?? "http://localhost:5000";

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(agentUrl.TrimEnd('/') + "/")
});

builder.Services.AddSingleton(new AgentOptions(agentUrl.TrimEnd('/')));

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<SensorsHubService>();
builder.Services.AddScoped<SensorApiService>();
builder.Services.AddScoped<MeasurementApiService>();
builder.Services.AddScoped<ThemeService>();

await builder.Build().RunAsync();
