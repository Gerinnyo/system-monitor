using System.Net.Http.Headers;
using System.Net.Http.Json;
using SystemMonitor.Shared.Measurements.Dtos;

namespace SystemMonitor.Dashboard.Services;

public sealed class MeasurementApiService(HttpClient httpClient, AuthService authService)
{
    public async Task<MeasurementsPaginatedDto?> QueryAsync(
        int? sensorId, DateTime? from, DateTime? to,
        int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var token = await authService.GetTokenAsync();

        var parts = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (sensorId.HasValue)
            parts.Add($"sensorId={sensorId.Value}");
        if (from.HasValue)
            parts.Add($"from={Uri.EscapeDataString(from.Value.ToString("o"))}");
        if (to.HasValue)
            parts.Add($"to={Uri.EscapeDataString(to.Value.ToString("o"))}");

        using var request = new HttpRequestMessage(HttpMethod.Get, "measurements?" + string.Join("&", parts));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<MeasurementsPaginatedDto>(cancellationToken: cancellationToken);
    }
}
