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
}
