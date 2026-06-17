using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using SystemMonitor.Agent.Configurations;

namespace SystemMonitor.Agent.Auth;

public sealed class JwtTokenProvider(IOptions<JwtConfiguration> jwtConfiguration)
{
    private const int TokenExpirationHours = 1;

    public string CreateFor(IdentityUser user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfiguration.Value.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            ]),
            Expires = DateTime.UtcNow.AddHours(TokenExpirationHours),
            SigningCredentials = credentials,
            Issuer = jwtConfiguration.Value.Issuer,
            Audience = jwtConfiguration.Value.Audience,
        };

        var tokenHandler = new JsonWebTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return token;
    }
}
