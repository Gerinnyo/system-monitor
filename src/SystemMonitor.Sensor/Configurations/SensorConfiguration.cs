namespace SystemMonitor.Sensor.Configurations;

public sealed record SensorConfiguration
{
    public int SensorId { get; set; }

    public int MeasurementPeriodMilliseconds { get; set; }
}
