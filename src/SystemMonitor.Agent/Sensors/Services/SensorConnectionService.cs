using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Sockets;
using SystemMonitor.Agent.Configurations;

namespace SystemMonitor.Agent.Sensors.Services;

public sealed class SensorConnectionService(
    IOptions<TcpConfiguration> tcpConfiguration,
    IServiceScopeFactory scopeFactory,
    ILogger<SensorConnectionService> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<int, SensorConnectionHandler> _connectionHandlers = [];

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var listener = new TcpListener(tcpConfiguration.Value.IPAddress, tcpConfiguration.Value.Port);
        listener.Start();

        logger.LogInformation("TCP listener started on port {Port}", tcpConfiguration.Value.Port);

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
        var remoteEndpoint = (client.Client.RemoteEndPoint as System.Net.IPEndPoint)!;
        string ipAddress = remoteEndpoint.Address.ToString();
        int port = remoteEndpoint.Port;

        using var scope = scopeFactory.CreateScope();
        var sensorService = scope.ServiceProvider.GetRequiredService<SensorService>();
        var sensorConnectionHandler = scope.ServiceProvider.GetRequiredService<SensorConnectionHandler>();

        try
        {
            var sensor = await sensorService.JoinAsync(ipAddress, port, cancellationToken).ConfigureAwait(false);
            var connectionContext = new SensorConnectionContext
            {
                Sensor = sensor,
                Client = client,
                IpAddress = (client.Client.RemoteEndPoint as System.Net.IPEndPoint)!.Address.ToString(),
                Port = (client.Client.RemoteEndPoint as System.Net.IPEndPoint)!.Port,
            };

            _connectionHandlers.TryAdd(sensor.Id, sensorConnectionHandler);
            sensorConnectionHandler.ConfigureContext(connectionContext);
            await NotifyConfigurationChangedAsync(sensor.Id, sensor.MeasurementPeriodMilliseconds, cancellationToken).ConfigureAwait(false);
            await sensorConnectionHandler.HandleSensorAsync(connectionContext, cancellationToken).ConfigureAwait(false);

            _connectionHandlers.TryRemove(sensor.Id, out _);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to establish connection to sensor {IpAddress}:{Port}", ipAddress, port);
        }

        client.Close();
        client.Dispose();
    }

    public async Task NotifyConfigurationChangedAsync(int id, int measurementPeriodMilliseconds, CancellationToken cancellationToken)
    {
        if (!_connectionHandlers.TryGetValue(id, out var connectionHandler))
        {
            return;
        }

        try
        {
            await connectionHandler.NotifyConfigurationChangedAsync(measurementPeriodMilliseconds, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to notify config sensor {SensorId}", id);
        }
    }
}
