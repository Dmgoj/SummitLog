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
    JwtTokenService jwtTokenService,
    AuthEmailService authEmailService,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        try
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await authEmailService.SendConfirmationEmailAsync(user, token);
        }
        catch (Exception ex)
        {
            // The account was already created successfully; a failed confirmation email is
            // recoverable via resend-confirmation and shouldn't fail the whole registration.
            logger.LogError(ex, "Failed to send confirmation email to {Email} after account creation.", user.Email);
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

        if (user.IsSoftDeleted)
        {
            return Unauthorized("Your account was suspended because it wasn't verified in time. Request a new confirmation email to reactivate it.");
        }

        var (token, expiresAt) = jwtTokenService.CreateToken(user);
        return Ok(new AuthResponse(token, expiresAt, user.Email!));
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return BadRequest("Invalid confirmation link.");
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return BadRequest("Invalid or expired confirmation link.");
        }

        if (user.IsSoftDeleted)
        {
            user.IsSoftDeleted = false;
            await userManager.UpdateAsync(user);
        }

        return Ok();
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null && !user.EmailConfirmed)
        {
            try
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                await authEmailService.SendConfirmationEmailAsync(user, token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send confirmation email to {Email}.", user.Email);
            }
        }

        return Ok("If that account exists and isn't verified yet, a confirmation email has been sent.");
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            try
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                await authEmailService.SendPasswordResetEmailAsync(user, token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send password reset email to {Email}.", user.Email);
            }
        }

        return Ok("If that account exists, a password reset email has been sent.");
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return BadRequest("Invalid or expired reset link.");
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        return Ok();
    }
}
