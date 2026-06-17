using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SystemMonitor.Agent.Persistence;
using SystemMonitor.Shared.Measurements.Dtos;
using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Agent.Sockets.SensorsState;

public sealed class SensorsStateSocket(
    ApplicationDatabaseContext dbContext,
    IHubContext<SensorsStateHub, ISensorsStateClient> hubContext,
    ILogger<SensorsStateSocket> logger)
{
    private const int MeasurementCount = 1;

    public async Task NotifyUpdateAsync(CancellationToken cancellationToken)
    {
        var sensors = await dbContext.Sensors
            .OrderByDescending(x => x.ConnectionState)
            .ThenByDescending(x => x.Id)
            .Select(x => new SensorDto
            {
                Id = x.Id,
                IpAddress = x.IpAddress,
                Port = x.Port,
                MeasurementPeriodMilliseconds = x.MeasurementPeriodMilliseconds,
                ConnectionState = x.ConnectionState.ToString(),
                Measurements = x.Measurements
                    .OrderByDescending(y => y.Timestamp)
                    .ThenByDescending(y => y.Id)
                    .Take(MeasurementCount)
                    .Select(y => new MeasurementDto
                    {
                        Id = y.Id,
                        SensorId = y.SensorId,
                        Timestamp = y.Timestamp,
                        MetricType = y.MetricType.ToString(),
                        Value = y.Value,
                        Unit = y.Unit,
                    }).ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        await hubContext.Clients.All.UpdateSensorStatesAsync(sensors).ConfigureAwait(false);
        logger.LogInformation("Notified clients about the state of sensors");
    }
}
