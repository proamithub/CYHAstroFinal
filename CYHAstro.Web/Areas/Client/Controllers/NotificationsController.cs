using System.Security.Claims;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Client.Controllers;

[Area("Client")]
[Authorize]
public sealed class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId <= 0) return Json(new { success = false, message = "Unauthorized" });

        var notifs = await _notificationService.GetUserNotificationsAsync(userId, 20, ct);
        var unread = await _notificationService.GetUnreadCountAsync(userId, ct);

        return Json(new
        {
            success = true,
            unreadCount = unread.IsSuccess ? unread.Value : 0,
            items = notifs.IsSuccess ? notifs.Value : []
        });
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead([FromForm] long id, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId <= 0) return Json(new { success = false });

        var res = await _notificationService.MarkAsReadAsync(id, userId, ct);
        return Json(new { success = res.IsSuccess });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId <= 0) return Json(new { success = false });

        var res = await _notificationService.MarkAllAsReadAsync(userId, ct);
        return Json(new { success = res.IsSuccess });
    }

    private long GetUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
