namespace SystemMonitor.Agent.Sockets.Sensor;

public static class SensorSocketSetup
{
    public static void MapSensorSocket(this WebApplication app)
    {
        app.MapHub<SensorHub>("/sensors/{id:int}");
    }
}
