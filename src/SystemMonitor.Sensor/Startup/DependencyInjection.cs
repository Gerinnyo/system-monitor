using Serilog;
using System.Runtime.InteropServices;

namespace SystemMonitor.Sensor.Startup;

public static class DependencyInjection
{
    public static void RegisterLogger(this HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.Console()
            .WriteTo.File("logs/sensor-.log", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 10 * 1024 * 1024, retainedFileCountLimit: 7)
            .CreateLogger();

        builder.Services.AddSerilog();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            builder.Logging.AddEventLog();
        }
    }
}
