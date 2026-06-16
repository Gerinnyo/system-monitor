
using Microsoft.EntityFrameworkCore;
using Serilog;
using SystemMonitor.Server.Measurements.Services;
using SystemMonitor.Server.Persistence;
using SystemMonitor.Server.Sensors.Services;
using SystemMonitor.Server.Startup;
using SystemMonitor.Shared.Notifications;

namespace SystemMonitor.Server.Startup;

public static class DependencyInjection
{
    public static WebApplicationBuilder RegisterLogger(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Host.UseSerilog((ctx, cfg) =>
            cfg.ReadFrom.Configuration(ctx.Configuration)
               .WriteTo.Console()
               .WriteTo.File("logs/server-.log", rollingInterval: RollingInterval.Day));

        return webApplicationBuilder;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDatabaseContext>(x => x.UseSqlite(configuration.GetConnectionString(nameof(ApplicationDatabaseContext))));
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<SensorService>();
        services.AddScoped<MeasurementService>();
        services.AddSingleton<ConnectionService>();
        services.AddScoped<SensorConnectionHandler>();
        services.AddScoped<Messenger>();
        services.AddHostedService(sp => sp.GetRequiredService<ConnectionService>());

        return services;
    }

    public static IServiceCollection AddCors(this IServiceCollection services, IConfiguration configuration)
    {
        const string AllowedOriginPath = "Cors:AllowedOrigin";
        services.AddCors(o => o.AddDefaultPolicy(x =>
            x.WithOrigins(configuration[AllowedOriginPath]!)
                .AllowAnyHeader()
                .AllowAnyMethod()));

        return services;
    }
}
