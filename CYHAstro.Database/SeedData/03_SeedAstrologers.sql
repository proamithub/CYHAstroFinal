-- ============================================================
-- CYHASTRO PLATFORM — SEED VERIFIED EXPERT ASTROLOGERS
-- Target Database: CyhAstroDB
-- ============================================================

USE CyhAstroDB;
GO

SET NOCOUNT ON;

DECLARE @Now DATETIME2 = GETUTCDATE();

-- Helper Table to hold seed profiles
DECLARE @AstroData TABLE (
    Email NVARCHAR(256),
    DisplayName NVARCHAR(150),
    PhoneNumber NVARCHAR(50),
    Gender NCHAR(1),
    AvatarUrl NVARCHAR(500),
    Specializations NVARCHAR(MAX),
    Languages NVARCHAR(500),
    Bio NVARCHAR(2000),
    ChatRate DECIMAL(8,2),
    AudioRate DECIMAL(8,2),
    VideoRate DECIMAL(8,2),
    Rating DECIMAL(3,2),
    TotalReviews INT,
    TotalConsultations INT,
    ExperienceYears INT
);

INSERT INTO @AstroData (
    Email, DisplayName, PhoneNumber, Gender, AvatarUrl,
    Specializations, Languages, Bio,
    ChatRate, AudioRate, VideoRate, Rating, TotalReviews, TotalConsultations, ExperienceYears
)
VALUES
(
    'acharya.raman@cyhastro.com', 'Acharya Raman Shastri', '+919810011221', 'M',
    'https://images.unsplash.com/photo-1544717305-2782549b5136?auto=format&fit=crop&q=80&w=400',
    '["Vedic Astrology", "Kundli Matching", "Vastu Shastra", "Gemology"]',
    '["Hindi", "English", "Sanskrit"]',
    'Gold Medalist in Vedic Jyotish with over 18 years of experience. Specializes in career progression, Kundli dosha nivaran, and precision marriage matching.',
    25.00, 35.00, 45.00, 4.96, 3420, 8950, 18
),
(
    'dr.meenakshi@cyhastro.com', 'Dr. Meenakshi Sundaram', '+919820022332', 'F',
    'https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?auto=format&fit=crop&q=80&w=400',
    '["KP Astrology", "Nadi Astrology", "Medical Astrology", "Prashna Kundli"]',
    '["English", "Tamil", "Hindi"]',
    'Renowned Krishnamurti Paddhati (KP) and Nadi scholar holding a doctorate in ancient predictive sciences. Known for pin-point time-of-event predictions.',
    30.00, 40.00, 50.00, 4.98, 4810, 11400, 22
),
(
    'tarot.sunita@cyhastro.com', 'Tarot Sunita Kapoor', '+919830033443', 'F',
    'https://images.unsplash.com/photo-1580489944761-15a19d654956?auto=format&fit=crop&q=80&w=400',
    '["Tarot Reading", "Angel Guidance", "Love & Relationships", "Crystal Healing"]',
    '["Hindi", "English", "Punjabi"]',
    'International Tarot Master and intuitive life coach. Empathetic and direct readings for relationship reconciliation, soulmate connection, and mental clarity.',
    18.00, 25.00, 35.00, 4.89, 2190, 6300, 9
),
(
    'pt.devratna@cyhastro.com', 'Pandit Devratna Ojha', '+919840044554', 'M',
    'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&q=80&w=400',
    '["Numerology", "Lal Kitab", "Corporate Astrology", "Name Correction"]',
    '["Hindi", "English"]',
    'Cheiro school of modern Numerology and Lal Kitab specialist. Has advised over 500 corporate founders and celebrities on brand names and favorable timings.',
    22.00, 32.00, 40.00, 4.92, 1980, 5200, 15
),
(
    'jyotishi.ananya@cyhastro.com', 'Jyotishi Ananya Sen', '+919850055665', 'F',
    'https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&q=80&w=400',
    '["Vedic Astrology", "Palmistry", "Face Reading", "Childhood Kundli"]',
    '["Hindi", "Bengali", "English"]',
    'Third-generation astrologer from Kolkata specializing in holistic planetary remedies, birth chart synthesis, and educational guidance for children.',
    15.00, 22.00, 30.00, 4.88, 1420, 4100, 11
),
(
    'acharya.vikram@cyhastro.com', 'Acharya Vikramaditya', '+919860066776', 'M',
    'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&q=80&w=400',
    '["Vedic Astrology", "Lal Kitab", "Muhurat", "Wealth & Finance"]',
    '["Hindi", "Gujarati", "English"]',
    'Authority on classical Vedic remedies, wealth yogas (Dhana Yogas), and shubh muhurat determination for business launches, real estate, and marriage.',
    22.00, 32.00, 42.00, 4.94, 2760, 7800, 14
);

-- Process each astrologer
DECLARE @Email NVARCHAR(256), @DisplayName NVARCHAR(150), @Phone NVARCHAR(50), @Gender NCHAR(1);
DECLARE @Avatar NVARCHAR(500), @Specs NVARCHAR(MAX), @Langs NVARCHAR(500), @Bio NVARCHAR(2000);
DECLARE @ChatRate DECIMAL(8,2), @AudioRate DECIMAL(8,2), @VideoRate DECIMAL(8,2);
DECLARE @Rating DECIMAL(3,2), @Reviews INT, @Consultations INT, @Exp INT;
DECLARE @UserId BIGINT;
DECLARE @UserPublicId NVARCHAR(32);
DECLARE @IdentityId NVARCHAR(450);
DECLARE @AstroPublicId NVARCHAR(32);

DECLARE astro_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT Email, DisplayName, PhoneNumber, Gender, AvatarUrl,
       Specializations, Languages, Bio,
       ChatRate, AudioRate, VideoRate, Rating, TotalReviews, TotalConsultations, ExperienceYears
FROM @AstroData;

OPEN astro_cursor;
FETCH NEXT FROM astro_cursor INTO @Email, @DisplayName, @Phone, @Gender, @Avatar,
                                  @Specs, @Langs, @Bio, @ChatRate, @AudioRate, @VideoRate,
                                  @Rating, @Reviews, @Consultations, @Exp;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @UserPublicId = 'usr_' + LOWER(SUBSTRING(REPLACE(CONVERT(NVARCHAR(36), NEWID()), '-', ''), 1, 16));
    SET @IdentityId = 'ident_' + LOWER(SUBSTRING(REPLACE(CONVERT(NVARCHAR(36), NEWID()), '-', ''), 1, 24));
    SET @AstroPublicId = 'ast_' + LOWER(SUBSTRING(REPLACE(CONVERT(NVARCHAR(36), NEWID()), '-', ''), 1, 16));
    SET @UserId = NULL;

    SELECT @UserId = Id FROM dbo.UserProfiles WHERE Email = @Email;

    IF @UserId IS NULL
    BEGIN
        -- Insert UserProfile
        INSERT INTO dbo.UserProfiles (
            PublicId, IdentityId, DisplayName, Email, PhoneNumber, Gender, AvatarUrl,
            IsEmailVerified, IsPhoneVerified, IsActive, CreatedAt
        )
        VALUES (
            @UserPublicId, @IdentityId, @DisplayName, @Email, @Phone, @Gender, @Avatar,
            1, 1, 1, @Now
        );
        SET @UserId = SCOPE_IDENTITY();

        -- Create Wallet
        INSERT INTO dbo.Wallets (UserId, PaidBalance, BonusBalance, CurrencyCode, RowVersion, CreatedAt)
        VALUES (@UserId, 500.00, 0.00, 'INR', 1, @Now);

        -- Create UserPreferences
        INSERT INTO dbo.UserPreferences (UserId, CreatedAt)
        VALUES (@UserId, @Now);
    END

    -- Insert or Update Astrologer Record
    IF NOT EXISTS (SELECT 1 FROM dbo.Astrologers WHERE UserId = @UserId)
    BEGIN
        INSERT INTO dbo.Astrologers (
            PublicId, UserId, DisplayName, Specializations, Languages, Bio,
            ChatRatePerMinute, AudioRatePerMinute, VideoRatePerMinute,
            Rating, TotalReviews, TotalConsultations, ExperienceYears,
            IsAvailable, IsVerified, IsActive, ProfilePhotoUrl, CommissionRatePct, CreatedAt
        )
        VALUES (
            @AstroPublicId, @UserId, @DisplayName, @Specs, @Langs, @Bio,
            @ChatRate, @AudioRate, @VideoRate,
            @Rating, @Reviews, @Consultations, @Exp,
            1, 1, 1, @Avatar, 20.00, @Now
        );
    END

    FETCH NEXT FROM astro_cursor INTO @Email, @DisplayName, @Phone, @Gender, @Avatar,
                                      @Specs, @Langs, @Bio, @ChatRate, @AudioRate, @VideoRate,
                                      @Rating, @Reviews, @Consultations, @Exp;
END;

CLOSE astro_cursor;
DEALLOCATE astro_cursor;

SELECT COUNT(1) AS TotalAstrologers FROM dbo.Astrologers;
GO
