using System.Data;
using CYHAstro.Core.Common;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using Dapper;

namespace CYHAstro.Infrastructure.Data.Repositories;

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
        const string sql = "SELECT * FROM dbo.BirthProfiles WHERE Id = @Id AND IsDeleted = 0";
        return await conn.QuerySingleOrDefaultAsync<BirthProfile>(sql, new { Id = id });
    }

    public async Task<BirthProfile?> GetByPublicIdAsync(string publicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT * FROM dbo.BirthProfiles WHERE PublicId = @PublicId AND IsDeleted = 0";
        return await conn.QuerySingleOrDefaultAsync<BirthProfile>(sql, new { PublicId = publicId });
    }

    public async Task<IReadOnlyList<BirthProfile>> GetByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT * FROM dbo.BirthProfiles 
            WHERE UserId = @UserId AND IsDeleted = 0 
            ORDER BY IsPrimary DESC, CreatedAt DESC";
        var result = await conn.QueryAsync<BirthProfile>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<BirthProfile?> GetPrimaryAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT TOP 1 * FROM dbo.BirthProfiles 
            WHERE UserId = @UserId AND IsPrimary = 1 AND IsDeleted = 0";
        return await conn.QuerySingleOrDefaultAsync<BirthProfile>(sql, new { UserId = userId });
    }

    public async Task<long> CreateAsync(BirthProfile profile, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        if (string.IsNullOrWhiteSpace(profile.PublicId))
        {
            profile.PublicId = "bpf_" + Guid.NewGuid().ToString("N")[..16];
        }

        var newId = await conn.QuerySingleAsync<long>(
            "dbo.usp_CreateBirthProfile",
            new
            {
                UserId = profile.UserId,
                PublicId = profile.PublicId,
                Name = profile.Name,
                Gender = profile.Gender,
                DateOfBirth = profile.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                TimeOfBirth = profile.TimeOfBirth.HasValue ? profile.TimeOfBirth.Value.ToTimeSpan() : (TimeSpan?)null,
                IsTimeApproximate = profile.IsTimeApproximate,
                BirthPlace = profile.BirthPlace,
                Latitude = profile.Latitude,
                Longitude = profile.Longitude,
                TimeZoneId = profile.TimeZoneId,
                UtcOffsetMinutes = profile.UtcOffsetMinutes,
                IsPrimary = profile.IsPrimary,
                RelationLabel = profile.RelationLabel
            },
            commandType: CommandType.StoredProcedure);

        return newId;
    }

    public async Task<Result> UpdateAsync(BirthProfile profile, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        var rows = await conn.ExecuteAsync(
            "dbo.usp_UpdateBirthProfile",
            new
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Name = profile.Name,
                Gender = profile.Gender,
                DateOfBirth = profile.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                TimeOfBirth = profile.TimeOfBirth.HasValue ? profile.TimeOfBirth.Value.ToTimeSpan() : (TimeSpan?)null,
                IsTimeApproximate = profile.IsTimeApproximate,
                BirthPlace = profile.BirthPlace,
                Latitude = profile.Latitude,
                Longitude = profile.Longitude,
                TimeZoneId = profile.TimeZoneId,
                UtcOffsetMinutes = profile.UtcOffsetMinutes,
                RelationLabel = profile.RelationLabel
            },
            commandType: CommandType.StoredProcedure);

        return rows > 0 
            ? Result.Success() 
            : Result.Failure("Birth profile not found or access denied.", Result.ErrorCodes.NotFound);
    }

    public async Task<Result> DeleteAsync(long id, long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            UPDATE dbo.BirthProfiles 
            SET IsDeleted = 1, DeletedAt = GETUTCDATE() 
            WHERE Id = @Id AND UserId = @UserId AND IsDeleted = 0";

        var rows = await conn.ExecuteAsync(sql, new { Id = id, UserId = userId });
        return rows > 0 
            ? Result.Success() 
            : Result.Failure("Birth profile not found or access denied.", Result.ErrorCodes.NotFound);
    }
}
