using SummitLog.Api.Controllers;
using SummitLog.Api.Data;
using SummitLog.Api.Dtos;
using SummitLog.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace SummitLog.Api.Tests.Controllers;

public class NotificationsControllerTests
{
    private static NotificationsController CreateController(AppDbContext db, string userId = "user-1")
    {
        var controller = new NotificationsController(db);
        controller.SetUser(userId);
        return controller;
    }

    [Fact]
    public async Task GetAll_OnlyReturnsCallingUsersNotifications_NewestFirst()
    {
        var db = TestHelpers.CreateDbContext();
        db.Notifications.Add(new Notification { UserId = "user-1", Type = NotificationType.TripInvite, Message = "Older", CreatedAt = DateTime.UtcNow.AddMinutes(-10) });
        db.Notifications.Add(new Notification { UserId = "user-1", Type = NotificationType.TripInvite, Message = "Newer", CreatedAt = DateTime.UtcNow });
        db.Notifications.Add(new Notification { UserId = "user-2", Type = NotificationType.TripInvite, Message = "Not mine", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = CreateController(db, "user-1");
        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var notifications = Assert.IsAssignableFrom<IReadOnlyList<NotificationDto>>(ok.Value);
        Assert.Equal(2, notifications.Count);
        Assert.Equal("Newer", notifications[0].Message);
    }

    [Fact]
    public async Task MarkRead_OwnNotification_SetsIsRead()
    {
        var db = TestHelpers.CreateDbContext();
        var notification = new Notification { UserId = "user-1", Type = NotificationType.TripInvite, Message = "Hi", CreatedAt = DateTime.UtcNow };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var controller = CreateController(db, "user-1");
        var result = await controller.MarkRead(notification.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.True(db.Notifications.Single().IsRead);
    }

    [Fact]
    public async Task MarkRead_OtherUsersNotification_ReturnsNotFound()
    {
        var db = TestHelpers.CreateDbContext();
        var notification = new Notification { UserId = "user-2", Type = NotificationType.TripInvite, Message = "Hi", CreatedAt = DateTime.UtcNow };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var controller = CreateController(db, "user-1");
        var result = await controller.MarkRead(notification.Id);

        Assert.IsType<NotFoundResult>(result);
        Assert.False(db.Notifications.Single().IsRead);
    }
}
