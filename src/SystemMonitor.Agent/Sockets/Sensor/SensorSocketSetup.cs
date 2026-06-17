namespace SystemMonitor.Agent.Sockets.Sensor;

public static class SensorSocketSetup
{
    public static void MapSensorSocket(this WebApplication app)
    {
        app.MapHub<SensorHub>("/sensors/{id:int}")
            .RequireAuthorization()
            .WithDescription("Hub endpoint that provides updates for a specific sensor by id. Clients should connect to /sensors/{id} and will receive UpdateSensorAsync callbacks.")
            .WithTags("sockets", "sensor");
    }
}
