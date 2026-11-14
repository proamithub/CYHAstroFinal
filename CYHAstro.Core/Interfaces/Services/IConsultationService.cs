using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Consultation;

namespace CYHAstro.Core.Interfaces.Services;

public interface IConsultationService
{
    Task<Result<ConsultationResponseDto>> RequestConsultationAsync(long userId, RequestConsultationDto request, CancellationToken ct = default);
    Task<Result<ConsultationResponseDto>> AcceptConsultationAsync(long astrologerUserId, string sessionPublicId, CancellationToken ct = default);
    Task<Result<ConsultationCompletedDto>> EndConsultationAsync(long actorUserId, string sessionPublicId, string endReason, CancellationToken ct = default);
    Task<Result<ConsultationResponseDto>> GetConsultationByPublicIdAsync(string sessionPublicId, CancellationToken ct = default);
    Task<Result<ConsultationResponseDto?>> GetActiveConsultationAsync(long userId, CancellationToken ct = default);
    Task<Result<string>> GetLiveKitTokenAsync(long userId, string sessionPublicId, CancellationToken ct = default);
    Task<Result<ConsultationMessageDto>> SaveMessageAsync(long senderUserId, string sessionPublicId, string message, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ConsultationMessageDto>>> GetMessagesAsync(string sessionPublicId, int limit = 100, CancellationToken ct = default);
    Task<Result<bool>> SubmitReviewAsync(long userId, CYHAstro.Core.DTOs.Astrologer.SubmitReviewRequestDto dto, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ConsultationResponseDto>>> GetIncomingRequestsForAstrologerAsync(long astrologerUserId, CancellationToken ct = default);
    Task<Result<bool>> RejectConsultationAsync(long astrologerUserId, string sessionPublicId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ConsultationResponseDto>>> GetAstrologerSessionsAsync(long astrologerUserId, string? status = null, int limit = 50, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ConsultationResponseDto>>> GetUserConsultationsAsync(long userId, int limit = 50, CancellationToken ct = default);
    Task HeartbeatAsync(long userId, string sessionPublicId, CancellationToken ct = default);
    Task UpdateDisconnectedAtAsync(long userId, DateTime disconnectedAt, CancellationToken ct = default);
    Task<Result<bool>> AutoEndStaleSessionsAsync(CancellationToken ct = default);
}
