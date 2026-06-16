using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SystemMonitor.Agent.Measurements.Entities;
using SystemMonitor.Agent.Sensors.Entities;

namespace SystemMonitor.Agent.Persistence;

public sealed class ApplicationDatabaseContext(DbContextOptions<ApplicationDatabaseContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public required DbSet<Sensor> Sensors { get; init; }

    public required DbSet<Measurement> Measurements { get; init; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Sensor>().HasIndex(x => new { x.IpAddress, x.Port, });
        builder.Entity<Measurement>().HasIndex(x => new { x.SensorId, x.Timestamp });
    }
}
