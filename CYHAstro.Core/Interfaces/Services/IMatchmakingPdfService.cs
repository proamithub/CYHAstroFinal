using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Astrology;

namespace CYHAstro.Core.Interfaces.Services;

public interface IMatchmakingPdfService
{
    Task<Result<byte[]>> GenerateMatchmakingPdfAsync(MatchmakingResultDto result, MatchmakingRequestDto request, CancellationToken ct = default);
}
