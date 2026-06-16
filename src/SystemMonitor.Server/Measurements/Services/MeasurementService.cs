using Microsoft.EntityFrameworkCore;
using SystemMonitor.Server.Measurements.Entities;
using SystemMonitor.Server.Persistence;
using SystemMonitor.Shared.Measurements;

namespace SystemMonitor.Server.Measurements.Services;

public sealed class MeasurementService(ApplicationDatabaseContext dbContext)
{
    public async Task StoreAsync(MeasurementCompletedEvent measurementCompletedEvent, CancellationToken cancellationToken)
    {
        var sensorExists = await dbContext.Sensors
            .Where(x => x.Id == measurementCompletedEvent.SensorId)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!sensorExists)
        {
            return;
        }

        var measurement = new Measurement
        {
            SensorId = measurementCompletedEvent.SensorId,
            Timestamp = measurementCompletedEvent.Timestamp,
            MetricType = Enum.TryParse(measurementCompletedEvent.MetricType, out MetricType metricType) ? metricType : MetricType.Unknown,
            Value = measurementCompletedEvent.Value,
            Unit = measurementCompletedEvent.Unit,
        };

        dbContext.Measurements.Add(measurement);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<MeasurementQueryResult> QueryAsync(int? sensorId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var measurementsQueryBuilder = dbContext.Measurements.Include(x => x.Sensor).AsQueryable();

        if (sensorId is not null)
        {
            measurementsQueryBuilder = measurementsQueryBuilder.Where(x => x.Id == sensorId);
        }

        if (from.HasValue)
        {
            measurementsQueryBuilder = measurementsQueryBuilder.Where(x => x.Timestamp >= from);
        }

        if (to.HasValue)
        {
            measurementsQueryBuilder = measurementsQueryBuilder.Where(x => x.Timestamp < to);
        }

        var total = await measurementsQueryBuilder.CountAsync();
        var measurements = await measurementsQueryBuilder.OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new MeasurementQueryResult
        {
            Measurements = measurements,
            Total = total,
        };
    }
}
