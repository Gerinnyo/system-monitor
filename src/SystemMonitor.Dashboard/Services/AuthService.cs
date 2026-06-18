using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace SystemMonitor.Dashboard.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> LoginAsync(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("auth/login", new { username, password });
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (result?.Token is not null)
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, result.Token);

        return result?.Token;
    }

    public async Task<List<string>> RegisterAsync(string username, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("users", new { username, password });
        if (response.IsSuccessStatusCode)
            return [];

        var error = await response.Content.ReadFromJsonAsync<IdentityErrorResponse>();
        return error?.Errors ?? ["Registration failed."];
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
    }

    private record LoginResponse(string Token);
    private record IdentityErrorResponse(List<string> Errors);
}
