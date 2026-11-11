using CYHAstro.Core.Common;
using CYHAstro.Core.Entities;

namespace CYHAstro.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for astrologer data.
/// </summary>
public interface IAstrologerRepository
{
    Task<Astrologer?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Astrologer?> GetByPublicIdAsync(string publicId, CancellationToken ct = default);
    Task<Astrologer?> GetByUserIdAsync(long userId, CancellationToken ct = default);

    Task<PagedResult<Astrologer>> GetListingAsync(
        string? search,
        string? specialization,
        string? language,
        bool? isAvailable,
        string sortBy,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<Result> SetAvailabilityAsync(long astrologerId, bool isAvailable, CancellationToken ct = default);
    Task<Result> UpdateRatingAsync(long astrologerId, decimal newRating, CancellationToken ct = default);
}
