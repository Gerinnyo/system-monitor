using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemMonitor.Server.Measurements.Services;

namespace SystemMonitor.Server.Measurements.Controllers;

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
        [FromQuery] int pageSize = 50)
    {
        var measurementQueryResult = await measurementService.QueryAsync(sensorId, from, to, page, pageSize).ConfigureAwait(false);
        var measurementQueryResponse = new MeasurementQueryResponse
        {
            PageNumber = page,
            PageSize = pageSize,
            Total = measurementQueryResult.Total,
            Measurements = measurementQueryResult.Measurements,
        };

        return Ok(measurementQueryResponse);
    }
}
