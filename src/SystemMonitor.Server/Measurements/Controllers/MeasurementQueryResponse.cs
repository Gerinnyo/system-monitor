using SystemMonitor.Server.Measurements.Entities;

namespace SystemMonitor.Server.Measurements.Controllers;

public sealed record MeasurementQueryResponse
{
    public required int PageNumber { get; init; }

    public required int PageSize { get; init; }

    public required int Total { get; init; }

    public required List<Measurement> Measurements { get; init; } = default!;
}
