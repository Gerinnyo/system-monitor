using Microsoft.EntityFrameworkCore;
using SystemMonitor.Agent.Persistence;
using SystemMonitor.Agent.Sensors.Entities;
using SystemMonitor.Shared.Measurements.Dtos;
using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Agent.Sensors.Services;

public sealed class SensorService(ApplicationDatabaseContext dbContext, SensorConnectionService notificationService)
{
    private const int DefaultMeasurementPeriodMilliseconds = 5000;
    private const int MeasurementCount = 10;

    public Task<List<SensorDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return dbContext.Sensors
            .OrderBy(x => x.ConnectionState)
            .ThenBy(x => x.Port)
            .Select(x => new SensorDto
            {
                Id = x.Id,
                IpAddress = x.IpAddress,
                Port = x.Port,
                MeasurementPeriodMilliseconds = x.MeasurementPeriodMilliseconds,
                ConnectionState = x.ConnectionState.ToString(),
                Measurements = x.Measurements
                    .OrderByDescending(y => y.Timestamp)
                    .ThenBy(y => y.Id)
                    .Take(1)
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
            .ToListAsync(cancellationToken);
    }

    public Task<SensorDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.Sensors
            .Where(x => x.Id == id)
            .Select(x => new SensorDto
            {
                Id = x.Id,
                IpAddress = x.IpAddress,
                Port = x.Port,
                MeasurementPeriodMilliseconds = x.MeasurementPeriodMilliseconds,
                ConnectionState = x.ConnectionState.ToString(),
                Measurements = x.Measurements
                    .OrderByDescending(y => y.Timestamp)
                    .ThenBy(y => y.Id)
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

    public Task DisonnectAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.Sensors
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(y => y.ConnectionState, ConnectionState.Disconnected), cancellationToken);
    }

    public async Task<bool> TryUpdateConfigurationAsync(int id, UpdateSensorConfigurationDto updateSensorConfigurationRequest, CancellationToken cancellationToken)
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
