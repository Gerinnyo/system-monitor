using Microsoft.AspNetCore.SignalR;

namespace SystemMonitor.Agent.Sockets.SensorsState;

public sealed class SensorsStateHub(SensorsStateSocket sensorsStateSocket, ILogger<SensorsStateHub> logger) : Hub<ISensorsStateClient>
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync().ConfigureAwait(false);
        await sensorsStateSocket.NotifyUpdateAsync(Context.ConnectionAborted).ConfigureAwait(false);
        logger.LogInformation("The state of sensors have been pushed to the connection initiator");
    }
}
