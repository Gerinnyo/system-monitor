
using Microsoft.EntityFrameworkCore;
using Serilog;
using SystemMonitor.Agent.Configurations;
using SystemMonitor.Agent.Measurements.Services;
using SystemMonitor.Agent.Persistence;
using SystemMonitor.Agent.Sensors.Services;
using SystemMonitor.Agent.Startup;
using SystemMonitor.Shared.Notifications;

namespace SystemMonitor.Agent.Startup;

public static class DependencyInjection
{
    public static WebApplicationBuilder RegisterLogger(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Host.UseSerilog((ctx, cfg) =>
            cfg.ReadFrom.Configuration(ctx.Configuration)
               .WriteTo.Console()
               .WriteTo.File("logs/agent-.log", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 10 * 1024 * 1024, retainedFileCountLimit: 7));

        return webApplicationBuilder;
    }

    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDatabaseContext>(x => x.UseSqlite(configuration.GetConnectionString(nameof(ApplicationDatabaseContext))));
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<SensorService>();
        services.AddScoped<MeasurementService>();
        services.AddSingleton<SensorConnectionService>();
        services.AddScoped<SensorConnectionHandler>();
        services.AddScoped<Messenger>();
        services.AddHostedService(sp => sp.GetRequiredService<SensorConnectionService>());

        services.Configure<TcpConfiguration>(configuration.GetSection(nameof(TcpConfiguration)));

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
