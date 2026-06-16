using Microsoft.Extensions.Options;
using System.Net.Sockets;
using System.Text.Json;
using SystemMonitor.Sensor.Configurations;
using SystemMonitor.Shared.Notifications;
using SystemMonitor.Shared.Sensors.Events;

namespace SystemMonitor.Sensor.Agent.Services;

public sealed class AgentConnectionService(
    IServiceScopeFactory serviceScopeFactory,
    Messenger messenger,
    IOptions<AgentConfiguration> agentConfiguration,
    SensorConfiguration sensorConfiguration,
    ILogger<AgentConnectionService> logger) : BackgroundService
{
    private const int ReconnectDelayMilliseconds = 3000;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var client = new TcpClient();

            try
            {
                await client.ConnectAsync(agentConfiguration.Value.Host, agentConfiguration.Value.Port, cancellationToken).ConfigureAwait(false);
                await ConfigureSensorAsync(client, cancellationToken).ConfigureAwait(false);
                await HandleAgentAsync(client, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Connection with the agent {Host}:{Port} could not be established", agentConfiguration.Value.Host, agentConfiguration.Value.Port);
            }

            var stream = client.GetStream();
            client.Close();
            stream.Dispose();
            client.Dispose();

            logger.LogInformation("Reconnecting in {ReconnectDelayMilliseconds} milliseconds", ReconnectDelayMilliseconds);
            await Task.Delay(ReconnectDelayMilliseconds, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ConfigureSensorAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();
        var eventEnvelope = await messenger.ReceiveAsync(stream, cancellationToken).ConfigureAwait(false);
        if (eventEnvelope is null || eventEnvelope.EventType != SensorConfigurationChangedEvent.Type)
        {
            throw new UnconfiguredSensorException();
        }

        var sensorConfigurationChangedEvent = JsonSerializer.Deserialize<SensorConfigurationChangedEvent>(eventEnvelope.Payload)!;
        sensorConfiguration.MeasurementPeriodMilliseconds = sensorConfigurationChangedEvent.MeasurementPeriodMilliseconds;
    }

    private async Task HandleAgentAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var scope = serviceScopeFactory.CreateScope();
        var agentConnectionHandler = scope.ServiceProvider.GetRequiredService<AgentConnectionHandler>();
        var agentConnectionContext = new AgentConnectionContext
        {
            Client = client,
        };

        agentConnectionHandler.ConfigureContext(agentConnectionContext);
        agentConnectionHandler.FireConfigurationChangeListener(cancellationToken);
        await agentConnectionHandler.HandleAgentAsync(cancellationToken).ConfigureAwait(false);
    }
}
