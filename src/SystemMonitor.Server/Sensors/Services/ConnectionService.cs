using System.Collections.Concurrent;
using System.Net.Sockets;
using SystemMonitor.Server.Configurations;
using SystemMonitor.Shared.Sensors;

namespace SystemMonitor.Server.Sensors.Services;

public class ConnectionService(
    TcpConfiguration tcpConfiguration,
    IServiceScopeFactory scopeFactory,
    ILogger<ConnectionService> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<TcpClient, SensorConnectionHandler> _connectionHandlers = [];

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(tcpConfiguration.IPAddress, tcpConfiguration.Port);
        listener.Start();

        logger.LogInformation("TCP listener started on port {Port}", tcpConfiguration.Port);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                FireConnectionHandlerAsync(client, cancellationToken);
                logger.LogInformation("New sensor connected from {Remote}", client.Client.RemoteEndPoint);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error accepting TCP connection");
            }
        }

        listener.Stop();
        listener.Dispose();
    }

    private async void FireConnectionHandlerAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sensorConnectionHandler = scope.ServiceProvider.GetRequiredService<SensorConnectionHandler>();

        _connectionHandlers.TryAdd(client, sensorConnectionHandler);
        await sensorConnectionHandler.HandleConnectionAsync(client, cancellationToken).ConfigureAwait(false);

        _connectionHandlers.TryRemove(client, out _);
        client.Dispose();
    }

    public async Task NotifyConfigurationChangedAsync(SensorConfigurationChangedEvent sensorConfigurationChangedEventPayload, CancellationToken cancellationToken)
    {
        var connectionHandlerEntry = _connectionHandlers.FirstOrDefault(x => x.Value.SensorId == sensorConfigurationChangedEventPayload.Id);
        if (connectionHandlerEntry.Key is null)
        {
            return;
        }

        try
        {
            await connectionHandlerEntry.Value.NotifyConfigurationChangedAsync(connectionHandlerEntry.Key, sensorConfigurationChangedEventPayload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to push config to sensor {SensorId}", sensorConfigurationChangedEventPayload.Id);
        }
    }
}
