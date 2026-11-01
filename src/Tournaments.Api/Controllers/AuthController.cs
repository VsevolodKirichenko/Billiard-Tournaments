using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tournaments.Api.Authentication;
using Tournaments.Api.Contracts;
using Tournaments.Infrastructure.Identity;

namespace Tournaments.Api.Controllers;

[ApiController, Route("auth")]
public sealed class AuthController(UserManager<ApplicationUser> users, TokenService tokens) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        // Self registration can never grant elevated roles.
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded) return BadRequest(new { errors = created.Errors.Select(e => e.Description) });
        var assigned = await users.AddToRoleAsync(user, Roles.Player);
        if (!assigned.Succeeded) return Problem("Could not assign the default role.");
        return Ok(await tokens.Issue(user));
    }
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || await users.IsLockedOutAsync(user)) return Unauthorized();
        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            return Unauthorized();
        }
        await users.ResetAccessFailedCountAsync(user);
        return Ok(await tokens.Issue(user));
    }
    [HttpPost("users/{id}/roles"), Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> AssignRole(string id, AssignRoleRequest request)
    {
        if (!Roles.All.Contains(request.Role)) return BadRequest(new { error = "Unknown role." });
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        var result = await users.AddToRoleAsync(user, request.Role);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors.Select(e => e.Description) });
    }
}
