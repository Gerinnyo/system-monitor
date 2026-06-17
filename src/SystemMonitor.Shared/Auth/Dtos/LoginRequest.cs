namespace SystemMonitor.Shared.Auth.Dtos;

public sealed record LoginRequest
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}
