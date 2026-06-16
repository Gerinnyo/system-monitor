namespace SystemMonitor.Shared.Measurements.Dtos;

public sealed record MeasurementDto
{
    public required int Id { get; init; }

    public required string SensorId { get; init; }

    public required DateTime Timestamp { get; init; }

    public required string MetricType { get; init; }

    public required double Value { get; init; }

    public required string Unit { get; init; }
}
