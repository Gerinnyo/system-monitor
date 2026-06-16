namespace SystemMonitor.Server.Sensors.Requests;

public sealed record UpdateSensorConfigurationRequest
{
    public required int MeasurementPeriodMilliseconds { get; set; }
}
