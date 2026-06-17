namespace SystemMonitor.Agent.Sockets.SensorsState;

public static class SensorsStateSocketSetup
{
    public static void MapSensorsStateSocket(this WebApplication app)
    {
        app.MapHub<SensorsStateHub>("/sensors-state")
            .RequireAuthorization()
            .WithDescription("Hub endpoint that broadcasts the state of the sensors. Clients should connect to /sensors-state and will receive UpdateSensorStatesAsync callbacks.")
            .WithTags("sockets", "sensors", "state");
    }
}
