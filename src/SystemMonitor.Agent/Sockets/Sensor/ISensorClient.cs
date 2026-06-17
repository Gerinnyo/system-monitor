using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Agent.Sockets.Sensor;

public interface ISensorClient
{
    Task UpdateSensorAsync(SensorDto sensorDto);
}
