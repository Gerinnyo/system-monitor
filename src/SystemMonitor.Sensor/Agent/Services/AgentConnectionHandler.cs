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

        while (!cancellationToken.IsCancellationRequested)
        {
            var measurements = measurementCollector.Collect();

            foreach (var measurement in measurements)
            {
                circularBuffer.Add(measurement);
                await NotifyMeasurementCompletedAsync(measurement, cancellationToken).ConfigureAwait(false);
            }

            await Task.Delay(sensorConfiguration.MeasurementPeriodMilliseconds, cancellationToken).ConfigureAwait(false);
        }
    }

    public async void FireConfigurationChangeListener(CancellationToken cancellationToken)
    {
        var stream = connectionContext.Client.GetStream();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var eventEnvelope = await messenger.ReceiveAsync(stream, cancellationToken);
                if (eventEnvelope is null)
                {
                    break;
                }

                await HandleConfigurationChangedAsync(eventEnvelope, cancellationToken).ConfigureAwait(false);
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

    private async Task HandleConfigurationChangedAsync(EventEnvelope eventEnvelope, CancellationToken cancellationToken)
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
    }
}
