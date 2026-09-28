using System.ComponentModel.DataAnnotations;

namespace SummitLog.Api.Dtos.Auth;

public record ConfirmEmailRequest(
    [Required] string UserId,
    [Required] string Token);
