namespace SystemMonitor.Shared.Auth.Dtos;

public record LoginResponse
{
    public required string Token { get; init; }
}
