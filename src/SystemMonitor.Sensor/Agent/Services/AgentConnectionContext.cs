using System.Net.Sockets;

namespace SystemMonitor.Sensor.Agent.Services;

public sealed record AgentConnectionContext
{
    public required TcpClient Client { get; init; }
}
