using System.ComponentModel.DataAnnotations;

namespace SummitLog.Api.Dtos.Auth;

public record RegisterRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password);
