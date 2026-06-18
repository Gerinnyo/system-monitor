
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;
using SystemMonitor.Agent.Auth;
using SystemMonitor.Agent.Configurations;
using SystemMonitor.Agent.Measurements.Services;
using SystemMonitor.Agent.Persistence;
using SystemMonitor.Agent.Sensors.Services;
using SystemMonitor.Agent.Sockets.Sensor;
using SystemMonitor.Agent.Sockets.SensorsState;
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
               .WriteTo.File("logs/agent-.log",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: 10 * 1024 * 1024,
                    retainedFileCountLimit: 7));

        return webApplicationBuilder;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDatabaseContext>(x => x.UseSqlite(configuration.GetConnectionString(nameof(ApplicationDatabaseContext))));

        services.AddScoped<SensorService>();
        services.AddScoped<SensorsStateSocket>();
        services.AddScoped<SensorSocket>();
        services.AddScoped<MeasurementService>();
        services.AddScoped<SensorConnectionHandler>();
        services.AddScoped<Messenger>();
        services.AddSingleton<SensorConnectionService>();
        services.AddSingleton<JwtTokenProvider>();
        services.AddHostedService(sp => sp.GetRequiredService<SensorConnectionService>());

        services.Configure<TcpConfiguration>(configuration.GetSection(nameof(TcpConfiguration)));
        services.Configure<JwtConfiguration>(configuration.GetSection(nameof(JwtConfiguration)));

        return services;
    }

    public static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services.AddIdentity<IdentityUser, IdentityRole>(x =>
        {
            x.Password.RequiredLength = 5;
            x.Password.RequireDigit = false;
            x.Password.RequireNonAlphanumeric = false;
            x.Password.RequireUppercase = false;
            x.Password.RequireLowercase = false;
            x.Password.RequireUppercase = false;
        })
        .AddEntityFrameworkStores<ApplicationDatabaseContext>();
        //.AddApiEndpoints();

        return services;
    }

    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtConfiguration = configuration.GetSection(nameof(JwtConfiguration)).Get<JwtConfiguration>()!;

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x => x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfiguration.Issuer,
            ValidAudience = jwtConfiguration.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfiguration.SecretKey))
        });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(o => o.AddDefaultPolicy(x => x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
        return services;
    }

    public static IServiceCollection AddSwaggerWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(x =>
        {
            x.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "System Monitor",
                Version = "v1",
                Description = "An agent working with system monitoring sensors",
            });
            x.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Name = "Authentication",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT",
            });
            x.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = JwtBearerDefaults.AuthenticationScheme }}, [] },
            });

            var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            x.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });

        return services;
    }
}
