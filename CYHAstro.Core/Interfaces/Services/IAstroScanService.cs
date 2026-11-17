using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Scans;

namespace CYHAstro.Core.Interfaces.Services;

public interface IAstroScanService
{
    /// <summary>
    /// Processes a palm scan image with Gemini Vision and authentic Vedic Hastarekha interpretation.
    /// </summary>
    Task<Result<AstroScanDetailDto>> ProcessPalmScanAsync(
        long userId,
        Stream imageStream,
        string fileName,
        string contentType,
        string handType = "RIGHT",
        long? birthProfileId = null,
        string language = "hi",
        CancellationToken ct = default);

    /// <summary>
    /// Processes a head/face scan image with Gemini Vision and classical Vedic Samudrika Shastra interpretation.
    /// </summary>
    Task<Result<AstroScanDetailDto>> ProcessHeadScanAsync(
        long userId,
        Stream imageStream,
        string fileName,
        string contentType,
        long? birthProfileId = null,
        string language = "hi",
        CancellationToken ct = default);

    /// <summary>
    /// Gets scan details by PublicId for the logged-in user.
    /// </summary>
    Task<Result<AstroScanDetailDto>> GetUserScanAsync(long userId, string publicId, CancellationToken ct = default);

    /// <summary>
    /// Gets scan history list for the logged-in user.
    /// </summary>
    Task<Result<List<AstroScanListItemDto>>> GetUserScanHistoryAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets current scan settings and pricing for the client app.
    /// </summary>
    Task<Result<AstroScanSettingsDto>> GetPublicSettingsAsync(CancellationToken ct = default);

    // --- Admin Operations ---

    /// <summary>
    /// Admin: Gets paginated list of all scans across the platform with filtering.
    /// </summary>
    Task<Result<PagedResult<AstroScanDetailDto>>> AdminGetAllScansAsync(
        string? scanType,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Admin: Gets detailed scan info with user, birth profile, image, and analysis.
    /// </summary>
    Task<Result<AstroScanDetailDto>> AdminGetScanDetailAsync(string publicId, CancellationToken ct = default);

    /// <summary>
    /// Admin: Adds astrologer manual notes and marks scan as reviewed.
    /// </summary>
    Task<Result<bool>> AdminUpdateReviewNotesAsync(
        string publicId,
        string adminNotes,
        string reviewerName,
        CancellationToken ct = default);

    /// <summary>
    /// Admin: Gets system scan settings.
    /// </summary>
    Task<Result<AstroScanSettingsDto>> AdminGetSettingsAsync(CancellationToken ct = default);

    /// <summary>
    /// Admin: Updates pricing, free limits, toggles, models, and prompts.
    /// </summary>
    Task<Result<bool>> AdminUpdateSettingsAsync(
        AstroScanSettingsDto settings,
        string adminName,
        CancellationToken ct = default);
}
