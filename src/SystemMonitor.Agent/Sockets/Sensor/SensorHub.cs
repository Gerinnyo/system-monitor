using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SystemMonitor.Agent.Sockets.Sensor;

[Authorize]
public sealed class SensorHub(SensorSocket sensorSocket, ILogger<SensorHub> logger) : Hub<ISensorClient>
{
    private const string IdentifierName = "id";

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync().ConfigureAwait(false);

        if (!TryGetSensorId(out int id))
        {
            logger.LogWarning("Could not resolve sensor id for connection {ConnectionId}", Context.ConnectionId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString(), Context.ConnectionAborted).ConfigureAwait(false);
        bool exists = await sensorSocket.TryNotifyUpdateAsync(id, Context.ConnectionAborted).ConfigureAwait(false);
        if (!exists)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, id.ToString(), Context.ConnectionAborted).ConfigureAwait(false);
            return;
        }

        logger.LogInformation("Sensor {SensorId} pushed to connection {ConnectionId} and added to group", id, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);

        if (!TryGetSensorId(out int id))
        {
            logger.LogWarning("Could not resolve sensor id for connection {ConnectionId}", Context.ConnectionId);
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, id.ToString(), Context.ConnectionAborted).ConfigureAwait(false);
        logger.LogInformation("Connection {ConnectionId} removed from sensor group", Context.ConnectionId);
    }

    private bool TryGetSensorId(out int id)
    {
        var httpContext = Context.GetHttpContext();
        if ((httpContext?.Request?.RouteValues?.TryGetValue(IdentifierName, out var routeValue) ?? false) && int.TryParse(routeValue?.ToString(), out id))
        {
            return true;
        }

        var pathSegments = httpContext?.Request?.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments?.Length >= 2 && int.TryParse(pathSegments[1], out id))
        {
            return true;
        }

        id = 0;
        return false;
    }
}
