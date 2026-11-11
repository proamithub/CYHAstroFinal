using System.Data;
using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Auth;
using CYHAstro.Core.DTOs.User;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.Services;

public sealed class ProfileService : IProfileService
{
    private readonly DbConnectionFactory _db;
    private readonly IBirthProfileRepository _birthRepo;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        DbConnectionFactory db,
        IBirthProfileRepository birthRepo,
        ILogger<ProfileService> logger)
    {
        _db = db;
        _birthRepo = birthRepo;
        _logger = logger;
    }

    public async Task<Result<UserProfileDto>> GetProfileAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT p.Id, p.PublicId, p.DisplayName, p.Email, p.PhoneNumber, p.AvatarUrl,
                   (w.PaidBalance + w.BonusBalance) AS WalletBalance
            FROM dbo.UserProfiles p
            LEFT JOIN dbo.Wallets w ON w.UserId = p.Id AND w.IsDeleted = 0
            WHERE p.Id = @UserId AND p.IsDeleted = 0";

        var profile = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { UserId = userId });
        if (profile == null)
        {
            return Result<UserProfileDto>.Failure("User profile not found.", Result.ErrorCodes.NotFound);
        }

        var dto = new UserProfileDto
        {
            Id = profile.Id,
            PublicId = profile.PublicId,
            DisplayName = profile.DisplayName,
            Email = profile.Email,
            PhoneNumber = profile.PhoneNumber,
            AvatarUrl = profile.AvatarUrl,
            WalletBalance = profile.WalletBalance ?? 0.00m,
            Roles = new List<string> { "User" }
        };

        return Result<UserProfileDto>.Success(dto);
    }

    public async Task<Result<UserProfileDto>> UpdateProfileAsync(
        long userId, UserProfileUpdateDto request, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            UPDATE dbo.UserProfiles
            SET DisplayName = COALESCE(NULLIF(@DisplayName, ''), DisplayName),
                PhoneNumber = COALESCE(@PhoneNumber, PhoneNumber),
                Gender = COALESCE(@Gender, Gender),
                AvatarUrl = COALESCE(@AvatarUrl, AvatarUrl),
                PreferredLanguage = COALESCE(@PreferredLanguage, PreferredLanguage),
                UpdatedAt = GETUTCDATE()
            WHERE Id = @UserId AND IsDeleted = 0";

        var rows = await conn.ExecuteAsync(sql, new
        {
            UserId = userId,
            request.DisplayName,
            request.PhoneNumber,
            request.Gender,
            request.AvatarUrl,
            request.PreferredLanguage
        });

        if (rows == 0)
        {
            return Result<UserProfileDto>.Failure("User profile not found.", Result.ErrorCodes.NotFound);
        }

        return await GetProfileAsync(userId, ct);
    }

    public async Task<Result<IReadOnlyList<BirthProfileDto>>> GetBirthProfilesAsync(long userId, CancellationToken ct = default)
    {
        var list = await _birthRepo.GetByUserIdAsync(userId, ct);
        var dtos = list.Select(MapToDto).ToList();
        return Result<IReadOnlyList<BirthProfileDto>>.Success(dtos);
    }

    public async Task<Result<BirthProfileDto>> GetBirthProfileByIdAsync(long profileId, long userId, CancellationToken ct = default)
    {
        var profile = await _birthRepo.GetByIdAsync(profileId, ct);
        if (profile == null || profile.UserId != userId)
        {
            return Result<BirthProfileDto>.Failure("Birth profile not found.", Result.ErrorCodes.NotFound);
        }

        return Result<BirthProfileDto>.Success(MapToDto(profile));
    }

    public async Task<Result<BirthProfileDto>> CreateBirthProfileAsync(
        long userId, CreateBirthProfileDto request, CancellationToken ct = default)
    {
        var entity = new BirthProfile
        {
            UserId = userId,
            PublicId = "bpf_" + Guid.NewGuid().ToString("N")[..16],
            Name = request.Name,
            Gender = request.Gender,
            DateOfBirth = DateOnly.FromDateTime(request.DateOfBirth),
            TimeOfBirth = request.TimeOfBirth.HasValue ? TimeOnly.FromTimeSpan(request.TimeOfBirth.Value) : null,
            IsTimeApproximate = request.IsTimeApproximate,
            BirthPlace = request.BirthPlace,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TimeZoneId = request.TimeZoneId,
            UtcOffsetMinutes = request.UtcOffsetMinutes,
            IsPrimary = request.IsPrimary,
            RelationLabel = request.RelationLabel
        };

        var newId = await _birthRepo.CreateAsync(entity, ct);
        entity.Id = newId;

        return Result<BirthProfileDto>.Success(MapToDto(entity));
    }

    public async Task<Result<BirthProfileDto>> UpdateBirthProfileAsync(
        long profileId, long userId, UpdateBirthProfileDto request, CancellationToken ct = default)
    {
        var existing = await _birthRepo.GetByIdAsync(profileId, ct);
        if (existing == null || existing.UserId != userId)
        {
            return Result<BirthProfileDto>.Failure("Birth profile not found.", Result.ErrorCodes.NotFound);
        }

        existing.Name = request.Name;
        existing.Gender = request.Gender;
        existing.DateOfBirth = DateOnly.FromDateTime(request.DateOfBirth);
        existing.TimeOfBirth = request.TimeOfBirth.HasValue ? TimeOnly.FromTimeSpan(request.TimeOfBirth.Value) : null;
        existing.IsTimeApproximate = request.IsTimeApproximate;
        existing.BirthPlace = request.BirthPlace;
        existing.Latitude = request.Latitude;
        existing.Longitude = request.Longitude;
        existing.TimeZoneId = request.TimeZoneId;
        existing.UtcOffsetMinutes = request.UtcOffsetMinutes;
        existing.IsPrimary = request.IsPrimary;
        existing.RelationLabel = request.RelationLabel;

        var result = await _birthRepo.UpdateAsync(existing, ct);
        if (!result.IsSuccess)
        {
            return Result<BirthProfileDto>.Failure(result.Error!, result.ErrorCode);
        }

        return Result<BirthProfileDto>.Success(MapToDto(existing));
    }

    public async Task<Result> DeleteBirthProfileAsync(long profileId, long userId, CancellationToken ct = default)
    {
        return await _birthRepo.DeleteAsync(profileId, userId, ct);
    }

    public async Task<Result<UserPreferenceDto>> GetPreferencesAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT * FROM dbo.UserPreferences WHERE UserId = @UserId";
        var prefs = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { UserId = userId });

        if (prefs == null)
        {
            return Result<UserPreferenceDto>.Success(new UserPreferenceDto());
        }

        return Result<UserPreferenceDto>.Success(new UserPreferenceDto
        {
            ReceiveDailyNotification = prefs.ReceiveDailyNotification,
            ReceivePromoAlerts = prefs.ReceivePromoAlerts,
            PreferredChartStyle = prefs.PreferredChartStyle,
            PreferredAyanamsha = prefs.PreferredAyanamsha,
            ThemeMode = prefs.ThemeMode
        });
    }

    public async Task<Result<UserPreferenceDto>> UpdatePreferencesAsync(
        long userId, UserPreferenceDto request, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            IF EXISTS (SELECT 1 FROM dbo.UserPreferences WHERE UserId = @UserId)
            BEGIN
                UPDATE dbo.UserPreferences
                SET ReceiveDailyNotification = @ReceiveDailyNotification,
                    ReceivePromoAlerts = @ReceivePromoAlerts,
                    PreferredChartStyle = @PreferredChartStyle,
                    PreferredAyanamsha = @PreferredAyanamsha,
                    ThemeMode = @ThemeMode,
                    UpdatedAt = GETUTCDATE()
                WHERE UserId = @UserId;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.UserPreferences (
                    UserId, ReceiveDailyNotification, ReceivePromoAlerts,
                    PreferredChartStyle, PreferredAyanamsha, ThemeMode, CreatedAt
                )
                VALUES (
                    @UserId, @ReceiveDailyNotification, @ReceivePromoAlerts,
                    @PreferredChartStyle, @PreferredAyanamsha, @ThemeMode, GETUTCDATE()
                );
            END";

        await conn.ExecuteAsync(sql, new
        {
            UserId = userId,
            request.ReceiveDailyNotification,
            request.ReceivePromoAlerts,
            request.PreferredChartStyle,
            request.PreferredAyanamsha,
            request.ThemeMode
        });

        return Result<UserPreferenceDto>.Success(request);
    }

    private static BirthProfileDto MapToDto(BirthProfile p) => new()
    {
        Id = p.Id,
        PublicId = p.PublicId,
        UserId = p.UserId,
        Name = p.Name,
        Gender = p.Gender,
        DateOfBirth = p.DateOfBirth.ToDateTime(TimeOnly.MinValue),
        TimeOfBirth = p.TimeOfBirth?.ToTimeSpan(),
        IsTimeApproximate = p.IsTimeApproximate,
        BirthPlace = p.BirthPlace,
        Latitude = p.Latitude,
        Longitude = p.Longitude,
        TimeZoneId = p.TimeZoneId,
        UtcOffsetMinutes = p.UtcOffsetMinutes,
        IsPrimary = p.IsPrimary,
        RelationLabel = p.RelationLabel,
        CreatedAt = p.CreatedAt
    };
}
