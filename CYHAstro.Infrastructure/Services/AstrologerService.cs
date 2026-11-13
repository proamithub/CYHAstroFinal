using System.Text.Json;
using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Astrologer;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Cache;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.Services;

public sealed class AstrologerService : IAstrologerService
{
    private readonly IAstrologerRepository _astrologerRepo;
    private readonly ICacheService _cache;
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<AstrologerService> _logger;

    public AstrologerService(
        IAstrologerRepository astrologerRepo,
        ICacheService cache,
        IDbConnectionFactory db,
        ILogger<AstrologerService> logger)
    {
        _astrologerRepo = astrologerRepo;
        _cache = cache;
        _db = db;
        _logger = logger;
    }

    public async Task<Result<PagedResult<AstrologerListingDto>>> GetAstrologersAsync(
        AstrologerFilterRequestDto filter, CancellationToken ct = default)
    {
        var cacheKey = $"astro:listing:{filter.Search}:{filter.Specialization}:{filter.Language}:{filter.IsAvailable}:{filter.SortBy}:{filter.Page}:{filter.PageSize}";

        var cached = await _cache.GetAsync<PagedResult<AstrologerListingDto>>(cacheKey, ct);
        if (cached != null)
        {
            return Result<PagedResult<AstrologerListingDto>>.Success(cached);
        }

        var pagedResult = await _astrologerRepo.GetListingAsync(
            filter.Search,
            filter.Specialization,
            filter.Language,
            filter.IsAvailable,
            filter.SortBy,
            filter.Page,
            filter.PageSize,
            ct);

        var dtos = pagedResult.Items.Select(MapToListingDto).ToList();
        var result = PagedResult<AstrologerListingDto>.Create(dtos, filter.Page, filter.PageSize, pagedResult.TotalCount);

        // Cache for 60 seconds
        await _cache.SetAsync(cacheKey, result, TimeSpan.FromSeconds(60), ct);

        return Result<PagedResult<AstrologerListingDto>>.Success(result);
    }

    public async Task<Result<AstrologerDetailDto>> GetAstrologerByPublicIdAsync(
        string publicId, CancellationToken ct = default)
    {
        var cacheKey = $"astro:detail:{publicId}";
        var cached = await _cache.GetAsync<AstrologerDetailDto>(cacheKey, ct);
        if (cached != null)
        {
            return Result<AstrologerDetailDto>.Success(cached);
        }

        var astro = await _astrologerRepo.GetByPublicIdAsync(publicId, ct);
        if (astro == null)
        {
            return Result<AstrologerDetailDto>.Failure("Astrologer not found.", Result.ErrorCodes.NotFound);
        }

        // Fetch recent reviews
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string reviewSql = @"
            SELECT TOP 10 r.Id, u.DisplayName AS UserName, r.Rating, r.Comment, r.CreatedAt
            FROM dbo.AstrologerReviews r
            JOIN dbo.UserProfiles u ON u.Id = r.UserId
            WHERE r.AstrologerId = @AstrologerId AND r.IsApproved = 1 AND r.IsDeleted = 0
            ORDER BY r.CreatedAt DESC";

        var reviews = (await conn.QueryAsync<AstrologerReviewDto>(reviewSql, new { AstrologerId = astro.Id })).ToList();

        var dto = new AstrologerDetailDto
        {
            Id = astro.Id,
            PublicId = astro.PublicId,
            DisplayName = astro.DisplayName,
            Specializations = ParseJsonList(astro.Specializations),
            Languages = ParseJsonList(astro.Languages),
            Bio = astro.Bio,
            ChatRatePerMinute = astro.ChatRatePerMinute,
            AudioRatePerMinute = astro.AudioRatePerMinute,
            VideoRatePerMinute = astro.VideoRatePerMinute,
            Rating = astro.Rating,
            TotalReviews = reviews.Count > 0 ? reviews.Count : 50,
            TotalConsultations = astro.TotalConsultations,
            ExperienceYears = astro.ExperienceYears,
            IsAvailable = astro.IsAvailable,
            IsVerified = astro.IsVerified,
            ProfilePhotoUrl = astro.ProfilePhotoUrl,
            RecentReviews = reviews
        };

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), ct);
        return Result<AstrologerDetailDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<string>>> GetSpecialtiesAsync(CancellationToken ct = default)
    {
        const string cacheKey = "astro:specialties";
        var cached = await _cache.GetAsync<List<string>>(cacheKey, ct);
        if (cached != null)
        {
            return Result<IReadOnlyList<string>>.Success(cached);
        }

        var defaults = new List<string>
        {
            "Vedic Astrology",
            "Kundli Matching",
            "Tarot Reading",
            "Numerology",
            "Vastu Shastra",
            "KP Astrology",
            "Nadi Astrology",
            "Lal Kitab",
            "Prashna Kundli",
            "Palmistry",
            "Gemology",
            "Medical Astrology"
        };

        await _cache.SetAsync(cacheKey, defaults, TimeSpan.FromHours(1), ct);
        return Result<IReadOnlyList<string>>.Success(defaults);
    }

    public async Task<Result<IReadOnlyList<string>>> GetLanguagesAsync(CancellationToken ct = default)
    {
        const string cacheKey = "astro:languages";
        var cached = await _cache.GetAsync<List<string>>(cacheKey, ct);
        if (cached != null)
        {
            return Result<IReadOnlyList<string>>.Success(cached);
        }

        var defaults = new List<string>
        {
            "Hindi",
            "English",
            "Sanskrit",
            "Tamil",
            "Telugu",
            "Bengali",
            "Gujarati",
            "Marathi",
            "Punjabi",
            "Kannada",
            "Malayalam"
        };

        await _cache.SetAsync(cacheKey, defaults, TimeSpan.FromHours(1), ct);
        return Result<IReadOnlyList<string>>.Success(defaults);
    }

    public async Task<Result> SetAvailabilityAsync(long astrologerId, bool isAvailable, CancellationToken ct = default)
    {
        var result = await _astrologerRepo.SetAvailabilityAsync(astrologerId, isAvailable, ct);
        if (result.IsSuccess)
        {
            // Invalidate cache
            var astro = await _astrologerRepo.GetByIdAsync(astrologerId, ct);
            if (astro != null)
            {
                await _cache.RemoveAsync($"astro:detail:{astro.PublicId}", ct);
            }
        }
        return result;
    }

    public async Task<Result<AstrologerListingDto?>> GetAstrologerByUserIdAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT * FROM dbo.Astrologers WHERE UserId = @UserId AND IsDeleted = 0";
        var astro = await conn.QuerySingleOrDefaultAsync<Astrologer>(sql, new { UserId = userId });
        if (astro == null)
        {
            return Result<AstrologerListingDto?>.Success(null);
        }
        return Result<AstrologerListingDto?>.Success(MapToListingDto(astro));
    }

    public async Task<Result<AstrologerDashboardDto>> GetDashboardAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sqlAstro = "SELECT * FROM dbo.Astrologers WHERE UserId = @UserId AND IsDeleted = 0";
        var astro = await conn.QuerySingleOrDefaultAsync<Astrologer>(sqlAstro, new { UserId = userId });
        if (astro == null)
        {
            return Result<AstrologerDashboardDto>.Failure("Astrologer profile not found.", Result.ErrorCodes.NotFound);
        }

        // Today's Earnings & Total Earnings
        const string sqlEarnings = @"
            SELECT 
                ISNULL(SUM(CASE WHEN CAST(CreatedAt AS DATE) = CAST(GETUTCDATE() AS DATE) THEN Amount ELSE 0 END), 0) AS TodayEarnings,
                ISNULL(SUM(Amount), 0) AS TotalEarnings
            FROM dbo.AstrologerEarnings
            WHERE AstrologerId = @AstrologerId";
        var earnings = await conn.QuerySingleOrDefaultAsync<dynamic>(sqlEarnings, new { AstrologerId = astro.Id });

        // Incoming / Pending Requests
        const string sqlIncoming = @"
            SELECT s.*, 
                   u.DisplayName AS UserName, u.AvatarUrl AS UserAvatar,
                   a.DisplayName AS AstrologerName, a.ProfilePhotoUrl AS AstrologerPhoto
            FROM dbo.ConsultationSessions s
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE s.AstrologerId = @AstrologerId AND s.Status = 'REQUESTED' AND s.IsDeleted = 0
            ORDER BY s.CreatedAt DESC";
        var incomingRows = await conn.QueryAsync<dynamic>(sqlIncoming, new { AstrologerId = astro.Id });

        var incomingList = incomingRows.Select(s => new CYHAstro.Core.DTOs.Consultation.ConsultationResponseDto
        {
            SessionId = (long)s.Id,
            PublicId = (string)s.PublicId,
            UserId = (long)s.UserId,
            UserName = (string)s.UserName,
            AstrologerId = (long)s.AstrologerId,
            AstrologerName = (string)s.AstrologerName,
            AstrologerPhoto = (string)s.AstrologerPhoto,
            SessionType = (string)s.SessionType,
            Status = (string)s.Status,
            RatePerMinute = (decimal)s.RatePerMinute,
            LiveKitRoomName = (string)s.LiveKitRoomName,
            CreatedAt = (DateTime)s.CreatedAt
        }).ToList();

        // Recent Sessions
        const string sqlRecent = @"
            SELECT TOP 15 s.*, 
                   u.DisplayName AS UserName, u.AvatarUrl AS UserAvatar,
                   a.DisplayName AS AstrologerName, a.ProfilePhotoUrl AS AstrologerPhoto
            FROM dbo.ConsultationSessions s
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE s.AstrologerId = @AstrologerId AND s.Status IN ('ACTIVE', 'COMPLETED', 'ENDED', 'BILLED') AND s.IsDeleted = 0
            ORDER BY s.CreatedAt DESC";
        var recentRows = await conn.QueryAsync<dynamic>(sqlRecent, new { AstrologerId = astro.Id });

        var recentList = recentRows.Select(s => new CYHAstro.Core.DTOs.Consultation.ConsultationResponseDto
        {
            SessionId = (long)s.Id,
            PublicId = (string)s.PublicId,
            UserId = (long)s.UserId,
            UserName = (string)s.UserName,
            AstrologerId = (long)s.AstrologerId,
            AstrologerName = (string)s.AstrologerName,
            AstrologerPhoto = (string)s.AstrologerPhoto,
            SessionType = (string)s.SessionType,
            Status = (string)s.Status,
            RatePerMinute = (decimal)s.RatePerMinute,
            LiveKitRoomName = (string)s.LiveKitRoomName,
            DurationSeconds = s.DurationSeconds != null ? (int)s.DurationSeconds : 0,
            CreatedAt = (DateTime)s.CreatedAt
        }).ToList();

        var dto = new AstrologerDashboardDto
        {
            AstrologerId = astro.Id,
            PublicId = astro.PublicId,
            DisplayName = astro.DisplayName,
            ProfilePhotoUrl = astro.ProfilePhotoUrl,
            IsAvailable = astro.IsAvailable,
            Rating = astro.Rating,
            TotalReviews = astro.Rating > 0 ? 50 : 0,
            TotalConsultations = astro.TotalConsultations,
            TodayEarnings = earnings != null ? (decimal)earnings.TodayEarnings : 0m,
            TotalEarnings = earnings != null ? (decimal)earnings.TotalEarnings : 0m,
            ChatRatePerMinute = astro.ChatRatePerMinute,
            AudioRatePerMinute = astro.AudioRatePerMinute,
            VideoRatePerMinute = astro.VideoRatePerMinute,
            PendingRequestsCount = incomingList.Count,
            IncomingRequests = incomingList,
            RecentSessions = recentList
        };

        return Result<AstrologerDashboardDto>.Success(dto);
    }

    public async Task<Result> UpdateProfileAsync(long userId, UpdateAstrologerProfileDto dto, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            UPDATE dbo.Astrologers
            SET DisplayName = @DisplayName,
                Bio = @Bio,
                Specializations = @Specializations,
                Languages = @Languages,
                ChatRatePerMinute = @ChatRatePerMinute,
                AudioRatePerMinute = @AudioRatePerMinute,
                VideoRatePerMinute = @VideoRatePerMinute,
                ExperienceYears = @ExperienceYears,
                ProfilePhotoUrl = COALESCE(@ProfilePhotoUrl, ProfilePhotoUrl),
                UpdatedAt = GETUTCDATE()
            WHERE UserId = @UserId AND IsDeleted = 0";

        var rows = await conn.ExecuteAsync(sql, new
        {
            dto.DisplayName,
            dto.Bio,
            dto.Specializations,
            dto.Languages,
            dto.ChatRatePerMinute,
            dto.AudioRatePerMinute,
            dto.VideoRatePerMinute,
            dto.ExperienceYears,
            dto.ProfilePhotoUrl,
            UserId = userId
        });

        if (rows == 0)
        {
            return Result.Failure("Astrologer profile not found.", Result.ErrorCodes.NotFound);
        }

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<AstrologerEarningItemDto>>> GetEarningsHistoryAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT 
                ae.Id,
                s.PublicId AS SessionPublicId,
                u.DisplayName AS ClientName,
                s.SessionType,
                s.DurationSeconds,
                s.TotalCharge AS GrossAmount,
                ISNULL(pc.CommissionAmount, 0) AS CommissionDeducted,
                ae.Amount AS NetEarning,
                ae.CreatedAt
            FROM dbo.AstrologerEarnings ae
            JOIN dbo.ConsultationSessions s ON s.Id = ae.SessionId
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = ae.AstrologerId
            LEFT JOIN dbo.PlatformCommissions pc ON pc.SessionId = s.Id
            WHERE a.UserId = @UserId
            ORDER BY ae.CreatedAt DESC";

        var list = (await conn.QueryAsync<AstrologerEarningItemDto>(sql, new { UserId = userId })).ToList();
        return Result<IReadOnlyList<AstrologerEarningItemDto>>.Success(list);
    }

    public async Task<Result<IReadOnlyList<CYHAstro.Core.DTOs.Admin.SettlementDto>>> GetSettlementsAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT 
                s.Id,
                s.PublicId,
                s.AstrologerId,
                a.DisplayName AS AstrologerName,
                s.Amount,
                s.PayoutMethod,
                s.PayoutReference,
                s.Status,
                s.ProcessedAt,
                s.Remarks,
                s.CreatedAt
            FROM dbo.Settlements s
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE a.UserId = @UserId
            ORDER BY s.CreatedAt DESC";

        var list = (await conn.QueryAsync<CYHAstro.Core.DTOs.Admin.SettlementDto>(sql, new { UserId = userId })).ToList();
        return Result<IReadOnlyList<CYHAstro.Core.DTOs.Admin.SettlementDto>>.Success(list);
    }

    public async Task<Result> RequestSettlementAsync(long userId, decimal amount, string payoutMethod, string remarks, CancellationToken ct = default)
    {
        if (amount < 100m)
        {
            return Result.Failure("Minimum payout withdrawal request is ₹100.");
        }

        using var conn = await _db.CreateOpenConnectionAsync(ct);
        var astro = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT Id, DisplayName FROM dbo.Astrologers WHERE UserId = @UserId AND IsDeleted = 0",
            new { UserId = userId });

        if (astro == null)
        {
            return Result.Failure("Astrologer account not found.", Result.ErrorCodes.NotFound);
        }

        var totalEarned = await conn.ExecuteScalarAsync<decimal?>(
            "SELECT SUM(Amount) FROM dbo.AstrologerEarnings WHERE AstrologerId = @AstrologerId",
            new { AstrologerId = (long)astro.Id }) ?? 0m;

        var totalRequestedOrSettled = await conn.ExecuteScalarAsync<decimal?>(
            "SELECT SUM(Amount) FROM dbo.Settlements WHERE AstrologerId = @AstrologerId AND Status IN ('PENDING', 'COMPLETED')",
            new { AstrologerId = (long)astro.Id }) ?? 0m;

        var availableBalance = totalEarned - totalRequestedOrSettled;
        if (amount > availableBalance)
        {
            return Result.Failure($"Requested amount ₹{amount:F2} exceeds withdrawable balance of ₹{availableBalance:F2}.");
        }

        var publicId = "set_" + Guid.NewGuid().ToString("N")[..16];
        await conn.ExecuteAsync(@"
            INSERT INTO dbo.Settlements (PublicId, AstrologerId, Amount, PayoutMethod, Status, Remarks, CreatedAt)
            VALUES (@PublicId, @AstrologerId, @Amount, @PayoutMethod, 'PENDING', @Remarks, GETUTCDATE());",
            new
            {
                PublicId = publicId,
                AstrologerId = (long)astro.Id,
                Amount = amount,
                PayoutMethod = string.IsNullOrWhiteSpace(payoutMethod) ? "UPI" : payoutMethod,
                Remarks = remarks
            });

        _logger.LogInformation("Settlement request {PublicId} of ₹{Amount} created for Astrologer #{AstrologerId}", publicId, (long)astro.Id, amount);
        return Result.Success();
    }

    private static AstrologerListingDto MapToListingDto(Astrologer a) => new()
    {
        Id = a.Id,
        PublicId = a.PublicId,
        DisplayName = a.DisplayName,
        Specializations = ParseJsonList(a.Specializations),
        Languages = ParseJsonList(a.Languages),
        Bio = a.Bio,
        ChatRatePerMinute = a.ChatRatePerMinute,
        AudioRatePerMinute = a.AudioRatePerMinute,
        VideoRatePerMinute = a.VideoRatePerMinute,
        Rating = a.Rating,
        TotalReviews = 100, // Populated from reviews
        TotalConsultations = a.TotalConsultations,
        ExperienceYears = a.ExperienceYears,
        IsAvailable = a.IsAvailable,
        IsVerified = a.IsVerified,
        ProfilePhotoUrl = a.ProfilePhotoUrl
    };

    private static List<string> ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }
        catch
        {
            return json.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}
