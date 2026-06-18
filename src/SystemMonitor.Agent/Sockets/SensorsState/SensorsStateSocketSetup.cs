namespace SystemMonitor.Agent.Sockets.SensorsState;

public static class SensorsStateSocketSetup
{
    public static void MapSensorsStateSocket(this WebApplication app)
    {
        app.MapHub<SensorsStateHub>("/sensors-state");
    }
}
