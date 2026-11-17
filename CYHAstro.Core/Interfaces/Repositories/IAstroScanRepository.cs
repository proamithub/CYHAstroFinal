using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Scans;

namespace CYHAstro.Core.Interfaces.Repositories;

public interface IAstroScanRepository
{
    Task<long> InsertScanAsync(
        string publicId,
        long userId,
        long? birthProfileId,
        string scanType,
        string? handType,
        string imageUrl,
        string status,
        decimal? confidenceScore,
        string extractedFeaturesJson,
        string analysisReportJson,
        decimal costCharged,
        int tokensUsed,
        string language,
        CancellationToken ct = default);

    Task<AstroScanDetailDto?> GetByPublicIdAsync(string publicId, CancellationToken ct = default);

    Task<List<AstroScanListItemDto>> GetUserScansAsync(long userId, CancellationToken ct = default);

    Task<int> GetUserScanCountAsync(long userId, string scanType, CancellationToken ct = default);

    Task<PagedResult<AstroScanDetailDto>> AdminGetScansAsync(
        string? scanType,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<bool> UpdateAdminReviewAsync(
        string publicId,
        string adminNotes,
        CancellationToken ct = default);

    Task<AstroScanSettingsDto> GetSettingsAsync(CancellationToken ct = default);

    Task<bool> UpdateSettingsAsync(
        AstroScanSettingsDto settings,
        string updatedBy,
        CancellationToken ct = default);
}
