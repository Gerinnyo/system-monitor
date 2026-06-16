namespace SystemMonitor.Shared.Sensors.Dtos;

public sealed record UpdateSensorConfigurationDto
{
    public required int MeasurementPeriodMilliseconds { get; set; }
}
