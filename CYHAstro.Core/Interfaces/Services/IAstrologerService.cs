using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Astrologer;

namespace CYHAstro.Core.Interfaces.Services;

public interface IAstrologerService
{
    Task<Result<PagedResult<AstrologerListingDto>>> GetAstrologersAsync(
        AstrologerFilterRequestDto filter, CancellationToken ct = default);

    Task<Result<AstrologerDetailDto>> GetAstrologerByPublicIdAsync(
        string publicId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<string>>> GetSpecialtiesAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<string>>> GetLanguagesAsync(CancellationToken ct = default);

    Task<Result> SetAvailabilityAsync(long astrologerId, bool isAvailable, CancellationToken ct = default);
    Task<Result<AstrologerDashboardDto>> GetDashboardAsync(long userId, CancellationToken ct = default);
    Task<Result<AstrologerListingDto?>> GetAstrologerByUserIdAsync(long userId, CancellationToken ct = default);
    Task<Result> UpdateProfileAsync(long userId, UpdateAstrologerProfileDto dto, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AstrologerEarningItemDto>>> GetEarningsHistoryAsync(long userId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CYHAstro.Core.DTOs.Admin.SettlementDto>>> GetSettlementsAsync(long userId, CancellationToken ct = default);
    Task<Result> RequestSettlementAsync(long userId, decimal amount, string payoutMethod, string remarks, CancellationToken ct = default);
}
