-- ============================================================
-- CynAstroDB — Phase 01: Initial Database Creation Script
-- SQL Server 2019+
-- Run as: DBA with db creation rights
-- ============================================================

USE master;
GO

-- Create Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CynAstroDB')
BEGIN
    CREATE DATABASE CynAstroDB
    COLLATE SQL_Latin1_General_CP1_CI_AS;
    PRINT 'Database CynAstroDB created.';
END
ELSE
    PRINT 'Database CynAstroDB already exists.';
GO

USE CynAstroDB;
GO

-- ============================================================
-- ASP.NET Identity Tables (managed by EF Core migrations)
-- Listed here for reference — do NOT create manually
-- ============================================================
-- AspNetUsers
-- AspNetRoles
-- AspNetUserRoles
-- AspNetUserClaims
-- AspNetUserLogins
-- AspNetRoleClaims
-- AspNetUserTokens

-- ============================================================
-- TABLE: UserProfiles
-- Extended profile linked to ASP.NET Identity user
-- ============================================================
IF OBJECT_ID('UserProfiles', 'U') IS NULL
BEGIN
    CREATE TABLE UserProfiles (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        PublicId        NVARCHAR(32)    NOT NULL,       -- Opaque URL-safe ID
        IdentityId      NVARCHAR(450)   NOT NULL,       -- FK → AspNetUsers.Id
        DisplayName     NVARCHAR(100)   NOT NULL,
        Gender          NCHAR(1)        NULL,           -- M, F, O
        AvatarUrl       NVARCHAR(500)   NULL,
        PhoneNumber     NVARCHAR(20)    NULL,
        IsPhoneVerified BIT             NOT NULL DEFAULT 0,
        PreferredLang   NVARCHAR(10)    NOT NULL DEFAULT 'en',
        IsActive        BIT             NOT NULL DEFAULT 1,
        LastLoginAt     DATETIME2       NULL,
        CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2       NULL,
        IsDeleted       BIT             NOT NULL DEFAULT 0,
        DeletedAt       DATETIME2       NULL,
        CONSTRAINT PK_UserProfiles       PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_UserProfiles_PublicId UNIQUE (PublicId),
        CONSTRAINT UQ_UserProfiles_IdentityId UNIQUE (IdentityId)
    );
    PRINT 'Table UserProfiles created.';
END
GO

-- ============================================================
-- TABLE: BirthProfiles
-- Multiple birth charts per user (self + family)
-- ============================================================
IF OBJECT_ID('BirthProfiles', 'U') IS NULL
BEGIN
    CREATE TABLE BirthProfiles (
        Id                  BIGINT IDENTITY(1,1) NOT NULL,
        PublicId            NVARCHAR(32)    NOT NULL,
        UserId              BIGINT          NOT NULL,
        Name                NVARCHAR(100)   NOT NULL,
        Gender              NCHAR(1)        NOT NULL,
        DateOfBirth         DATE            NOT NULL,
        TimeOfBirth         TIME(0)         NULL,
        IsTimeApproximate   BIT             NOT NULL DEFAULT 0,
        BirthPlace          NVARCHAR(200)   NOT NULL,
        Latitude            DECIMAL(10,7)   NOT NULL DEFAULT 0,
        Longitude           DECIMAL(10,7)   NOT NULL DEFAULT 0,
        TimeZoneId          NVARCHAR(100)   NOT NULL,
        UtcOffsetMinutes    INT             NOT NULL DEFAULT 330, -- IST default
        IsPrimary           BIT             NOT NULL DEFAULT 0,
        RelationLabel       NVARCHAR(50)    NOT NULL DEFAULT 'Self',
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2       NULL,
        IsDeleted           BIT             NOT NULL DEFAULT 0,
        DeletedAt           DATETIME2       NULL,
        CONSTRAINT PK_BirthProfiles         PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_BirthProfiles_PublicId UNIQUE (PublicId),
        CONSTRAINT FK_BirthProfiles_User     FOREIGN KEY (UserId) REFERENCES UserProfiles(Id)
    );
    PRINT 'Table BirthProfiles created.';
END
GO

-- ============================================================
-- TABLE: Wallets
-- One wallet per user. Balances tracked via WalletTransactions.
-- ============================================================
IF OBJECT_ID('Wallets', 'U') IS NULL
BEGIN
    CREATE TABLE Wallets (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        UserId          BIGINT          NOT NULL,
        PaidBalance     DECIMAL(12,2)   NOT NULL DEFAULT 0,
        BonusBalance    DECIMAL(12,2)   NOT NULL DEFAULT 0,
        CurrencyCode    NCHAR(3)        NOT NULL DEFAULT 'INR',
        RowVersion      BIGINT          NOT NULL DEFAULT 0, -- Optimistic concurrency
        CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2       NULL,
        IsDeleted       BIT             NOT NULL DEFAULT 0,
        CONSTRAINT PK_Wallets           PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Wallets_UserId    UNIQUE (UserId),
        CONSTRAINT FK_Wallets_User      FOREIGN KEY (UserId) REFERENCES UserProfiles(Id),
        CONSTRAINT CK_Wallets_Paid      CHECK (PaidBalance >= 0),
        CONSTRAINT CK_Wallets_Bonus     CHECK (BonusBalance >= 0)
    );
    PRINT 'Table Wallets created.';
END
GO

-- ============================================================
-- TABLE: WalletTransactions (Immutable Ledger)
-- NEVER UPDATE or DELETE rows. Append-only.
-- ============================================================
IF OBJECT_ID('WalletTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE WalletTransactions (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        WalletId        BIGINT          NOT NULL,
        UserId          BIGINT          NOT NULL,
        TransactionType NVARCHAR(10)    NOT NULL, -- CREDIT, DEBIT, BONUS, REFUND
        BalanceType     NVARCHAR(10)    NOT NULL, -- PAID, BONUS
        Amount          DECIMAL(12,2)   NOT NULL,
        BalanceBefore   DECIMAL(12,2)   NOT NULL,
        BalanceAfter    DECIMAL(12,2)   NOT NULL,
        ReferenceType   NVARCHAR(30)    NOT NULL, -- RECHARGE, CONSULTATION, REFUND, ADMIN
        ReferenceId     BIGINT          NULL,
        Description     NVARCHAR(500)   NOT NULL,
        IdempotencyKey  NVARCHAR(100)   NOT NULL,
        CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        IsDeleted       BIT             NOT NULL DEFAULT 0, -- Should always be 0 (audit)
        CONSTRAINT PK_WalletTransactions        PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_WalletTx_IdempotencyKey   UNIQUE (IdempotencyKey),
        CONSTRAINT FK_WalletTx_Wallet           FOREIGN KEY (WalletId) REFERENCES Wallets(Id),
        CONSTRAINT FK_WalletTx_User             FOREIGN KEY (UserId) REFERENCES UserProfiles(Id),
        CONSTRAINT CK_WalletTx_Amount           CHECK (Amount > 0),
        CONSTRAINT CK_WalletTx_Type             CHECK (TransactionType IN ('CREDIT','DEBIT','BONUS','REFUND')),
        CONSTRAINT CK_WalletTx_BalanceType      CHECK (BalanceType IN ('PAID','BONUS'))
    );
    PRINT 'Table WalletTransactions created.';
END
GO

-- ============================================================
-- TABLE: Astrologers
-- ============================================================
IF OBJECT_ID('Astrologers', 'U') IS NULL
BEGIN
    CREATE TABLE Astrologers (
        Id                  BIGINT IDENTITY(1,1) NOT NULL,
        PublicId            NVARCHAR(32)    NOT NULL,
        UserId              BIGINT          NOT NULL,
        DisplayName         NVARCHAR(100)   NOT NULL,
        Specializations     NVARCHAR(MAX)   NOT NULL DEFAULT '[]', -- JSON
        Languages           NVARCHAR(500)   NOT NULL DEFAULT '["hi","en"]', -- JSON
        Bio                 NVARCHAR(2000)  NULL,
        ChatRatePerMinute   DECIMAL(8,2)    NOT NULL DEFAULT 10,
        AudioRatePerMinute  DECIMAL(8,2)    NOT NULL DEFAULT 15,
        VideoRatePerMinute  DECIMAL(8,2)    NOT NULL DEFAULT 20,
        Rating              DECIMAL(3,2)    NOT NULL DEFAULT 0,
        TotalConsultations  INT             NOT NULL DEFAULT 0,
        IsAvailable         BIT             NOT NULL DEFAULT 0,
        IsVerified          BIT             NOT NULL DEFAULT 0,
        IsActive            BIT             NOT NULL DEFAULT 1,
        ProfilePhotoUrl     NVARCHAR(500)   NULL,
        ExperienceYears     INT             NOT NULL DEFAULT 0,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2       NULL,
        IsDeleted           BIT             NOT NULL DEFAULT 0,
        DeletedAt           DATETIME2       NULL,
        CONSTRAINT PK_Astrologers           PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Astrologers_PublicId  UNIQUE (PublicId),
        CONSTRAINT UQ_Astrologers_UserId    UNIQUE (UserId),
        CONSTRAINT FK_Astrologers_User      FOREIGN KEY (UserId) REFERENCES UserProfiles(Id),
        CONSTRAINT CK_Astrologers_Rating    CHECK (Rating BETWEEN 0 AND 5)
    );
    PRINT 'Table Astrologers created.';
END
GO

-- ============================================================
-- TABLE: ConsultationSessions
-- ============================================================
IF OBJECT_ID('ConsultationSessions', 'U') IS NULL
BEGIN
    CREATE TABLE ConsultationSessions (
        Id                  BIGINT IDENTITY(1,1) NOT NULL,
        PublicId            NVARCHAR(32)    NOT NULL,
        UserId              BIGINT          NOT NULL,
        AstrologerId        BIGINT          NOT NULL,
        SessionType         NVARCHAR(10)    NOT NULL, -- CHAT, AUDIO, VIDEO
        Status              NVARCHAR(15)    NOT NULL DEFAULT 'REQUESTED',
        AcceptedAt          DATETIME2       NULL,
        ConnectedAt         DATETIME2       NULL,     -- Server-verified
        EndedAt             DATETIME2       NULL,
        DurationSeconds     INT             NOT NULL DEFAULT 0,  -- Server-calculated
        RatePerMinute       DECIMAL(8,2)    NOT NULL,
        TotalCharge         DECIMAL(12,2)   NOT NULL DEFAULT 0,
        LiveKitRoomName     NVARCHAR(100)   NULL,
        EndReason           NVARCHAR(30)    NULL,
        IsBilled            BIT             NOT NULL DEFAULT 0,
        IdempotencyKey      NVARCHAR(100)   NULL,
        CreatedAt           DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2       NULL,
        IsDeleted           BIT             NOT NULL DEFAULT 0,
        CONSTRAINT PK_ConsultationSessions          PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Sessions_PublicId             UNIQUE (PublicId),
        CONSTRAINT FK_Sessions_User                 FOREIGN KEY (UserId) REFERENCES UserProfiles(Id),
        CONSTRAINT FK_Sessions_Astrologer           FOREIGN KEY (AstrologerId) REFERENCES Astrologers(Id),
        CONSTRAINT CK_Sessions_Type                 CHECK (SessionType IN ('CHAT','AUDIO','VIDEO')),
        CONSTRAINT CK_Sessions_Status               CHECK (Status IN ('REQUESTED','ACCEPTED','ACTIVE','ENDED','BILLED','CANCELLED','FAILED')),
        CONSTRAINT CK_Sessions_Duration             CHECK (DurationSeconds >= 0)
    );
    PRINT 'Table ConsultationSessions created.';
END
GO

-- ============================================================
-- TABLE: AuditLogs
-- ============================================================
IF OBJECT_ID('AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE AuditLogs (
        Id          BIGINT IDENTITY(1,1) NOT NULL,
        UserId      BIGINT          NULL,
        Action      NVARCHAR(100)   NOT NULL,
        EntityType  NVARCHAR(100)   NULL,
        EntityId    NVARCHAR(100)   NULL,
        OldValues   NVARCHAR(MAX)   NULL, -- JSON
        NewValues   NVARCHAR(MAX)   NULL, -- JSON
        IpAddress   NVARCHAR(45)    NULL,
        UserAgent   NVARCHAR(500)   NULL,
        CreatedAt   DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id)
    );
    PRINT 'Table AuditLogs created.';
END
GO

-- ============================================================
-- TABLE: RefreshTokens
-- ============================================================
IF OBJECT_ID('RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE RefreshTokens (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        UserId          BIGINT          NOT NULL,
        TokenHash       NVARCHAR(256)   NOT NULL, -- Stored as SHA-256 hash
        DeviceInfo      NVARCHAR(500)   NULL,
        IpAddress       NVARCHAR(45)    NULL,
        ExpiresAt       DATETIME2       NOT NULL,
        RevokedAt       DATETIME2       NULL,
        ReplacedByToken NVARCHAR(256)   NULL,
        CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        IsRevoked       BIT             NOT NULL DEFAULT 0,
        CONSTRAINT PK_RefreshTokens             PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_RefreshTokens_Hash        UNIQUE (TokenHash),
        CONSTRAINT FK_RefreshTokens_User        FOREIGN KEY (UserId) REFERENCES UserProfiles(Id)
    );
    PRINT 'Table RefreshTokens created.';
END
GO

-- ============================================================
-- INDEXES
-- ============================================================

-- UserProfiles
CREATE NONCLUSTERED INDEX IX_UserProfiles_IdentityId ON UserProfiles(IdentityId) WHERE IsDeleted = 0;

-- BirthProfiles
CREATE NONCLUSTERED INDEX IX_BirthProfiles_UserId ON BirthProfiles(UserId) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_BirthProfiles_UserId_Primary ON BirthProfiles(UserId, IsPrimary) WHERE IsDeleted = 0;

-- WalletTransactions (heavy read)
CREATE NONCLUSTERED INDEX IX_WalletTx_UserId_Date ON WalletTransactions(UserId, CreatedAt DESC) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_WalletTx_ReferenceType_Id ON WalletTransactions(ReferenceType, ReferenceId) WHERE IsDeleted = 0;

-- Astrologers
CREATE NONCLUSTERED INDEX IX_Astrologers_IsAvailable ON Astrologers(IsAvailable, IsActive, IsVerified) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_Astrologers_Rating ON Astrologers(Rating DESC) WHERE IsDeleted = 0 AND IsActive = 1;

-- ConsultationSessions
CREATE NONCLUSTERED INDEX IX_Sessions_UserId ON ConsultationSessions(UserId, CreatedAt DESC) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_Sessions_AstrologerId ON ConsultationSessions(AstrologerId, CreatedAt DESC) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_Sessions_RoomName ON ConsultationSessions(LiveKitRoomName) WHERE LiveKitRoomName IS NOT NULL AND IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_Sessions_Status ON ConsultationSessions(Status) WHERE IsDeleted = 0;

-- RefreshTokens
CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId) WHERE IsRevoked = 0;

-- AuditLogs (time-series — no IsDeleted)
CREATE NONCLUSTERED INDEX IX_AuditLogs_UserId ON AuditLogs(UserId, CreatedAt DESC);
CREATE NONCLUSTERED INDEX IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt DESC);

PRINT 'All indexes created.';
GO

PRINT '=== CynAstroDB Phase 01 Schema Complete ===';
GO
