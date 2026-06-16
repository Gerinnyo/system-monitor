namespace SystemMonitor.Shared.Measurements.Dtos;

public sealed record MeasurementsPaginatedDto
{
    public required int PageNumber { get; init; }

    public required int PageSize { get; init; }

    public required int Total { get; init; }

    public required List<MeasurementDto> Measurements { get; init; } = default!;
}
