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
    private readonly DbConnectionFactory _db;
    private readonly ILogger<AstrologerService> _logger;

    public AstrologerService(
        IAstrologerRepository astrologerRepo,
        ICacheService cache,
        DbConnectionFactory db,
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
