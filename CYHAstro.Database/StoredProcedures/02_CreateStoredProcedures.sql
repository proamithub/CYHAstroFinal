-- ============================================================
-- CYHASTRO PLATFORM — ALL STORED PROCEDURES
-- Target Database: CyhAstroDB
-- SQL Server 2016+ Compatibility
-- ============================================================

USE CyhAstroDB;
GO

-- ============================================================
-- PROCEDURE: usp_RegisterOrLoginUser
-- Handles external/password login: creates UserProfile & Wallet if new
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_RegisterOrLoginUser
    @IdentityId         NVARCHAR(450),
    @Email              NVARCHAR(256),
    @DisplayName        NVARCHAR(150),
    @PhoneNumber        NVARCHAR(50) = NULL,
    @AvatarUrl          NVARCHAR(500) = NULL,
    @PublicId           NVARCHAR(32)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @UserId BIGINT;

        SELECT @UserId = Id 
        FROM dbo.UserProfiles 
        WHERE IdentityId = @IdentityId OR Email = @Email;

        IF @UserId IS NULL
        BEGIN
            -- Insert Profile
            INSERT INTO dbo.UserProfiles (
                PublicId, IdentityId, DisplayName, Email, PhoneNumber, AvatarUrl,
                IsEmailVerified, IsActive, LastLoginAt, CreatedAt
            )
            VALUES (
                @PublicId, @IdentityId, @DisplayName, @Email, @PhoneNumber, @AvatarUrl,
                1, 1, GETUTCDATE(), GETUTCDATE()
            );

            SET @UserId = SCOPE_IDENTITY();

            -- Initialize User Wallet
            INSERT INTO dbo.Wallets (UserId, PaidBalance, BonusBalance, CurrencyCode, RowVersion, CreatedAt)
            VALUES (@UserId, 0.00, 0.00, 'INR', 1, GETUTCDATE());

            -- Initialize User Preferences
            INSERT INTO dbo.UserPreferences (UserId, CreatedAt)
            VALUES (@UserId, GETUTCDATE());
        END
        ELSE
        BEGIN
            -- Update LastLoginAt & Avatar
            UPDATE dbo.UserProfiles
            SET LastLoginAt = GETUTCDATE(),
                AvatarUrl = COALESCE(@AvatarUrl, AvatarUrl),
                PhoneNumber = COALESCE(@PhoneNumber, PhoneNumber),
                UpdatedAt = GETUTCDATE()
            WHERE Id = @UserId;
        END

        COMMIT TRANSACTION;

        SELECT p.*, w.PaidBalance, w.BonusBalance, (w.PaidBalance + w.BonusBalance) AS TotalBalance
        FROM dbo.UserProfiles p
        JOIN dbo.Wallets w ON w.UserId = p.Id
        WHERE p.Id = @UserId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ============================================================
-- PROCEDURE: usp_GetUserProfile
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_GetUserProfile
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.*, w.PaidBalance, w.BonusBalance, (w.PaidBalance + w.BonusBalance) AS TotalBalance
    FROM dbo.UserProfiles p
    LEFT JOIN dbo.Wallets w ON w.UserId = p.Id
    WHERE p.Id = @UserId AND p.IsDeleted = 0;
END;
GO

-- ============================================================
-- PROCEDURE: usp_CreateBirthProfile
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_CreateBirthProfile
    @UserId             BIGINT,
    @PublicId           NVARCHAR(32),
    @Name               NVARCHAR(150),
    @Gender             NCHAR(1),
    @DateOfBirth        DATE,
    @TimeOfBirth        TIME(0) = NULL,
    @IsTimeApproximate  BIT = 0,
    @BirthPlace         NVARCHAR(250),
    @Latitude           DECIMAL(10,7),
    @Longitude          DECIMAL(10,7),
    @TimeZoneId         NVARCHAR(100),
    @UtcOffsetMinutes   INT = 330,
    @IsPrimary          BIT = 0,
    @RelationLabel      NVARCHAR(50) = 'Self'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    BEGIN TRY
        -- If this profile is marked primary, unmark others for this user
        IF @IsPrimary = 1
        BEGIN
            UPDATE dbo.BirthProfiles
            SET IsPrimary = 0, UpdatedAt = GETUTCDATE()
            WHERE UserId = @UserId AND IsDeleted = 0;
        END

        INSERT INTO dbo.BirthProfiles (
            PublicId, UserId, Name, Gender, DateOfBirth, TimeOfBirth,
            IsTimeApproximate, BirthPlace, Latitude, Longitude, TimeZoneId,
            UtcOffsetMinutes, IsPrimary, RelationLabel, CreatedAt
        )
        VALUES (
            @PublicId, @UserId, @Name, @Gender, @DateOfBirth, @TimeOfBirth,
            @IsTimeApproximate, @BirthPlace, @Latitude, @Longitude, @TimeZoneId,
            @UtcOffsetMinutes, @IsPrimary, @RelationLabel, GETUTCDATE()
        );

        DECLARE @ProfileId BIGINT = SCOPE_IDENTITY();
        COMMIT TRANSACTION;

        SELECT @ProfileId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ============================================================
-- PROCEDURE: usp_UpdateBirthProfile
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_UpdateBirthProfile
    @Id                 BIGINT,
    @UserId             BIGINT,
    @Name               NVARCHAR(150),
    @Gender             NCHAR(1),
    @DateOfBirth        DATE,
    @TimeOfBirth        TIME(0) = NULL,
    @IsTimeApproximate  BIT = 0,
    @BirthPlace         NVARCHAR(250),
    @Latitude           DECIMAL(10,7),
    @Longitude          DECIMAL(10,7),
    @TimeZoneId         NVARCHAR(100),
    @UtcOffsetMinutes   INT = 330,
    @RelationLabel      NVARCHAR(50) = 'Self'
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.BirthProfiles
    SET Name = @Name,
        Gender = @Gender,
        DateOfBirth = @DateOfBirth,
        TimeOfBirth = @TimeOfBirth,
        IsTimeApproximate = @IsTimeApproximate,
        BirthPlace = @BirthPlace,
        Latitude = @Latitude,
        Longitude = @Longitude,
        TimeZoneId = @TimeZoneId,
        UtcOffsetMinutes = @UtcOffsetMinutes,
        RelationLabel = @RelationLabel,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @Id AND UserId = @UserId AND IsDeleted = 0;

    SELECT @@ROWCOUNT;
END;
GO

-- ============================================================
-- PROCEDURE: usp_GetWalletByUserId
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_GetWalletByUserId
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, UserId, PaidBalance, BonusBalance, CurrencyCode, RowVersion, CreatedAt, UpdatedAt
    FROM dbo.Wallets
    WHERE UserId = @UserId AND IsDeleted = 0;
END;
GO

-- ============================================================
-- PROCEDURE: usp_CreditWallet
-- Atomically credits user wallet and appends immutable ledger record
-- Idempotency key prevents duplicate credits
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_CreditWallet
    @UserId         BIGINT,
    @Amount         DECIMAL(12,2),
    @BalanceType    NVARCHAR(10), -- PAID or BONUS
    @ReferenceType  NVARCHAR(30),
    @ReferenceId    BIGINT = NULL,
    @Description    NVARCHAR(500),
    @IdempotencyKey NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- 1. Check idempotency first
    IF EXISTS (SELECT 1 FROM dbo.WalletTransactions WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT TOP 1 * FROM dbo.WalletTransactions WHERE IdempotencyKey = @IdempotencyKey;
        RETURN;
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @WalletId BIGINT;
        DECLARE @PaidBefore DECIMAL(12,2);
        DECLARE @BonusBefore DECIMAL(12,2);
        DECLARE @TotalBefore DECIMAL(12,2);
        DECLARE @TotalAfter DECIMAL(12,2);

        -- Lock wallet row for update
        SELECT @WalletId = Id, 
               @PaidBefore = PaidBalance, 
               @BonusBefore = BonusBalance
        FROM dbo.Wallets WITH (UPDLOCK, ROWLOCK)
        WHERE UserId = @UserId AND IsDeleted = 0;

        IF @WalletId IS NULL
        BEGIN
            RAISERROR('Wallet not found for the user.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        SET @TotalBefore = @PaidBefore + @BonusBefore;

        IF @BalanceType = 'BONUS'
        BEGIN
            UPDATE dbo.Wallets
            SET BonusBalance = BonusBalance + @Amount,
                RowVersion = RowVersion + 1,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @WalletId;
        END
        ELSE
        BEGIN
            UPDATE dbo.Wallets
            SET PaidBalance = PaidBalance + @Amount,
                RowVersion = RowVersion + 1,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @WalletId;
        END

        SET @TotalAfter = @TotalBefore + @Amount;

        -- Record Immutable Ledger
        INSERT INTO dbo.WalletTransactions (
            WalletId, UserId, TransactionType, BalanceType, Amount,
            BalanceBefore, BalanceAfter, ReferenceType, ReferenceId,
            Description, IdempotencyKey, CreatedAt
        )
        VALUES (
            @WalletId, @UserId, 'CREDIT', @BalanceType, @Amount,
            @TotalBefore, @TotalAfter, @ReferenceType, @ReferenceId,
            @Description, @IdempotencyKey, GETUTCDATE()
        );

        DECLARE @TxId BIGINT = SCOPE_IDENTITY();
        COMMIT TRANSACTION;

        SELECT * FROM dbo.WalletTransactions WHERE Id = @TxId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ============================================================
-- PROCEDURE: usp_DebitWallet
-- Transactionally debits bonus balance first, then paid balance
-- Fails if total balance < @Amount
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_DebitWallet
    @UserId         BIGINT,
    @Amount         DECIMAL(12,2),
    @ReferenceType  NVARCHAR(30),
    @ReferenceId    BIGINT = NULL,
    @Description    NVARCHAR(500),
    @IdempotencyKey NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Check idempotency
    IF EXISTS (SELECT 1 FROM dbo.WalletTransactions WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT TOP 1 * FROM dbo.WalletTransactions WHERE IdempotencyKey = @IdempotencyKey;
        RETURN;
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @WalletId BIGINT;
        DECLARE @PaidBefore DECIMAL(12,2);
        DECLARE @BonusBefore DECIMAL(12,2);
        DECLARE @TotalBefore DECIMAL(12,2);
        DECLARE @RemainingToDebit DECIMAL(12,2) = @Amount;
        DECLARE @BonusDeducted DECIMAL(12,2) = 0.00;
        DECLARE @PaidDeducted DECIMAL(12,2) = 0.00;

        SELECT @WalletId = Id, 
               @PaidBefore = PaidBalance, 
               @BonusBefore = BonusBalance
        FROM dbo.Wallets WITH (UPDLOCK, ROWLOCK)
        WHERE UserId = @UserId AND IsDeleted = 0;

        IF @WalletId IS NULL
        BEGIN
            RAISERROR('Wallet not found for the user.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        SET @TotalBefore = @PaidBefore + @BonusBefore;

        -- Check sufficient balance
        IF @TotalBefore < @Amount
        BEGIN
            RAISERROR('Insufficient wallet balance.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Consume Bonus first
        IF @BonusBefore > 0.00
        BEGIN
            IF @BonusBefore >= @RemainingToDebit
            BEGIN
                SET @BonusDeducted = @RemainingToDebit;
                SET @RemainingToDebit = 0.00;
            END
            ELSE
            BEGIN
                SET @BonusDeducted = @BonusBefore;
                SET @RemainingToDebit = @RemainingToDebit - @BonusBefore;
            END
        END

        -- Consume Paid balance for remaining
        IF @RemainingToDebit > 0.00
        BEGIN
            SET @PaidDeducted = @RemainingToDebit;
        END

        -- Apply wallet debit
        UPDATE dbo.Wallets
        SET PaidBalance = PaidBalance - @PaidDeducted,
            BonusBalance = BonusBalance - @BonusDeducted,
            RowVersion = RowVersion + 1,
            UpdatedAt = GETUTCDATE()
        WHERE Id = @WalletId;

        DECLARE @TotalAfter DECIMAL(12,2) = @TotalBefore - @Amount;

        -- Append to Immutable Ledger
        INSERT INTO dbo.WalletTransactions (
            WalletId, UserId, TransactionType, BalanceType, Amount,
            BalanceBefore, BalanceAfter, ReferenceType, ReferenceId,
            Description, IdempotencyKey, CreatedAt
        )
        VALUES (
            @WalletId, @UserId, 'DEBIT', 
            CASE WHEN @PaidDeducted > 0 THEN 'PAID' ELSE 'BONUS' END,
            @Amount, @TotalBefore, @TotalAfter, @ReferenceType, @ReferenceId,
            @Description, @IdempotencyKey, GETUTCDATE()
        );

        DECLARE @TxId BIGINT = SCOPE_IDENTITY();
        COMMIT TRANSACTION;

        SELECT * FROM dbo.WalletTransactions WHERE Id = @TxId;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- ============================================================
-- PROCEDURE: usp_GetWalletTransactions
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_GetWalletTransactions
    @UserId     BIGINT,
    @Page       INT = 1,
    @PageSize   INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    SELECT *
    FROM dbo.WalletTransactions
    WHERE UserId = @UserId AND IsDeleted = 0
    ORDER BY CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- ============================================================
-- PROCEDURE: usp_GetAstrologerListing
-- High performance filtered & paginated astrologer search
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_GetAstrologerListing
    @Search         NVARCHAR(100) = NULL,
    @Specialization NVARCHAR(100) = NULL,
    @Language       NVARCHAR(50) = NULL,
    @IsAvailable    BIT = NULL,
    @SortBy         NVARCHAR(30) = 'RATING_DESC',
    @Page           INT = 1,
    @PageSize       INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    -- Query items
    SELECT a.*
    FROM dbo.Astrologers a
    WHERE a.IsDeleted = 0
      AND a.IsActive = 1
      AND (@Search IS NULL OR a.DisplayName LIKE '%' + @Search + '%')
      AND (@Specialization IS NULL OR a.Specializations LIKE '%' + @Specialization + '%')
      AND (@Language IS NULL OR a.Languages LIKE '%' + @Language + '%')
      AND (@IsAvailable IS NULL OR a.IsAvailable = @IsAvailable)
    ORDER BY 
        CASE WHEN @SortBy = 'RATING_DESC' THEN a.Rating END DESC,
        CASE WHEN @SortBy = 'PRICE_ASC' THEN a.ChatRatePerMinute END ASC,
        CASE WHEN @SortBy = 'PRICE_DESC' THEN a.ChatRatePerMinute END DESC,
        CASE WHEN @SortBy = 'EXPERIENCE_DESC' THEN a.ExperienceYears END DESC,
        a.TotalConsultations DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    -- Query total count
    SELECT COUNT(1)
    FROM dbo.Astrologers a
    WHERE a.IsDeleted = 0
      AND a.IsActive = 1
      AND (@Search IS NULL OR a.DisplayName LIKE '%' + @Search + '%')
      AND (@Specialization IS NULL OR a.Specializations LIKE '%' + @Specialization + '%')
      AND (@Language IS NULL OR a.Languages LIKE '%' + @Language + '%')
      AND (@IsAvailable IS NULL OR a.IsAvailable = @IsAvailable);
END;
GO

-- ============================================================
-- PROCEDURE: usp_UpdateAstrologerRating
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_UpdateAstrologerRating
    @AstrologerId   BIGINT,
    @NewRating      DECIMAL(3,2)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Astrologers
    SET Rating = ((Rating * TotalReviews) + @NewRating) / (TotalReviews + 1),
        TotalReviews = TotalReviews + 1,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @AstrologerId AND IsDeleted = 0;

    SELECT @@ROWCOUNT;
END;
GO

-- ============================================================
-- PROCEDURE: usp_CreateConsultationSession
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_CreateConsultationSession
    @PublicId           NVARCHAR(32),
    @UserId             BIGINT,
    @AstrologerId       BIGINT,
    @SessionType        NVARCHAR(10),
    @RatePerMinute      DECIMAL(8,2),
    @LiveKitRoomName    NVARCHAR(100) = NULL,
    @IdempotencyKey     NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ConsultationSessions (
        PublicId, UserId, AstrologerId, SessionType, Status,
        RatePerMinute, LiveKitRoomName, IdempotencyKey, CreatedAt
    )
    VALUES (
        @PublicId, @UserId, @AstrologerId, @SessionType, 'REQUESTED',
        @RatePerMinute, @LiveKitRoomName, @IdempotencyKey, GETUTCDATE()
    );

    SELECT SCOPE_IDENTITY();
END;
GO

-- ============================================================
-- PROCEDURE: usp_GetConsultationById
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_GetConsultationById
    @Id BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.*, 
           u.DisplayName AS UserName, u.AvatarUrl AS UserAvatar,
           a.DisplayName AS AstrologerName, a.ProfilePhotoUrl AS AstrologerPhoto
    FROM dbo.ConsultationSessions s
    JOIN dbo.UserProfiles u ON u.Id = s.UserId
    JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
    WHERE s.Id = @Id AND s.IsDeleted = 0;
END;
GO

-- ============================================================
-- PROCEDURE: usp_GetConsultationByPublicId
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_GetConsultationByPublicId
    @PublicId NVARCHAR(32)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.*, 
           u.DisplayName AS UserName, u.AvatarUrl AS UserAvatar,
           a.DisplayName AS AstrologerName, a.ProfilePhotoUrl AS AstrologerPhoto
    FROM dbo.ConsultationSessions s
    JOIN dbo.UserProfiles u ON u.Id = s.UserId
    JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
    WHERE s.PublicId = @PublicId AND s.IsDeleted = 0;
END;
GO

-- ============================================================
-- PROCEDURE: usp_UpdateConsultationStatus
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_UpdateConsultationStatus
    @SessionId  BIGINT,
    @Status     NVARCHAR(20),
    @EndReason  NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ConsultationSessions
    SET Status = @Status,
        EndReason = COALESCE(@EndReason, EndReason),
        AcceptedAt = CASE WHEN @Status = 'ACCEPTED' AND AcceptedAt IS NULL THEN GETUTCDATE() ELSE AcceptedAt END,
        UpdatedAt = GETUTCDATE()
    WHERE Id = @SessionId AND IsDeleted = 0;

    SELECT @@ROWCOUNT;
END;
GO

-- ============================================================
-- PROCEDURE: usp_GetUserConsultations
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_GetUserConsultations
    @UserId     BIGINT,
    @Page       INT = 1,
    @PageSize   INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    SELECT s.*, a.DisplayName AS AstrologerName, a.ProfilePhotoUrl AS AstrologerPhoto
    FROM dbo.ConsultationSessions s
    JOIN dbo.Astrologers a ON a.Id = s.AstrologerId
    WHERE s.UserId = @UserId AND s.IsDeleted = 0
    ORDER BY s.CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(1)
    FROM dbo.ConsultationSessions
    WHERE UserId = @UserId AND IsDeleted = 0;
END;
GO

-- ============================================================
-- PROCEDURE: usp_GetAstrologerConsultations
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_GetAstrologerConsultations
    @AstrologerId BIGINT,
    @Page         INT = 1,
    @PageSize     INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;

    SELECT s.*, u.DisplayName AS UserName, u.AvatarUrl AS UserAvatar
    FROM dbo.ConsultationSessions s
    JOIN dbo.UserProfiles u ON u.Id = s.UserId
    WHERE s.AstrologerId = @AstrologerId AND s.IsDeleted = 0
    ORDER BY s.CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(1)
    FROM dbo.ConsultationSessions
    WHERE AstrologerId = @AstrologerId AND IsDeleted = 0;
END;
GO

-- ============================================================
-- PROCEDURE: usp_SaveConsultationMessage
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_SaveConsultationMessage
    @SessionId      BIGINT,
    @SenderId       BIGINT,
    @SenderType     NVARCHAR(20),
    @MessageText    NVARCHAR(MAX),
    @AttachmentUrl  NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ConsultationMessages (SessionId, SenderId, SenderType, MessageText, AttachmentUrl, CreatedAt)
    VALUES (@SessionId, @SenderId, @SenderType, @MessageText, @AttachmentUrl, GETUTCDATE());

    SELECT SCOPE_IDENTITY();
END;
GO

-- ============================================================
-- PROCEDURE: usp_GetConsultationMessages
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_GetConsultationMessages
    @SessionId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM dbo.ConsultationMessages
    WHERE SessionId = @SessionId
    ORDER BY CreatedAt ASC;
END;
GO

-- ============================================================
-- PROCEDURE: usp_RecordConsultationEvent
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_RecordConsultationEvent
    @SessionId      BIGINT,
    @EventType      NVARCHAR(50),
    @EventPayload   NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.ConsultationEvents (SessionId, EventType, EventPayload, ServerTimestamp)
    VALUES (@SessionId, @EventType, @EventPayload, GETUTCDATE());
END;
GO

-- ============================================================
-- PROCEDURE: usp_SaveAuditLog
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_SaveAuditLog
    @UserId     BIGINT = NULL,
    @Action     NVARCHAR(100),
    @EntityType NVARCHAR(100) = NULL,
    @EntityId   NVARCHAR(100) = NULL,
    @OldValues  NVARCHAR(MAX) = NULL,
    @NewValues  NVARCHAR(MAX) = NULL,
    @IpAddress  NVARCHAR(45) = NULL,
    @UserAgent  NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLogs (UserId, Action, EntityType, EntityId, OldValues, NewValues, IpAddress, UserAgent, CreatedAt)
    VALUES (@UserId, @Action, @EntityType, @EntityId, @OldValues, @NewValues, @IpAddress, @UserAgent, GETUTCDATE());
END;
GO
