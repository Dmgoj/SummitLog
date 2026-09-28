using Microsoft.EntityFrameworkCore;
using SummitLog.Api.Data;

namespace SummitLog.Api.Services;

public class SoftDeleteUnverifiedUsersService(IServiceScopeFactory scopeFactory, ILogger<SoftDeleteUnverifiedUsersService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan VerificationWindow = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SoftDeleteExpiredAccountsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run unverified-account cleanup pass.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task SoftDeleteExpiredAccountsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow - VerificationWindow;

        var expiredCount = await db.Users
            .Where(u => !u.EmailConfirmed && !u.IsSoftDeleted && u.CreatedAt < cutoff)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.IsSoftDeleted, true), cancellationToken);

        if (expiredCount > 0)
        {
            logger.LogInformation("Soft-deleted {Count} unverified account(s) past the 24-hour confirmation window.", expiredCount);
        }
    }
}
