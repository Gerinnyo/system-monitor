using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SystemMonitor.Agent.Persistence;
using SystemMonitor.Agent.Sockets.Sensor;
using SystemMonitor.Agent.Sockets.SensorsState;

namespace SystemMonitor.Agent.Startup;

public static class StartupExtensions
{
    public static void UsePersistence(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var pendingMigrations = dbContext.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            dbContext.Database.Migrate();
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        userManager.SeedDatabase();
    }

    private static void SeedDatabase(this UserManager<IdentityUser> userManager)
    {
        const string AdminUserName = "admin";
        const string AdminPassword = "admin";

        bool adminExists = userManager.FindByNameAsync(AdminUserName).GetAwaiter().GetResult() is not null;
        if (adminExists)
        {
            return;
        }

        var admin = new IdentityUser { UserName = AdminUserName, };
        userManager.CreateAsync(admin, AdminPassword).GetAwaiter().GetResult();
    }

    public static WebApplication MapIdentityEndpoints(this WebApplication app)
    {
        //app.MapIdentityApi<IdentityUser>();
        return app;
    }

    public static WebApplication MapSocketEndpoints(this WebApplication app)
    {
        app.MapSensorsStateSocket();
        app.MapSensorSocket();

        return app;
    }
}
