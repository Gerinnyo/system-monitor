using Microsoft.EntityFrameworkCore;
using SystemMonitor.Agent.Measurements.Entities;
using SystemMonitor.Agent.Persistence;
using SystemMonitor.Agent.Sockets.Sensor;
using SystemMonitor.Shared.Measurements.Dtos;

namespace SystemMonitor.Agent.Measurements.Services;

public sealed class MeasurementService(ApplicationDatabaseContext dbContext, SensorSocket sensorSocket)
{
    public async Task StoreAsync(Measurement measurement, CancellationToken cancellationToken)
    {
        var sensorExists = await dbContext.Sensors
            .Where(x => x.Id == measurement.SensorId)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!sensorExists)
        {
            return;
        }

        dbContext.Measurements.Add(measurement);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await sensorSocket.NotifyUpdateAsync(measurement.SensorId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<MeasurementQueryResult> QueryAsync(int? sensorId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken)
    {
        var measurementsQueryBuilder = dbContext.Measurements.Include(x => x.Sensor).AsQueryable();

        if (sensorId is not null)
        {
            measurementsQueryBuilder = measurementsQueryBuilder.Where(x => x.SensorId == sensorId);
        }

        if (from.HasValue)
        {
            measurementsQueryBuilder = measurementsQueryBuilder.Where(x => x.Timestamp >= from);
        }

        if (to.HasValue)
        {
            measurementsQueryBuilder = measurementsQueryBuilder.Where(x => x.Timestamp < to);
        }

        var total = await measurementsQueryBuilder.CountAsync(cancellationToken).ConfigureAwait(false);
        var measurements = await measurementsQueryBuilder.OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MeasurementDto
            {
                Id = x.Id,
                SensorId = x.SensorId,
                Timestamp = x.Timestamp,
                MetricType = x.MetricType.ToString(),
                Value = x.Value,
                Unit = x.Unit,
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new MeasurementQueryResult
        {
            Measurements = measurements,
            Total = total,
        };
    }
}
