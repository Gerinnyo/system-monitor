using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemMonitor.Server.Sensors.Services;
using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Server.Sensors.Controllers;

[ApiController]
[Route("sensors")]
[Authorize]
public class SensorsController(SensorService sensorService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var sensors = await sensorService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(sensors);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var sensor = await sensorService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return sensor is not null ? Ok(sensor) : NotFound();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateConfigurationAsync(Guid id, [FromBody] UpdateSensorConfigurationDto updateSensorConfiguration, CancellationToken cancellationToken)
    {
        bool updated = await sensorService.TryUpdateConfigurationAsync(id, updateSensorConfiguration, cancellationToken).ConfigureAwait(false);
        return updated ? Ok() : BadRequest();
    }
}
