using SummitLog.Api.Services;

namespace SummitLog.Api.Tests.Controllers;

public class ThrowingEmailSender : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
        => throw new HttpRequestException("Simulated email provider failure.");
}
