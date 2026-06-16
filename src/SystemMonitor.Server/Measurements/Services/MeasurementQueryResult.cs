using SystemMonitor.Server.Measurements.Entities;

namespace SystemMonitor.Server.Measurements.Services;

public sealed record MeasurementQueryResult
{
    public required int Total { get; init; }

    public required List<Measurement> Measurements { get; init; } = default!;
}
