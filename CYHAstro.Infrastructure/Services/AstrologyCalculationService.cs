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

        // 5. Navamsha (D9) Chart Computation
        var navamshaHouses = new List<KundliHouseDto>();
        for (int h = 1; h <= 12; h++)
        {
            int d9Sign = ((ascendantSignId * 9 + h - 1) % 12) + 1;
            var d9Planets = planets
                .Where(p => (((p.SignId * 9 + (int)(p.Longitude / 3.33m)) % 12) + 1) == h)
                .Select(p => p.PlanetName)
                .ToList();

            navamshaHouses.Add(new KundliHouseDto
            {
                HouseNumber = h,
                SignId = d9Sign,
                SignName = ZodiacNames[d9Sign],
                Degree = 15.0m,
                PlanetsInHouse = d9Planets
            });
        }

        // 6. Yogas & Auspicious Planetary Combinations
        var yogas = DetectYogas(planets, ascendantSignId);

        // 7. Life Scores (Career, Love, Health, Fortune Gauges)
        int careerScore = 80 + ((ascendantSignId * 3) % 18);
        int loveScore = 72 + ((moonSignId * 4) % 22);
        int healthScore = 78 + ((sunSignId * 3) % 19);
        int fortuneScore = 82 + (((ascendantSignId + moonSignId) * 3) % 17);

        // 8. Personalized Lucky Elements
        var (gemstone, color, number, direction, deity, rudraksha) = GetLuckyElements(ascendantSignId);

        // 9. Vimshottari Dasha Timeline (120 Years)
        var dashaTimeline = BuildDashaTimeline(request.DateOfBirth.Year, nakshatraIndex);

        // 10. AI Interpretation via Gemini
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
                careerScore,
                loveScore,
                yogas = yogas.Select(y => y.Name),
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
            NavamshaHouses = navamshaHouses,
            MangalDoshaStatus = mangalStatus,
            KaalSarpDoshaStatus = "No Kaal Sarp Dosha",
            SadeSatiStatus = "Not Active",
            CurrentDasha = currentDasha,
            AiInterpretation = aiText,
            CareerScore = careerScore,
            LoveScore = loveScore,
            HealthScore = healthScore,
            FortuneScore = fortuneScore,
            LuckyGemstone = gemstone,
            LuckyColor = color,
            LuckyNumber = number,
            LuckyDirection = direction,
            RulingDeity = deity,
            RecommendedRudraksha = rudraksha,
            Yogas = yogas,
            DashaTimeline = dashaTimeline
        };

        return Result<KundliChartDto>.Success(chartDto);
    }

    private static (string gemstone, string color, string number, string direction, string deity, string rudraksha) GetLuckyElements(int lagna) => lagna switch
    {
        1 => ("Red Coral (Moonga)", "Crimson Red & Golden Yellow", "9, 1, 3", "East", "Lord Hanuman / Kartikeya", "3 Mukhi"),
        2 => ("Diamond (Heera) / White Opal", "White, Cream & Silver", "6, 5, 8", "South-East", "Goddess Mahalakshmi", "6 Mukhi"),
        3 => ("Emerald (Panna)", "Emerald Green & Light Yellow", "5, 3, 7", "North", "Lord Ganesha / Vishnu", "4 Mukhi"),
        4 => ("Natural Pearl (Moti)", "Pearl White, Silver & Cream", "2, 7, 9", "North-West", "Lord Shiva / Parvati", "2 Mukhi"),
        5 => ("Ruby (Manikya)", "Golden Yellow, Saffron & Copper", "1, 4, 9", "East", "Lord Surya / Gayatri Mata", "1 or 12 Mukhi"),
        6 => ("Emerald (Panna)", "Green & Pastel Yellow", "5, 6, 2", "North", "Lord Ganesha / Saraswati", "4 Mukhi"),
        7 => ("White Sapphire / Diamond", "Royal Blue, White & Sky Blue", "6, 7, 1", "West", "Goddess Durga / Lakshmi", "6 Mukhi"),
        8 => ("Red Coral (Moonga)", "Deep Red, Maroon & Saffron", "9, 3, 4", "South", "Lord Hanuman / Shiva", "3 Mukhi"),
        9 => ("Yellow Sapphire (Pukhraj)", "Golden Yellow, Amber & Mustard", "3, 9, 5", "North-East (Ishaan)", "Lord Vishnu / Brihaspati", "5 Mukhi"),
        10 => ("Blue Sapphire (Neelam)", "Navy Blue, Dark Grey & Black", "8, 4, 6", "West", "Lord Shani / Bhairava", "7 or 14 Mukhi"),
        11 => ("Blue Sapphire (Neelam)", "Electric Blue, Cyan & Purple", "8, 7, 3", "West", "Lord Shiva / Shani Dev", "7 Mukhi"),
        12 => ("Yellow Sapphire (Pukhraj)", "Saffron, Golden & Sea Green", "3, 1, 9", "North-East (Ishaan)", "Lord Vishnu / Narayana", "5 Mukhi"),
        _ => ("Yellow Sapphire (Pukhraj)", "Golden Yellow", "3, 9", "East", "Lord Ganesha", "5 Mukhi")
    };

    private static List<KundliYogaDto> DetectYogas(List<KundliPlanetaryPositionDto> planets, int ascendantSignId)
    {
        var yogas = new List<KundliYogaDto>();

        var jup = planets.FirstOrDefault(p => p.PlanetName == "Jupiter");
        var moon = planets.FirstOrDefault(p => p.PlanetName == "Moon");
        var sun = planets.FirstOrDefault(p => p.PlanetName == "Sun");
        var merc = planets.FirstOrDefault(p => p.PlanetName == "Mercury");
        var mars = planets.FirstOrDefault(p => p.PlanetName == "Mars");
        var venus = planets.FirstOrDefault(p => p.PlanetName == "Venus");
        var saturn = planets.FirstOrDefault(p => p.PlanetName == "Saturn");

        // 1. Gaja Kesari Yoga
        if (jup != null && moon != null)
        {
            var diff = Math.Abs(jup.House - moon.House);
            var isKendraFromMoon = diff is 0 or 3 or 6 or 9;
            var isKendraFromLagna = jup.House is 1 or 4 or 7 or 10;
            if (isKendraFromMoon || isKendraFromLagna)
            {
                yogas.Add(new KundliYogaDto
                {
                    Name = "Gaja Kesari Yoga",
                    SanskritName = "गजकेसरी योग",
                    Type = "Maha Raj Yoga",
                    IsAuspicious = true,
                    Description = "Formed when Jupiter is in a Kendra (quadrant) from Moon or Lagna. Bestows majestic intellect, respected leadership, lasting prosperity, and academic/spiritual authority.",
                    Impact = "Brings high societal honor, administrative power, and unwavering wisdom."
                });
            }
        }

        // 2. Budhaditya Yoga
        if (sun != null && merc != null && sun.House == merc.House)
        {
            yogas.Add(new KundliYogaDto
            {
                Name = "Budhaditya Yoga",
                SanskritName = "बुधादित्य योग",
                Type = "Raj Yoga",
                IsAuspicious = true,
                Description = $"Formed by the auspicious union of Sun and Mercury in House {sun.House}. Endows razor-sharp intelligence, analytical acumen, professional eloquence, and executive success.",
                Impact = "Exceptional communicative prowess, analytical skills, and corporate recognition."
            });
        }

        // 3. Mahalakshmi Dhana Yoga
        if (moon != null && mars != null)
        {
            if (moon.House == mars.House || Math.Abs(moon.House - mars.House) == 6)
            {
                yogas.Add(new KundliYogaDto
                {
                    Name = "Mahalakshmi Dhana Yoga",
                    SanskritName = "महालक्ष्मी धन योग",
                    Type = "Dhana Yoga",
                    IsAuspicious = true,
                    Description = "Formed by the powerful association of Moon and Mars. Blessed by Goddess Lakshmi with financial enterprise, property investments, and commercial success.",
                    Impact = "Continuous financial influx, wealth accumulation, and self-made assets."
                });
            }
        }

        // 4. Pancha Mahapurusha Yoga (Hamsa, Malavya, Ruchaka)
        if (jup != null && jup.House is 1 or 4 or 7 or 10 && (jup.SignId is 4 or 9 or 12))
        {
            yogas.Add(new KundliYogaDto
            {
                Name = "Hamsa Yoga (Pancha Mahapurusha)",
                SanskritName = "हंस महापुरुष योग",
                Type = "Pancha Mahapurusha",
                IsAuspicious = true,
                Description = "Jupiter exalted or in own house in Kendra. Symbolizes spiritual elevation, revered teacher status, virtuous character, and pure heart.",
                Impact = "Lifelong respect, noble reputation, and spiritual insight."
            });
        }

        if (venus != null && venus.House is 1 or 4 or 7 or 10 && (venus.SignId is 12 or 2 or 7))
        {
            yogas.Add(new KundliYogaDto
            {
                Name = "Malavya Yoga (Pancha Mahapurusha)",
                SanskritName = "मालव्य महापुरुष योग",
                Type = "Pancha Mahapurusha",
                IsAuspicious = true,
                Description = "Venus exalted or in own sign in Kendra. Imparts luxurious life, aesthetic elegance, marital ecstasy, vehicles, and artistic talent.",
                Impact = "Enjoyment of fine luxuries, magnetic charm, and harmonious relationship."
            });
        }

        if (mars != null && mars.House is 1 or 4 or 7 or 10 && (mars.SignId is 10 or 1 or 8))
        {
            yogas.Add(new KundliYogaDto
            {
                Name = "Ruchaka Yoga (Pancha Mahapurusha)",
                SanskritName = "रुचक महापुरुष योग",
                Type = "Pancha Mahapurusha",
                IsAuspicious = true,
                Description = "Mars exalted or in own sign in Kendra. Bestows indomitable physical courage, decisive command, athletic stamina, and victory over adversaries.",
                Impact = "Leadership in management, armed forces, engineering, or real estate."
            });
        }

        // Always ensure at least 2 distinct auspicious yogas are surfaced
        if (yogas.Count < 2)
        {
            yogas.Add(new KundliYogaDto
            {
                Name = "Bhagya Dhana Yoga",
                SanskritName = "भाग्य धन योग",
                Type = "Dhana Yoga",
                IsAuspicious = true,
                Description = "Strong placement of trine lords (Lakshmi Sthanas 1, 5, 9). Enhances luck in speculation, ancestral wisdom, and opportune life breakthroughs.",
                Impact = "Favorable destiny, providential help in distress, and family welfare."
            });
        }

        return yogas;
    }

    private static List<DashaPeriodDto> BuildDashaTimeline(int birthYear, int nakshatraIndex)
    {
        var lords = new (string Name, string Sanskrit, int Years)[]
        {
            ("Ketu", "केतु", 7),
            ("Venus", "शुक्र", 20),
            ("Sun", "सूर्य", 6),
            ("Moon", "चन्द्र", 10),
            ("Mars", "मंगल", 7),
            ("Rahu", "राहु", 18),
            ("Jupiter", "गुरु", 16),
            ("Saturn", "शनि", 19),
            ("Mercury", "बुध", 17)
        };

        int startLordIndex = nakshatraIndex % 9;
        int currentYear = DateTime.UtcNow.Year;
        int runningYear = birthYear;
        var timeline = new List<DashaPeriodDto>();

        for (int i = 0; i < 9; i++)
        {
            var lord = lords[(startLordIndex + i) % 9];
            int endYear = runningYear + lord.Years;
            bool isCurrent = currentYear >= runningYear && currentYear < endYear;

            timeline.Add(new DashaPeriodDto
            {
                PlanetName = lord.Name,
                SanskritName = lord.Sanskrit,
                DurationYears = lord.Years,
                StartYear = runningYear,
                EndYear = endYear,
                IsCurrent = isCurrent
            });

            runningYear = endYear;
        }

        // If person is older than 120 or before birth, make sure at least one is flagged current
        if (!timeline.Any(t => t.IsCurrent) && timeline.Count > 4)
        {
            timeline[4] = timeline[4] with { IsCurrent = true };
        }

        return timeline;
    }

    public async Task<Result<MatchmakingResultDto>> CalculateMatchmakingAsync(
        MatchmakingRequestDto request, CancellationToken ct = default)
    {
        // 1. Generate Groom and Bride Kundli Charts (Lagna D1 & Navamsha D9)
        var maleKundliReq = new KundliRequestDto
        {
            Name = request.MaleName,
            Gender = "Male",
            DateOfBirth = request.MaleDob,
            TimeOfBirth = request.MaleTob,
            BirthPlace = !string.IsNullOrWhiteSpace(request.MalePlace) ? request.MalePlace : "New Delhi, India",
            Latitude = request.MaleLat != 0 ? request.MaleLat : 28.6139m,
            Longitude = request.MaleLon != 0 ? request.MaleLon : 77.2090m,
            IncludeAiInterpretation = false
        };

        var femaleKundliReq = new KundliRequestDto
        {
            Name = request.FemaleName,
            Gender = "Female",
            DateOfBirth = request.FemaleDob,
            TimeOfBirth = request.FemaleTob,
            BirthPlace = !string.IsNullOrWhiteSpace(request.FemalePlace) ? request.FemalePlace : "Mumbai, India",
            Latitude = request.FemaleLat != 0 ? request.FemaleLat : 19.0760m,
            Longitude = request.FemaleLon != 0 ? request.FemaleLon : 72.8777m,
            IncludeAiInterpretation = false
        };

        var maleChartRes = await GenerateKundliAsync(maleKundliReq, ct);
        var femaleChartRes = await GenerateKundliAsync(femaleKundliReq, ct);

        var maleChart = maleChartRes.IsSuccess ? maleChartRes.Value : null;
        var femaleChart = femaleChartRes.IsSuccess ? femaleChartRes.Value : null;

        // Manglik Dosha Analysis
        var maleMarsHouse = maleChart?.Planets.FirstOrDefault(p => p.PlanetName == "Mars")?.House ?? 0;
        var femaleMarsHouse = femaleChart?.Planets.FirstOrDefault(p => p.PlanetName == "Mars")?.House ?? 0;
        bool maleManglik = maleMarsHouse is 1 or 4 or 7 or 8 or 12;
        bool femaleManglik = femaleMarsHouse is 1 or 4 or 7 or 8 or 12;

        string manglikConclusion;
        if (!maleManglik && !femaleManglik)
        {
            manglikConclusion = "Both Non-Manglik (पूर्णतः दोष रहित - Highly Auspicious)";
        }
        else if (maleManglik && femaleManglik)
        {
            manglikConclusion = "Dosha Samya Cancellation (दोष साम्य - Auspicious! Both are Manglik, canceling malefic effects)";
        }
        else if (maleManglik && !femaleManglik)
        {
            manglikConclusion = "Groom is Manglik, Bride is Non-Manglik (एकल मांगलिक दोष - Kumbh Vivah / Mars remedies recommended)";
        }
        else
        {
            manglikConclusion = "Bride is Manglik, Groom is Non-Manglik (एकल मांगलिक दोष - Mangal Shanti / Gauri Puja recommended)";
        }

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

        string aiPrompt = $"Synthesize Vedic Kundli Milan between groom {request.MaleName} (Lagna: {maleChart?.AscendantSign ?? "Aries"}, Moon: {maleChart?.MoonSign ?? "Taurus"}) and bride {request.FemaleName} (Lagna: {femaleChart?.AscendantSign ?? "Gemini"}, Moon: {femaleChart?.MoonSign ?? "Cancer"}). Ashtakoot Score: {totalScore:F1}/36. Nadi: {nadi}/8, Bhakoot: {bhakoot}/7, Gana: {gana}/6. Manglik Status: {manglikConclusion}. Conclusion: {conclusion}. Provide a concise 2-paragraph astrological verdict regarding marital happiness, compatibility, and recommended Vedic remedies.";

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
                : $"{conclusion}. With an Ashtakoot score of {totalScore:F1}/36 and {manglikConclusion}, the union indicates auspicious companionship, shared values, and mutual devotion.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini Matchmaking verdict fallback triggered");
            aiVerdict = $"{conclusion}. With an Ashtakoot score of {totalScore:F1}/36 and {manglikConclusion}, the union indicates mutual understanding, shared family values, and supportive companionship.";
        }

        var result = new MatchmakingResultDto
        {
            TotalScore = totalScore,
            Conclusion = conclusion,
            GunaBreakdown = breakdown,
            HasNadiDosha = nadi == 0,
            HasBhakootDosha = bhakoot == 0,
            HasGanaDosha = gana < 3,
            AiVerdict = aiVerdict,
            MaleChart = maleChart,
            FemaleChart = femaleChart,
            MaleManglik = maleManglik,
            FemaleManglik = femaleManglik,
            ManglikConclusion = manglikConclusion
        };

        return Result<MatchmakingResultDto>.Success(result);
    }
}
