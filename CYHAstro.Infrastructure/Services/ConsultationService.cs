using System.Data;
using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Consultation;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.Services;

public sealed class ConsultationService : IConsultationService
{
    private readonly IDbConnectionFactory _db;
    private readonly IAstrologerRepository _astrologerRepo;
    private readonly IWalletService _walletService;
    private readonly IWalletRepository _walletRepo;
    private readonly ILiveKitTokenService _liveKitTokenService;
    private readonly ILogger<ConsultationService> _logger;

    public ConsultationService(
        IDbConnectionFactory db,
        IAstrologerRepository astrologerRepo,
        IWalletService walletService,
        IWalletRepository walletRepo,
        ILiveKitTokenService liveKitTokenService,
        ILogger<ConsultationService> logger)
    {
        _db = db;
        _astrologerRepo = astrologerRepo;
        _walletService = walletService;
        _walletRepo = walletRepo;
        _liveKitTokenService = liveKitTokenService;
        _logger = logger;
    }

    public async Task<Result<ConsultationResponseDto>> RequestConsultationAsync(long userId, RequestConsultationDto request, CancellationToken ct = default)
    {
        if (userId <= 0) return Result<ConsultationResponseDto>.Failure(Result.ErrorCodes.Unauthorized, "User not authenticated.");

        var astro = await _astrologerRepo.GetByPublicIdAsync(request.AstrologerPublicId, ct);
        if (astro == null || !astro.IsActive || !astro.IsAvailable)
        {
            return Result<ConsultationResponseDto>.Failure(Result.ErrorCodes.NotFound, "Astrologer is currently offline. Please try again when the astrologer is online.");
        }

        var sessionType = request.SessionType.ToUpperInvariant();
        if (sessionType != "CHAT" && sessionType != "AUDIO" && sessionType != "VIDEO")
        {
            sessionType = "CHAT";
        }

        var rate = sessionType switch
        {
            "AUDIO" => astro.AudioRatePerMinute,
            "VIDEO" => astro.VideoRatePerMinute,
            _ => astro.ChatRatePerMinute
        };

        // Minimum balance required is 5 minutes
        var requiredBalance = rate * 5;
        var balanceRes = await _walletService.GetBalanceAsync(userId, ct);
        if (!balanceRes.IsSuccess || balanceRes.Value!.TotalBalance < requiredBalance)
        {
            var currentBal = balanceRes.IsSuccess ? balanceRes.Value!.TotalBalance : 0m;
            return Result<ConsultationResponseDto>.Failure(
                Result.ErrorCodes.InsufficientBalance,
                $"Insufficient wallet balance (₹{currentBal:F2}). You need at least ₹{requiredBalance:F2} (5 minutes) to start a {sessionType} consultation with {astro.DisplayName}. Please recharge your wallet.");
        }

        var sessionPublicId = "sess_" + Guid.NewGuid().ToString("N")[..16];
        var roomName = "room_" + sessionPublicId;

        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sqlInsert = @"
            INSERT INTO dbo.ConsultationSessions (
                PublicId, UserId, AstrologerId, SessionType, Status,
                RatePerMinute, LiveKitRoomName, IdempotencyKey, CreatedAt
            )
            VALUES (
                @PublicId, @UserId, @AstrologerId, @SessionType, 'REQUESTED',
                @RatePerMinute, @LiveKitRoomName, @IdempotencyKey, GETUTCDATE()
            );
            SELECT SCOPE_IDENTITY();
        ";

        var sessionId = await conn.ExecuteScalarAsync<long>(sqlInsert, new
        {
            PublicId = sessionPublicId,
            UserId = userId,
            AstrologerId = astro.Id,
            SessionType = sessionType,
            RatePerMinute = rate,
            LiveKitRoomName = roomName,
            IdempotencyKey = "req_" + sessionPublicId
        });

        var userProfile = await conn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT DisplayName FROM dbo.UserProfiles WHERE Id = @UserId", new { UserId = userId });
        var userName = userProfile != null ? (string)userProfile.DisplayName : "Seeker";

        string? liveKitToken = null;
        if (sessionType == "AUDIO" || sessionType == "VIDEO")
        {
            liveKitToken = _liveKitTokenService.GenerateAccessToken(roomName, "usr_" + userId, userName);
        }

        var response = new ConsultationResponseDto
        {
            SessionId = sessionId,
            PublicId = sessionPublicId,
            UserId = userId,
            UserName = userName,
            AstrologerId = astro.Id,
            AstrologerName = astro.DisplayName,
            AstrologerPhoto = astro.ProfilePhotoUrl,
            SessionType = sessionType,
            Status = "REQUESTED",
            RatePerMinute = rate,
            LiveKitRoomName = roomName,
            LiveKitToken = liveKitToken,
            DurationSeconds = 0,
            CreatedAt = DateTime.UtcNow
        };

        return Result<ConsultationResponseDto>.Success(response);
    }

    public async Task<Result<ConsultationResponseDto>> AcceptConsultationAsync(long astrologerUserId, string sessionPublicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sqlGet = @"
            SELECT s.*, 
                   u.DisplayName AS UserName, u.AvatarUrl AS UserAvatar,
                   a.DisplayName AS AstrologerName, a.ProfilePhotoUrl AS AstrologerPhoto, a.UserId AS AstroUserId
            FROM dbo.ConsultationSessions s
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE s.PublicId = @PublicId AND s.IsDeleted = 0;
        ";

        var session = await conn.QuerySingleOrDefaultAsync<dynamic>(sqlGet, new { PublicId = sessionPublicId });
        if (session == null)
        {
            return Result<ConsultationResponseDto>.Failure(Result.ErrorCodes.NotFound, "Session not found.");
        }

        const string sqlUpdate = @"
            UPDATE dbo.ConsultationSessions
            SET Status = 'ACTIVE',
                ConnectedAt = GETUTCDATE(),
                AcceptedAt = GETUTCDATE(),
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id;
        ";

        await conn.ExecuteAsync(sqlUpdate, new { Id = (long)session.Id });

        string? token = null;
        if ((string)session.SessionType == "AUDIO" || (string)session.SessionType == "VIDEO")
        {
            token = _liveKitTokenService.GenerateAccessToken(
                (string)session.LiveKitRoomName, "ast_" + (long)session.AstrologerId, (string)session.AstrologerName);
        }

        var response = new ConsultationResponseDto
        {
            SessionId = (long)session.Id,
            PublicId = (string)session.PublicId,
            UserId = (long)session.UserId,
            UserName = (string)session.UserName,
            AstrologerId = (long)session.AstrologerId,
            AstrologerName = (string)session.AstrologerName,
            AstrologerPhoto = (string)session.AstrologerPhoto,
            SessionType = (string)session.SessionType,
            Status = "ACTIVE",
            RatePerMinute = (decimal)session.RatePerMinute,
            LiveKitRoomName = (string)session.LiveKitRoomName,
            LiveKitToken = token,
            ConnectedAt = DateTime.UtcNow,
            DurationSeconds = 0,
            CreatedAt = (DateTime)session.CreatedAt
        };

        return Result<ConsultationResponseDto>.Success(response);
    }

    public async Task<Result<ConsultationCompletedDto>> EndConsultationAsync(long actorUserId, string sessionPublicId, string endReason, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sqlGet = @"
            SELECT s.*, 
                   u.DisplayName AS UserName,
                   a.DisplayName AS AstrologerName, a.UserId AS AstroUserId, a.CommissionRatePct
            FROM dbo.ConsultationSessions s
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE s.PublicId = @PublicId AND s.IsDeleted = 0;
        ";

        var session = await conn.QuerySingleOrDefaultAsync<dynamic>(sqlGet, new { PublicId = sessionPublicId });
        if (session == null)
        {
            return Result<ConsultationCompletedDto>.Failure(Result.ErrorCodes.NotFound, "Session not found.");
        }

        if ((string)session.Status == "ENDED" || (bool)session.IsBilled)
        {
            var bal = await _walletService.GetBalanceAsync((long)session.UserId, ct);
            return Result<ConsultationCompletedDto>.Success(new ConsultationCompletedDto
            {
                SessionPublicId = sessionPublicId,
                DurationSeconds = (int)session.DurationSeconds,
                TotalCharge = (decimal)session.TotalCharge,
                UserRemainingBalance = bal.IsSuccess ? bal.Value!.TotalBalance : 0m,
                Status = "ENDED"
            });
        }

        DateTime startTime = session.ConnectedAt != null ? (DateTime)session.ConnectedAt : (DateTime)session.CreatedAt;
        var durationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds;
        if (durationSeconds < 0) durationSeconds = 0;

        decimal totalCharge = 0m;
        decimal commission = 0m;
        decimal astroEarnings = 0m;

        if (durationSeconds >= 5 && (string)session.Status != "CANCELLED")
        {
            var billedMinutes = Math.Max(1, (int)Math.Ceiling(durationSeconds / 60.0));
            var ratePerMin = (decimal)session.RatePerMinute;
            totalCharge = billedMinutes * ratePerMin;

            // Debit user wallet
            var debitRes = await _walletRepo.DebitAsync(
                (long)session.UserId,
                totalCharge,
                "CONSULTATION",
                (long)session.Id,
                $"Consultation ({session.SessionType}) with {session.AstrologerName} for {billedMinutes}m",
                $"debit_{sessionPublicId}",
                ct);

            if (!debitRes.IsSuccess)
            {
                // In case of insufficient balance, debit whatever is left
                var userBal = await _walletService.GetBalanceAsync((long)session.UserId, ct);
                if (userBal.IsSuccess && userBal.Value!.TotalBalance > 0)
                {
                    totalCharge = userBal.Value.TotalBalance;
                    await _walletRepo.DebitAsync(
                        (long)session.UserId, totalCharge, "CONSULTATION", (long)session.Id,
                        $"Consultation remaining balance spend ({session.SessionType})", $"debit_rem_{sessionPublicId}", ct);
                }
            }

            var commissionPct = (decimal)session.CommissionRatePct;
            commission = Math.Round(totalCharge * (commissionPct / 100m), 2);
            astroEarnings = totalCharge - commission;

            // Record platform commission
            const string sqlCommission = @"
                INSERT INTO dbo.PlatformCommissions (SessionId, GrossAmount, CommissionPct, CommissionAmount, CreatedAt)
                VALUES (@SessionId, @GrossAmount, @CommissionPct, @CommissionAmount, GETUTCDATE());
            ";
            await conn.ExecuteAsync(sqlCommission, new
            {
                SessionId = (long)session.Id,
                GrossAmount = totalCharge,
                CommissionPct = commissionPct,
                CommissionAmount = commission
            });

            // Record astrologer earnings & credit astrologer wallet
            const string sqlEarnings = @"
                INSERT INTO dbo.AstrologerEarnings (AstrologerId, SessionId, Amount, EarningType, Status, CreatedAt)
                VALUES (@AstrologerId, @SessionId, @Amount, 'CONSULTATION', 'SETTLED', GETUTCDATE());
            ";
            await conn.ExecuteAsync(sqlEarnings, new
            {
                AstrologerId = (long)session.AstrologerId,
                SessionId = (long)session.Id,
                Amount = astroEarnings
            });

            if (astroEarnings > 0 && session.AstroUserId != null)
            {
                await _walletRepo.CreditAsync(
                    (long)session.AstroUserId,
                    astroEarnings,
                    "PAID",
                    "CONSULTATION",
                    (long)session.Id,
                    $"Earnings from consultation #{session.Id}",
                    $"credit_astro_{sessionPublicId}",
                    ct);
            }
        }

        const string sqlFinish = @"
            UPDATE dbo.ConsultationSessions
            SET Status = 'ENDED',
                EndedAt = GETUTCDATE(),
                DurationSeconds = @DurationSeconds,
                TotalCharge = @TotalCharge,
                EndReason = @EndReason,
                IsBilled = 1,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id;

            UPDATE dbo.Astrologers
            SET TotalConsultations = TotalConsultations + 1
            WHERE Id = @AstrologerId;
        ";

        await conn.ExecuteAsync(sqlFinish, new
        {
            Id = (long)session.Id,
            DurationSeconds = durationSeconds,
            TotalCharge = totalCharge,
            EndReason = endReason ?? "ENDED",
            AstrologerId = (long)session.AstrologerId
        });

        var finalBal = await _walletService.GetBalanceAsync((long)session.UserId, ct);

        return Result<ConsultationCompletedDto>.Success(new ConsultationCompletedDto
        {
            SessionPublicId = sessionPublicId,
            DurationSeconds = durationSeconds,
            TotalCharge = totalCharge,
            UserRemainingBalance = finalBal.IsSuccess ? finalBal.Value!.TotalBalance : 0m,
            AstrologerEarnings = astroEarnings,
            PlatformCommission = commission,
            Status = "ENDED"
        });
    }

    public async Task<Result<ConsultationResponseDto>> GetConsultationByPublicIdAsync(string sessionPublicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT s.*, 
                   u.DisplayName AS UserName, u.AvatarUrl AS UserAvatar,
                   a.DisplayName AS AstrologerName, a.ProfilePhotoUrl AS AstrologerPhoto
            FROM dbo.ConsultationSessions s
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE s.PublicId = @PublicId AND s.IsDeleted = 0;
        ";

        var session = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { PublicId = sessionPublicId });
        if (session == null)
        {
            return Result<ConsultationResponseDto>.Failure(Result.ErrorCodes.NotFound, "Session not found.");
        }

        var duration = session.ConnectedAt != null
            ? (int)(DateTime.UtcNow - (DateTime)session.ConnectedAt).TotalSeconds
            : (int)session.DurationSeconds;

        var dto = new ConsultationResponseDto
        {
            SessionId = (long)session.Id,
            PublicId = (string)session.PublicId,
            UserId = (long)session.UserId,
            UserName = (string)session.UserName,
            AstrologerId = (long)session.AstrologerId,
            AstrologerName = (string)session.AstrologerName,
            AstrologerPhoto = (string)session.AstrologerPhoto,
            SessionType = (string)session.SessionType,
            Status = (string)session.Status,
            RatePerMinute = (decimal)session.RatePerMinute,
            LiveKitRoomName = (string)session.LiveKitRoomName,
            ConnectedAt = session.ConnectedAt != null ? (DateTime)session.ConnectedAt : null,
            DurationSeconds = duration,
            CreatedAt = (DateTime)session.CreatedAt
        };

        return Result<ConsultationResponseDto>.Success(dto);
    }

    public async Task<Result<ConsultationResponseDto?>> GetActiveConsultationAsync(long userId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT TOP 1 s.*, 
                   u.DisplayName AS UserName, u.AvatarUrl AS UserAvatar,
                   a.DisplayName AS AstrologerName, a.ProfilePhotoUrl AS AstrologerPhoto
            FROM dbo.ConsultationSessions s
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE (s.UserId = @UserId OR a.UserId = @UserId)
              AND s.Status IN ('REQUESTED', 'ACCEPTED', 'ACTIVE')
              AND s.IsDeleted = 0
            ORDER BY s.CreatedAt DESC;
        ";

        var session = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { UserId = userId });
        if (session == null)
        {
            return Result<ConsultationResponseDto?>.Success(null);
        }

        var dto = new ConsultationResponseDto
        {
            SessionId = (long)session.Id,
            PublicId = (string)session.PublicId,
            UserId = (long)session.UserId,
            UserName = (string)session.UserName,
            AstrologerId = (long)session.AstrologerId,
            AstrologerName = (string)session.AstrologerName,
            AstrologerPhoto = (string)session.AstrologerPhoto,
            SessionType = (string)session.SessionType,
            Status = (string)session.Status,
            RatePerMinute = (decimal)session.RatePerMinute,
            LiveKitRoomName = (string)session.LiveKitRoomName,
            ConnectedAt = session.ConnectedAt != null ? (DateTime)session.ConnectedAt : null,
            DurationSeconds = (int)session.DurationSeconds,
            CreatedAt = (DateTime)session.CreatedAt
        };

        return Result<ConsultationResponseDto?>.Success(dto);
    }

    public async Task<Result<string>> GetLiveKitTokenAsync(long userId, string sessionPublicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT s.*, 
                   u.DisplayName AS UserName,
                   a.DisplayName AS AstrologerName, a.UserId AS AstroUserId
            FROM dbo.ConsultationSessions s
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE s.PublicId = @PublicId AND s.IsDeleted = 0;
        ";

        var session = await conn.QuerySingleOrDefaultAsync<dynamic>(sql, new { PublicId = sessionPublicId });
        if (session == null)
        {
            return Result<string>.Failure(Result.ErrorCodes.NotFound, "Session not found.");
        }

        bool isSeeker = (long)session.UserId == userId;
        bool isAstrologer = session.AstroUserId != null && (long)session.AstroUserId == userId;

        if (!isSeeker && !isAstrologer)
        {
            return Result<string>.Failure(Result.ErrorCodes.Unauthorized, "Participant not part of this session.");
        }

        string participantId = isSeeker ? "usr_" + userId : "ast_" + (long)session.AstrologerId;
        string participantName = isSeeker ? (string)session.UserName : (string)session.AstrologerName;

        var token = _liveKitTokenService.GenerateAccessToken((string)session.LiveKitRoomName, participantId, participantName);
        return Result<string>.Success(token);
    }

    public async Task<Result<ConsultationMessageDto>> SaveMessageAsync(long senderUserId, string sessionPublicId, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Result<ConsultationMessageDto>.Failure(Result.ErrorCodes.ValidationFailed, "Message cannot be empty.");
        }

        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sqlGetSession = @"
            SELECT s.Id, s.UserId, s.AstrologerId, a.UserId AS AstroUserId,
                   u.DisplayName AS UserName, a.DisplayName AS AstroName
            FROM dbo.ConsultationSessions s
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE s.PublicId = @PublicId AND s.IsDeleted = 0;
        ";

        var session = await conn.QuerySingleOrDefaultAsync<dynamic>(sqlGetSession, new { PublicId = sessionPublicId });
        if (session == null)
        {
            return Result<ConsultationMessageDto>.Failure(Result.ErrorCodes.NotFound, "Session not found.");
        }

        string senderType = (long)session.UserId == senderUserId ? "USER" : "ASTROLOGER";
        string senderName = senderType == "USER" ? (string)session.UserName : (string)session.AstroName;

        const string sqlInsert = @"
            INSERT INTO dbo.ConsultationMessages (SessionId, SenderId, SenderType, MessageText, CreatedAt)
            VALUES (@SessionId, @SenderId, @SenderType, @MessageText, GETUTCDATE());
            SELECT SCOPE_IDENTITY();
        ";

        var msgId = await conn.ExecuteScalarAsync<long>(sqlInsert, new
        {
            SessionId = (long)session.Id,
            SenderId = senderUserId,
            SenderType = senderType,
            MessageText = message
        });

        return Result<ConsultationMessageDto>.Success(new ConsultationMessageDto
        {
            Id = msgId,
            SessionPublicId = sessionPublicId,
            SenderType = senderType,
            SenderName = senderName,
            Message = message,
            SentAt = DateTime.UtcNow
        });
    }

    public async Task<Result<IReadOnlyList<ConsultationMessageDto>>> GetMessagesAsync(string sessionPublicId, int limit = 100, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT TOP (@Limit)
                m.Id,
                @SessionPublicId AS SessionPublicId,
                m.SenderType,
                CASE 
                    WHEN m.SenderType = 'USER' THEN u.DisplayName
                    WHEN m.SenderType = 'ASTROLOGER' THEN a.DisplayName
                    ELSE 'System'
                END AS SenderName,
                m.MessageText AS Message,
                m.CreatedAt AS SentAt
            FROM dbo.ConsultationMessages m
            JOIN dbo.ConsultationSessions s ON m.SessionId = s.Id
            LEFT JOIN dbo.UserProfiles u ON s.UserId = u.Id
            LEFT JOIN dbo.Astrologers a ON s.AstrologerId = a.Id
            WHERE s.PublicId = @SessionPublicId
            ORDER BY m.CreatedAt ASC;
        ";

        var list = (await conn.QueryAsync<ConsultationMessageDto>(sql, new { SessionPublicId = sessionPublicId, Limit = limit })).ToList();
        return Result<IReadOnlyList<ConsultationMessageDto>>.Success(list);
    }

    public async Task<Result<bool>> SubmitReviewAsync(long userId, CYHAstro.Core.DTOs.Astrologer.SubmitReviewRequestDto dto, CancellationToken ct = default)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
        {
            return Result<bool>.Failure(Result.ErrorCodes.ValidationFailed, "Rating must be between 1 and 5 stars.");
        }

        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sqlGetSession = @"
            SELECT s.Id, s.AstrologerId, s.UserId
            FROM dbo.ConsultationSessions s
            WHERE s.PublicId = @PublicId AND s.IsDeleted = 0;
        ";

        var session = await conn.QuerySingleOrDefaultAsync<dynamic>(sqlGetSession, new { PublicId = dto.SessionPublicId });
        if (session == null)
        {
            return Result<bool>.Failure(Result.ErrorCodes.NotFound, "Consultation session not found.");
        }

        if ((long)session.UserId != userId)
        {
            return Result<bool>.Failure(Result.ErrorCodes.Unauthorized, "Only the consultation client can submit a review.");
        }

        // Ensure table exists
        const string sqlEnsureTable = @"
            IF OBJECT_ID('dbo.AstrologerReviews', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AstrologerReviews (
                    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    SessionId BIGINT NOT NULL,
                    UserId BIGINT NOT NULL,
                    AstrologerId BIGINT NOT NULL,
                    Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
                    Comment NVARCHAR(1000) NULL,
                    IsApproved BIT NOT NULL DEFAULT 1,
                    IsDeleted BIT NOT NULL DEFAULT 0,
                    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                );
                CREATE NONCLUSTERED INDEX IX_AstrologerReviews_Astro ON dbo.AstrologerReviews(AstrologerId, IsApproved, CreatedAt DESC);
            END
        ";
        await conn.ExecuteAsync(sqlEnsureTable);

        // Check if already reviewed
        const string sqlCheck = "SELECT COUNT(1) FROM dbo.AstrologerReviews WHERE SessionId = @SessionId AND UserId = @UserId AND IsDeleted = 0";
        var count = await conn.ExecuteScalarAsync<int>(sqlCheck, new { SessionId = (long)session.Id, UserId = userId });
        if (count > 0)
        {
            return Result<bool>.Failure(Result.ErrorCodes.Conflict, "Review has already been submitted for this consultation.");
        }

        // Insert review
        const string sqlInsert = @"
            INSERT INTO dbo.AstrologerReviews (SessionId, UserId, AstrologerId, Rating, Comment, IsApproved, IsDeleted, CreatedAt)
            VALUES (@SessionId, @UserId, @AstrologerId, @Rating, @Comment, 1, 0, GETUTCDATE());
        ";
        await conn.ExecuteAsync(sqlInsert, new
        {
            SessionId = (long)session.Id,
            UserId = userId,
            AstrologerId = (long)session.AstrologerId,
            Rating = dto.Rating,
            Comment = dto.ReviewText
        });

        // Recalculate Astrologer Rating
        const string sqlUpdateRating = @"
            UPDATE dbo.Astrologers
            SET Rating = (SELECT ISNULL(ROUND(AVG(CAST(Rating AS DECIMAL(3,2))), 2), 5.00) FROM dbo.AstrologerReviews WHERE AstrologerId = @AstrologerId AND IsApproved = 1 AND IsDeleted = 0),
                TotalReviews = (SELECT COUNT(1) FROM dbo.AstrologerReviews WHERE AstrologerId = @AstrologerId AND IsApproved = 1 AND IsDeleted = 0),
                UpdatedAt = GETUTCDATE()
            WHERE Id = @AstrologerId;
        ";
        await conn.ExecuteAsync(sqlUpdateRating, new { AstrologerId = (long)session.AstrologerId });

        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<ConsultationResponseDto>>> GetIncomingRequestsForAstrologerAsync(long astrologerUserId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT s.*, 
                   u.DisplayName AS UserName, u.AvatarUrl AS UserAvatar,
                   a.DisplayName AS AstrologerName, a.ProfilePhotoUrl AS AstrologerPhoto
            FROM dbo.ConsultationSessions s
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE a.UserId = @UserId AND s.Status = 'REQUESTED' AND s.IsDeleted = 0
            ORDER BY s.CreatedAt DESC;
        ";

        var rows = await conn.QueryAsync<dynamic>(sql, new { UserId = astrologerUserId });
        var list = rows.Select(s => new ConsultationResponseDto
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

        return Result<IReadOnlyList<ConsultationResponseDto>>.Success(list);
    }

    public async Task<Result<bool>> RejectConsultationAsync(long astrologerUserId, string sessionPublicId, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE s
            SET s.Status = 'CANCELLED',
                s.EndReason = 'ASTROLOGER_REJECT',
                s.EndedAt = GETUTCDATE(),
                s.UpdatedAt = GETUTCDATE()
            FROM dbo.ConsultationSessions s
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE s.PublicId = @PublicId AND a.UserId = @UserId AND s.Status = 'REQUESTED';
        ";

        var rows = await conn.ExecuteAsync(sql, new { PublicId = sessionPublicId, UserId = astrologerUserId });
        if (rows == 0)
        {
            return Result<bool>.Failure(Result.ErrorCodes.NotFound, "Pending consultation request not found or unauthorized.");
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<ConsultationResponseDto>>> GetAstrologerSessionsAsync(long astrologerUserId, string? status = null, int limit = 50, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        var sql = @"
            SELECT TOP (@Limit) s.*, 
                   u.DisplayName AS UserName, u.AvatarUrl AS UserAvatar,
                   a.DisplayName AS AstrologerName, a.ProfilePhotoUrl AS AstrologerPhoto
            FROM dbo.ConsultationSessions s
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE a.UserId = @UserId AND s.IsDeleted = 0";

        if (!string.IsNullOrWhiteSpace(status))
        {
            sql += " AND s.Status = @Status";
        }

        sql += " ORDER BY s.CreatedAt DESC;";

        var rows = await conn.QueryAsync<dynamic>(sql, new { UserId = astrologerUserId, Status = status, Limit = limit });
        var list = rows.Select(s => new ConsultationResponseDto
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
            ConnectedAt = s.ConnectedAt != null ? (DateTime)s.ConnectedAt : null,
            CreatedAt = (DateTime)s.CreatedAt
        }).ToList();

        return Result<IReadOnlyList<ConsultationResponseDto>>.Success(list);
    }

    public async Task<Result<IReadOnlyList<ConsultationResponseDto>>> GetUserConsultationsAsync(long userId, int limit = 50, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT TOP (@Limit) s.*, 
                   u.DisplayName AS UserName, u.AvatarUrl AS UserAvatar,
                   a.DisplayName AS AstrologerName, a.ProfilePhotoUrl AS AstrologerPhoto
            FROM dbo.ConsultationSessions s
            JOIN dbo.UserProfiles u ON u.Id = s.UserId
            JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
            WHERE s.UserId = @UserId AND s.IsDeleted = 0
            ORDER BY s.CreatedAt DESC;";

        var rows = await conn.QueryAsync<dynamic>(sql, new { UserId = userId, Limit = limit });
        var list = rows.Select(s => new ConsultationResponseDto
        {
            SessionId = (long)s.Id,
            PublicId = (string)s.PublicId,
            UserId = (long)s.UserId,
            UserName = (string)s.UserName,
            AstrologerId = (long)s.AstrologerId,
            AstrologerName = (string)s.AstrologerName,
            AstrologerPhoto = s.AstrologerPhoto != null ? (string)s.AstrologerPhoto : null,
            SessionType = (string)s.SessionType,
            Status = (string)s.Status,
            RatePerMinute = (decimal)s.RatePerMinute,
            TotalCharge = s.TotalCharge != null ? (decimal)s.TotalCharge : 0m,
            AstrologerEarnings = s.AstrologerEarnings != null ? (decimal)s.AstrologerEarnings : 0m,
            PlatformCommission = s.PlatformCommission != null ? (decimal)s.PlatformCommission : 0m,
            LiveKitRoomName = (string)s.LiveKitRoomName,
            DurationSeconds = s.DurationSeconds != null ? (int)s.DurationSeconds : 0,
            ConnectedAt = s.ConnectedAt != null ? (DateTime?)s.ConnectedAt : null,
            CreatedAt = (DateTime)s.CreatedAt
        }).ToList();

        return Result<IReadOnlyList<ConsultationResponseDto>>.Success(list);
    }

    public async Task HeartbeatAsync(long userId, string sessionPublicId, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(
                @"UPDATE dbo.ConsultationSessions 
                  SET LastHeartbeatAt = GETUTCDATE(), DisconnectedAt = NULL
                  WHERE PublicId = @PublicId AND UserId = @UserId AND Status = 'ACTIVE'",
                new { PublicId = sessionPublicId, UserId = userId });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Heartbeat failed for session {SessionPublicId}", sessionPublicId);
        }
    }

    public async Task UpdateDisconnectedAtAsync(long userId, DateTime disconnectedAt, CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);
            await conn.ExecuteAsync(
                @"UPDATE dbo.ConsultationSessions 
                  SET DisconnectedAt = @DisconnectedAt
                  WHERE UserId = @UserId AND Status = 'ACTIVE' AND DisconnectedAt IS NULL",
                new { UserId = userId, DisconnectedAt = disconnectedAt });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UpdateDisconnectedAt failed for user {UserId}", userId);
        }
    }

    public async Task<Result<bool>> AutoEndStaleSessionsAsync(CancellationToken ct = default)
    {
        try
        {
            using var conn = await _db.CreateOpenConnectionAsync(ct);
            // Find sessions ACTIVE but LastHeartbeatAt > 90 seconds ago (or DisconnectedAt > 90 seconds ago)
            var staleSessions = await conn.QueryAsync<dynamic>(
                @"SELECT s.PublicId, s.UserId, s.AstrologerId, s.ConnectedAt, s.AcceptedAt, s.RatePerMinute,
                         a.CommissionRatePct, a.UserId AS AstroUserId
                  FROM dbo.ConsultationSessions s
                  JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
                  WHERE s.Status = 'ACTIVE' AND s.IsDeleted = 0
                    AND (
                        (s.LastHeartbeatAt IS NOT NULL AND DATEDIFF(SECOND, s.LastHeartbeatAt, GETUTCDATE()) > 90)
                        OR
                        (s.DisconnectedAt IS NOT NULL AND DATEDIFF(SECOND, s.DisconnectedAt, GETUTCDATE()) > 60)
                        OR
                        (s.LastHeartbeatAt IS NULL AND s.ConnectedAt IS NOT NULL AND DATEDIFF(SECOND, s.ConnectedAt, GETUTCDATE()) > 300)
                    )");

            int count = 0;
            foreach (var stale in staleSessions)
            {
                try
                {
                    // Use the existing EndConsultationAsync logic
                    // ActorUserId = 0 means system auto-end
                    await EndConsultationAsync(0, (string)stale.PublicId, "TIMEOUT", ct);
                    count++;
                    _logger.LogInformation("Auto-ended stale session {PublicId}", (string)stale.PublicId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to auto-end stale session {PublicId}", (string)stale.PublicId);
                }
            }

            return Result<bool>.Success(count > 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AutoEndStaleSessionsAsync failed");
            return Result<bool>.Failure("AUTO_END_FAILED", ex.Message);
        }
    }
}
