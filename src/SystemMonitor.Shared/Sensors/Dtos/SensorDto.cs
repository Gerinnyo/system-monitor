using SystemMonitor.Shared.Measurements.Dtos;

namespace SystemMonitor.Shared.Sensors.Dtos;

public sealed record SensorDto
{
    public required string Id { get; init; }

    public required string IpAddress { get; init; }

    public required int Port { get; init; }

    public required int MeasurementPeriodMilliseconds { get; init; }

    public required string ConnectionState { get; init; }

    public List<MeasurementDto> Measurements { get; init; } = [];
}
