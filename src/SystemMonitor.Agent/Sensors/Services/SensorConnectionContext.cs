using System.Net.Sockets;
using SystemMonitor.Agent.Sensors.Entities;

namespace SystemMonitor.Agent.Sensors.Services;

public sealed record SensorConnectionContext
{
    public required Sensor Sensor { get; init; }

    public required TcpClient Client { get; init; }

    public required string IpAddress { get; init; }

    public required int Port { get; init; }
}
