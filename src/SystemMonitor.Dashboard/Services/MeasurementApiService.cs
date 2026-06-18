using System.Net.Http.Headers;
using System.Net.Http.Json;
using SystemMonitor.Shared.Measurements.Dtos;

namespace SystemMonitor.Dashboard.Services;

public sealed class MeasurementApiService(HttpClient httpClient, AuthService authService, ILogger<MeasurementApiService> logger)
{
    public async Task<MeasurementsPaginatedDto?> QueryAsync(
        int? sensorId, DateTime? from, DateTime? to,
        int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Querying measurements — sensor: {SensorId}, from: {From}, to: {To}, page: {Page}/{PageSize}",
            sensorId?.ToString() ?? "all", from, to, page, pageSize);

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
        {
            logger.LogWarning("Measurements query failed — HTTP {StatusCode}", (int)response.StatusCode);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<MeasurementsPaginatedDto>(cancellationToken: cancellationToken);
        logger.LogInformation("Measurements query returned {Total} total record(s)", result?.Total ?? 0);
        return result;
    }
}
