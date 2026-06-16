using Microsoft.Extensions.Options;
using SystemMonitor.Sensor.Configurations;
using SystemMonitor.Sensor.Measurements.Collectors;

namespace SystemMonitor.Sensor.Agent.Buffers;

public sealed class CircularBuffer(IOptions<BufferConfiguration> bufferConfiguration, ILogger<CircularBuffer> logger)
{
    private readonly Queue<Measurement> _buffer = [];

    public void Add(Measurement measurement)
    {
        if (_buffer.Count == bufferConfiguration.Value.BufferCapacity)
        {
            _buffer.Dequeue();
        }

        _buffer.Enqueue(measurement);
        logger.LogInformation("Measurement {Measurement} completed and added to the buffer", measurement);
    }
}
