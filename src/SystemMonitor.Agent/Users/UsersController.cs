using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SystemMonitor.Shared.Users.Dtos;

namespace SystemMonitor.Agent.Users;

/// <summary>
/// API controller for managing users.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public sealed class UsersController(UserManager<IdentityUser> userManager) : ControllerBase
{
    /// <summary>
    /// Creates a new user with the specified credentials.
    /// </summary>
    /// <param name="request">The user creation request containing username and password.</param>
    /// <returns>The newly created user with their ID and username.</returns>
    /// <response code="200">Successfully created the user.</response>
    /// <response code="400">Bad request - user creation failed (invalid password, duplicate username, etc.).</response>
    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IdentityErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = new IdentityUser { UserName = request.Username };
        var result = await userManager.CreateAsync(user, request.Password).ConfigureAwait(false);

        return result.Succeeded ? Ok() : BadRequest(new IdentityErrorResponse { Errors = result.Errors.Select(x => x.Description), });
    }
}
