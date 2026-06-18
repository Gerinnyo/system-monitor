using System.Net.Http.Headers;
using System.Net.Http.Json;
using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Dashboard.Services;

public class SensorApiService(HttpClient httpClient, AuthService authService)
{
    public async Task<bool> UpdatePeriodAsync(int sensorId, int periodMs, CancellationToken cancellationToken = default)
    {
        var token = await authService.GetTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Put, $"sensors/{sensorId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new UpdateSensorConfigurationDto { MeasurementPeriodMilliseconds = periodMs });
        var response = await httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
