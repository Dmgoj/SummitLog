using System.ComponentModel.DataAnnotations;

namespace SummitLog.Api.Dtos.Auth;

public record LoginRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(100)] string Password);
