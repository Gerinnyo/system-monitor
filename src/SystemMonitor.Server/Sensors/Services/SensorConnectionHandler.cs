using System.Net.Sockets;
using System.Text.Json;
using SystemMonitor.Server.Measurements.Services;
using SystemMonitor.Server.Sensors.Requests;
using SystemMonitor.Shared.Measurements;
using SystemMonitor.Shared.Notifications;
using SystemMonitor.Shared.Sensors;

namespace SystemMonitor.Server.Sensors.Services;

public class SensorConnectionHandler(Messenger messenger, IServiceScopeFactory serviceScopeFactory, ILogger<SensorConnectionHandler> logger)
{
    public string SensorId { get; private set; } = string.Empty;

    public async Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var eventEnvelope = await messenger.ReceiveAsync(stream, cancellationToken);
                if (eventEnvelope is null)
                {
                    break;
                }

                await HandleEventAsync(client, eventEnvelope, cancellationToken).ConfigureAwait(false);
            }
        }
        // TODO Handle if sensor id is not initialized yet
        catch (Exception exception) when (exception is IOException or SocketException)
        {
            logger.LogInformation("Sensor {SensorId} disconnected", SensorId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error handling sensor {SensorId}", SensorId);
        }

        await DisconnectAsync(cancellationToken);
        client.Close();
    }

    public async Task NotifyConfigurationChangedAsync(TcpClient client, SensorConfigurationChangedEvent sensorConfigurationChangedEvent, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();
        var eventEnvelope = new EventEnvelope
        {
            EventType = SensorConfigurationChangedEvent.Type,
            Payload = JsonSerializer.Serialize(sensorConfigurationChangedEvent)
        };

        await messenger.SendAsync(stream, eventEnvelope, cancellationToken);
    }

    private async Task HandleEventAsync(TcpClient client, EventEnvelope eventEnvelope, CancellationToken cancellationToken)
    {
        switch (eventEnvelope.EventType)
        {
            case SensorConfigurationChangedEvent.Type:
                var sensorStartedEvent = JsonSerializer.Deserialize<SensorStartedEvent>(eventEnvelope.Payload)!;
                await HandleSensorStartedAsync(client, sensorStartedEvent, cancellationToken).ConfigureAwait(false);

                break;

            case SensorStartedEvent.Type:
                var measurementCompletedEvent = JsonSerializer.Deserialize<MeasurementCompletedEvent>(eventEnvelope.Payload)!;
                await HandleMeasurementCompletedAsync(measurementCompletedEvent, cancellationToken).ConfigureAwait(false);

                break;
        }
    }

    private async Task HandleSensorStartedAsync(TcpClient client, SensorStartedEvent sensorStartedEvent, CancellationToken cancellationToken)
    {
        SensorId = sensorStartedEvent.SensorId;
        var ipAddress = (client.Client.RemoteEndPoint as System.Net.IPEndPoint)!.Address.ToString();

        using var scope = serviceScopeFactory.CreateScope();
        var sensorService = scope.ServiceProvider.GetRequiredService<SensorService>();

        var joinSensorRequest = new JoinSensorRequest
        {
            HostName = sensorStartedEvent.Hostname,
            IpAddress = ipAddress,
        };
        await sensorService.JoinAsync(joinSensorRequest, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Sensor joined: {SensorId} ({Hostname})", sensorStartedEvent.SensorId, sensorStartedEvent.Hostname);
    }

    private async Task HandleMeasurementCompletedAsync(MeasurementCompletedEvent measurementCompletedEvent, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var measurementService = scope.ServiceProvider.GetRequiredService<MeasurementService>();

        await measurementService.StoreAsync(measurementCompletedEvent, cancellationToken).ConfigureAwait(false);
    }

    private async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var sensorService = scope.ServiceProvider.GetRequiredService<SensorService>();

        await sensorService.DisonnectAsync(SensorId, cancellationToken).ConfigureAwait(false);
    }
}
