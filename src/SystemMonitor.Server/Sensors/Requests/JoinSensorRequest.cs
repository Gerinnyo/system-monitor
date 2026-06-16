namespace SystemMonitor.Server.Sensors.Requests;

public sealed record JoinSensorRequest
{
    public int MeasurementPeriodMilliseconds => 1000;

    public required string HostName { get; init; }

    public required string IpAddress { get; init; }
}
