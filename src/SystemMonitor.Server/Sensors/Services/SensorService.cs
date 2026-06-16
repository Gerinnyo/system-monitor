using Microsoft.EntityFrameworkCore;
using SystemMonitor.Server.Persistence;
using SystemMonitor.Server.Sensors.Entities;
using SystemMonitor.Server.Sensors.Requests;
using SystemMonitor.Shared.Sensors;

namespace SystemMonitor.Server.Sensors.Services;

public class SensorService(ApplicationDatabaseContext dbContext, ConnectionService notificationService)
{
    private const int MeasurementCount = 10;

    public Task<List<Sensor>> GetAllAsync(CancellationToken cancellationToken)
    {
        return dbContext.Sensors.OrderBy(x => x.HostName).ToListAsync(cancellationToken);
    }

    public Task<Sensor?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return dbContext.Sensors
            .Include(x => x.Measurements
                .OrderByDescending(y => y.Timestamp)
                .Take(MeasurementCount))
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task JoinAsync(JoinSensorRequest joinSensorRequest, CancellationToken cancellationToken)
    {
        var sensor = await dbContext.Sensors
            .Where(x => x.IpAddress == joinSensorRequest.IpAddress)
            .Where(x => x.HostName == joinSensorRequest.HostName)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sensor is null)
        {
            sensor = new Sensor
            {
                HostName = joinSensorRequest.HostName,
                IpAddress = joinSensorRequest.IpAddress,
                ConnectionState = ConnectionState.Connected,
                MeasurementPeriodMilliseconds = joinSensorRequest.MeasurementPeriodMilliseconds,
            };

            dbContext.Sensors.Add(sensor);
        }
        else
        {
            sensor.ConnectionState = ConnectionState.Connected;
        }

        // TODO wrap into transaction
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var sensorConfigurationChangedEvent = new SensorConfigurationChangedEvent
        {
            Id = sensor.Id,
            MeasurementPeriodMilliseconds = sensor.MeasurementPeriodMilliseconds,
        };
        await notificationService.NotifyConfigurationChangedAsync(sensorConfigurationChangedEvent, cancellationToken);
    }

    public Task DisonnectAsync(string id, CancellationToken cancellationToken)
    {
        return dbContext.Sensors
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.ConnectionState, ConnectionState.Disconnected), cancellationToken);
    }

    public async Task UpdateConfigurationAsync(string id, UpdateSensorConfigurationRequest updateSensorConfigurationRequest, CancellationToken cancellationToken)
    {
        var sensor = await dbContext.Sensors
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sensor is null)
        {
            return;
        }

        if (sensor.MeasurementPeriodMilliseconds != updateSensorConfigurationRequest.MeasurementPeriodMilliseconds)
        {
            return;
        }

        // TODO process in transaction
        sensor.MeasurementPeriodMilliseconds = updateSensorConfigurationRequest.MeasurementPeriodMilliseconds;
        await dbContext.SaveChangesAsync(cancellationToken);

        var sensorConfigurationChangedEvent = new SensorConfigurationChangedEvent
        {
            Id = id,
            MeasurementPeriodMilliseconds = updateSensorConfigurationRequest.MeasurementPeriodMilliseconds,
        };
        await notificationService.NotifyConfigurationChangedAsync(sensorConfigurationChangedEvent, cancellationToken);
    }
}
