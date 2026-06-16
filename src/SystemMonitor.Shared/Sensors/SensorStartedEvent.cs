namespace SystemMonitor.Shared.Sensors;

public sealed record SensorStartedEvent
{
    public const string Type = nameof(SensorStartedEvent);

    public required string SensorId { get; init; }

    public required string Hostname { get; init; }
}
