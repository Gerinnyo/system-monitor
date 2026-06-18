using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace SystemMonitor.Dashboard.Services;

public class AuthService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<AuthService> logger)
{
    private const string TokenKey = "authToken";

    public async Task<string?> LoginAsync(string username, string password)
    {
        logger.LogInformation("Login attempt for user '{Username}'", username);

        var response = await httpClient.PostAsJsonAsync("auth/login", new { username, password });
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Login failed for '{Username}' — HTTP {StatusCode}", username, (int)response.StatusCode);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (result?.Token is not null)
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, result.Token);
            logger.LogInformation("Login successful for '{Username}'", username);
        }

        return result?.Token;
    }

    public async Task<List<string>> RegisterAsync(string username, string password)
    {
        logger.LogInformation("Registration attempt for user '{Username}'", username);

        var response = await httpClient.PostAsJsonAsync("users", new { username, password });
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("Registration successful for '{Username}'", username);
            return [];
        }

        var error = await response.Content.ReadFromJsonAsync<IdentityErrorResponse>();
        var errors = error?.Errors ?? ["Registration failed."];
        logger.LogWarning("Registration failed for '{Username}': {Errors}", username, string.Join(", ", errors));
        return errors;
    }

    public async Task LogoutAsync()
    {
        logger.LogInformation("User logged out");
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
    }

    public async Task<string?> GetTokenAsync()
        => await jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);

    private record LoginResponse(string Token);
    private record IdentityErrorResponse(List<string> Errors);
}
