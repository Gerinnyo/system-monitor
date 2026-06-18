using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace SystemMonitor.Dashboard.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthService _authService;
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal());

    public CustomAuthStateProvider(AuthService authService)
    {
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            return Anonymous;

        var claims = ParseClaimsFromJwt(token);
        var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim is not null && long.TryParse(expClaim.Value, out var exp))
        {
            if (DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow)
            {
                await _authService.LogoutAsync();
                return Anonymous;
            }
        }

        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
    }

    public void NotifyUserAuthentication(string token)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLogout()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
        return keyValuePairs?.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())) ?? [];
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        return (base64.Length % 4) switch
        {
            2 => Convert.FromBase64String(base64 + "=="),
            3 => Convert.FromBase64String(base64 + "="),
            _ => Convert.FromBase64String(base64)
        };
    }
}
