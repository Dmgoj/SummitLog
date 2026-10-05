using System.Security.Claims;
using SummitLog.Api.Data;
using SummitLog.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SummitLog.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(AppDbContext db) : ControllerBase
{
    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("User id claim is missing.");

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetAll()
    {
        var userId = CurrentUserId;

        var notifications = await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(n.Id, n.Type.ToString(), n.TripId, n.Message, n.IsRead, n.CreatedAt))
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = CurrentUserId;

        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        await db.SaveChangesAsync();

        return NoContent();
    }
}
