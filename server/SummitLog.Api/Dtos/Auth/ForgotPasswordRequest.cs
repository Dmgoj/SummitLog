using System.ComponentModel.DataAnnotations;

namespace SummitLog.Api.Dtos.Auth;

public record ForgotPasswordRequest(
    [Required, EmailAddress, StringLength(256)] string Email);
