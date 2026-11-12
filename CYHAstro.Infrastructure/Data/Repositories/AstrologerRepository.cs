using System.Data;
using CYHAstro.Core.Common;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using Dapper;

namespace CYHAstro.Infrastructure.Data.Repositories;

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
        const string sql = "SELECT * FROM dbo.Astrologers WHERE Id = @Id AND IsDeleted = 0";
        return await conn.QuerySingleOrDefaultAsync<Astrologer>(sql, new { Id = id });
    }

    public async Task<Astrologer?> GetByPublicIdAsync(string publicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT * FROM dbo.Astrologers WHERE PublicId = @PublicId AND IsDeleted = 0";
        return await conn.QuerySingleOrDefaultAsync<Astrologer>(sql, new { PublicId = publicId });
    }

    public async Task<Astrologer?> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT * FROM dbo.Astrologers WHERE UserId = @UserId AND IsDeleted = 0";
        return await conn.QuerySingleOrDefaultAsync<Astrologer>(sql, new { UserId = userId });
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
            "dbo.usp_GetAstrologerListing",
            new
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search,
                Specialization = string.IsNullOrWhiteSpace(specialization) ? null : specialization,
                Language = string.IsNullOrWhiteSpace(language) ? null : language,
                IsAvailable = isAvailable,
                SortBy = string.IsNullOrWhiteSpace(sortBy) ? "RATING_DESC" : sortBy,
                Page = page < 1 ? 1 : page,
                PageSize = pageSize < 1 ? 20 : pageSize
            },
            commandType: CommandType.StoredProcedure);

        var items = (await multi.ReadAsync<Astrologer>()).ToList();
        var totalCount = await multi.ReadSingleOrDefaultAsync<int>();

        return PagedResult<Astrologer>.Create(items, page, pageSize, totalCount);
    }

    public async Task<Result> SetAvailabilityAsync(long astrologerId, bool isAvailable, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            UPDATE dbo.Astrologers 
            SET IsAvailable = @IsAvailable, UpdatedAt = GETUTCDATE() 
            WHERE Id = @Id AND IsDeleted = 0";
        var rows = await conn.ExecuteAsync(sql, new { Id = astrologerId, IsAvailable = isAvailable });
        return rows > 0 
            ? Result.Success() 
            : Result.Failure("Astrologer not found.", Result.ErrorCodes.NotFound);
    }

    public async Task<Result> UpdateRatingAsync(long astrologerId, decimal newRating, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            "dbo.usp_UpdateAstrologerRating",
            new { AstrologerId = astrologerId, NewRating = newRating },
            commandType: CommandType.StoredProcedure);

        return rows > 0 
            ? Result.Success() 
            : Result.Failure("Astrologer not found.", Result.ErrorCodes.NotFound);
    }
}
