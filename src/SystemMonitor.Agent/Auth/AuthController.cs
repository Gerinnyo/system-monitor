using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SystemMonitor.Shared.Auth.Dtos;

namespace SystemMonitor.Agent.Auth;

/// <summary>
/// API controller for user authentication and JWT token generation.
/// </summary>
[ApiController]
[Route("[controller]")]
public sealed class AuthController(
    UserManager<IdentityUser> userManager,
    JwtTokenProvider jwtTokenProvider,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Authenticates a user and generates a JWT token.
    /// </summary>
    /// <param name="request">The login credentials containing username and password.</param>
    /// <returns>A JWT token for authenticated users.</returns>
    /// <response code="200">Successfully authenticated. Returns a JWT token.</response>
    /// <response code="401">Unauthorized - invalid username or password.</response>
    [HttpPost("login")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        logger.LogInformation("Login attempt for {Username}", request.Username);

        var user = await userManager.FindByNameAsync(request.Username).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("Login failed for {Username}: user not found", request.Username);
            return Unauthorized();
        }

        bool passwordValid = await userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false);
        if (!passwordValid)
        {
            logger.LogWarning("Login failed for {Username}: invalid password", request.Username);
            return Unauthorized();
        }

        var token = jwtTokenProvider.CreateFor(user);
        logger.LogInformation("Login successful for {Username}", request.Username);

        return Ok(token);
    }
}
