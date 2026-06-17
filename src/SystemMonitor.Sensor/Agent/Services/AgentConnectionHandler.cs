using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using System.Text.Json;
using SystemMonitor.Sensor.Agent.Buffers;
using SystemMonitor.Sensor.Configurations;
using SystemMonitor.Sensor.Measurements.Collectors;
using SystemMonitor.Shared.Measurements.Events;
using SystemMonitor.Shared.Notifications;
using SystemMonitor.Shared.Sensors.Events;

namespace SystemMonitor.Sensor.Agent.Services;

public sealed class AgentConnectionHandler(
    IEnumerable<IMeasurementCollector> measurementCollectors,
    SensorConfiguration sensorConfiguration,
    Messenger messenger,
    IOptions<AgentConfiguration> agentConfiguration,
    CircularBuffer circularBuffer,
    ILogger<AgentConnectionHandler> logger)
{
    private readonly OSPlatform _platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows : (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux : throw new UnsupportedPlatformException());

    private AgentConnectionContext connectionContext = default!;

    public void ConfigureContext(AgentConnectionContext connectionContext)
    {
        this.connectionContext = connectionContext;
    }

    public async Task HandleAgentAsync(CancellationToken cancellationToken)
    {
        var measurementCollector = measurementCollectors.ToDictionary(x => x.Platform)[_platform];

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var measurements = measurementCollector.Collect();

                foreach (var measurement in measurements)
                {
                    circularBuffer.Add(measurement);
                    logger.LogDebug("Collected measurement for sensor {SensorId} timestamp={Timestamp}", sensorConfiguration.SensorId, measurement.Timestamp);

                    await NotifyMeasurementCompletedAsync(measurement, cancellationToken).ConfigureAwait(false);
                    logger.LogInformation("Notified measurement completed for sensor {SensorId}", sensorConfiguration.SensorId);
                }

                await Task.Delay(sensorConfiguration.MeasurementPeriodMilliseconds, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occured during measurement collection of sensor {SensorId}", sensorConfiguration.SensorId);
        }
    }

    public async void FireConfigurationChangeListener(CancellationToken cancellationToken)
    {
        var stream = connectionContext.Client.GetStream();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug("Waiting for configuration envelope from agent {Host}:{Port}", agentConfiguration.Value.Host, agentConfiguration.Value.Port);
                var eventEnvelope = await messenger.ReceiveAsync(stream, cancellationToken).ConfigureAwait(false);
                if (eventEnvelope is null)
                {
                    break;
                }

                logger.LogInformation("Received configuration event of type {EventType} from agent", eventEnvelope.EventType);
                HandleConfigurationChanged(eventEnvelope);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Configuration change listener has lost connection with the agent {Host}:{Port}", agentConfiguration.Value.Host, agentConfiguration.Value.Port);
        }

        connectionContext.Client.Close();
        stream.Dispose();
        connectionContext.Client.Dispose();
    }

    private void HandleConfigurationChanged(EventEnvelope eventEnvelope)
    {
        if (eventEnvelope.EventType != SensorConfigurationChangedEvent.Type)
        {
            return;
        }

        var sensorConfigurationChangedEvent = JsonSerializer.Deserialize<SensorConfigurationChangedEvent>(eventEnvelope.Payload)!;
        sensorConfiguration.MeasurementPeriodMilliseconds = sensorConfigurationChangedEvent.MeasurementPeriodMilliseconds;
    }

    private async Task NotifyMeasurementCompletedAsync(Measurement measurement, CancellationToken cancellationToken)
    {
        var stream = connectionContext.Client.GetStream();
        var measurementCompletedEvent = new MeasurementCompletedEvent
        {
            Timestamp = measurement.Timestamp,
            MetricType = measurement.MetricType,
            Value = measurement.Value,
            Unit = measurement.Unit,
        };

        var eventEnvelope = new EventEnvelope
        {
            EventType = MeasurementCompletedEvent.Type,
            Payload = JsonSerializer.Serialize(measurementCompletedEvent)
        };

        await messenger.SendAsync(stream, eventEnvelope, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Sent measurement event for sensor {SensorId}", sensorConfiguration.SensorId);
    }
}
