using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Dashboard.Services;

public class SensorsHubService : IAsyncDisposable
{
    private readonly AuthService _authService;
    private HubConnection? _connection;

    public IReadOnlyList<SensorDto> Sensors { get; private set; } = [];
    public HubConnectionState ConnectionState => _connection?.State ?? HubConnectionState.Disconnected;
    public event Action? OnChange;

    public SensorsHubService(AuthService authService)
    {
        _authService = authService;
    }

    public async Task StartAsync()
    {
        if (_connection is not null)
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5000/sensors-state", options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = _authService.GetTokenAsync;
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<IEnumerable<SensorDto>>("UpdateSensorStatesAsync", sensors =>
        {
            Sensors = sensors.ToList();
            OnChange?.Invoke();
        });

        _connection.Reconnecting += _ => { OnChange?.Invoke(); return Task.CompletedTask; };
        _connection.Reconnected += _ => { OnChange?.Invoke(); return Task.CompletedTask; };
        _connection.Closed += _ => { OnChange?.Invoke(); return Task.CompletedTask; };

        await _connection.StartAsync();
        OnChange?.Invoke();
    }

    public async Task StopAsync()
    {
        if (_connection is null)
            return;

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
