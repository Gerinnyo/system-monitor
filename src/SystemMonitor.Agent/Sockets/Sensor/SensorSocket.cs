using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SystemMonitor.Agent.Persistence;
using SystemMonitor.Shared.Measurements.Dtos;
using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Agent.Sockets.Sensor;

public sealed class SensorSocket(
    ApplicationDatabaseContext dbContext,
    IHubContext<SensorHub, ISensorClient> hubContext,
    ILogger<SensorSocket> logger)
{
    private const int MeasurementCount = 20;

    public async Task NotifyUpdateAsync(int sensorId, CancellationToken cancellationToken)
    {
        var sensor = await dbContext.Sensors
            .Where(x => x.Id == sensorId)
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
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sensor is null)
        {
            logger.LogWarning("The sensor {SensorId} that initiated the push is not found", sensorId);
        }

        await hubContext.Clients.Group(sensorId.ToString()).UpdateSensorAsync(sensor!).ConfigureAwait(false);
        logger.LogInformation("Notified clients about the update of sensor {SensorId}", sensorId);
    }
}
