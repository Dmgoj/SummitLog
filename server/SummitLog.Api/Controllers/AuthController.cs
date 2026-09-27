using SummitLog.Api.Dtos.Auth;
using SummitLog.Api.Models;
using SummitLog.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SummitLog.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("AuthRateLimitPolicy")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    JwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        return Created(string.Empty, new { userId = user.Id, email = user.Email });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Unauthorized();
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Unauthorized();
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var (token, expiresAt) = jwtTokenService.CreateToken(user);
        return Ok(new AuthResponse(token, expiresAt, user.Email!));
    }
}
