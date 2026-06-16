using System.Net;

namespace SystemMonitor.Agent.Configurations;

public sealed class TcpConfiguration
{
    public IPAddress IPAddress => IPAddress.Any;

    public required int Port { get; init; }
}
