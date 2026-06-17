namespace SystemMonitor.Shared.Users.Dtos;

public sealed record CreateUserRequest
{
    public required string Username { get; init; }

    public required string Password { get; init; }
}
