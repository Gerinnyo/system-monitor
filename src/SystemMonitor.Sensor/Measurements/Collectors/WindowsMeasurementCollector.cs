using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SystemMonitor.Sensor.Measurements.Collectors;

#pragma warning disable CA1416 // Validate platform compatibility
public sealed class WindowsMeasurementCollector : IMeasurementCollector
{
    private readonly PerformanceCounter _cpuSensor = new("Processor", "% Processor Time", "_Total");
    private readonly PerformanceCounter _memorySensor = new("Memory", "% Committed Bytes In Use");

    public OSPlatform Platform { get; } = OSPlatform.Windows;

    public WindowsMeasurementCollector()
    {
        // First read is always 0 on Windows — discard it
        _cpuSensor.NextValue();
        _memorySensor.NextValue();
    }

    public List<Measurement> Collect()
    {
        DateTime timestamp = DateTime.UtcNow;
        double cpu = _cpuSensor.NextValue();
        double memory = _memorySensor.NextValue();

        return [
            new()
            {
                Timestamp = timestamp,
                MetricType = "CPU",
                Value = cpu,
                Unit = "%",
            },
             new()
            {
                Timestamp = timestamp,
                MetricType = "Memory",
                Value = memory,
                Unit = "%",
            },
        ];
    }

    public void Dispose()
    {
        _cpuSensor?.Dispose();
        _memorySensor?.Dispose();
    }
}
#pragma warning restore CA1416 // Validate platform compatibility
