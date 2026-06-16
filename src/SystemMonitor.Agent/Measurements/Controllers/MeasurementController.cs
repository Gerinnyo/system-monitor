using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemMonitor.Agent.Measurements.Services;
using SystemMonitor.Shared.Measurements.Dtos;

namespace SystemMonitor.Agent.Measurements.Controllers;

[ApiController]
[Route("measurements")]
[Authorize]
public sealed class MeasurementsController(MeasurementService measurementService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] int? sensorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var measurementQueryResult = await measurementService.QueryAsync(sensorId, from, to, page, pageSize, cancellationToken).ConfigureAwait(false);
        var measurements = new MeasurementsPaginatedDto
        {
            PageNumber = page,
            PageSize = pageSize,
            Total = measurementQueryResult.Total,
            Measurements = measurementQueryResult.Measurements,
        };

        return Ok(measurements);
    }
}
