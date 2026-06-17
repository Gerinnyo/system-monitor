using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemMonitor.Agent.Measurements.Services;
using SystemMonitor.Shared.Measurements.Dtos;

namespace SystemMonitor.Agent.Measurements.Endpoints;

/// <summary>
/// API controller for managing sensor measurements.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public sealed class MeasurementsController(MeasurementService measurementService, ILogger<MeasurementsController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of measurements based on filter criteria.
    /// </summary>
    /// <param name="sensorId">Optional sensor ID to filter measurements by a specific sensor.</param>
    /// <param name="from">Optional start date to filter measurements from this date onwards.</param>
    /// <param name="to">Optional end date to filter measurements up to this date.</param>
    /// <param name="page">The page number for pagination (default: 1).</param>
    /// <param name="pageSize">The number of measurements per page (default: 50).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A paginated collection of measurements matching the filter criteria.</returns>
    /// <response code="200">Successfully retrieved measurements.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MeasurementsPaginatedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        logger.LogInformation("Query returned {count} measurements (total={total}) for page {page}", measurements.Measurements?.Count ?? 0, measurements.Total, page);
        return Ok(measurements);
    }
}
