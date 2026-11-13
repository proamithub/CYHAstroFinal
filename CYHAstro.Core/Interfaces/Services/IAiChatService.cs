using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Ai;

namespace CYHAstro.Core.Interfaces.Services;

public interface IAiChatService
{
    Task<Result<List<AiChatSessionDto>>> GetUserSessionsAsync(long userId, CancellationToken ct = default);
    Task<Result<AiChatSessionDto>> GetSessionAsync(long userId, string sessionPublicId, CancellationToken ct = default);
    Task<Result<SendAiChatMessageResponseDto>> SendMessageAsync(long userId, SendAiChatMessageRequestDto request, CancellationToken ct = default);
    Task<Result<bool>> DeleteSessionAsync(long userId, string sessionPublicId, CancellationToken ct = default);
}
