using System.Data;
using System.Text.Json;
using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Scans;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.Data.Repositories;

public sealed class AstroScanRepository : IAstroScanRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<AstroScanRepository> _logger;

    public AstroScanRepository(IDbConnectionFactory db, ILogger<AstroScanRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<long> InsertScanAsync(
        string publicId,
        long userId,
        long? birthProfileId,
        string scanType,
        string? handType,
        string imageUrl,
        string status,
        decimal? confidenceScore,
        string extractedFeaturesJson,
        string analysisReportJson,
        decimal costCharged,
        int tokensUsed,
        string language,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            INSERT INTO dbo.AstroScans (
                PublicId, UserId, BirthProfileId, ScanType, HandType, ImageUrl,
                Status, ConfidenceScore, ExtractedFeaturesJson, AnalysisReportJson,
                CostCharged, TokensUsed, Language, CreatedAt
            )
            VALUES (
                @PublicId, @UserId, @BirthProfileId, @ScanType, @HandType, @ImageUrl,
                @Status, @ConfidenceScore, @ExtractedFeaturesJson, @AnalysisReportJson,
                @CostCharged, @TokensUsed, @Language, GETUTCDATE()
            );
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        var id = await conn.ExecuteScalarAsync<long>(sql, new
        {
            PublicId = publicId,
            UserId = userId,
            BirthProfileId = birthProfileId,
            ScanType = scanType,
            HandType = handType,
            ImageUrl = imageUrl,
            Status = status,
            ConfidenceScore = confidenceScore,
            ExtractedFeaturesJson = extractedFeaturesJson,
            AnalysisReportJson = analysisReportJson,
            CostCharged = costCharged,
            TokensUsed = tokensUsed,
            Language = language
        });

        return id;
    }

    public async Task<AstroScanDetailDto?> GetByPublicIdAsync(string publicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT s.Id, s.PublicId, s.UserId, s.BirthProfileId, s.ScanType, s.HandType,
                   s.ImageUrl, s.Status, s.ConfidenceScore, s.AnalysisReportJson,
                   s.AdminNotes, s.IsAdminReviewed, s.CostCharged, s.TokensUsed,
                   s.Language, s.CreatedAt, s.UpdatedAt,
                   u.DisplayName AS UserName,
                   u.Email AS UserEmail,
                   u.PhoneNumber AS UserPhone,
                   p.Name AS BirthProfileName
            FROM dbo.AstroScans s
            INNER JOIN dbo.UserProfiles u ON s.UserId = u.Id
            LEFT JOIN dbo.BirthProfiles p ON s.BirthProfileId = p.Id
            WHERE s.PublicId = @PublicId;";

        var row = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { PublicId = publicId });
        if (row == null) return null;

        return MapToDetailDto(row);
    }

    public async Task<List<AstroScanListItemDto>> GetUserScansAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT Id, PublicId, ScanType, HandType, ImageUrl, AnalysisReportJson,
                   CostCharged, CreatedAt, IsAdminReviewed
            FROM dbo.AstroScans
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC;";

        var rows = await conn.QueryAsync<dynamic>(sql, new { UserId = userId });
        var list = new List<AstroScanListItemDto>();

        foreach (var r in rows)
        {
            var report = ParseReport(r.AnalysisReportJson != null ? (string)r.AnalysisReportJson : string.Empty);
            list.Add(new AstroScanListItemDto
            {
                Id = (long)r.Id,
                PublicId = (string)r.PublicId,
                ScanType = (string)r.ScanType,
                HandType = r.HandType != null ? (string)r.HandType : null,
                ImageUrl = (string)r.ImageUrl,
                Title = report.Title ?? $"{r.ScanType} Scan",
                Summary = report.OverviewSummary ?? string.Empty,
                AuspiciousScore = report.OverallAuspiciousnessScore,
                CostCharged = (decimal)r.CostCharged,
                CreatedAt = (DateTime)r.CreatedAt,
                IsAdminReviewed = (bool)r.IsAdminReviewed
            });
        }

        return list;
    }

    public async Task<int> GetUserScanCountAsync(long userId, string scanType, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT COUNT(1)
            FROM dbo.AstroScans
            WHERE UserId = @UserId AND ScanType = @ScanType AND Status = 'COMPLETED';";

        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId, ScanType = scanType });
    }

    public async Task<PagedResult<AstroScanDetailDto>> AdminGetScansAsync(
        string? scanType,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        var whereClause = "WHERE 1=1";
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(scanType))
        {
            whereClause += " AND s.ScanType = @ScanType";
            p.Add("ScanType", scanType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            whereClause += " AND (u.DisplayName LIKE @Search OR u.Email LIKE @Search OR u.PhoneNumber LIKE @Search OR s.PublicId LIKE @Search)";
            p.Add("Search", $"%{search}%");
        }

        var countSql = $@"
            SELECT COUNT(1)
            FROM dbo.AstroScans s
            INNER JOIN dbo.UserProfiles u ON s.UserId = u.Id
            {whereClause};";

        var totalItems = await conn.ExecuteScalarAsync<int>(countSql, p);

        var offset = (page - 1) * pageSize;
        p.Add("Offset", offset);
        p.Add("PageSize", pageSize);

        var sql = $@"
            SELECT s.Id, s.PublicId, s.UserId, s.BirthProfileId, s.ScanType, s.HandType,
                   s.ImageUrl, s.Status, s.ConfidenceScore, s.AnalysisReportJson,
                   s.AdminNotes, s.IsAdminReviewed, s.CostCharged, s.TokensUsed,
                   s.Language, s.CreatedAt, s.UpdatedAt,
                   u.DisplayName AS UserName,
                   u.Email AS UserEmail,
                   u.PhoneNumber AS UserPhone,
                   p.Name AS BirthProfileName
            FROM dbo.AstroScans s
            INNER JOIN dbo.UserProfiles u ON s.UserId = u.Id
            LEFT JOIN dbo.BirthProfiles p ON s.BirthProfileId = p.Id
            {whereClause}
            ORDER BY s.CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        var rows = await conn.QueryAsync<dynamic>(sql, p);
        var items = rows.Select(MapToDetailDto).ToList();

        return new PagedResult<AstroScanDetailDto>
        {
            Items = items,
            TotalCount = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> UpdateAdminReviewAsync(
        string publicId,
        string adminNotes,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            UPDATE dbo.AstroScans
            SET AdminNotes = @AdminNotes,
                IsAdminReviewed = 1,
                UpdatedAt = GETUTCDATE()
            WHERE PublicId = @PublicId;";

        var affected = await conn.ExecuteAsync(sql, new { PublicId = publicId, AdminNotes = adminNotes });
        return affected > 0;
    }

    public async Task<AstroScanSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT TOP 1
                Id, IsPalmScanEnabled, IsHeadScanEnabled,
                PalmScanPrice, HeadScanPrice, FreeScansPerUser,
                GeminiModel, PalmSystemPrompt, HeadSystemPrompt,
                UpdatedAt, UpdatedBy
            FROM dbo.AstroScanSettings
            ORDER BY Id ASC;";

        var row = await conn.QuerySingleOrDefaultAsync<AstroScanSettingsDto>(sql);
        if (row != null) return row;

        // Fallback default
        return new AstroScanSettingsDto
        {
            Id = 1,
            IsPalmScanEnabled = true,
            IsHeadScanEnabled = true,
            PalmScanPrice = 0.00m,
            HeadScanPrice = 0.00m,
            FreeScansPerUser = 1,
            GeminiModel = "gemini-1.5-flash"
        };
    }

    public async Task<bool> UpdateSettingsAsync(
        AstroScanSettingsDto settings,
        string updatedBy,
        CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            UPDATE dbo.AstroScanSettings
            SET IsPalmScanEnabled = @IsPalmScanEnabled,
                IsHeadScanEnabled = @IsHeadScanEnabled,
                PalmScanPrice = @PalmScanPrice,
                HeadScanPrice = @HeadScanPrice,
                FreeScansPerUser = @FreeScansPerUser,
                GeminiModel = @GeminiModel,
                PalmSystemPrompt = @PalmSystemPrompt,
                HeadSystemPrompt = @HeadSystemPrompt,
                UpdatedAt = GETUTCDATE(),
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id;

            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO dbo.AstroScanSettings (
                    IsPalmScanEnabled, IsHeadScanEnabled, PalmScanPrice, HeadScanPrice,
                    FreeScansPerUser, GeminiModel, PalmSystemPrompt, HeadSystemPrompt,
                    UpdatedAt, UpdatedBy
                )
                VALUES (
                    @IsPalmScanEnabled, @IsHeadScanEnabled, @PalmScanPrice, @HeadScanPrice,
                    @FreeScansPerUser, @GeminiModel, @PalmSystemPrompt, @HeadSystemPrompt,
                    GETUTCDATE(), @UpdatedBy
                );
            END";

        await conn.ExecuteAsync(sql, new
        {
            settings.Id,
            settings.IsPalmScanEnabled,
            settings.IsHeadScanEnabled,
            settings.PalmScanPrice,
            settings.HeadScanPrice,
            settings.FreeScansPerUser,
            settings.GeminiModel,
            settings.PalmSystemPrompt,
            settings.HeadSystemPrompt,
            UpdatedBy = updatedBy
        });

        return true;
    }

    private static AstroScanDetailDto MapToDetailDto(dynamic r)
    {
        string reportJson = r.AnalysisReportJson != null ? (string)r.AnalysisReportJson : string.Empty;
        var report = ParseReport(reportJson);

        return new AstroScanDetailDto
        {
            Id = (long)r.Id,
            PublicId = (string)r.PublicId,
            UserId = (long)r.UserId,
            UserName = r.UserName != null ? (string)r.UserName : $"User #{r.UserId}",
            UserEmail = r.UserEmail != null ? (string)r.UserEmail : string.Empty,
            UserPhone = r.UserPhone != null ? (string)r.UserPhone : null,
            BirthProfileId = r.BirthProfileId != null ? (long?)r.BirthProfileId : null,
            BirthProfileName = r.BirthProfileName != null ? (string)r.BirthProfileName : null,
            ScanType = (string)r.ScanType,
            HandType = r.HandType != null ? (string)r.HandType : null,
            ImageUrl = (string)r.ImageUrl,
            Status = (string)r.Status,
            ConfidenceScore = r.ConfidenceScore != null ? (decimal?)r.ConfidenceScore : null,
            Report = report,
            AdminNotes = r.AdminNotes != null ? (string)r.AdminNotes : null,
            IsAdminReviewed = (bool)r.IsAdminReviewed,
            CostCharged = (decimal)r.CostCharged,
            TokensUsed = (int)r.TokensUsed,
            Language = (string)r.Language,
            CreatedAt = (DateTime)r.CreatedAt,
            UpdatedAt = r.UpdatedAt != null ? (DateTime?)r.UpdatedAt : null
        };
    }

    private static AstroScanReportDto ParseReport(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new AstroScanReportDto();
        try
        {
            return JsonSerializer.Deserialize<AstroScanReportDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AstroScanReportDto();
        }
        catch
        {
            return new AstroScanReportDto
            {
                Title = "Astrological Scan Analysis",
                OverviewSummary = json
            };
        }
    }
}
