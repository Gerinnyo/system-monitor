using System.Runtime.InteropServices;
using SystemMonitor.Sensor.Configurations;

namespace SystemMonitor.Sensor.Measurements.Collectors;

public sealed class LinuxMeasurementCollector(SensorConfiguration sensorConfiguration, ILogger<WindowsMeasurementCollector> logger) : IMeasurementCollector
{
    public OSPlatform Platform { get; } = OSPlatform.Linux;

    public List<Measurement> Collect()
    {
        try
        {

            DateTime timestamp = DateTime.UtcNow;

            return [
                new()
                {
                    Timestamp = timestamp,
                    MetricType = "CPU",
                    Value = MeasureCpu(),
                    Unit = "%",
                },
                 new()
                {
                    Timestamp = timestamp,
                    MetricType = "Memory",
                    Value = MeasureMemory(),
                    Unit = "KBytes",
                },
            ];
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to collect measurement of sensor {SensorId}", sensorConfiguration.SensorId);
        }

        return [];
    }

    private static double MeasureCpu()
    {
        var cpu = File.ReadAllLines("/proc/stat").First(x => x.StartsWith("cpu "));
        var cpuParts = cpu.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        long total = cpuParts.Take(4).Sum(long.Parse);
        long busy = cpuParts.Take(3).Sum(long.Parse);

        return 100 * busy / total;
    }

    private static double MeasureMemory()
    {
        var lines = File.ReadAllLines("/proc/meminfo");
        long total = long.Parse(lines.First(l => l.StartsWith("MemTotal:")).Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
        long available = long.Parse(lines.First(l => l.StartsWith("MemAvailable:")).Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);

        return 100.0 * (total - available) / total;
    }

    public void Dispose() { }
}
