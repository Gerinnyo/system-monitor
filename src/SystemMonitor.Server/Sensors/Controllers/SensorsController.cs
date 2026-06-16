using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SystemMonitor.Server.Sensors.Services;
using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Server.Sensors.Controllers;

[ApiController]
[Route("sensors")]
[Authorize]
public class SensorsController(SensorService sensorService, CancellationToken cancellationToken) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var sensors = await sensorService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(sensors);
    }

    [HttpGet("{id:string}")]
    public async Task<IActionResult> GetByIdAsync(string id)
    {
        var sensor = await sensorService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return sensor is not null ? Ok(sensor) : NotFound();
    }

    [HttpPut("{id:string}")]
    public async Task<IActionResult> UpdateConfigurationAsync(string id, [FromBody] UpdateSensorConfigurationDto updateSensorConfiguration)
    {
        await sensorService.UpdateConfigurationAsync(id, updateSensorConfiguration, cancellationToken).ConfigureAwait(false);
        return Ok();
    }
}
