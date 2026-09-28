using System.Net;
using SummitLog.Api.Models;

namespace SummitLog.Api.Services;

public class AuthEmailService(IEmailSender emailSender, string clientBaseUrl)
{
    public Task SendConfirmationEmailAsync(ApplicationUser user, string token, CancellationToken cancellationToken = default)
    {
        var link = $"{clientBaseUrl}/confirm-email?userId={WebUtility.UrlEncode(user.Id)}&token={WebUtility.UrlEncode(token)}";
        var html = $"""
            <p>Welcome to SummitLog!</p>
            <p>Confirm your email address to keep your account active: <a href="{link}">Confirm email</a></p>
            <p>If you don't confirm within 24 hours, your account will be suspended until you do.</p>
            """;
        return emailSender.SendAsync(user.Email!, "Confirm your SummitLog account", html, cancellationToken);
    }

    public Task SendPasswordResetEmailAsync(ApplicationUser user, string token, CancellationToken cancellationToken = default)
    {
        var link = $"{clientBaseUrl}/reset-password?userId={WebUtility.UrlEncode(user.Id)}&token={WebUtility.UrlEncode(token)}";
        var html = $"""
            <p>We received a request to reset your SummitLog password.</p>
            <p><a href="{link}">Reset your password</a></p>
            <p>If you didn't request this, you can safely ignore this email.</p>
            """;
        return emailSender.SendAsync(user.Email!, "Reset your SummitLog password", html, cancellationToken);
    }
}
