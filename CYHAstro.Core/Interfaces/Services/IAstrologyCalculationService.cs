using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Astrology;

namespace CYHAstro.Core.Interfaces.Services;

public interface IAstrologyCalculationService
{
    Task<Result<DailyHoroscopeDto>> GetDailyHoroscopeAsync(
        int zodiacSignId, DateOnly? date = null, string language = "en", CancellationToken ct = default);

    Task<Result<KundliChartDto>> GenerateKundliAsync(
        KundliRequestDto request, CancellationToken ct = default);

    Task<Result<MatchmakingResultDto>> CalculateMatchmakingAsync(
        MatchmakingRequestDto request, CancellationToken ct = default);
}
