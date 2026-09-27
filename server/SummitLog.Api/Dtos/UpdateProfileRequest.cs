using System.ComponentModel.DataAnnotations;

namespace SummitLog.Api.Dtos;

public record UpdateProfileRequest(
    [StringLength(100)] string? FirstName,
    [StringLength(100)] string? LastName);
