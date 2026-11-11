using CYHAstro.Core.Common;
using CYHAstro.Core.Entities;

namespace CYHAstro.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for consultation sessions.
/// </summary>
public interface IConsultationRepository
{
    Task<ConsultationSession?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ConsultationSession?> GetByPublicIdAsync(string publicId, CancellationToken ct = default);
    Task<ConsultationSession?> GetByRoomNameAsync(string roomName, CancellationToken ct = default);

    Task<long> CreateAsync(ConsultationSession session, CancellationToken ct = default);

    Task<Result> UpdateStatusAsync(
        long sessionId,
        string status,
        string? endReason = null,
        CancellationToken ct = default);

    Task<Result> SetConnectedAtAsync(long sessionId, DateTime connectedAt, CancellationToken ct = default);
    Task<Result> SetEndedAtAsync(long sessionId, DateTime endedAt, int durationSeconds, CancellationToken ct = default);
    Task<Result> SetBilledAsync(long sessionId, decimal totalCharge, CancellationToken ct = default);

    Task<PagedResult<ConsultationSession>> GetUserSessionsAsync(
        long userId, int page, int pageSize, CancellationToken ct = default);

    Task<PagedResult<ConsultationSession>> GetAstrologerSessionsAsync(
        long astrologerId, int page, int pageSize, CancellationToken ct = default);
}
