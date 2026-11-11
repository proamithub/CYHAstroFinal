namespace CynAstro.Core.Constants;

/// <summary>
/// Application-wide constants for CynAstro platform.
/// </summary>
public static class AppConstants
{
    public static class Roles
    {
        public const string User = "User";
        public const string Astrologer = "Astrologer";
        public const string Admin = "Admin";
        public const string SuperAdmin = "SuperAdmin";
        public const string Support = "Support";
    }

    public static class Cache
    {
        public const string HoroscopePrefix = "horoscope";
        public const string PanchangPrefix = "panchang";
        public const string AstrologerAvailabilityPrefix = "astrologer:availability";
        public const string AstrologerListingPrefix = "astrologer:listing";
        public const string KundliPublicPrefix = "kundli:public";

        public static class TTL
        {
            public static readonly TimeSpan Horoscope = TimeSpan.FromHours(1);
            public static readonly TimeSpan Panchang = TimeSpan.FromHours(24);
            public static readonly TimeSpan AstrologerAvailability = TimeSpan.FromSeconds(60);
            public static readonly TimeSpan AstrologerListing = TimeSpan.FromMinutes(5);
            public static readonly TimeSpan KundliPublic = TimeSpan.FromMinutes(30);
        }
    }

    public static class Pagination
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
    }

    public static class Jwt
    {
        public const int AccessTokenExpiryMinutes = 15;
        public const int RefreshTokenExpiryDays = 30;
    }

    public static class Consultation
    {
        public const int BillingIntervalSeconds = 60;
        public const int MaxSessionDurationMinutes = 120;
        public const decimal MinimumWalletBalance = 10m;
    }

    public static class FileUpload
    {
        public const long MaxProfilePhotoSizeBytes = 5 * 1024 * 1024; // 5 MB
        public static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    }
}
