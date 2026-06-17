namespace SystemMonitor.Shared.Users.Dtos;

public record IdentityErrorResponse
{
    public required IEnumerable<string> Errors { get; init; }
}
