using System.Data;
using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Admin;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.Services;

public sealed class AdminService : IAdminService
{
    private readonly DbConnectionFactory _db;
    private readonly ILogger<AdminService> _logger;

    public AdminService(DbConnectionFactory db, ILogger<AdminService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<AdminDashboardDto>> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);

            const string sqlStats = @"
                SELECT 
                    (SELECT COUNT(1) FROM dbo.UserProfiles WHERE IsDeleted = 0) AS TotalUsers,
                    (SELECT COUNT(1) FROM dbo.Astrologers WHERE IsDeleted = 0) AS TotalAstrologers,
                    (SELECT COUNT(1) FROM dbo.Astrologers WHERE IsDeleted = 0 AND IsActive = 1 AND IsVerified = 1) AS ActiveAstrologers,
                    (SELECT COUNT(1) FROM dbo.Astrologers WHERE IsDeleted = 0 AND IsVerified = 0) AS PendingAstrologers,
                    (SELECT COUNT(1) FROM dbo.ConsultationSessions WHERE IsDeleted = 0) AS TotalConsultations,
                    (SELECT ISNULL(SUM(Amount), 0) FROM dbo.WalletTransactions WHERE TransactionType = 'CREDIT' AND ReferenceType = 'RECHARGE') AS TotalPlatformRevenue,
                    (SELECT ISNULL(SUM(Amount), 0) FROM dbo.AstrologerEarnings) AS TotalAstrologerPayouts,
                    (SELECT ISNULL(SUM(CommissionAmount), 0) FROM dbo.PlatformCommissions) AS TotalPlatformCommission,
                    (SELECT ISNULL(SUM(PaidBalance + BonusBalance), 0) FROM dbo.Wallets) AS TotalWalletLiability;
            ";

            var summary = await conn.QuerySingleAsync<dynamic>(sqlStats);

            const string sqlRecentTx = @"
                SELECT TOP 10
                    wt.Id,
                    wt.UserId,
                    ISNULL(u.DisplayName, 'User #' + CAST(wt.UserId AS NVARCHAR(20))) AS UserDisplayName,
                    wt.TransactionType,
                    wt.BalanceType,
                    wt.Amount,
                    wt.BalanceBefore,
                    wt.BalanceAfter,
                    wt.ReferenceType,
                    wt.Description,
                    wt.CreatedAt
                FROM dbo.WalletTransactions wt
                LEFT JOIN dbo.UserProfiles u ON wt.UserId = u.Id
                ORDER BY wt.CreatedAt DESC;
            ";

            var recentTx = (await conn.QueryAsync<AdminTransactionDto>(sqlRecentTx)).ToList();

            const string sqlRecentConsultations = @"
                SELECT TOP 10
                    cs.Id,
                    cs.PublicId,
                    ISNULL(u.DisplayName, 'User #' + CAST(cs.UserId AS NVARCHAR(20))) AS UserName,
                    ISNULL(a.DisplayName, 'Astrologer #' + CAST(cs.AstrologerId AS NVARCHAR(20))) AS AstrologerName,
                    cs.SessionType,
                    cs.Status,
                    ISNULL(cs.DurationSeconds, 0) AS DurationSeconds,
                    ISNULL(cs.TotalCharge, 0) AS TotalAmount,
                    ISNULL(pc.CommissionAmount, 0) AS PlatformCommission,
                    ISNULL(ae.Amount, 0) AS AstrologerEarnings,
                    cs.CreatedAt
                FROM dbo.ConsultationSessions cs
                LEFT JOIN dbo.UserProfiles u ON cs.UserId = u.Id
                LEFT JOIN dbo.Astrologers a ON cs.AstrologerId = a.Id
                LEFT JOIN dbo.PlatformCommissions pc ON cs.Id = pc.SessionId
                LEFT JOIN dbo.AstrologerEarnings ae ON cs.Id = ae.SessionId
                ORDER BY cs.CreatedAt DESC;
            ";

            var recentConsultations = (await conn.QueryAsync<AdminConsultationDto>(sqlRecentConsultations)).ToList();

            var result = new AdminDashboardDto
            {
                TotalUsers = (long)summary.TotalUsers,
                TotalAstrologers = (int)summary.TotalAstrologers,
                ActiveAstrologers = (int)summary.ActiveAstrologers,
                PendingAstrologers = (int)summary.PendingAstrologers,
                TotalConsultations = (int)summary.TotalConsultations,
                TotalPlatformRevenue = (decimal)summary.TotalPlatformRevenue,
                TotalAstrologerPayouts = (decimal)summary.TotalAstrologerPayouts,
                TotalPlatformCommission = (decimal)summary.TotalPlatformCommission,
                TotalWalletLiability = (decimal)summary.TotalWalletLiability,
                RecentTransactions = recentTx,
                RecentConsultations = recentConsultations
            };

            return Result<AdminDashboardDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load admin dashboard stats");
            return Result<AdminDashboardDto>.Failure(Result.ErrorCodes.DatabaseError, "Failed to load admin stats: " + ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<AdminAstrologerDto>>> GetAstrologersAsync(string? status = null, string? search = null, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);

            var sql = @"
                SELECT 
                    Id,
                    PublicId,
                    DisplayName,
                    Specializations,
                    Languages,
                    Bio,
                    ChatRatePerMinute,
                    AudioRatePerMinute,
                    VideoRatePerMinute,
                    Rating,
                    TotalReviews,
                    TotalConsultations,
                    ExperienceYears,
                    IsAvailable,
                    IsVerified,
                    IsActive,
                    CommissionRatePct,
                    ProfilePhotoUrl,
                    CreatedAt
                FROM dbo.Astrologers
                WHERE IsDeleted = 0
                  AND (@Search IS NULL OR DisplayName LIKE '%' + @Search + '%')
                  AND (
                      @Status IS NULL 
                      OR (@Status = 'Verified' AND IsVerified = 1 AND IsActive = 1)
                      OR (@Status = 'Pending' AND IsVerified = 0)
                      OR (@Status = 'Inactive' AND IsActive = 0)
                  )
                ORDER BY CreatedAt DESC;
            ";

            var list = (await conn.QueryAsync<AdminAstrologerDto>(sql, new { Search = search, Status = status })).ToList();
            return Result<IReadOnlyList<AdminAstrologerDto>>.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get admin astrologers list");
            return Result<IReadOnlyList<AdminAstrologerDto>>.Failure(Result.ErrorCodes.DatabaseError, ex.Message);
        }
    }

    public async Task<Result<AdminAstrologerDto>> GetAstrologerByIdAsync(long id, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);

            const string sql = @"
                SELECT 
                    Id,
                    PublicId,
                    DisplayName,
                    Specializations,
                    Languages,
                    Bio,
                    ChatRatePerMinute,
                    AudioRatePerMinute,
                    VideoRatePerMinute,
                    Rating,
                    TotalReviews,
                    TotalConsultations,
                    ExperienceYears,
                    IsAvailable,
                    IsVerified,
                    IsActive,
                    CommissionRatePct,
                    ProfilePhotoUrl,
                    CreatedAt
                FROM dbo.Astrologers
                WHERE Id = @Id AND IsDeleted = 0;
            ";

            var astro = await conn.QuerySingleOrDefaultAsync<AdminAstrologerDto>(sql, new { Id = id });
            if (astro == null)
            {
                return Result<AdminAstrologerDto>.Failure(Result.ErrorCodes.NotFound, "Astrologer not found");
            }

            return Result<AdminAstrologerDto>.Success(astro);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get astrologer by id {Id}", id);
            return Result<AdminAstrologerDto>.Failure(Result.ErrorCodes.DatabaseError, ex.Message);
        }
    }

    public async Task<Result> UpdateAstrologerStatusAsync(AdminUpdateAstrologerDto request, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);

            const string sql = @"
                UPDATE dbo.Astrologers
                SET 
                    IsVerified = @IsVerified,
                    IsActive = @IsActive,
                    CommissionRatePct = @CommissionRatePct,
                    ChatRatePerMinute = @ChatRatePerMinute,
                    AudioRatePerMinute = @AudioRatePerMinute,
                    VideoRatePerMinute = @VideoRatePerMinute,
                    UpdatedAt = GETUTCDATE()
                WHERE Id = @Id;
            ";

            var affected = await conn.ExecuteAsync(sql, request);
            if (affected == 0)
            {
                return Result.Failure(Result.ErrorCodes.NotFound, "Astrologer not found.");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update astrologer {Id}", request.Id);
            return Result.Failure(Result.ErrorCodes.DatabaseError, ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<AdminRechargePackageDto>>> GetRechargePackagesAsync(CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);

            const string sql = @"
                SELECT 
                    Id,
                    Name,
                    Amount,
                    BonusPercentage,
                    BonusAmount,
                    IsPopular,
                    DisplayOrder,
                    IsActive
                FROM dbo.RechargePackages
                ORDER BY DisplayOrder ASC, Amount ASC;
            ";

            var packages = (await conn.QueryAsync<AdminRechargePackageDto>(sql)).ToList();
            return Result<IReadOnlyList<AdminRechargePackageDto>>.Success(packages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recharge packages");
            return Result<IReadOnlyList<AdminRechargePackageDto>>.Failure(Result.ErrorCodes.DatabaseError, ex.Message);
        }
    }

    public async Task<Result> SaveRechargePackageAsync(AdminRechargePackageDto package, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);

            if (package.Id > 0)
            {
                const string sqlUpdate = @"
                    UPDATE dbo.RechargePackages
                    SET 
                        Name = @Name,
                        Amount = @Amount,
                        BonusPercentage = @BonusPercentage,
                        BonusAmount = @BonusAmount,
                        IsPopular = @IsPopular,
                        DisplayOrder = @DisplayOrder,
                        IsActive = @IsActive,
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @Id;
                ";

                await conn.ExecuteAsync(sqlUpdate, package);
            }
            else
            {
                const string sqlInsert = @"
                    INSERT INTO dbo.RechargePackages (Name, Amount, BonusPercentage, BonusAmount, IsPopular, DisplayOrder, IsActive, CreatedAt)
                    VALUES (@Name, @Amount, @BonusPercentage, @BonusAmount, @IsPopular, @DisplayOrder, @IsActive, GETUTCDATE());
                ";

                await conn.ExecuteAsync(sqlInsert, package);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save recharge package");
            return Result.Failure(Result.ErrorCodes.DatabaseError, ex.Message);
        }
    }

    public async Task<Result> TogglePackageStatusAsync(long packageId, bool isActive, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);

            const string sql = @"
                UPDATE dbo.RechargePackages
                SET IsActive = @IsActive, UpdatedAt = GETUTCDATE()
                WHERE Id = @Id;
            ";

            await conn.ExecuteAsync(sql, new { Id = packageId, IsActive = isActive });
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle recharge package status");
            return Result.Failure(Result.ErrorCodes.DatabaseError, ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<AdminTransactionDto>>> GetPlatformTransactionsAsync(string? type = null, int limit = 50, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);

            const string sql = @"
                SELECT TOP (@Limit)
                    wt.Id,
                    wt.UserId,
                    ISNULL(u.DisplayName, 'User #' + CAST(wt.UserId AS NVARCHAR(20))) AS UserDisplayName,
                    wt.TransactionType,
                    wt.BalanceType,
                    wt.Amount,
                    wt.BalanceBefore,
                    wt.BalanceAfter,
                    wt.ReferenceType,
                    wt.Description,
                    wt.CreatedAt
                FROM dbo.WalletTransactions wt
                LEFT JOIN dbo.UserProfiles u ON wt.UserId = u.Id
                WHERE (@Type IS NULL OR wt.TransactionType = @Type)
                ORDER BY wt.CreatedAt DESC;
            ";

            var transactions = (await conn.QueryAsync<AdminTransactionDto>(sql, new { Type = type, Limit = limit })).ToList();
            return Result<IReadOnlyList<AdminTransactionDto>>.Success(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load platform transactions");
            return Result<IReadOnlyList<AdminTransactionDto>>.Failure(Result.ErrorCodes.DatabaseError, ex.Message);
        }
    }
}
