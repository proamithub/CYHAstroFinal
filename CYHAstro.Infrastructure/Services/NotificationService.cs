using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.User;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.Services;

public sealed class NotificationService : INotificationService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IDbConnectionFactory db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> GetUserNotificationsAsync(long userId, int limit = 20, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);
            const string sql = @"
                SELECT TOP (@Limit)
                    Id,
                    UserId,
                    Title,
                    Message,
                    ActionUrl,
                    Type,
                    IsRead,
                    CreatedAt
                FROM dbo.Notifications
                WHERE UserId = @UserId
                ORDER BY CreatedAt DESC";

            var list = (await conn.QueryAsync<NotificationDto>(sql, new { UserId = userId, Limit = limit })).ToList();
            return Result<IReadOnlyList<NotificationDto>>.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notifications for user {UserId}", userId);
            return Result<IReadOnlyList<NotificationDto>>.Failure("Could not load notifications: " + ex.Message);
        }
    }

    public async Task<Result<int>> GetUnreadCountAsync(long userId, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);
            const string sql = "SELECT COUNT(1) FROM dbo.Notifications WHERE UserId = @UserId AND IsRead = 0";
            var count = await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count unread notifications for user {UserId}", userId);
            return Result<int>.Failure("Count failed: " + ex.Message);
        }
    }

    public async Task<Result> MarkAsReadAsync(long notificationId, long userId, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);
            const string sql = @"
                UPDATE dbo.Notifications
                SET IsRead = 1, ReadAt = GETUTCDATE()
                WHERE Id = @Id AND UserId = @UserId";

            await conn.ExecuteAsync(sql, new { Id = notificationId, UserId = userId });
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification #{Id} as read", notificationId);
            return Result.Failure("Mark as read failed: " + ex.Message);
        }
    }

    public async Task<Result> MarkAllAsReadAsync(long userId, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);
            const string sql = @"
                UPDATE dbo.Notifications
                SET IsRead = 1, ReadAt = GETUTCDATE()
                WHERE UserId = @UserId AND IsRead = 0";

            await conn.ExecuteAsync(sql, new { UserId = userId });
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read for user {UserId}", userId);
            return Result.Failure("Mark all as read failed: " + ex.Message);
        }
    }

    public async Task<Result> CreateNotificationAsync(long userId, string title, string message, string type = "SYSTEM", string? actionUrl = null, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);
            const string sql = @"
                INSERT INTO dbo.Notifications (UserId, Title, Message, ActionUrl, Type, IsRead, CreatedAt)
                VALUES (@UserId, @Title, @Message, @ActionUrl, @Type, 0, GETUTCDATE())";

            await conn.ExecuteAsync(sql, new
            {
                UserId = userId,
                Title = title,
                Message = message,
                ActionUrl = actionUrl,
                Type = type
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}", userId);
            return Result.Failure("Notification creation failed: " + ex.Message);
        }
    }
}
