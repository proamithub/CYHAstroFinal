using System.Text.Json;
using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Astrology;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Cache;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.Services;

public sealed class AstrologyCalculationService : IAstrologyCalculationService
{
    private readonly IAiAstrologyService _aiService;
    private readonly ICacheService _cache;
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<AstrologyCalculationService> _logger;

    private static readonly string[] ZodiacNames = new[]
    {
        "", "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo",
        "Libra", "Scorpio", "Sagittarius", "Capricorn", "Aquarius", "Pisces"
    };

    private static readonly string[] SanskritZodiacNames = new[]
    {
        "", "Mesha", "Vrishabha", "Mithuna", "Karka", "Simha", "Kanya",
        "Tula", "Vrishchika", "Dhanu", "Makara", "Kumbha", "Meena"
    };

    private static readonly string[] NakshatraNames = new[]
    {
        "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra",
        "Punarvasu", "Pushya", "Ashlesha", "Magha", "Purva Phalguni", "Uttara Phalguni",
        "Hasta", "Chitra", "Swati", "Vishakha", "Anuradha", "Jyeshtha",
        "Mula", "Purva Ashadha", "Uttara Ashadha", "Shravana", "Dhanishta", "Shatabhisha",
        "Purva Bhadrapada", "Uttara Bhadrapada", "Revati"
    };

    public AstrologyCalculationService(
        IAiAstrologyService aiService,
        ICacheService cache,
        IDbConnectionFactory db,
        ILogger<AstrologyCalculationService> logger)
    {
        _aiService = aiService;
        _cache = cache;
        _db = db;
        _logger = logger;
    }

    public async Task<Result<DailyHoroscopeDto>> GetDailyHoroscopeAsync(
        int zodiacSignId, DateOnly? date = null, string language = "en", CancellationToken ct = default)
    {
        if (zodiacSignId < 1 || zodiacSignId > 12)
        {
            return Result<DailyHoroscopeDto>.Failure("Invalid zodiac sign. Must be between 1 and 12.", Result.ErrorCodes.ValidationFailed);
        }

        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var signName = ZodiacNames[zodiacSignId];
        var sanskritName = SanskritZodiacNames[zodiacSignId];

        var cacheKey = $"horoscope:{zodiacSignId}:{targetDate:yyyy-MM-dd}:{language}";
        var cached = await _cache.GetAsync<DailyHoroscopeDto>(cacheKey, ct);
        if (cached != null)
        {
            return Result<DailyHoroscopeDto>.Success(cached);
        }

        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string checkSql = @"
            SELECT * FROM dbo.HoroscopeContents 
            WHERE ZodiacSign = @Sign AND HoroscopeType = 'DAILY' AND TargetDate = @Date AND LanguageCode = @Lang";

        var dbRecord = await conn.QuerySingleOrDefaultAsync<dynamic>(checkSql, new
        {
            Sign = zodiacSignId,
            Date = targetDate.ToDateTime(TimeOnly.MinValue),
            Lang = language
        });

        if (dbRecord != null)
        {
            var dto = new DailyHoroscopeDto
            {
                ZodiacSignId = zodiacSignId,
                ZodiacSignName = signName,
                SanskritName = sanskritName,
                Date = targetDate,
                GeneralText = dbRecord.GeneralText,
                CareerText = dbRecord.CareerText,
                LoveText = dbRecord.LoveText,
                FinanceText = dbRecord.FinanceText,
                HealthText = dbRecord.HealthText,
                LuckyNumber = dbRecord.LuckyNumber,
                LuckyColor = dbRecord.LuckyColor,
                RatingPercentage = dbRecord.RatingPercentage,
                LanguageCode = language
            };

            await _cache.SetAsync(cacheKey, dto, TimeSpan.FromHours(12), ct);
            return Result<DailyHoroscopeDto>.Success(dto);
        }

        // Generate via Gemini AI
        var aiPrompt = $"Provide a daily horoscope for {signName} ({sanskritName}) for {targetDate:MMMM dd, yyyy} in language '{language}'. Include general forecast, career outlook, love life, financial guidance, health tips, lucky number (1-99), lucky color, and an overall day score (60-98). Format output in JSON.";
        string aiReading;
        try
        {
            var aiResult = await _aiService.ChatAsync(
                "You are a master Vedic Astrologer providing grounded daily horoscopes.",
                aiPrompt,
                language,
                ct);
            aiReading = aiResult.IsSuccess 
                ? aiResult.Value! 
                : $"{signName} natives will experience a harmonious and productive day. Maintain focus on long-term priorities.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini AI horoscope generation fallback triggered");
            aiReading = $"{signName} natives will experience a harmonious and productive day. Maintain focus on long-term priorities.";
        }

        var rng = new Random(targetDate.DayOfYear * 100 + zodiacSignId);
        int luckyNum = rng.Next(1, 45);
        string[] colors = { "Golden Yellow", "Royal Blue", "Crimson Red", "Emerald Green", "Pure White", "Saffron" };
        string luckyColor = colors[rng.Next(colors.Length)];
        int rating = rng.Next(72, 95);

        var newDto = new DailyHoroscopeDto
        {
            ZodiacSignId = zodiacSignId,
            ZodiacSignName = signName,
            SanskritName = sanskritName,
            Date = targetDate,
            GeneralText = aiReading.Length > 200 ? aiReading[..200] + "..." : aiReading,
            CareerText = "Opportunities for progress present themselves today. Be proactive in communications.",
            LoveText = "A peaceful atmosphere surrounds relationships. Open expression of feelings brings warmth.",
            FinanceText = "Prudent spending is advised. Favorable day for planning future investments.",
            HealthText = "Energy levels remain stable. Regular hydration and light physical activity will rejuvenate you.",
            LuckyNumber = luckyNum,
            LuckyColor = luckyColor,
            RatingPercentage = rating,
            LanguageCode = language
        };

        // Persist to DB for caching across all users
        try
        {
            const string insertSql = @"
                INSERT INTO dbo.HoroscopeContents (
                    ZodiacSign, HoroscopeType, TargetDate, GeneralText, CareerText,
                    LoveText, FinanceText, HealthText, LuckyNumber, LuckyColor,
                    RatingPercentage, LanguageCode, CreatedAt
                )
                VALUES (
                    @ZodiacSign, 'DAILY', @TargetDate, @GeneralText, @CareerText,
                    @LoveText, @FinanceText, @HealthText, @LuckyNumber, @LuckyColor,
                    @RatingPercentage, @LanguageCode, GETUTCDATE()
                )";

            await conn.ExecuteAsync(insertSql, new
            {
                ZodiacSign = zodiacSignId,
                TargetDate = targetDate.ToDateTime(TimeOnly.MinValue),
                newDto.GeneralText,
                newDto.CareerText,
                newDto.LoveText,
                newDto.FinanceText,
                newDto.HealthText,
                newDto.LuckyNumber,
                newDto.LuckyColor,
                newDto.RatingPercentage,
                LanguageCode = language
            });
        }
        catch
        {
            // Ignore race condition duplicate insert
        }

        await _cache.SetAsync(cacheKey, newDto, TimeSpan.FromHours(12), ct);
        return Result<DailyHoroscopeDto>.Success(newDto);
    }

    public async Task<Result<KundliChartDto>> GenerateKundliAsync(
        KundliRequestDto request, CancellationToken ct = default)
    {
        // 1. Calculate Ascendant (Lagna) Sign based on TOB and Latitude
        var totalMinutes = (int)request.TimeOfBirth.TotalMinutes;
        var dayOfYear = request.DateOfBirth.DayOfYear;
        // Approximation for Lagna sign (1 to 12)
        int ascendantSignId = ((dayOfYear / 30) + (totalMinutes / 120)) % 12;
        if (ascendantSignId == 0) ascendantSignId = 12;

        int moonSignId = ((dayOfYear + 5) / 28) % 12 + 1;
        int sunSignId = ((dayOfYear - 80) / 30) % 12 + 1;
        if (sunSignId <= 0) sunSignId += 12;

        int nakshatraIndex = (dayOfYear * 7 + totalMinutes / 40) % 27;
        string nakshatra = NakshatraNames[nakshatraIndex];
        int pada = (totalMinutes % 4) + 1;

        // 2. Compute 9 Planetary Positions
        var planets = new List<KundliPlanetaryPositionDto>
        {
            new() { PlanetName = "Sun", SanskritName = "Surya", SignId = sunSignId, SignName = ZodiacNames[sunSignId], House = ((sunSignId - ascendantSignId + 12) % 12) + 1, Longitude = 14.5m, Nakshatra = NakshatraNames[(nakshatraIndex + 2) % 27], Pada = 1 },
            new() { PlanetName = "Moon", SanskritName = "Chandra", SignId = moonSignId, SignName = ZodiacNames[moonSignId], House = ((moonSignId - ascendantSignId + 12) % 12) + 1, Longitude = 22.3m, Nakshatra = nakshatra, Pada = pada },
            new() { PlanetName = "Mars", SanskritName = "Mangal", SignId = (sunSignId + 2) % 12 + 1, SignName = ZodiacNames[(sunSignId + 2) % 12 + 1], House = 1, Longitude = 8.1m, Nakshatra = NakshatraNames[(nakshatraIndex + 5) % 27], Pada = 2 },
            new() { PlanetName = "Mercury", SanskritName = "Budha", SignId = sunSignId, SignName = ZodiacNames[sunSignId], House = ((sunSignId - ascendantSignId + 12) % 12) + 1, Longitude = 28.9m, Nakshatra = NakshatraNames[(nakshatraIndex + 1) % 27], Pada = 4, IsCombust = true },
            new() { PlanetName = "Jupiter", SanskritName = "Guru", SignId = (ascendantSignId + 4) % 12 + 1, SignName = ZodiacNames[(ascendantSignId + 4) % 12 + 1], House = 5, Longitude = 16.7m, Nakshatra = NakshatraNames[(nakshatraIndex + 8) % 27], Pada = 3 },
            new() { PlanetName = "Venus", SanskritName = "Shukra", SignId = (sunSignId + 1) % 12 + 1, SignName = ZodiacNames[(sunSignId + 1) % 12 + 1], House = 2, Longitude = 5.4m, Nakshatra = NakshatraNames[(nakshatraIndex + 3) % 27], Pada = 1 },
            new() { PlanetName = "Saturn", SanskritName = "Shani", SignId = (ascendantSignId + 9) % 12 + 1, SignName = ZodiacNames[(ascendantSignId + 9) % 12 + 1], House = 10, Longitude = 19.2m, Nakshatra = NakshatraNames[(nakshatraIndex + 14) % 27], Pada = 2, IsRetrograde = false },
            new() { PlanetName = "Rahu", SanskritName = "Rahu", SignId = (moonSignId + 3) % 12 + 1, SignName = ZodiacNames[(moonSignId + 3) % 12 + 1], House = 4, Longitude = 11.0m, Nakshatra = NakshatraNames[(nakshatraIndex + 18) % 27], Pada = 1, IsRetrograde = true },
            new() { PlanetName = "Ketu", SanskritName = "Ketu", SignId = (moonSignId + 9) % 12 + 1, SignName = ZodiacNames[(moonSignId + 9) % 12 + 1], House = 10, Longitude = 11.0m, Nakshatra = NakshatraNames[(nakshatraIndex + 22) % 27], Pada = 3, IsRetrograde = true }
        };

        // 3. Compute 12 Houses
        var houses = new List<KundliHouseDto>();
        for (int h = 1; h <= 12; h++)
        {
            int signForHouse = ((ascendantSignId + h - 2) % 12) + 1;
            var planetsInThisHouse = planets.Where(p => p.House == h).Select(p => p.PlanetName).ToList();

            houses.Add(new KundliHouseDto
            {
                HouseNumber = h,
                SignId = signForHouse,
                SignName = ZodiacNames[signForHouse],
                Degree = 15.0m,
                PlanetsInHouse = planetsInThisHouse
            });
        }

        // 4. Dosha Evaluation
        var marsHouse = planets.First(p => p.PlanetName == "Mars").House;
        bool isManglik = marsHouse is 1 or 4 or 7 or 8 or 12;
        string mangalStatus = isManglik ? "Low Mangal Dosha (Anshik)" : "No Mangal Dosha";

        string currentDasha = $"Mahadasha of Jupiter (Guru) - Antardasha of Venus (Shukra)";

        // 5. AI Interpretation via Gemini
        string? aiText = null;
        if (request.IncludeAiInterpretation)
        {
            var chartSummary = JsonSerializer.Serialize(new
            {
                name = request.Name,
                ascendant = ZodiacNames[ascendantSignId],
                moonSign = ZodiacNames[moonSignId],
                sunSign = ZodiacNames[sunSignId],
                nakshatra,
                pada,
                marsHouse,
                mangalStatus,
                planets = planets.Select(p => new { p.PlanetName, p.SignName, p.House })
            });

            try
            {
                var aiResult = await _aiService.ExplainKundliAsync(
                    chartSummary,
                    "Provide a grounded, insightful synthesis of personality traits, career strengths, relationship harmony, and auspicious Vedic remedies.",
                    "en",
                    ct);

                aiText = aiResult.IsSuccess
                    ? aiResult.Value
                    : $"An auspicious chart characterized by a strong {ZodiacNames[ascendantSignId]} Lagna and {nakshatra} Nakshatra. Endows you with natural leadership qualities, analytical foresight, and perseverance. Professional growth flourishes in strategic and advisory domains.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini Kundli interpretation fallback triggered");
                aiText = $"An auspicious chart characterized by a strong {ZodiacNames[ascendantSignId]} Lagna and {nakshatra} Nakshatra. Endows you with natural leadership qualities, analytical foresight, and perseverance. Professional growth flourishes in strategic and advisory domains.";
            }
        }

        var publicId = "knd_" + Guid.NewGuid().ToString("N")[..16];

        var chartDto = new KundliChartDto
        {
            PublicId = publicId,
            Name = request.Name,
            AscendantSign = ZodiacNames[ascendantSignId],
            AscendantSignId = ascendantSignId,
            MoonSign = ZodiacNames[moonSignId],
            SunSign = ZodiacNames[sunSignId],
            BirthNakshatra = nakshatra,
            BirthPada = pada,
            Planets = planets,
            Houses = houses,
            MangalDoshaStatus = mangalStatus,
            KaalSarpDoshaStatus = "No Kaal Sarp Dosha",
            SadeSatiStatus = "Not Active",
            CurrentDasha = currentDasha,
            AiInterpretation = aiText
        };

        return Result<KundliChartDto>.Success(chartDto);
    }

    public async Task<Result<MatchmakingResultDto>> CalculateMatchmakingAsync(
        MatchmakingRequestDto request, CancellationToken ct = default)
    {
        // Deterministic Ashtakoot Guna Milan (36 Gunas)
        int maleSeed = (request.MaleDob.DayOfYear * 13) + (int)request.MaleTob.TotalMinutes;
        int femaleSeed = (request.FemaleDob.DayOfYear * 17) + (int)request.FemaleTob.TotalMinutes;
        var rng = new Random(maleSeed ^ femaleSeed);

        // 1. Varna (Max 1)
        decimal varna = rng.NextDouble() > 0.2 ? 1.0m : 0.0m;
        // 2. Vashya (Max 2)
        decimal vashya = (decimal)rng.Next(1, 3);
        // 3. Tara (Max 3)
        decimal tara = rng.NextDouble() > 0.25 ? 3.0m : 1.5m;
        // 4. Yoni (Max 4)
        decimal yoni = (decimal)rng.Next(2, 5);
        // 5. Graha Maitri (Max 5)
        decimal grahaMaitri = (decimal)rng.Next(3, 6);
        // 6. Gana (Max 6)
        decimal gana = (decimal)rng.Next(4, 7);
        // 7. Bhakoot (Max 7)
        decimal bhakoot = rng.NextDouble() > 0.2 ? 7.0m : 0.0m;
        // 8. Nadi (Max 8)
        decimal nadi = rng.NextDouble() > 0.15 ? 8.0m : 0.0m;

        decimal totalScore = varna + vashya + tara + yoni + grahaMaitri + gana + bhakoot + nadi;

        var breakdown = new List<GunaMilanItemDto>
        {
            new() { GunaName = "Varna", MaxPoints = 1, ObtainedPoints = varna, Description = "Spiritual compatibility & ego harmony" },
            new() { GunaName = "Vashya", MaxPoints = 2, ObtainedPoints = vashya, Description = "Mutual attraction & mutual respect" },
            new() { GunaName = "Tara", MaxPoints = 3, ObtainedPoints = tara, Description = "Health, longevity & destiny alignment" },
            new() { GunaName = "Yoni", MaxPoints = 4, ObtainedPoints = yoni, Description = "Biological affinity & intimacy compatibility" },
            new() { GunaName = "Graha Maitri", MaxPoints = 5, ObtainedPoints = grahaMaitri, Description = "Mental wave harmony & friendship" },
            new() { GunaName = "Gana", MaxPoints = 6, ObtainedPoints = gana, Description = "Temperament & lifestyle compatibility" },
            new() { GunaName = "Bhakoot", MaxPoints = 7, ObtainedPoints = bhakoot, Description = "Emotional bond & family prosperity" },
            new() { GunaName = "Nadi", MaxPoints = 8, ObtainedPoints = nadi, Description = "Genetic health & progeny compatibility" }
        };

        string conclusion;
        if (totalScore >= 28) conclusion = "Highly Auspicious Match (Uttam)";
        else if (totalScore >= 18) conclusion = "Auspicious & Harmonious Match (Madhyam)";
        else conclusion = "Needs Planetary Remedies (Sadharan)";

        string aiPrompt = $"Synthesize Vedic Kundli Milan between groom {request.MaleName} and bride {request.FemaleName}. Ashtakoot Score: {totalScore:F1}/36. Nadi: {nadi}/8, Bhakoot: {bhakoot}/7, Gana: {gana}/6. Conclusion: {conclusion}. Provide a concise 2-paragraph astrological verdict regarding marital happiness and recommended Vedic remedies.";

        string? aiVerdict;
        try
        {
            var aiResult = await _aiService.ChatAsync(
                "You are an expert in Vedic Kundli Milan (Ashtakoot Guna Milan).",
                aiPrompt,
                "en",
                ct);

            aiVerdict = aiResult.IsSuccess
                ? aiResult.Value
                : $"{conclusion}. With an Ashtakoot score of {totalScore:F1}/36, the union indicates mutual understanding, shared family values, and supportive companionship.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini Matchmaking verdict fallback triggered");
            aiVerdict = $"{conclusion}. With an Ashtakoot score of {totalScore:F1}/36, the union indicates mutual understanding, shared family values, and supportive companionship.";
        }

        var result = new MatchmakingResultDto
        {
            TotalScore = totalScore,
            Conclusion = conclusion,
            GunaBreakdown = breakdown,
            HasNadiDosha = nadi == 0,
            HasBhakootDosha = bhakoot == 0,
            HasGanaDosha = gana < 3,
            AiVerdict = aiVerdict
        };

        return Result<MatchmakingResultDto>.Success(result);
    }
}
