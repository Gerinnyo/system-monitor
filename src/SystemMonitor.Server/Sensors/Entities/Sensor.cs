using SystemMonitor.Server.Measurements.Entities;

namespace SystemMonitor.Server.Sensors.Entities;

public sealed class Sensor
{
    public string Id { get; set; } = default!;

    public required string IpAddress { get; set; }

    public required int Port { get; set; }

    public required int MeasurementPeriodMilliseconds { get; set; }

    public required ConnectionState ConnectionState { get; set; }

    public ICollection<Measurement> Measurements { get; set; } = default!;
}
