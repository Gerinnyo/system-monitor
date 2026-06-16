using SystemMonitor.Server.Sensors.Entities;

namespace SystemMonitor.Server.Measurements.Entities;

public sealed class Measurement
{
    public int Id { get; set; }

    public required Guid SensorId { get; set; }

    public Sensor Sensor { get; set; } = default!;

    public required DateTime Timestamp { get; set; }

    public required MetricType MetricType { get; set; }

    public required double Value { get; set; }

    public required string Unit { get; set; }
}
