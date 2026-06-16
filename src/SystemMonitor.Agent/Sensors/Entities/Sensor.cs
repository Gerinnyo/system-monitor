using SystemMonitor.Agent.Measurements.Entities;

namespace SystemMonitor.Agent.Sensors.Entities;

public sealed class Sensor
{
    public int Id { get; set; }

    public required string IpAddress { get; set; }

    public required int Port { get; set; }

    public required int MeasurementPeriodMilliseconds { get; set; }

    public required ConnectionState ConnectionState { get; set; }

    public ICollection<Measurement> Measurements { get; set; } = default!;
}
