namespace SystemMonitor.Agent.Configurations;

public sealed record JwtConfiguration
{
    public required string SecretKey { get; init; }

    public required string Issuer { get; init; }

    public required string Audience { get; init; }
}
