using CYHAstro.Core.Common;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Infrastructure.Data;
using Dapper;
using System.Data;

namespace CYHAstro.Infrastructure.Repositories;

/// <summary>
/// Dapper-based birth profile repository.
/// Enforces ownership checks on all mutations.
/// </summary>
public sealed class BirthProfileRepository : IBirthProfileRepository
{
    private readonly IDbConnectionFactory _db;

    public BirthProfileRepository(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<BirthProfile?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<BirthProfile>(
            "SELECT * FROM BirthProfiles WHERE Id = @Id AND IsDeleted = 0",
            new { Id = id });
    }

    public async Task<BirthProfile?> GetByPublicIdAsync(string publicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<BirthProfile>(
            "SELECT * FROM BirthProfiles WHERE PublicId = @PublicId AND IsDeleted = 0",
            new { PublicId = publicId });
    }

    public async Task<IReadOnlyList<BirthProfile>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<BirthProfile>(
            "SELECT * FROM BirthProfiles WHERE UserId = @UserId AND IsDeleted = 0 ORDER BY IsPrimary DESC, CreatedAt ASC",
            new { UserId = userId });
        return rows.ToList().AsReadOnly();
    }

    public async Task<BirthProfile?> GetPrimaryAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<BirthProfile>(
            "SELECT TOP 1 * FROM BirthProfiles WHERE UserId = @UserId AND IsPrimary = 1 AND IsDeleted = 0",
            new { UserId = userId });
    }

    public async Task<long> CreateAsync(BirthProfile profile, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(
            "usp_CreateBirthProfile",
            new
            {
                profile.UserId,
                profile.PublicId,
                profile.Name,
                profile.Gender,
                profile.DateOfBirth,
                profile.TimeOfBirth,
                profile.IsTimeApproximate,
                profile.BirthPlace,
                profile.Latitude,
                profile.Longitude,
                profile.TimeZoneId,
                profile.UtcOffsetMinutes,
                profile.IsPrimary,
                profile.RelationLabel
            },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Result> UpdateAsync(BirthProfile profile, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var rows = await conn.ExecuteAsync(
            "usp_UpdateBirthProfile",
            new
            {
                profile.Id,
                profile.UserId,
                profile.Name,
                profile.Gender,
                profile.DateOfBirth,
                profile.TimeOfBirth,
                profile.IsTimeApproximate,
                profile.BirthPlace,
                profile.Latitude,
                profile.Longitude,
                profile.TimeZoneId,
                profile.UtcOffsetMinutes,
                profile.RelationLabel
            },
            commandType: CommandType.StoredProcedure);
        return rows > 0 ? Result.Success() : Result.Failure("Profile not found.", Result.ErrorCodes.NotFound);
    }

    public async Task<Result> DeleteAsync(long id, long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        // Ownership check is embedded in the WHERE clause — server-side, not client-trusted
        var rows = await conn.ExecuteAsync(
            "UPDATE BirthProfiles SET IsDeleted = 1, DeletedAt = GETUTCDATE() WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
        return rows > 0 ? Result.Success() : Result.Failure("Profile not found or access denied.", Result.ErrorCodes.Forbidden);
    }
}
