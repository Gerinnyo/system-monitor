using SystemMonitor.Agent.Sensors.Entities;

namespace SystemMonitor.Agent.Measurements.Entities;

public sealed class Measurement
{
    public int Id { get; set; }

    public required int SensorId { get; set; }

    public Sensor Sensor { get; set; } = default!;

    public required DateTime Timestamp { get; set; }

    public required MetricType MetricType { get; set; }

    public required double Value { get; set; }

    public required string Unit { get; set; }
}
