using CYHAstro.Core.Common;
using CYHAstro.Core.Entities;

namespace CYHAstro.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for birth profiles.
/// </summary>
public interface IBirthProfileRepository
{
    Task<BirthProfile?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<BirthProfile?> GetByPublicIdAsync(string publicId, CancellationToken ct = default);
    Task<IReadOnlyList<BirthProfile>> GetByUserIdAsync(long userId, CancellationToken ct = default);
    Task<BirthProfile?> GetPrimaryAsync(long userId, CancellationToken ct = default);
    Task<long> CreateAsync(BirthProfile profile, CancellationToken ct = default);
    Task<Result> UpdateAsync(BirthProfile profile, CancellationToken ct = default);
    Task<Result> DeleteAsync(long id, long userId, CancellationToken ct = default); // Soft delete with ownership check
}
