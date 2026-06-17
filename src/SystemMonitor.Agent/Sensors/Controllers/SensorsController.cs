using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemMonitor.Agent.Sensors.Services;
using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Agent.Sensors.Controllers;

/// <summary>
/// API controller for managing sensors and their configurations.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public sealed class SensorsController(SensorService sensorService, ILogger<SensorsController> logger) : ControllerBase
{
    /// <summary>
    /// Retrieves all sensors.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A collection of all sensors.</returns>
    /// <response code="200">Successfully retrieved all sensors.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<SensorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var sensors = await sensorService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("GetAllAsync returned {count} sensors", sensors?.Count ?? 0);

        return Ok(sensors);
    }

    /// <summary>
    /// Retrieves a specific sensor by its ID.
    /// </summary>
    /// <param name="id">The ID of the sensor to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The sensor with the specified ID, or a 404 response if not found.</returns>
    /// <response code="200">Successfully retrieved the sensor.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    /// <response code="404">Sensor not found.</response>
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SensorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var sensor = await sensorService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (sensor is null)
        {
            logger.LogInformation("Sensor id={id} not found", id);
            return NotFound();
        }

        logger.LogInformation("GetByIdAsync found sensor id={id}", id);
        return Ok(sensor);
    }

    /// <summary>
    /// Updates the configuration of a specific sensor.
    /// </summary>
    /// <param name="id">The ID of the sensor to update.</param>
    /// <param name="updateSensorConfiguration">The new sensor configuration data.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A 200 response if update was successful, or 400 if the sensor could not be updated.</returns>
    /// <response code="200">Successfully updated the sensor configuration.</response>
    /// <response code="400">Bad request - sensor configuration update failed.</response>
    /// <response code="401">Unauthorized - authentication required.</response>
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateConfigurationAsync(int id, [FromBody] UpdateSensorConfigurationDto updateSensorConfiguration, CancellationToken cancellationToken)
    {
        bool updated = await sensorService.TryUpdateConfigurationAsync(id, updateSensorConfiguration, cancellationToken).ConfigureAwait(false);
        if (updated)
        {
            logger.LogInformation("Updated sensor id={id} configuration successfully", id);
            return Ok();
        }

        logger.LogWarning("Failed to update sensor id={id} configuration", id);
        return BadRequest();
    }
}
