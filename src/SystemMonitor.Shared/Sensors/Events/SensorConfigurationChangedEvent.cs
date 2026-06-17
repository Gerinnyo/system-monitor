namespace SystemMonitor.Shared.Sensors.Events;

public sealed record SensorConfigurationChangedEvent
{
    public const string Type = nameof(SensorConfigurationChangedEvent);

    public required int SensorId { get; init; }

    public required int MeasurementPeriodMilliseconds { get; init; }
}
