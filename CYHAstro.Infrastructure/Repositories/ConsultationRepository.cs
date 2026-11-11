using CYHAstro.Core.Common;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace CYHAstro.Infrastructure.Repositories;

/// <summary>
/// Dapper-based consultation session repository.
/// All billing events use stored procedures for transaction safety.
/// </summary>
public sealed class ConsultationRepository : IConsultationRepository
{
    private readonly IDbConnectionFactory _db;

    public ConsultationRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<ConsultationSession?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ConsultationSession>(
            "usp_GetConsultationById",
            new { Id = id },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<ConsultationSession?> GetByPublicIdAsync(string publicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ConsultationSession>(
            "usp_GetConsultationByPublicId",
            new { PublicId = publicId },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<ConsultationSession?> GetByRoomNameAsync(string roomName, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<ConsultationSession>(
            "SELECT TOP 1 * FROM ConsultationSessions WHERE LiveKitRoomName = @RoomName AND IsDeleted = 0",
            new { RoomName = roomName });
    }

    public async Task<long> CreateAsync(ConsultationSession session, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(
            "usp_CreateConsultationSession",
            new
            {
                session.PublicId,
                session.UserId,
                session.AstrologerId,
                session.SessionType,
                session.RatePerMinute,
                session.LiveKitRoomName,
                session.IdempotencyKey
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Result> UpdateStatusAsync(long sessionId, string status, string? endReason = null, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            "usp_UpdateConsultationStatus",
            new { SessionId = sessionId, Status = status, EndReason = endReason },
            commandType: CommandType.StoredProcedure);
        return rows > 0 ? Result.Success() : Result.Failure("Session not found.", Result.ErrorCodes.NotFound);
    }

    public async Task<Result> SetConnectedAtAsync(long sessionId, DateTime connectedAt, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            "UPDATE ConsultationSessions SET ConnectedAt = @ConnectedAt, Status = 'ACTIVE', UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            new { Id = sessionId, ConnectedAt = connectedAt });
        return rows > 0 ? Result.Success() : Result.Failure("Session not found.", Result.ErrorCodes.NotFound);
    }

    public async Task<Result> SetEndedAtAsync(long sessionId, DateTime endedAt, int durationSeconds, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            "UPDATE ConsultationSessions SET EndedAt = @EndedAt, DurationSeconds = @DurationSeconds, Status = 'ENDED', UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            new { Id = sessionId, EndedAt = endedAt, DurationSeconds = durationSeconds });
        return rows > 0 ? Result.Success() : Result.Failure("Session not found.", Result.ErrorCodes.NotFound);
    }

    public async Task<Result> SetBilledAsync(long sessionId, decimal totalCharge, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            "UPDATE ConsultationSessions SET IsBilled = 1, TotalCharge = @TotalCharge, Status = 'BILLED', UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            new { Id = sessionId, TotalCharge = totalCharge });
        return rows > 0 ? Result.Success() : Result.Failure("Session not found.", Result.ErrorCodes.NotFound);
    }

    public async Task<PagedResult<ConsultationSession>> GetUserSessionsAsync(
        long userId, int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var multi = await conn.QueryMultipleAsync(
            "usp_GetUserConsultations",
            new { UserId = userId, Page = page, PageSize = pageSize },
            commandType: CommandType.StoredProcedure);

        var items = (await multi.ReadAsync<ConsultationSession>()).ToList();
        var total = await multi.ReadSingleAsync<int>();
        return PagedResult<ConsultationSession>.Create(items.AsReadOnly(), total, page, pageSize);
    }

    public async Task<PagedResult<ConsultationSession>> GetAstrologerSessionsAsync(
        long astrologerId, int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var multi = await conn.QueryMultipleAsync(
            "usp_GetAstrologerConsultations",
            new { AstrologerId = astrologerId, Page = page, PageSize = pageSize },
            commandType: CommandType.StoredProcedure);

        var items = (await multi.ReadAsync<ConsultationSession>()).ToList();
        var total = await multi.ReadSingleAsync<int>();
        return PagedResult<ConsultationSession>.Create(items.AsReadOnly(), total, page, pageSize);
    }
}
