using System.Runtime.InteropServices;

namespace SystemMonitor.Sensor.Measurements.Collectors;

public interface IMeasurementCollector : IDisposable
{
    OSPlatform Platform { get; }

    public List<Measurement> Collect();
}
