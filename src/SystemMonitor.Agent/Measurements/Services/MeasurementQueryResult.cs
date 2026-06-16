using SystemMonitor.Shared.Measurements.Dtos;

namespace SystemMonitor.Agent.Measurements.Services;

public sealed record MeasurementQueryResult
{
    public required int Total { get; init; }

    public required List<MeasurementDto> Measurements { get; init; } = default!;
}
