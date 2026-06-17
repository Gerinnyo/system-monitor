using SystemMonitor.Shared.Sensors.Dtos;

namespace SystemMonitor.Agent.Sockets.SensorsState;

public interface ISensorsStateClient
{
    Task UpdateSensorStatesAsync(IEnumerable<SensorDto> sensors);
}
