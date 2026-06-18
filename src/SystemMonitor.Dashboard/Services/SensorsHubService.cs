using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Dashboard.Services;

public class SensorsHubService(AuthService authService, AgentOptions agentOptions, ILogger<SensorsHubService> logger) : IAsyncDisposable
{
    private HubConnection? _connection;

    public IReadOnlyList<SensorDto> Sensors { get; private set; } = [];
    public HubConnectionState ConnectionState => _connection?.State ?? HubConnectionState.Disconnected;
    public event Action? OnChange;

    public async Task StartAsync()
    {
        if (_connection is not null)
            return;

        logger.LogInformation("Connecting to sensors-state hub");

        _connection = new HubConnectionBuilder()
            .WithUrl($"{agentOptions.BaseUrl}/sensors-state", options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = authService.GetTokenAsync;
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<IEnumerable<SensorDto>>("UpdateSensorStatesAsync", sensors =>
        {
            Sensors = sensors.ToList();
            logger.LogInformation("Received state update — {Count} sensor(s)", Sensors.Count);
            OnChange?.Invoke();
        });

        _connection.Reconnecting += ex =>
        {
            logger.LogWarning("Hub reconnecting: {Reason}", ex?.Message);
            OnChange?.Invoke();
            return Task.CompletedTask;
        };

        _connection.Reconnected += id =>
        {
            logger.LogInformation("Hub reconnected (connection ID: {Id})", id);
            OnChange?.Invoke();
            return Task.CompletedTask;
        };

        _connection.Closed += ex =>
        {
            if (ex is null)
                logger.LogInformation("Hub connection closed");
            else
                logger.LogError(ex, "Hub connection closed with error");
            OnChange?.Invoke();
            return Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync();
            logger.LogInformation("Connected to sensors-state hub");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to sensors-state hub");
        }

        OnChange?.Invoke();
    }

    public async Task StopAsync()
    {
        if (_connection is null)
            return;

        logger.LogInformation("Stopping sensors-state hub connection");
        await _connection.StopAsync();
        await _connection.DisposeAsync();
        _connection = null;
        Sensors = [];
        OnChange?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
