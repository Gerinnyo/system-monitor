using Microsoft.EntityFrameworkCore;
using SystemMonitor.Server.Persistence;
using SystemMonitor.Server.Sensors.Entities;
using SystemMonitor.Shared.Measurements.Dtos;
using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Server.Sensors.Services;

public class SensorService(ApplicationDatabaseContext dbContext, SensorConnectionService notificationService)
{
    private const int DefaultMeasurementPeriodMilliseconds = 1000;
    private const int MeasurementCount = 10;

    public Task<List<SensorDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return dbContext.Sensors
            .Include(x => x.Measurements
                .OrderByDescending(y => y.Timestamp)
                .Take(1))
            .OrderBy(x => x.ConnectionState)
            .ThenBy(x => x.Port)
            .Select(x => new SensorDto
            {
                Id = x.Id.ToString(),
                IpAddress = x.IpAddress,
                Port = x.Port,
                MeasurementPeriodMilliseconds = x.MeasurementPeriodMilliseconds,
                ConnectionState = x.ConnectionState.ToString(),
                Measurements = x.Measurements.Select(y => new MeasurementDto
                {
                    Id = y.Id,
                    SensorId = y.SensorId.ToString(),
                    Timestamp = y.Timestamp,
                    MetricType = y.MetricType.ToString(),
                    Value = y.Value,
                    Unit = y.Unit,
                }).ToList(),
            })
            .ToListAsync(cancellationToken);
    }

    public Task<SensorDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Sensors
            .Include(x => x.Measurements
                .OrderByDescending(y => y.Timestamp)
                .Take(MeasurementCount))
            .Where(x => x.Id == id)
            .Select(x => new SensorDto
            {
                Id = x.Id.ToString(),
                IpAddress = x.IpAddress,
                Port = x.Port,
                MeasurementPeriodMilliseconds = x.MeasurementPeriodMilliseconds,
                ConnectionState = x.ConnectionState.ToString(),
                Measurements = x.Measurements.Select(y => new MeasurementDto
                {
                    Id = y.Id,
                    SensorId = y.SensorId.ToString(),
                    Timestamp = y.Timestamp,
                    MetricType = y.MetricType.ToString(),
                    Value = y.Value,
                    Unit = y.Unit,
                }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Sensor> JoinAsync(string ipAddress, int port, CancellationToken cancellationToken)
    {
        var sensor = await dbContext.Sensors
            .Where(x => x.IpAddress == ipAddress)
            .Where(x => x.Port == port)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sensor is null)
        {
            sensor = new Sensor
            {
                Id = Guid.NewGuid(),
                IpAddress = ipAddress,
                Port = port,
                ConnectionState = ConnectionState.Connected,
                MeasurementPeriodMilliseconds = DefaultMeasurementPeriodMilliseconds,
            };

            dbContext.Sensors.Add(sensor);
        }
        else
        {
            sensor.ConnectionState = ConnectionState.Connected;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return sensor;
    }

    public Task DisonnectAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Sensors
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.ConnectionState, ConnectionState.Disconnected), cancellationToken);
    }

    public async Task<bool> TryUpdateConfigurationAsync(Guid id, UpdateSensorConfigurationDto updateSensorConfigurationRequest, CancellationToken cancellationToken)
    {
        var sensor = await dbContext.Sensors
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (sensor is null)
        {
            return false;
        }

        if (sensor.MeasurementPeriodMilliseconds == updateSensorConfigurationRequest.MeasurementPeriodMilliseconds)
        {
            return true;
        }

        // TODO process in transaction
        sensor.MeasurementPeriodMilliseconds = updateSensorConfigurationRequest.MeasurementPeriodMilliseconds;
        await dbContext.SaveChangesAsync(cancellationToken);
        await notificationService.NotifyConfigurationChangedAsync(id, updateSensorConfigurationRequest.MeasurementPeriodMilliseconds, cancellationToken);

        return true;
    }
}
