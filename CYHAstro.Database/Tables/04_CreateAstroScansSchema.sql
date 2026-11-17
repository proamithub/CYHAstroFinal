-- =========================================================================
-- CYHAstro - AstroScans & AstroScanSettings Tables
-- Migration: 04_CreateAstroScansSchema.sql
-- Supports Palm Scan (Hastarekha) & Head/Face Scan (Samudrika Shastra)
-- =========================================================================

IF OBJECT_ID('dbo.AstroScans', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AstroScans (
        Id                  BIGINT IDENTITY(1,1)    NOT NULL,
        PublicId            NVARCHAR(32)            NOT NULL,
        UserId              BIGINT                  NOT NULL,
        BirthProfileId      BIGINT                  NULL,
        ScanType            NVARCHAR(20)            NOT NULL, -- 'PALM', 'HEAD'
        HandType            NVARCHAR(10)            NULL,     -- 'RIGHT', 'LEFT', 'DOMINANT'
        ImageUrl            NVARCHAR(500)           NOT NULL,
        Status              NVARCHAR(20)            NOT NULL DEFAULT 'COMPLETED', -- 'COMPLETED', 'FAILED', 'FLAGGED'
        ConfidenceScore     DECIMAL(5,2)            NULL,
        ExtractedFeaturesJson NVARCHAR(MAX)         NULL,     -- JSON array of detected lines/mounts/marks
        AnalysisReportJson  NVARCHAR(MAX)           NOT NULL, -- Full structured Vedic interpretation report
        AdminNotes          NVARCHAR(MAX)           NULL,     -- Astrologer / Admin manual verification notes
        IsAdminReviewed     BIT                     NOT NULL DEFAULT 0,
        CostCharged         DECIMAL(10,2)           NOT NULL DEFAULT 0.00,
        TokensUsed          INT                     NOT NULL DEFAULT 0,
        Language            NVARCHAR(10)            NOT NULL DEFAULT 'hi',
        CreatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt           DATETIME2               NULL,

        CONSTRAINT PK_AstroScans PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_AstroScans_PublicId UNIQUE (PublicId),
        CONSTRAINT FK_AstroScans_User FOREIGN KEY (UserId) REFERENCES dbo.UserProfiles(Id),
        CONSTRAINT FK_AstroScans_BirthProfile FOREIGN KEY (BirthProfileId) REFERENCES dbo.BirthProfiles(Id)
    );

    CREATE NONCLUSTERED INDEX IX_AstroScans_User ON dbo.AstroScans(UserId, CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_AstroScans_Type ON dbo.AstroScans(ScanType, CreatedAt DESC);
    CREATE NONCLUSTERED INDEX IX_AstroScans_Status ON dbo.AstroScans(Status);
END
GO

IF OBJECT_ID('dbo.AstroScanSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AstroScanSettings (
        Id                  INT IDENTITY(1,1)       NOT NULL,
        IsPalmScanEnabled   BIT                     NOT NULL DEFAULT 1,
        IsHeadScanEnabled   BIT                     NOT NULL DEFAULT 1,
        PalmScanPrice       DECIMAL(10,2)           NOT NULL DEFAULT 0.00,
        HeadScanPrice       DECIMAL(10,2)           NOT NULL DEFAULT 0.00,
        FreeScansPerUser    INT                     NOT NULL DEFAULT 1,
        GeminiModel         NVARCHAR(100)           NOT NULL DEFAULT 'gemini-2.0-flash',
        PalmSystemPrompt    NVARCHAR(MAX)           NULL,
        HeadSystemPrompt    NVARCHAR(MAX)           NULL,
        UpdatedAt           DATETIME2               NOT NULL DEFAULT GETUTCDATE(),
        UpdatedBy           NVARCHAR(100)           NULL,

        CONSTRAINT PK_AstroScanSettings PRIMARY KEY CLUSTERED (Id)
    );

    -- Seed default configuration
    INSERT INTO dbo.AstroScanSettings (
        IsPalmScanEnabled,
        IsHeadScanEnabled,
        PalmScanPrice,
        HeadScanPrice,
        FreeScansPerUser,
        GeminiModel,
        PalmSystemPrompt,
        HeadSystemPrompt,
        UpdatedAt,
        UpdatedBy
    )
    VALUES (
        1,
        1,
        0.00, -- Free by default or customizable via admin
        0.00,
        1,
        'gemini-2.0-flash',
        N'You are a revered Vedic Palmistry (Hastarekha Shastra) and Samudrika Shastra Grandmaster. Analyze the palm photo with meticulous Vedic precision. Identify Life line (Ayushya rekha), Head line (Matru/Dhi rekha), Heart line (Hridaya/Pitri rekha), Fate line (Bhagya rekha), Sun line (Surya rekha), and major planetary mounts (Jupiter/Guru, Saturn/Shani, Sun/Surya, Mercury/Budh, Venus/Shukra, Moon/Chandra, Mars/Mangal). Provide an authentic, insightful, culturally rooted analysis with practical Vedic remedies.',
        N'You are a venerable Vedic Samudrika Shastra (Face & Forehead reading / Phrenology) Grandmaster. Analyze the forehead and head structure with classical Indian physiognomy principles. Examine forehead width, frontal curvature, forehead planetary lines (Lines of Saturn, Jupiter, Mars, Sun, Venus, Mercury, Moon), eyebrow arch, and overall facial Tejas (spiritual luster). Provide a deep, uplifting, and psychologically precise analysis with Vedic remedies.',
        GETUTCDATE(),
        'System'
    );
END
GO
