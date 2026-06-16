namespace SystemMonitor.Shared.Sensors;

public sealed record SensorConfigurationChangedEvent
{
    public const string Type = nameof(SensorConfigurationChangedEvent);

    public required string Id { get; init; }

    public required int MeasurementPeriodMilliseconds { get; init; }
}
