using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.User;

namespace CYHAstro.Core.Interfaces.Services;

public interface INotificationService
{
    Task<Result<IReadOnlyList<NotificationDto>>> GetUserNotificationsAsync(long userId, int limit = 20, CancellationToken ct = default);
    Task<Result<int>> GetUnreadCountAsync(long userId, CancellationToken ct = default);
    Task<Result> MarkAsReadAsync(long notificationId, long userId, CancellationToken ct = default);
    Task<Result> MarkAllAsReadAsync(long userId, CancellationToken ct = default);
    Task<Result> CreateNotificationAsync(long userId, string title, string message, string type = "SYSTEM", string? actionUrl = null, CancellationToken ct = default);
}
