using System.Net.Http.Headers;
using System.Net.Http.Json;
using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Dashboard.Services;

public class SensorApiService(HttpClient httpClient, AuthService authService, ILogger<SensorApiService> logger)
{
    public async Task<bool> UpdatePeriodAsync(int sensorId, int periodMs, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Updating measurement period for sensor {SensorId} to {PeriodMs} ms", sensorId, periodMs);

        var token = await authService.GetTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Put, $"sensors/{sensorId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new UpdateSensorConfigurationDto { MeasurementPeriodMilliseconds = periodMs });

        var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
            logger.LogInformation("Period update successful for sensor {SensorId}", sensorId);
        else
            logger.LogWarning("Period update failed for sensor {SensorId} — HTTP {StatusCode}", sensorId, (int)response.StatusCode);

        return response.IsSuccessStatusCode;
    }
}
