using Microsoft.AspNetCore.Identity;

namespace SummitLog.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePicturePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsSoftDeleted { get; set; }
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
    public DateTime? LastLocationUpdatedAt { get; set; }
}
