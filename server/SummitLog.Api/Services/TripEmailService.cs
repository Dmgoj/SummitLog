using SummitLog.Api.Models;

namespace SummitLog.Api.Services;

public class TripEmailService(IEmailSender emailSender, string clientBaseUrl)
{
    public Task SendTripInviteEmailAsync(ApplicationUser invitee, ApplicationUser creator, Peak peak, Trip trip, CancellationToken cancellationToken = default)
    {
        var link = $"{clientBaseUrl}/trips/{trip.Id}";
        var creatorName = string.IsNullOrWhiteSpace(creator.FirstName) ? creator.Email : creator.FirstName;
        var html = $"""
            <p>{creatorName} is organizing a trip to {peak.Name}, a peak on your bucket list.</p>
            <p><a href="{link}">View the trip</a> to join or decline.</p>
            """;
        return emailSender.SendAsync(invitee.Email!, $"Someone's planning a trip to {peak.Name}", html, cancellationToken);
    }
}
