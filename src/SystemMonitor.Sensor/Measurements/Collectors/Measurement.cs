namespace SystemMonitor.Sensor.Measurements.Collectors;

public sealed record Measurement
{
    public required DateTime Timestamp { get; init; }

    public required string MetricType { get; init; }

    public required double Value { get; init; }

    public required string Unit { get; init; }
}
