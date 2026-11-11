using CYHAstro.Core.Common;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace CYHAstro.Infrastructure.Repositories;

/// <summary>
/// Dapper-based astrologer repository with Redis caching for listings.
/// </summary>
public sealed class AstrologerRepository : IAstrologerRepository
{
    private readonly IDbConnectionFactory _db;

    public AstrologerRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<Astrologer?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Astrologer>(
            "SELECT * FROM Astrologers WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
    }

    public async Task<Astrologer?> GetByPublicIdAsync(string publicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Astrologer>(
            "SELECT * FROM Astrologers WHERE PublicId = @PublicId AND IsDeleted = 0",
            new { PublicId = publicId });
    }

    public async Task<Astrologer?> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Astrologer>(
            "SELECT * FROM Astrologers WHERE UserId = @UserId AND IsDeleted = 0",
            new { UserId = userId });
    }

    public async Task<PagedResult<Astrologer>> GetListingAsync(
        string? search,
        string? specialization,
        string? language,
        bool? isAvailable,
        string sortBy,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        using var multi = await conn.QueryMultipleAsync(
            "usp_GetAstrologerListing",
            new
            {
                Search = search,
                Specialization = specialization,
                Language = language,
                IsAvailable = isAvailable,
                SortBy = sortBy,
                Page = page,
                PageSize = pageSize
            },
            commandType: CommandType.StoredProcedure);

        var items = (await multi.ReadAsync<Astrologer>()).ToList();
        var total = await multi.ReadSingleAsync<int>();
        return PagedResult<Astrologer>.Create(items.AsReadOnly(), total, page, pageSize);
    }

    public async Task<Result> SetAvailabilityAsync(long astrologerId, bool isAvailable, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            "UPDATE Astrologers SET IsAvailable = @IsAvailable, UpdatedAt = GETUTCDATE() WHERE Id = @Id",
            new { Id = astrologerId, IsAvailable = isAvailable });
        return rows > 0 ? Result.Success() : Result.Failure("Astrologer not found.", Result.ErrorCodes.NotFound);
    }

    public async Task<Result> UpdateRatingAsync(long astrologerId, decimal newRating, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            "usp_UpdateAstrologerRating",
            new { AstrologerId = astrologerId, NewRating = newRating },
            commandType: CommandType.StoredProcedure);
        return rows > 0 ? Result.Success() : Result.Failure("Astrologer not found.", Result.ErrorCodes.NotFound);
    }
}
