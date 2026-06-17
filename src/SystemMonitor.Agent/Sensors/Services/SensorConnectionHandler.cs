using System.Net.Sockets;
using System.Text.Json;
using SystemMonitor.Agent.Measurements.Entities;
using SystemMonitor.Agent.Measurements.Services;
using SystemMonitor.Shared.Measurements.Events;
using SystemMonitor.Shared.Notifications;
using SystemMonitor.Shared.Sensors.Events;

namespace SystemMonitor.Agent.Sensors.Services;

public sealed class SensorConnectionHandler(Messenger messenger, IServiceScopeFactory serviceScopeFactory, ILogger<SensorConnectionHandler> logger)
{
    private SensorConnectionContext connectionContext = default!;

    public void ConfigureContext(SensorConnectionContext connectionContext)
    {
        this.connectionContext = connectionContext;
    }

    public async Task HandleSensorAsync(SensorConnectionContext connectionContext, CancellationToken cancellationToken)
    {
        var stream = connectionContext.Client.GetStream();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var eventEnvelope = await messenger.ReceiveAsync(stream, cancellationToken).ConfigureAwait(false);
                if (eventEnvelope is null)
                {
                    break;
                }

                await HandleMeasurementCompletedAsync(eventEnvelope, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is IOException or SocketException)
        {
            logger.LogInformation("Sensor {SensorId} disconnected", connectionContext.Sensor.Id);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error handling sensor {SensorId}", connectionContext.Sensor.Id);
        }

        await DisconnectAsync(cancellationToken).ConfigureAwait(false);
        connectionContext.Client.Close();
        stream.Dispose();
        connectionContext.Client.Dispose();
    }

    private async Task HandleMeasurementCompletedAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken)
    {
        if (eventEnvelope.EventType != MeasurementCompletedEvent.Type)
        {
            return;
        }

        var measurementCompletedEvent = JsonSerializer.Deserialize<MeasurementCompletedEvent>(eventEnvelope.Payload)!;
        var measurement = new Measurement
        {
            SensorId = connectionContext.Sensor.Id,
            Timestamp = measurementCompletedEvent.Timestamp,
            MetricType = Enum.TryParse(measurementCompletedEvent.MetricType, out MetricType metricType) ? metricType : MetricType.Unknown,
            Value = measurementCompletedEvent.Value,
            Unit = measurementCompletedEvent.Unit,
        };

        using var scope = serviceScopeFactory.CreateScope();
        var measurementService = scope.ServiceProvider.GetRequiredService<MeasurementService>();
        await measurementService.StoreAsync(measurement, cancellationToken).ConfigureAwait(false);
    }

    public async Task NotifyConfigurationChangedAsync(int measurementPeriodMilliseconds, CancellationToken cancellationToken)
    {
        var stream = connectionContext.Client.GetStream();
        var sensorConfigurationChangedEvent = new SensorConfigurationChangedEvent
        {
            MeasurementPeriodMilliseconds = measurementPeriodMilliseconds,
        };

        var eventEnvelope = new EventEnvelope
        {
            EventType = SensorConfigurationChangedEvent.Type,
            Payload = JsonSerializer.Serialize(sensorConfigurationChangedEvent)
        };

        await messenger.SendAsync(stream, eventEnvelope, cancellationToken).ConfigureAwait(false);
    }

    private async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var sensorService = scope.ServiceProvider.GetRequiredService<SensorService>();

        await sensorService.DisonnectAsync(connectionContext.Sensor.Id, cancellationToken).ConfigureAwait(false);
    }
}
