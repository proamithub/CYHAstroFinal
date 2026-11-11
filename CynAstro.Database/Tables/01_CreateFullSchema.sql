-- ============================================================
-- CYNASTRO PLATFORM — FULL PRODUCTION DATABASE SCHEMA
-- Target Database: CyhAstroDB
-- SQL Server 2016+ Compatibility
-- ============================================================

-- Ensure using CyhAstroDB
USE CyhAstroDB;
GO

-- ============================================================
-- SECTION 1: ASP.NET CORE IDENTITY TABLES
-- ============================================================

IF OBJECT_ID('dbo.AspNetRoles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles (
        Id                  NVARCHAR(450)   NOT NULL,
        Name                NVARCHAR(256)   NULL,
        NormalizedName      NVARCHAR(256)   NULL,
        ConcurrencyStamp    NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_AspNetRoles PRIMARY KEY CLUSTERED (Id)
    );
    CREATE UNIQUE NONCLUSTERED INDEX RoleNameIndex ON dbo.AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
END
GO

IF OBJECT_ID('dbo.AspNetUsers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers (
        Id                      NVARCHAR(450)       NOT NULL,
        UserName                NVARCHAR(256)       NULL,
        NormalizedUserName      NVARCHAR(256)       NULL,
        Email                   NVARCHAR(256)       NULL,
        NormalizedEmail         NVARCHAR(256)       NULL,
        EmailConfirmed          BIT                 NOT NULL DEFAULT 0,
        PasswordHash            NVARCHAR(MAX)       NULL,
        SecurityStamp           NVARCHAR(MAX)       NULL,
        ConcurrencyStamp        NVARCHAR(MAX)       NULL,
        PhoneNumber             NVARCHAR(50)        NULL,
        PhoneNumberConfirmed    BIT                 NOT NULL DEFAULT 0,
        TwoFactorEnabled        BIT                 NOT NULL DEFAULT 0,
        LockoutEnd              DATETIMEOFFSET      NULL,
        LockoutEnabled          BIT                 NOT NULL DEFAULT 1,
        AccessFailedCount       INT                 NOT NULL DEFAULT 0,
        DisplayName             NVARCHAR(150)       NOT NULL DEFAULT '',
        CreatedAt               DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
        LastLoginAt             DATETIME2           NULL,
        CONSTRAINT PK_AspNetUsers PRIMARY KEY CLUSTERED (Id)
    );
    CREATE UNIQUE NONCLUSTERED INDEX UserNameIndex ON dbo.AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
    CREATE NONCLUSTERED INDEX EmailIndex ON dbo.AspNetUsers(NormalizedEmail);
END
GO

IF OBJECT_ID('dbo.AspNetUserRoles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles (
        UserId NVARCHAR(450) NOT NULL,
        RoleId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY CLUSTERED (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_User FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles(RoleId);
END
GO

IF OBJECT_ID('dbo.AspNetUserClaims', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserClaims (
        Id          INT IDENTITY(1,1) NOT NULL,
        UserId      NVARCHAR(450)     NOT NULL,
        ClaimType   NVARCHAR(MAX)     NULL,
        ClaimValue  NVARCHAR(MAX)     NULL,
        CONSTRAINT PK_AspNetUserClaims PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AspNetUserClaims_User FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims(UserId);
END
GO

IF OBJECT_ID('dbo.AspNetUserLogins', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserLogins (
        LoginProvider       NVARCHAR(128) NOT NULL,
        ProviderKey         NVARCHAR(128) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId              NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY CLUSTERED (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_User FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins(UserId);
END
GO

IF OBJECT_ID('dbo.AspNetRoleClaims', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoleClaims (
        Id          INT IDENTITY(1,1) NOT NULL,
        RoleId      NVARCHAR(450)     NOT NULL,
        ClaimType   NVARCHAR(MAX)     NULL,
        ClaimValue  NVARCHAR(MAX)     NULL,
        CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AspNetRoleClaims_Role FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims(RoleId);
END
GO

IF OBJECT_ID('dbo.AspNetUserTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserTokens (
        UserId        NVARCHAR(450) NOT NULL,
        LoginProvider NVARCHAR(128) NOT NULL,
        Name          NVARCHAR(128) NOT NULL,
        Value         NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY CLUSTERED (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_User FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- SECTION 2: USER PROFILE & SETTINGS TABLES
-- ============================================================

IF OBJECT_ID('dbo.UserProfiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserProfiles (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        PublicId            NVARCHAR(32)            NOT NULL, -- Random opaque public ID for URLs
        IdentityId          NVARCHAR(450)           NOT NULL,
        DisplayName         NVARCHAR(150)           NOT NULL,
        Email               NVARCHAR(256)           NOT NULL,
        PhoneNumber         NVARCHAR(50)            NULL,
        Gender              NCHAR(1)                NULL, -- M, F, O
        AvatarUrl           NVARCHAR(500)           NULL,
        IsEmailVerified     BIT                     NOT NULL DEFAULT 0,
        IsPhoneVerified     BIT                     NOT NULL DEFAULT 0,
        IsActive            BIT                     NOT NULL DEFAULT 1,
        PreferredLanguage   NVARCHAR(10)            NOT NULL DEFAULT 'en',
        LastLoginAt         DATETIME2               NULL,
        CreatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2               NULL,
        IsDeleted           BIT                     NOT NULL DEFAULT 0,
        DeletedAt           DATETIME2               NULL,
        CONSTRAINT PK_UserProfiles PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_UserProfiles_PublicId UNIQUE (PublicId),
        CONSTRAINT UQ_UserProfiles_IdentityId UNIQUE (IdentityId)
    );
    CREATE NONCLUSTERED INDEX IX_UserProfiles_Email ON dbo.UserProfiles(Email) WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID('dbo.BirthProfiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BirthProfiles (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        PublicId            NVARCHAR(32)            NOT NULL,
        UserId              BIGINT                  NOT NULL,
        Name                NVARCHAR(150)           NOT NULL,
        Gender              NCHAR(1)                NOT NULL, -- M, F, O
        DateOfBirth         DATE                    NOT NULL,
        TimeOfBirth         TIME(0)                 NULL,
        IsTimeApproximate   BIT                     NOT NULL DEFAULT 0,
        BirthPlace          NVARCHAR(250)           NOT NULL,
        Latitude            DECIMAL(10,7)           NOT NULL DEFAULT 0,
        Longitude           DECIMAL(10,7)           NOT NULL DEFAULT 0,
        TimeZoneId          NVARCHAR(100)           NOT NULL DEFAULT 'Asia/Kolkata',
        UtcOffsetMinutes    INT                     NOT NULL DEFAULT 330, -- IST Default (+5:30)
        IsPrimary           BIT                     NOT NULL DEFAULT 0,
        RelationLabel       NVARCHAR(50)            NOT NULL DEFAULT 'Self', -- Self, Spouse, Child, Parent, Friend
        CreatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2               NULL,
        IsDeleted           BIT                     NOT NULL DEFAULT 0,
        DeletedAt           DATETIME2               NULL,
        CONSTRAINT PK_BirthProfiles PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_BirthProfiles_PublicId UNIQUE (PublicId),
        CONSTRAINT FK_BirthProfiles_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id)
    );
    CREATE NONCLUSTERED INDEX IX_BirthProfiles_UserId ON dbo.BirthProfiles(UserId, IsDeleted);
    CREATE NONCLUSTERED INDEX IX_BirthProfiles_UserId_Primary ON dbo.BirthProfiles(UserId, IsPrimary) WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID('dbo.UserLocations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserLocations (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        UserId          BIGINT                  NOT NULL,
        City            NVARCHAR(100)           NOT NULL,
        State           NVARCHAR(100)           NULL,
        Country         NVARCHAR(100)           NOT NULL DEFAULT 'India',
        Latitude        DECIMAL(10,7)           NOT NULL,
        Longitude       DECIMAL(10,7)           NOT NULL,
        TimeZoneId      NVARCHAR(100)           NOT NULL DEFAULT 'Asia/Kolkata',
        IsCurrent       BIT                     NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2               NULL,
        CONSTRAINT PK_UserLocations PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_UserLocations_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id)
    );
    CREATE NONCLUSTERED INDEX IX_UserLocations_UserId ON dbo.UserLocations(UserId, IsCurrent);
END
GO

IF OBJECT_ID('dbo.UserPreferences', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPreferences (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        UserId              BIGINT                  NOT NULL,
        ReceiveDailyNotification BIT                NOT NULL DEFAULT 1,
        ReceivePromoAlerts  BIT                     NOT NULL DEFAULT 1,
        PreferredChartStyle NVARCHAR(20)            NOT NULL DEFAULT 'NorthIndian', -- NorthIndian, SouthIndian, EastIndian
        PreferredAyanamsha  NVARCHAR(30)            NOT NULL DEFAULT 'Lahiri',
        ThemeMode           NVARCHAR(10)            NOT NULL DEFAULT 'Dark', -- Dark, Light, System
        CreatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2               NULL,
        CONSTRAINT PK_UserPreferences PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_UserPreferences_User UNIQUE (UserId),
        CONSTRAINT FK_UserPreferences_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id)
    );
END
GO

IF OBJECT_ID('dbo.RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        UserId              BIGINT                  NOT NULL,
        TokenHash           NVARCHAR(256)           NOT NULL, -- SHA-256 hash of refresh token
        DeviceInfo          NVARCHAR(500)           NULL,
        IpAddress           NVARCHAR(45)            NULL,
        ExpiresAt           DATETIME2               NOT NULL,
        RevokedAt           DATETIME2               NULL,
        ReplacedByTokenHash NVARCHAR(256)           NULL,
        CreatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        IsRevoked           BIT                     NOT NULL DEFAULT 0,
        CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_RefreshTokens_Hash UNIQUE (TokenHash),
        CONSTRAINT FK_RefreshTokens_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id)
    );
    CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens(UserId, IsRevoked);
END
GO

IF OBJECT_ID('dbo.UserSessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSessions (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        UserId          BIGINT                  NOT NULL,
        SessionToken    NVARCHAR(128)           NOT NULL,
        DeviceType      NVARCHAR(50)            NOT NULL DEFAULT 'Web', -- Web, Android, iOS
        UserAgent       NVARCHAR(500)           NULL,
        IpAddress       NVARCHAR(45)            NULL,
        LastActivityAt  DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        ExpiresAt       DATETIME2               NOT NULL,
        IsActive        BIT                     NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_UserSessions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_UserSessions_Token UNIQUE (SessionToken),
        CONSTRAINT FK_UserSessions_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id)
    );
    CREATE NONCLUSTERED INDEX IX_UserSessions_UserId ON dbo.UserSessions(UserId, IsActive);
END
GO

-- ============================================================
-- SECTION 3: WALLET & FINANCIAL TABLES (IMMUTABLE LEDGER)
-- ============================================================

IF OBJECT_ID('dbo.Wallets', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Wallets (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        UserId          BIGINT                  NOT NULL,
        PaidBalance     DECIMAL(12,2)           NOT NULL DEFAULT 0.00,
        BonusBalance    DECIMAL(12,2)           NOT NULL DEFAULT 0.00,
        CurrencyCode    NCHAR(3)                NOT NULL DEFAULT 'INR',
        RowVersion      BIGINT                  NOT NULL DEFAULT 0, -- Optimistic concurrency control
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2               NULL,
        IsDeleted       BIT                     NOT NULL DEFAULT 0,
        CONSTRAINT PK_Wallets PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Wallets_UserId UNIQUE (UserId),
        CONSTRAINT FK_Wallets_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id),
        CONSTRAINT CK_Wallets_PaidBalance CHECK (PaidBalance >= 0.00),
        CONSTRAINT CK_Wallets_BonusBalance CHECK (BonusBalance >= 0.00)
    );
END
GO

IF OBJECT_ID('dbo.WalletTransactions', 'U') IS NULL
BEGIN
    -- Immutable Ledger: Insert Only, Never Update, Never Delete
    CREATE TABLE dbo.WalletTransactions (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        WalletId            BIGINT                  NOT NULL,
        UserId              BIGINT                  NOT NULL,
        TransactionType     NVARCHAR(20)            NOT NULL, -- CREDIT, DEBIT, BONUS, REFUND
        BalanceType         NVARCHAR(10)            NOT NULL, -- PAID, BONUS
        Amount              DECIMAL(12,2)           NOT NULL,
        BalanceBefore       DECIMAL(12,2)           NOT NULL,
        BalanceAfter        DECIMAL(12,2)           NOT NULL,
        ReferenceType       NVARCHAR(30)            NOT NULL, -- RECHARGE, CONSULTATION, REPORT, REFUND, ADMIN_ADJUST
        ReferenceId         BIGINT                  NULL,
        Description         NVARCHAR(500)           NOT NULL,
        IdempotencyKey      NVARCHAR(100)           NOT NULL,
        CreatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        IsDeleted           BIT                     NOT NULL DEFAULT 0,
        CONSTRAINT PK_WalletTransactions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_WalletTransactions_IdempotencyKey UNIQUE (IdempotencyKey),
        CONSTRAINT FK_WalletTransactions_Wallet FOREIGN KEY (WalletId) REFERENCES dbo.Wallets(Id),
        CONSTRAINT FK_WalletTransactions_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id),
        CONSTRAINT CK_WalletTransactions_Amount CHECK (Amount > 0.00),
        CONSTRAINT CK_WalletTransactions_Type CHECK (TransactionType IN ('CREDIT', 'DEBIT', 'BONUS', 'REFUND')),
        CONSTRAINT CK_WalletTransactions_BalanceType CHECK (BalanceType IN ('PAID', 'BONUS'))
    );
    CREATE NONCLUSTERED INDEX IX_WalletTransactions_UserDate ON dbo.WalletTransactions(UserId, CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_WalletTransactions_Ref ON dbo.WalletTransactions(ReferenceType, ReferenceId);
END
GO

IF OBJECT_ID('dbo.RechargePackages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RechargePackages (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        Name            NVARCHAR(100)           NOT NULL,
        Amount          DECIMAL(12,2)           NOT NULL,
        BonusPercentage DECIMAL(5,2)            NOT NULL DEFAULT 0.00,
        BonusAmount     DECIMAL(12,2)           NOT NULL DEFAULT 0.00,
        IsPopular       BIT                     NOT NULL DEFAULT 0,
        DisplayOrder    INT                     NOT NULL DEFAULT 0,
        IsActive        BIT                     NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2               NULL,
        CONSTRAINT PK_RechargePackages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_RechargePackages_Amount CHECK (Amount > 0.00)
    );
END
GO

IF OBJECT_ID('dbo.BonusRules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BonusRules (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        RuleName            NVARCHAR(100)           NOT NULL,
        MinRechargeAmount   DECIMAL(12,2)           NOT NULL DEFAULT 0.00,
        BonusPercentage     DECIMAL(5,2)            NOT NULL DEFAULT 0.00,
        FlatBonusAmount     DECIMAL(12,2)           NOT NULL DEFAULT 0.00,
        ValidFrom           DATETIME2               NOT NULL,
        ValidUntil          DATETIME2               NULL,
        IsFirstRechargeOnly BIT                     NOT NULL DEFAULT 0,
        IsActive            BIT                     NOT NULL DEFAULT 1,
        CreatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_BonusRules PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        PublicId            NVARCHAR(32)            NOT NULL,
        UserId              BIGINT                  NOT NULL,
        PackageId           BIGINT                  NULL,
        Amount              DECIMAL(12,2)           NOT NULL,
        Currency            NCHAR(3)                NOT NULL DEFAULT 'INR',
        PaymentGateway      NVARCHAR(50)            NOT NULL, -- Razorpay, Cashfree, Stripe
        GatewayOrderId      NVARCHAR(150)           NULL,
        GatewayPaymentId    NVARCHAR(150)           NULL,
        GatewaySignature    NVARCHAR(500)           NULL,
        Status              NVARCHAR(20)            NOT NULL DEFAULT 'INITIATED', -- INITIATED, SUCCESS, FAILED, REFUNDED
        FailureReason       NVARCHAR(500)           NULL,
        IdempotencyKey      NVARCHAR(100)           NOT NULL,
        PaidAt              DATETIME2               NULL,
        CreatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2               NULL,
        CONSTRAINT PK_Payments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Payments_PublicId UNIQUE (PublicId),
        CONSTRAINT UQ_Payments_IdempotencyKey UNIQUE (IdempotencyKey),
        CONSTRAINT FK_Payments_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id),
        CONSTRAINT FK_Payments_Package FOREIGN KEY (PackageId) REFERENCES dbo.RechargePackages(Id)
    );
    CREATE NONCLUSTERED INDEX IX_Payments_UserId ON dbo.Payments(UserId, CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_Payments_GatewayOrder ON dbo.Payments(GatewayOrderId);
END
GO

IF OBJECT_ID('dbo.PaymentWebhookLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentWebhookLogs (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        Gateway         NVARCHAR(50)            NOT NULL,
        EventId         NVARCHAR(150)           NULL,
        EventType       NVARCHAR(100)           NOT NULL,
        Payload         NVARCHAR(MAX)           NOT NULL,
        Headers         NVARCHAR(MAX)           NULL,
        IsSignatureValid BIT                    NOT NULL DEFAULT 0,
        IsProcessed     BIT                     NOT NULL DEFAULT 0,
        ProcessedAt     DATETIME2               NULL,
        ErrorMessage    NVARCHAR(MAX)           NULL,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_PaymentWebhookLogs PRIMARY KEY CLUSTERED (Id)
    );
    CREATE NONCLUSTERED INDEX IX_PaymentWebhookLogs_Event ON dbo.PaymentWebhookLogs(Gateway, EventId);
END
GO

-- ============================================================
-- SECTION 4: ASTROLOGY CONTENT & CALCULATION TABLES
-- ============================================================

IF OBJECT_ID('dbo.KundliCharts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.KundliCharts (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        PublicId            NVARCHAR(32)            NOT NULL,
        BirthProfileId      BIGINT                  NOT NULL,
        ChartStyle          NVARCHAR(20)            NOT NULL DEFAULT 'NorthIndian',
        Ayanamsha           NVARCHAR(30)            NOT NULL DEFAULT 'Lahiri',
        AscendantSign       INT                     NOT NULL,
        MoonSign            INT                     NOT NULL,
        SunSign             INT                     NOT NULL,
        PlanetaryPositions  NVARCHAR(MAX)           NOT NULL, -- JSON formatted computed planetary longitudes & nakshatras
        HousePositions      NVARCHAR(MAX)           NOT NULL, -- JSON formatted 12 houses
        DashaPeriods        NVARCHAR(MAX)           NOT NULL, -- JSON formatted Vimshottari dasha hierarchy
        DivisionalCharts    NVARCHAR(MAX)           NULL,     -- JSON formatted D9, D10, etc.
        DoshaEvaluations    NVARCHAR(MAX)           NULL,     -- JSON (Mangal, Kaal Sarp, Sade Sati)
        CalculatedAt        DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_KundliCharts PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_KundliCharts_PublicId UNIQUE (PublicId),
        CONSTRAINT FK_KundliCharts_BirthProfile FOREIGN KEY (BirthProfileId) REFERENCES dbo.BirthProfiles(Id)
    );
    CREATE NONCLUSTERED INDEX IX_KundliCharts_Profile ON dbo.KundliCharts(BirthProfileId);
END
GO

IF OBJECT_ID('dbo.HoroscopeContents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HoroscopeContents (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        ZodiacSign      INT                     NOT NULL, -- 1 = Aries, 12 = Pisces
        HoroscopeType   NVARCHAR(20)            NOT NULL, -- DAILY, WEEKLY, MONTHLY, YEARLY
        TargetDate      DATE                    NOT NULL,
        GeneralText     NVARCHAR(MAX)           NOT NULL,
        CareerText      NVARCHAR(MAX)           NULL,
        LoveText        NVARCHAR(MAX)           NULL,
        FinanceText     NVARCHAR(MAX)           NULL,
        HealthText      NVARCHAR(MAX)           NULL,
        LuckyNumber     INT                     NULL,
        LuckyColor      NVARCHAR(50)            NULL,
        RatingPercentage INT                    NULL,
        LanguageCode    NVARCHAR(10)            NOT NULL DEFAULT 'en',
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_HoroscopeContents PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_HoroscopeContents_UniqueEntry UNIQUE (ZodiacSign, HoroscopeType, TargetDate, LanguageCode)
    );
    CREATE NONCLUSTERED INDEX IX_HoroscopeContents_Lookup ON dbo.HoroscopeContents(ZodiacSign, HoroscopeType, TargetDate, LanguageCode);
END
GO

IF OBJECT_ID('dbo.PanchangRecords', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PanchangRecords (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        TargetDate      DATE                    NOT NULL,
        Latitude        DECIMAL(10,7)           NOT NULL,
        Longitude       DECIMAL(10,7)           NOT NULL,
        TimeZoneId      NVARCHAR(100)           NOT NULL DEFAULT 'Asia/Kolkata',
        Sunrise         TIME(0)                 NOT NULL,
        Sunset          TIME(0)                 NOT NULL,
        Moonrise        TIME(0)                 NULL,
        Moonset         TIME(0)                 NULL,
        TithiName       NVARCHAR(100)           NOT NULL,
        TithiEndTime    TIME(0)                 NULL,
        NakshatraName   NVARCHAR(100)           NOT NULL,
        NakshatraEndTime TIME(0)                NULL,
        YogaName        NVARCHAR(100)           NOT NULL,
        KaranaName      NVARCHAR(100)           NOT NULL,
        RahuKaalStart   TIME(0)                 NULL,
        RahuKaalEnd     TIME(0)                 NULL,
        AbhijitStart    TIME(0)                 NULL,
        AbhijitEnd      TIME(0)                 NULL,
        PanchangJson    NVARCHAR(MAX)           NOT NULL, -- Full detailed computation JSON
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_PanchangRecords PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_PanchangRecords_Lookup UNIQUE (TargetDate, Latitude, Longitude)
    );
END
GO

IF OBJECT_ID('dbo.AstrologyReports', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AstrologyReports (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        PublicId        NVARCHAR(32)            NOT NULL,
        UserId          BIGINT                  NOT NULL,
        BirthProfileId  BIGINT                  NOT NULL,
        ReportType      NVARCHAR(50)            NOT NULL, -- PREMIUM_KUNDLI, MARRIAGE, CAREER, FINANCE, ANNUAL
        Status          NVARCHAR(20)            NOT NULL DEFAULT 'QUEUED', -- QUEUED, PROCESSING, COMPLETED, FAILED
        PdfFilePath     NVARCHAR(500)           NULL,
        PdfFileSizeBytes BIGINT                 NULL,
        TotalPages      INT                     NULL,
        PriceCharged    DECIMAL(12,2)           NOT NULL DEFAULT 0.00,
        ErrorMessage    NVARCHAR(MAX)           NULL,
        GeneratedAt     DATETIME2               NULL,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_AstrologyReports PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_AstrologyReports_PublicId UNIQUE (PublicId),
        CONSTRAINT FK_AstrologyReports_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id),
        CONSTRAINT FK_AstrologyReports_Profile FOREIGN KEY (BirthProfileId) REFERENCES dbo.BirthProfiles(Id)
    );
    CREATE NONCLUSTERED INDEX IX_AstrologyReports_User ON dbo.AstrologyReports(UserId, CreatedAt DESC);
END
GO

-- ============================================================
-- SECTION 5: ASTROLOGERS & CONSULTATION SESSIONS
-- ============================================================

IF OBJECT_ID('dbo.Astrologers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Astrologers (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        PublicId            NVARCHAR(32)            NOT NULL,
        UserId              BIGINT                  NOT NULL,
        DisplayName         NVARCHAR(150)           NOT NULL,
        Specializations     NVARCHAR(MAX)           NOT NULL DEFAULT '[]', -- JSON array: ["Vedic", "Tarot", "Numerology"]
        Languages           NVARCHAR(500)           NOT NULL DEFAULT '["hi","en"]', -- JSON array
        Bio                 NVARCHAR(2000)          NULL,
        ChatRatePerMinute   DECIMAL(8,2)            NOT NULL DEFAULT 10.00,
        AudioRatePerMinute  DECIMAL(8,2)            NOT NULL DEFAULT 15.00,
        VideoRatePerMinute  DECIMAL(8,2)            NOT NULL DEFAULT 20.00,
        Rating              DECIMAL(3,2)            NOT NULL DEFAULT 5.00,
        TotalReviews        INT                     NOT NULL DEFAULT 0,
        TotalConsultations  INT                     NOT NULL DEFAULT 0,
        ExperienceYears     INT                     NOT NULL DEFAULT 1,
        IsAvailable         BIT                     NOT NULL DEFAULT 0,
        IsVerified          BIT                     NOT NULL DEFAULT 0,
        IsActive            BIT                     NOT NULL DEFAULT 1,
        ProfilePhotoUrl     NVARCHAR(500)           NULL,
        CommissionRatePct   DECIMAL(5,2)            NOT NULL DEFAULT 20.00, -- Platform Commission %
        CreatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2               NULL,
        IsDeleted           BIT                     NOT NULL DEFAULT 0,
        DeletedAt           DATETIME2               NULL,
        CONSTRAINT PK_Astrologers PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Astrologers_PublicId UNIQUE (PublicId),
        CONSTRAINT UQ_Astrologers_UserId UNIQUE (UserId),
        CONSTRAINT FK_Astrologers_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id),
        CONSTRAINT CK_Astrologers_Rating CHECK (Rating BETWEEN 0.00 AND 5.00)
    );
    CREATE NONCLUSTERED INDEX IX_Astrologers_Listing ON dbo.Astrologers(IsAvailable, IsActive, IsVerified, Rating DESC) WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID('dbo.AstrologerAvailability', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AstrologerAvailability (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        AstrologerId    BIGINT                  NOT NULL,
        DayOfWeek       INT                     NOT NULL, -- 0 = Sunday, 6 = Saturday
        StartTime       TIME(0)                 NOT NULL,
        EndTime         TIME(0)                 NOT NULL,
        IsActive        BIT                     NOT NULL DEFAULT 1,
        CONSTRAINT PK_AstrologerAvailability PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AstrologerAvailability_Astrologer FOREIGN KEY (AstrologerId) REFERENCES dbo.Astrologers(Id)
    );
END
GO

IF OBJECT_ID('dbo.ConsultationSessions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsultationSessions (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        PublicId            NVARCHAR(32)            NOT NULL,
        UserId              BIGINT                  NOT NULL,
        AstrologerId        BIGINT                  NOT NULL,
        SessionType         NVARCHAR(10)            NOT NULL, -- CHAT, AUDIO, VIDEO
        Status              NVARCHAR(20)            NOT NULL DEFAULT 'REQUESTED', -- REQUESTED, ACCEPTED, ACTIVE, ENDED, BILLED, CANCELLED, FAILED
        AcceptedAt          DATETIME2               NULL,
        ConnectedAt         DATETIME2               NULL, -- Server verified media/chat connection
        EndedAt             DATETIME2               NULL,
        DurationSeconds     INT                     NOT NULL DEFAULT 0, -- Server calculated only!
        RatePerMinute       DECIMAL(8,2)            NOT NULL,
        TotalCharge         DECIMAL(12,2)           NOT NULL DEFAULT 0.00,
        LiveKitRoomName     NVARCHAR(100)           NULL,
        EndReason           NVARCHAR(50)            NULL, -- USER_END, ASTROLOGER_END, TIMEOUT, DISCONNECT, INSUFFICIENT_FUNDS
        IsBilled            BIT                     NOT NULL DEFAULT 0,
        IdempotencyKey      NVARCHAR(100)           NULL,
        CreatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2               NULL,
        IsDeleted           BIT                     NOT NULL DEFAULT 0,
        CONSTRAINT PK_ConsultationSessions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_ConsultationSessions_PublicId UNIQUE (PublicId),
        CONSTRAINT FK_ConsultationSessions_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id),
        CONSTRAINT FK_ConsultationSessions_Astrologer FOREIGN KEY (AstrologerId) REFERENCES dbo.Astrologers(Id),
        CONSTRAINT CK_ConsultationSessions_Type CHECK (SessionType IN ('CHAT', 'AUDIO', 'VIDEO')),
        CONSTRAINT CK_ConsultationSessions_Status CHECK (Status IN ('REQUESTED', 'ACCEPTED', 'ACTIVE', 'ENDED', 'BILLED', 'CANCELLED', 'FAILED'))
    );
    CREATE NONCLUSTERED INDEX IX_ConsultationSessions_User ON dbo.ConsultationSessions(UserId, CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_ConsultationSessions_Astrologer ON dbo.ConsultationSessions(AstrologerId, CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_ConsultationSessions_Room ON dbo.ConsultationSessions(LiveKitRoomName) WHERE LiveKitRoomName IS NOT NULL;
END
GO

IF OBJECT_ID('dbo.ConsultationParticipants', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsultationParticipants (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        SessionId           BIGINT                  NOT NULL,
        UserId              BIGINT                  NOT NULL,
        RoleInSession       NVARCHAR(20)            NOT NULL, -- USER, ASTROLOGER
        JoinedAt            DATETIME2               NULL,
        LeftAt              DATETIME2               NULL,
        ConnectionStatus    NVARCHAR(20)            NOT NULL DEFAULT 'OFFLINE', -- ONLINE, OFFLINE, RECONNECTING
        CONSTRAINT PK_ConsultationParticipants PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ConsultationParticipants_Session FOREIGN KEY (SessionId) REFERENCES dbo.ConsultationSessions(Id),
        CONSTRAINT FK_ConsultationParticipants_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id)
    );
END
GO

IF OBJECT_ID('dbo.ConsultationMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsultationMessages (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        SessionId       BIGINT                  NOT NULL,
        SenderId        BIGINT                  NOT NULL,
        SenderType      NVARCHAR(20)            NOT NULL, -- USER, ASTROLOGER, SYSTEM
        MessageText     NVARCHAR(MAX)           NOT NULL,
        AttachmentUrl   NVARCHAR(500)           NULL,
        IsRead          BIT                     NOT NULL DEFAULT 0,
        ReadAt          DATETIME2               NULL,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_ConsultationMessages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ConsultationMessages_Session FOREIGN KEY (SessionId) REFERENCES dbo.ConsultationSessions(Id)
    );
    CREATE NONCLUSTERED INDEX IX_ConsultationMessages_Session ON dbo.ConsultationMessages(SessionId, CreatedAt ASC);
END
GO

IF OBJECT_ID('dbo.ConsultationEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsultationEvents (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        SessionId       BIGINT                  NOT NULL,
        EventType       NVARCHAR(50)            NOT NULL, -- JOINED, LEFT, RECONNECTED, PULSE_BILLING, FORCE_TERMINATE
        EventPayload    NVARCHAR(MAX)           NULL,     -- JSON details
        ServerTimestamp DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_ConsultationEvents PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ConsultationEvents_Session FOREIGN KEY (SessionId) REFERENCES dbo.ConsultationSessions(Id)
    );
    CREATE NONCLUSTERED INDEX IX_ConsultationEvents_Session ON dbo.ConsultationEvents(SessionId, ServerTimestamp ASC);
END
GO

IF OBJECT_ID('dbo.ConsultationBilling', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsultationBilling (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        SessionId           BIGINT                  NOT NULL,
        UserId              BIGINT                  NOT NULL,
        AstrologerId        BIGINT                  NOT NULL,
        TotalMinutesBilled  DECIMAL(8,2)            NOT NULL,
        RatePerMinute       DECIMAL(8,2)            NOT NULL,
        GrossAmount         DECIMAL(12,2)           NOT NULL,
        DiscountAmount      DECIMAL(12,2)           NOT NULL DEFAULT 0.00,
        NetAmount           DECIMAL(12,2)           NOT NULL,
        AstrologerShare     DECIMAL(12,2)           NOT NULL,
        PlatformShare       DECIMAL(12,2)           NOT NULL,
        WalletTxId          BIGINT                  NULL,
        CreatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_ConsultationBilling PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_ConsultationBilling_Session UNIQUE (SessionId),
        CONSTRAINT FK_ConsultationBilling_Session FOREIGN KEY (SessionId) REFERENCES dbo.ConsultationSessions(Id)
    );
END
GO

IF OBJECT_ID('dbo.AstrologerEarnings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AstrologerEarnings (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        AstrologerId    BIGINT                  NOT NULL,
        SessionId       BIGINT                  NULL,
        Amount          DECIMAL(12,2)           NOT NULL,
        EarningType     NVARCHAR(30)            NOT NULL, -- CONSULTATION, BONUS, TIP, ADJUSTMENT
        Status          NVARCHAR(20)            NOT NULL DEFAULT 'UNSETTLED', -- UNSETTLED, SETTLED, WITHHELD
        SettlementId    BIGINT                  NULL,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_AstrologerEarnings PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AstrologerEarnings_Astrologer FOREIGN KEY (AstrologerId) REFERENCES dbo.Astrologers(Id)
    );
    CREATE NONCLUSTERED INDEX IX_AstrologerEarnings_Status ON dbo.AstrologerEarnings(AstrologerId, Status);
END
GO

IF OBJECT_ID('dbo.PlatformCommissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlatformCommissions (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        SessionId       BIGINT                  NOT NULL,
        GrossAmount     DECIMAL(12,2)           NOT NULL,
        CommissionPct   DECIMAL(5,2)            NOT NULL,
        CommissionAmount DECIMAL(12,2)          NOT NULL,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_PlatformCommissions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_PlatformCommissions_Session FOREIGN KEY (SessionId) REFERENCES dbo.ConsultationSessions(Id)
    );
END
GO

IF OBJECT_ID('dbo.Settlements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Settlements (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        PublicId        NVARCHAR(32)            NOT NULL,
        AstrologerId    BIGINT                  NOT NULL,
        Amount          DECIMAL(12,2)           NOT NULL,
        PayoutMethod    NVARCHAR(50)            NOT NULL, -- UPI, BANK_TRANSFER, NEFT
        PayoutReference NVARCHAR(150)           NULL,
        Status          NVARCHAR(20)            NOT NULL DEFAULT 'PENDING', -- PENDING, COMPLETED, FAILED
        ProcessedAt     DATETIME2               NULL,
        Remarks         NVARCHAR(500)           NULL,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_Settlements PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Settlements_PublicId UNIQUE (PublicId),
        CONSTRAINT FK_Settlements_Astrologer FOREIGN KEY (AstrologerId) REFERENCES dbo.Astrologers(Id)
    );
END
GO

-- ============================================================
-- SECTION 6: CMS, LOCALIZATION, AI & SYSTEM AUDIT
-- ============================================================

IF OBJECT_ID('dbo.Notifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications (
        Id          BIGINT IDENTITY(1,1)    NOT NULL,
        UserId      BIGINT                  NOT NULL,
        Title       NVARCHAR(200)           NOT NULL,
        Message     NVARCHAR(1000)          NOT NULL,
        ActionUrl   NVARCHAR(500)           NULL,
        Type        NVARCHAR(50)            NOT NULL DEFAULT 'GENERAL', -- CONSULTATION, WALLET, HOROSCOPE, PROMO
        IsRead      BIT                     NOT NULL DEFAULT 0,
        ReadAt      DATETIME2               NULL,
        CreatedAt   DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id)
    );
    CREATE NONCLUSTERED INDEX IX_Notifications_User ON dbo.Notifications(UserId, IsRead, CreatedAt DESC);
END
GO

IF OBJECT_ID('dbo.Languages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Languages (
        Id          INT IDENTITY(1,1)       NOT NULL,
        Code        NVARCHAR(10)            NOT NULL, -- en, hi, bn, te, mr, ta, gu
        Name        NVARCHAR(100)           NOT NULL, -- English, Hindi, Bengali
        NativeName  NVARCHAR(100)           NOT NULL, -- English, हिन्दी, বাংলা
        IsActive    BIT                     NOT NULL DEFAULT 1,
        IsDefault   BIT                     NOT NULL DEFAULT 0,
        CONSTRAINT PK_Languages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Languages_Code UNIQUE (Code)
    );
END
GO

IF OBJECT_ID('dbo.Translations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Translations (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        LanguageCode    NVARCHAR(10)            NOT NULL,
        ResourceKey     NVARCHAR(150)           NOT NULL,
        TranslationValue NVARCHAR(MAX)          NOT NULL,
        UpdatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_Translations PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Translations_KeyLang UNIQUE (LanguageCode, ResourceKey)
    );
    CREATE NONCLUSTERED INDEX IX_Translations_Lookup ON dbo.Translations(LanguageCode, ResourceKey);
END
GO

IF OBJECT_ID('dbo.AIConversations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIConversations (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        PublicId        NVARCHAR(32)            NOT NULL,
        UserId          BIGINT                  NOT NULL,
        BirthProfileId  BIGINT                  NULL,
        Topic           NVARCHAR(100)           NOT NULL DEFAULT 'General',
        Title           NVARCHAR(200)           NOT NULL,
        ConversationJson NVARCHAR(MAX)          NOT NULL, -- Archived chat messages
        TokensUsed      INT                     NOT NULL DEFAULT 0,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2               NULL,
        CONSTRAINT PK_AIConversations PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_AIConversations_PublicId UNIQUE (PublicId),
        CONSTRAINT FK_AIConversations_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id)
    );
    CREATE NONCLUSTERED INDEX IX_AIConversations_User ON dbo.AIConversations(UserId, CreatedAt DESC);
END
GO

IF OBJECT_ID('dbo.AIUsageLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIUsageLogs (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        UserId          BIGINT                  NOT NULL,
        PromptTokens    INT                     NOT NULL,
        CompletionTokens INT                    NOT NULL,
        TotalTokens     INT                     NOT NULL,
        ModelName       NVARCHAR(100)           NOT NULL,
        LatencyMs       INT                     NOT NULL,
        CostEstimated   DECIMAL(8,4)            NOT NULL DEFAULT 0.0000,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_AIUsageLogs PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AIUsageLogs_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id)
    );
END
GO

IF OBJECT_ID('dbo.ReportTemplates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportTemplates (
        Id              INT IDENTITY(1,1)       NOT NULL,
        ReportType      NVARCHAR(50)            NOT NULL,
        Version         INT                     NOT NULL DEFAULT 1,
        TemplateName    NVARCHAR(150)           NOT NULL,
        CoverDesignKey  NVARCHAR(50)            NOT NULL DEFAULT 'VedicGold',
        Price           DECIMAL(12,2)           NOT NULL DEFAULT 199.00,
        IsActive        BIT                     NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_ReportTemplates PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_ReportTemplates_Type UNIQUE (ReportType)
    );
END
GO

IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs (
        Id          BIGINT IDENTITY(1,1)    NOT NULL,
        UserId      BIGINT                  NULL,
        Action      NVARCHAR(100)           NOT NULL,
        EntityType  NVARCHAR(100)           NULL,
        EntityId    NVARCHAR(100)           NULL,
        OldValues   NVARCHAR(MAX)           NULL, -- JSON
        NewValues   NVARCHAR(MAX)           NULL, -- JSON
        IpAddress   NVARCHAR(45)            NULL,
        UserAgent   NVARCHAR(500)           NULL,
        CreatedAt   DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id)
    );
    CREATE NONCLUSTERED INDEX IX_AuditLogs_User ON dbo.AuditLogs(UserId, CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Date ON dbo.AuditLogs(CreatedAt DESC);
END
GO

IF OBJECT_ID('dbo.SecurityEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SecurityEvents (
        Id              BIGINT IDENTITY(1,1)    NOT NULL,
        UserId          BIGINT                  NULL,
        EventType       NVARCHAR(100)           NOT NULL, -- LOGIN_FAILED, ACCOUNT_LOCKED, PASSWORD_RESET_REQUEST, SUSPICIOUS_REQUEST
        Severity        NVARCHAR(20)            NOT NULL DEFAULT 'WARNING', -- INFO, WARNING, CRITICAL
        Description     NVARCHAR(1000)          NOT NULL,
        IpAddress       NVARCHAR(45)            NULL,
        UserAgent       NVARCHAR(500)           NULL,
        CreatedAt       DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_SecurityEvents PRIMARY KEY CLUSTERED (Id)
    );
    CREATE NONCLUSTERED INDEX IX_SecurityEvents_SeverityDate ON dbo.SecurityEvents(Severity, CreatedAt DESC);
END
GO

-- ============================================================
-- SECTION 7: SEED ESSENTIAL LOOKUPS & SYSTEM DATA
-- ============================================================

-- Roles
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE Name = 'SuperAdmin')
    INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (NEWID(), 'SuperAdmin', 'SUPERADMIN', NEWID());
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE Name = 'Admin')
    INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE Name = 'Astrologer')
    INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (NEWID(), 'Astrologer', 'ASTROLOGER', NEWID());
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE Name = 'User')
    INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (NEWID(), 'User', 'USER', NEWID());
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE Name = 'Support')
    INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (NEWID(), 'Support', 'SUPPORT', NEWID());
GO

-- Languages
IF NOT EXISTS (SELECT 1 FROM dbo.Languages WHERE Code = 'en')
    INSERT INTO dbo.Languages (Code, Name, NativeName, IsActive, IsDefault) VALUES ('en', 'English', 'English', 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.Languages WHERE Code = 'hi')
    INSERT INTO dbo.Languages (Code, Name, NativeName, IsActive, IsDefault) VALUES ('hi', 'Hindi', N'हिन्दी', 1, 0);
IF NOT EXISTS (SELECT 1 FROM dbo.Languages WHERE Code = 'bn')
    INSERT INTO dbo.Languages (Code, Name, NativeName, IsActive, IsDefault) VALUES ('bn', 'Bengali', N'বাংলা', 1, 0);
IF NOT EXISTS (SELECT 1 FROM dbo.Languages WHERE Code = 'mr')
    INSERT INTO dbo.Languages (Code, Name, NativeName, IsActive, IsDefault) VALUES ('mr', 'Marathi', N'मराठी', 1, 0);
IF NOT EXISTS (SELECT 1 FROM dbo.Languages WHERE Code = 'gu')
    INSERT INTO dbo.Languages (Code, Name, NativeName, IsActive, IsDefault) VALUES ('gu', 'Gujarati', N'ગુજરાતી', 1, 0);
GO

-- Recharge Packages
IF NOT EXISTS (SELECT 1 FROM dbo.RechargePackages WHERE Amount = 100.00)
    INSERT INTO dbo.RechargePackages (Name, Amount, BonusPercentage, BonusAmount, IsPopular, DisplayOrder)
    VALUES ('Starter', 100.00, 0.00, 0.00, 0, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.RechargePackages WHERE Amount = 250.00)
    INSERT INTO dbo.RechargePackages (Name, Amount, BonusPercentage, BonusAmount, IsPopular, DisplayOrder)
    VALUES ('Popular Value', 250.00, 10.00, 25.00, 1, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.RechargePackages WHERE Amount = 500.00)
    INSERT INTO dbo.RechargePackages (Name, Amount, BonusPercentage, BonusAmount, IsPopular, DisplayOrder)
    VALUES ('Best Value', 500.00, 20.00, 100.00, 0, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.RechargePackages WHERE Amount = 1000.00)
    INSERT INTO dbo.RechargePackages (Name, Amount, BonusPercentage, BonusAmount, IsPopular, DisplayOrder)
    VALUES ('Premium VIP', 1000.00, 25.00, 250.00, 0, 4);
GO

-- Report Templates
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplates WHERE ReportType = 'PREMIUM_KUNDLI')
    INSERT INTO dbo.ReportTemplates (ReportType, TemplateName, Price) VALUES ('PREMIUM_KUNDLI', 'Comprehensive Vedic Kundli Report', 199.00);
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplates WHERE ReportType = 'MARRIAGE')
    INSERT INTO dbo.ReportTemplates (ReportType, TemplateName, Price) VALUES ('MARRIAGE', 'Kundli Matching & Marriage Analysis', 299.00);
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplates WHERE ReportType = 'CAREER')
    INSERT INTO dbo.ReportTemplates (ReportType, TemplateName, Price) VALUES ('CAREER', 'Career & Profession Astrology Report', 249.00);
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplates WHERE ReportType = 'FINANCE')
    INSERT INTO dbo.ReportTemplates (ReportType, TemplateName, Price) VALUES ('FINANCE', 'Wealth & Finance Astrology Report', 249.00);
IF NOT EXISTS (SELECT 1 FROM dbo.ReportTemplates WHERE ReportType = 'ANNUAL')
    INSERT INTO dbo.ReportTemplates (ReportType, TemplateName, Price) VALUES ('ANNUAL', 'Yearly Varshphal Transit Report', 399.00);
GO
