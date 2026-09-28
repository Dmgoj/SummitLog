using System.ComponentModel.DataAnnotations;

namespace SummitLog.Api.Dtos.Auth;

public record ResetPasswordRequest(
    [Required] string UserId,
    [Required] string Token,
    [Required, StringLength(100, MinimumLength = 8)] string NewPassword);
