using Microsoft.AspNetCore.SignalR;

namespace SystemMonitor.Agent.Sockets.Sensor;

public sealed class SensorHub(SensorSocket sensorSocket, ILogger<SensorHub> logger) : Hub<ISensorClient>
{
    private const string IdentifierName = "id";

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync().ConfigureAwait(false);

        if (!(Context.GetHttpContext()?.Request?.RouteValues?.TryGetValue(IdentifierName, out var idRouteValue) ?? false) ||
            !int.TryParse(idRouteValue?.ToString() ?? string.Empty, out int id))
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, id.ToString(), Context.ConnectionAborted).ConfigureAwait(false);
        await sensorSocket.NotifyUpdateAsync(id, Context.ConnectionAborted).ConfigureAwait(false);
        logger.LogInformation("The sensor has been pushed to the connection initiator and has beend added to the group");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);

        if (!(Context.GetHttpContext()?.Request?.RouteValues?.TryGetValue(IdentifierName, out var idRouteValue) ?? false) ||
            !int.TryParse(idRouteValue?.ToString() ?? string.Empty, out int id))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, id.ToString(), Context.ConnectionAborted).ConfigureAwait(false);
        logger.LogInformation("The connection inititor has been removed from the group");
    }
}
