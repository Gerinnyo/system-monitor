namespace SystemMonitor.Shared.Measurements.Events;

public sealed record MeasurementCompletedEvent
{
    public const string Type = nameof(MeasurementCompletedEvent);

    public required string SensorId { get; init; }

    public required DateTime Timestamp { get; init; }

    public required string MetricType { get; init; }

    public required double Value { get; init; }

    public required string Unit { get; init; }
}
