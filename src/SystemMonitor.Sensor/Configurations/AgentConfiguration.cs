namespace SystemMonitor.Sensor.Configurations;

public sealed record AgentConfiguration
{
    public required string Host { get; init; }

    public required int Port { get; init; }
}
