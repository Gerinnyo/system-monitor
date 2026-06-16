namespace SystemMonitor.Shared.Notifications;

public sealed record EventEnvelope
{
    public required string EventType { get; init; }

    public required string Payload { get; init; }
}
