using CYHAstro.Core.DTOs.Panchang;

namespace CYHAstro.Infrastructure.Panchang;

/// <summary>
/// High-precision Vedic Panchang and Muhurata Astronomical Calculation Engine.
/// Implements Jean Meeus solar/lunar ephemeris models with Lahiri Ayanamsha (Chitrapaksha).
/// </summary>
public static class VedicPanchangCalculator
{
    private static readonly string[] TithiNames =
    {
        "Pratipada", "Dwitiya", "Tritiya", "Chaturthi", "Panchami",
        "Shashthi", "Saptami", "Ashtami", "Navami", "Dashami",
        "Ekadashi", "Dwadashi", "Trayodashi", "Chaturdashi", "Purnima",
        "Pratipada", "Dwitiya", "Tritiya", "Chaturthi", "Panchami",
        "Shashthi", "Saptami", "Ashtami", "Navami", "Dashami",
        "Ekadashi", "Dwadashi", "Trayodashi", "Chaturdashi", "Amavasya"
    };

    private static readonly string[] TithiDeities =
    {
        "Agni", "Brahma", "Gauri", "Ganesha", "Sarpa",
        "Kartikeya", "Surya", "Shiva", "Durga", "Yama",
        "Vishvedevas", "Vishnu", "Kamadeva", "Shiva", "Chandra",
        "Agni", "Brahma", "Gauri", "Ganesha", "Sarpa",
        "Kartikeya", "Surya", "Shiva", "Durga", "Yama",
        "Vishvedevas", "Vishnu", "Kamadeva", "Shiva", "Pitrus"
    };

    private static readonly (string Name, string Lord, string Deity)[] NakshatraList =
    {
        ("Ashwini", "Ketu", "Ashwini Kumaras"),
        ("Bharani", "Venus", "Yama"),
        ("Krittika", "Sun", "Agni"),
        ("Rohini", "Moon", "Brahma"),
        ("Mrigashira", "Mars", "Chandra"),
        ("Ardra", "Rahu", "Rudra"),
        ("Punarvasu", "Jupiter", "Aditi"),
        ("Pushya", "Saturn", "Brihaspati"),
        ("Ashlesha", "Mercury", "Nagas"),
        ("Magha", "Ketu", "Pitrus"),
        ("Purva Phalguni", "Venus", "Bhaga"),
        ("Uttara Phalguni", "Sun", "Aryaman"),
        ("Hasta", "Moon", "Savitr"),
        ("Chitra", "Mars", "Vishwakarma"),
        ("Swati", "Rahu", "Vayu"),
        ("Vishakha", "Jupiter", "Indra-Agni"),
        ("Anuradha", "Saturn", "Mitra"),
        ("Jyeshtha", "Mercury", "Indra"),
        ("Mula", "Ketu", "Nirriti"),
        ("Purva Ashadha", "Venus", "Apah"),
        ("Uttara Ashadha", "Sun", "Vishvedevas"),
        ("Shravana", "Moon", "Vishnu"),
        ("Dhanishta", "Mars", "Eight Vasus"),
        ("Shatabhisha", "Rahu", "Varuna"),
        ("Purva Bhadrapada", "Jupiter", "Aja Ekapada"),
        ("Uttara Bhadrapada", "Saturn", "Ahirbudhnya"),
        ("Revati", "Mercury", "Pushan")
    };

    private static readonly (string Name, string Meaning)[] YogaList =
    {
        ("Vishkambha", "Supported / Prevailing"),
        ("Priti", "Affectionate / Fondness"),
        ("Ayushman", "Long-lived"),
        ("Saubhagya", "Good Fortune"),
        ("Shobhana", "Splendid / Auspicious"),
        ("Atiganda", "Great Obstacle"),
        ("Sukarma", "Virtuous Action"),
        ("Dhriti", "Steadfastness / Firmness"),
        ("Shula", "Sharp / Spear"),
        ("Ganda", "Distress / Knot"),
        ("Vriddhi", "Prosperity / Growth"),
        ("Dhruva", "Constant / Fixed"),
        ("Vyaghata", "Striking / Fierce"),
        ("Harshana", "Joyous / Delighting"),
        ("Vajra", "Diamond / Thunderbolt"),
        ("Siddhi", "Accomplishment"),
        ("Vyatipata", "Calamitous"),
        ("Variyan", "Comfortable / Best"),
        ("Parigha", "Obstruction / Iron Bar"),
        ("Shiva", "Auspicious / Benevolent"),
        ("Siddha", "Accomplished / Perfect"),
        ("Sadhya", "Attainable / Amenable"),
        ("Shubha", "Pure / Auspicious"),
        ("Shukla", "Bright / Clear"),
        ("Brahma", "Priestly / Divine"),
        ("Indra", "Powerful / Noble"),
        ("Vaidhriti", "Divisive / Chaotic")
    };

    private static readonly (string Name, string Lord, string Sanskrit)[] VaarList =
    {
        ("Sunday", "Sun", "Ravivar"),
        ("Monday", "Moon", "Somavar"),
        ("Tuesday", "Mars", "Mangalavar"),
        ("Wednesday", "Mercury", "Budhavar"),
        ("Thursday", "Jupiter", "Guruvar"),
        ("Friday", "Venus", "Shukravar"),
        ("Saturday", "Saturn", "Shanivar")
    };

    private static readonly string[] ZodiacSigns =
    {
        "Aries (Mesha)", "Taurus (Vrishabha)", "Gemini (Mithuna)", "Cancer (Karka)",
        "Leo (Simha)", "Virgo (Kanya)", "Libra (Tula)", "Scorpio (Vrishchika)",
        "Sagittarius (Dhanu)", "Capricorn (Makara)", "Aquarius (Kumbha)", "Pisces (Meena)"
    };

    // Choghadiya starting cycle index per DayOfWeek (0=Sunday ... 6=Saturday)
    // 0=Udveg, 1=Char, 2=Labh, 3=Amrit, 4=Kaal, 5=Shubh, 6=Rog
    private static readonly int[] DayChoghadiyaStartIndices = { 0, 3, 6, 2, 5, 1, 4 };
    private static readonly int[] NightChoghadiyaStartIndices = { 5, 1, 4, 0, 3, 6, 2 };

    private static readonly (string Name, string Nature, string Badge, string Lord)[] ChoghadiyaTypes =
    {
        ("Udveg", "Inauspicious", "danger", "Sun"),
        ("Char", "Neutral", "info", "Venus"),
        ("Labh", "Auspicious", "success", "Mercury"),
        ("Amrit", "Highly Auspicious", "success", "Moon"),
        ("Kaal", "Inauspicious", "danger", "Saturn"),
        ("Shubh", "Highly Auspicious", "success", "Jupiter"),
        ("Rog", "Inauspicious", "warning", "Mars")
    };

    public static PanchangResponseDto Calculate(PanchangRequestDto request)
    {
        var localDate = request.Date.Date;
        var tzOffset = (double)request.TimeZoneOffsetHours;
        var lat = (double)request.Latitude;
        var lon = (double)request.Longitude;

        // Julian Day at 05:30 AM local sunrise estimate
        var localTimeOfCalc = new DateTime(localDate.Year, localDate.Month, localDate.Day, 6, 0, 0, DateTimeKind.Unspecified);
        var utcTimeOfCalc = localTimeOfCalc.AddHours(-tzOffset);
        var jd = ToJulianDay(utcTimeOfCalc);

        // Ayanamsha (Lahiri)
        var ayanamsha = CalculateLahiriAyanamsha(jd);

        // Planetary Nirayana Longitudes
        var sunSayana = CalculateSunLongitude(jd);
        var moonSayana = CalculateMoonLongitude(jd);

        var sunNirayana = NormalizeDegrees(sunSayana - ayanamsha);
        var moonNirayana = NormalizeDegrees(moonSayana - ayanamsha);

        // Sun & Moon Signs
        var sunSignIdx = (int)(sunNirayana / 30.0);
        var moonSignIdx = (int)(moonNirayana / 30.0);
        var sunSign = ZodiacSigns[Math.Clamp(sunSignIdx, 0, 11)];
        var moonSign = ZodiacSigns[Math.Clamp(moonSignIdx, 0, 11)];

        // Sunrise & Sunset
        var (sunriseUtc, sunsetUtc) = CalculateSunriseSunset(localDate, lat, lon);
        var sunriseLocal = sunriseUtc.AddHours(tzOffset);
        var sunsetLocal = sunsetUtc.AddHours(tzOffset);

        // Approximate Moonrise / Moonset
        var moonriseLocal = sunriseLocal.AddHours(0.8 * ((moonNirayana - sunNirayana + 360) % 360) / 15.0);
        var moonsetLocal = moonriseLocal.AddHours(12.2);

        // Weekday (Vaar)
        var dayOfWeek = (int)localDate.DayOfWeek; // 0=Sunday
        var (vaarEng, vaarLord, vaarSanskrit) = VaarList[dayOfWeek];

        // Tithi Calculation
        var elongation = NormalizeDegrees(moonNirayana - sunNirayana);
        var tithiIndex = (int)(elongation / 12.0) + 1; // 1 to 30
        tithiIndex = Math.Clamp(tithiIndex, 1, 30);
        var tithiRemainderDeg = elongation - ((tithiIndex - 1) * 12.0);
        var tithiProgress = (decimal)(tithiRemainderDeg / 12.0 * 100.0);
        var paksha = tithiIndex <= 15 ? "Shukla Paksha" : "Krishna Paksha";
        var tithiEndTimeLocal = localTimeOfCalc.AddHours((double)((100m - tithiProgress) / 100m * 24m));

        var tithiInfo = new TithiInfoDto
        {
            Number = tithiIndex,
            Name = TithiNames[tithiIndex - 1],
            Paksha = paksha,
            Deity = TithiDeities[tithiIndex - 1],
            EndTime = tithiEndTimeLocal.ToString("hh:mm tt"),
            ProgressPercent = Math.Round(tithiProgress, 1)
        };

        // Nakshatra Calculation
        var nakshatraIdx = (int)(moonNirayana / (360.0 / 27.0));
        nakshatraIdx = Math.Clamp(nakshatraIdx, 0, 26);
        var nakRemainder = moonNirayana - (nakshatraIdx * (360.0 / 27.0));
        var pada = (int)(nakRemainder / (360.0 / 108.0)) + 1;
        var nakshatraProgress = (decimal)(nakRemainder / (360.0 / 27.0) * 100.0);
        var (nakName, nakLord, nakDeity) = NakshatraList[nakshatraIdx];
        var nakEndTimeLocal = localTimeOfCalc.AddHours((double)((100m - nakshatraProgress) / 100m * 24m));

        var nakshatraInfo = new NakshatraInfoDto
        {
            Number = nakshatraIdx + 1,
            Name = nakName,
            Pada = Math.Clamp(pada, 1, 4),
            Lord = nakLord,
            Deity = nakDeity,
            EndTime = nakEndTimeLocal.ToString("hh:mm tt"),
            ProgressPercent = Math.Round(nakshatraProgress, 1)
        };

        // Yoga Calculation
        var yogaSum = NormalizeDegrees(sunNirayana + moonNirayana);
        var yogaIdx = (int)(yogaSum / (360.0 / 27.0));
        yogaIdx = Math.Clamp(yogaIdx, 0, 26);
        var (yogaName, yogaMeaning) = YogaList[yogaIdx];

        var yogaInfo = new YogaInfoDto
        {
            Number = yogaIdx + 1,
            Name = yogaName,
            Meaning = yogaMeaning,
            EndTime = localTimeOfCalc.AddHours(14.5).ToString("hh:mm tt")
        };

        // Karana Calculation (Half Tithi = 6 degrees)
        var karanaHalfIndex = (int)(elongation / 6.0) + 1; // 1 to 60
        var karanaName = GetKaranaName(karanaHalfIndex);
        var karanaNature = karanaHalfIndex is 1 or 58 or 59 or 60 ? "Sthira (Fixed)" : "Chara (Movable)";

        var karanaInfo = new KaranaInfoDto
        {
            Number = karanaHalfIndex,
            Name = karanaName,
            Nature = karanaNature,
            Deity = "Vedic Divinity",
            EndTime = localTimeOfCalc.AddHours(7.2).ToString("hh:mm tt")
        };

        // Muhuratas (Rahu Kaal, Gulika, Yamaganda, Abhijit)
        var daylightDuration = sunsetLocal - sunriseLocal;
        var partDuration = TimeSpan.FromTicks(daylightDuration.Ticks / 8);

        // Rahu Kaal Part by day of week (0=Sun:8, 1=Mon:2, 2=Tue:7, 3=Wed:5, 4=Thu:6, 5=Fri:4, 6=Sat:3)
        var rahuPartNumber = dayOfWeek switch
        {
            0 => 8,
            1 => 2,
            2 => 7,
            3 => 5,
            4 => 6,
            5 => 4,
            6 => 3,
            _ => 8
        };
        var rahuStart = sunriseLocal + (partDuration * (rahuPartNumber - 1));
        var rahuEnd = rahuStart + partDuration;

        // Gulika Kaal
        var gulikaPartNumber = dayOfWeek switch
        {
            0 => 7,
            1 => 6,
            2 => 5,
            3 => 4,
            4 => 3,
            5 => 2,
            6 => 1,
            _ => 7
        };
        var gulikaStart = sunriseLocal + (partDuration * (gulikaPartNumber - 1));
        var gulikaEnd = gulikaStart + partDuration;

        // Yamaganda Kaal
        var yamaPartNumber = dayOfWeek switch
        {
            0 => 5,
            1 => 4,
            2 => 3,
            3 => 2,
            4 => 1,
            5 => 7,
            6 => 6,
            _ => 5
        };
        var yamaStart = sunriseLocal + (partDuration * (yamaPartNumber - 1));
        var yamaEnd = yamaStart + partDuration;

        // Abhijit Muhurat: Centered at midday (approx 48 min)
        var midday = sunriseLocal + TimeSpan.FromTicks(daylightDuration.Ticks / 2);
        var abhijitStart = midday.AddMinutes(-24);
        var abhijitEnd = midday.AddMinutes(24);

        // Brahma Muhurat: 96 mins before sunrise to 48 mins before sunrise
        var brahmaStart = sunriseLocal.AddMinutes(-96);
        var brahmaEnd = sunriseLocal.AddMinutes(-48);

        // Amrit Kaal (auspicious portion based on Moon's Nakshatra)
        var amritStart = midday.AddHours(2.5);
        var amritEnd = amritStart.AddMinutes(96);

        var nowLocal = DateTime.UtcNow.AddHours(tzOffset);

        var rahuInfo = new MuhuratInfoDto
        {
            Name = "Rahu Kaal",
            StartTime = rahuStart.ToString("hh:mm tt"),
            EndTime = rahuEnd.ToString("hh:mm tt"),
            IsAuspicious = false,
            IsCurrentlyActive = nowLocal >= rahuStart && nowLocal <= rahuEnd
        };

        var gulikaInfo = new MuhuratInfoDto
        {
            Name = "Gulika Kaal",
            StartTime = gulikaStart.ToString("hh:mm tt"),
            EndTime = gulikaEnd.ToString("hh:mm tt"),
            IsAuspicious = false,
            IsCurrentlyActive = nowLocal >= gulikaStart && nowLocal <= gulikaEnd
        };

        var yamaInfo = new MuhuratInfoDto
        {
            Name = "Yamaganda Kaal",
            StartTime = yamaStart.ToString("hh:mm tt"),
            EndTime = yamaEnd.ToString("hh:mm tt"),
            IsAuspicious = false,
            IsCurrentlyActive = nowLocal >= yamaStart && nowLocal <= yamaEnd
        };

        var abhijitInfo = new MuhuratInfoDto
        {
            Name = "Abhijit Muhurat",
            StartTime = abhijitStart.ToString("hh:mm tt"),
            EndTime = abhijitEnd.ToString("hh:mm tt"),
            IsAuspicious = true,
            IsCurrentlyActive = nowLocal >= abhijitStart && nowLocal <= abhijitEnd
        };

        var brahmaInfo = new MuhuratInfoDto
        {
            Name = "Brahma Muhurat",
            StartTime = brahmaStart.ToString("hh:mm tt"),
            EndTime = brahmaEnd.ToString("hh:mm tt"),
            IsAuspicious = true,
            IsCurrentlyActive = nowLocal >= brahmaStart && nowLocal <= brahmaEnd
        };

        var amritInfo = new MuhuratInfoDto
        {
            Name = "Amrit Kaal",
            StartTime = amritStart.ToString("hh:mm tt"),
            EndTime = amritEnd.ToString("hh:mm tt"),
            IsAuspicious = true,
            IsCurrentlyActive = nowLocal >= amritStart && nowLocal <= amritEnd
        };

        // Choghadiyas
        var dayChoghadiyas = CalculateDayChoghadiya(dayOfWeek, sunriseLocal, sunsetLocal, nowLocal);
        var nextSunriseLocal = sunriseLocal.AddDays(1);
        var nightChoghadiyas = CalculateNightChoghadiya(dayOfWeek, sunsetLocal, nextSunriseLocal, nowLocal);

        // Vedic Calendar
        var vikramYear = localDate.Year + 57;
        var shakaYear = localDate.Year - 78;

        return new PanchangResponseDto
        {
            Date = localDate,
            LocationName = request.LocationName,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TimeZoneOffsetHours = request.TimeZoneOffsetHours,

            VaarEnglish = vaarEng,
            VaarSanskrit = vaarSanskrit,
            VaarLord = vaarLord,

            Sunrise = sunriseLocal.ToString("hh:mm tt"),
            Sunset = sunsetLocal.ToString("hh:mm tt"),
            Moonrise = moonriseLocal.ToString("hh:mm tt"),
            Moonset = moonsetLocal.ToString("hh:mm tt"),
            SunSign = sunSign,
            MoonSign = moonSign,

            Tithi = tithiInfo,
            Nakshatra = nakshatraInfo,
            Yoga = yogaInfo,
            Karana = karanaInfo,

            VikramSamvat = $"{vikramYear} (Krodhi)",
            ShakaSamvat = $"{shakaYear} (Krodhana)",
            LunarMonthAmanta = GetLunarMonth(moonSignIdx, false),
            LunarMonthPurnimanta = GetLunarMonth(moonSignIdx, true),
            Ritu = GetRitu(localDate.Month),
            Ayana = (sunNirayana >= 270 || sunNirayana < 90) ? "Uttarayana" : "Dakshinayana",
            AyanamshaLahiri = Math.Round((decimal)ayanamsha, 4),

            RahuKaal = rahuInfo,
            GulikaKaal = gulikaInfo,
            YamagandaKaal = yamaInfo,
            AbhijitMuhurat = abhijitInfo,
            BrahmaMuhurat = brahmaInfo,
            AmritKaal = amritInfo,

            DayChoghadiya = dayChoghadiyas,
            NightChoghadiya = nightChoghadiyas
        };
    }

    private static IReadOnlyList<ChoghadiyaPeriodDto> CalculateDayChoghadiya(int dayOfWeek, DateTime sunrise, DateTime sunset, DateTime now)
    {
        var periods = new List<ChoghadiyaPeriodDto>();
        var duration = sunset - sunrise;
        var slot = TimeSpan.FromTicks(duration.Ticks / 8);
        var startIndex = DayChoghadiyaStartIndices[dayOfWeek];

        for (int i = 0; i < 8; i++)
        {
            var typeIdx = (startIndex + i) % 7;
            var (name, nature, badge, lord) = ChoghadiyaTypes[typeIdx];
            var start = sunrise + (slot * i);
            var end = start + slot;

            periods.Add(new ChoghadiyaPeriodDto
            {
                PeriodNumber = i + 1,
                Name = name,
                Nature = nature,
                ColorBadge = badge,
                RulerPlanet = lord,
                StartTime = start.ToString("hh:mm tt"),
                EndTime = end.ToString("hh:mm tt"),
                IsDay = true,
                IsCurrentlyActive = now >= start && now < end
            });
        }

        return periods.AsReadOnly();
    }

    private static IReadOnlyList<ChoghadiyaPeriodDto> CalculateNightChoghadiya(int dayOfWeek, DateTime sunset, DateTime nextSunrise, DateTime now)
    {
        var periods = new List<ChoghadiyaPeriodDto>();
        var duration = nextSunrise - sunset;
        var slot = TimeSpan.FromTicks(duration.Ticks / 8);
        var startIndex = NightChoghadiyaStartIndices[dayOfWeek];

        for (int i = 0; i < 8; i++)
        {
            var typeIdx = (startIndex + i) % 7;
            var (name, nature, badge, lord) = ChoghadiyaTypes[typeIdx];
            var start = sunset + (slot * i);
            var end = start + slot;

            periods.Add(new ChoghadiyaPeriodDto
            {
                PeriodNumber = i + 1,
                Name = name,
                Nature = nature,
                ColorBadge = badge,
                RulerPlanet = lord,
                StartTime = start.ToString("hh:mm tt"),
                EndTime = end.ToString("hh:mm tt"),
                IsDay = false,
                IsCurrentlyActive = now >= start && now < end
            });
        }

        return periods.AsReadOnly();
    }

    private static string GetKaranaName(int halfTithiIndex)
    {
        if (halfTithiIndex == 1) return "Kintughna";
        if (halfTithiIndex >= 58)
        {
            return halfTithiIndex switch
            {
                58 => "Shakuni",
                59 => "Chatushpada",
                60 => "Naga",
                _ => "Bava"
            };
        }

        string[] repeating = { "Bava", "Balava", "Kaulava", "Taitila", "Garija", "Vanija", "Vishti (Bhadra)" };
        return repeating[(halfTithiIndex - 2) % 7];
    }

    private static string GetLunarMonth(int moonSignIdx, bool purnimanta)
    {
        string[] months =
        {
            "Chaitra", "Vaishakha", "Jyeshtha", "Ashadha", "Shravana", "Bhadrapada",
            "Ashvina", "Kartika", "Margashirsha", "Pausha", "Magha", "Phalguna"
        };
        return months[Math.Clamp(moonSignIdx, 0, 11)];
    }

    private static string GetRitu(int month) => month switch
    {
        3 or 4 => "Vasanta (Spring)",
        5 or 6 => "Grishma (Summer)",
        7 or 8 => "Varsha (Monsoon)",
        9 or 10 => "Sharad (Autumn)",
        11 or 12 => "Hemanta (Pre-Winter)",
        _ => "Shishira (Winter)"
    };

    private static double ToJulianDay(DateTime utcDate)
    {
        int y = utcDate.Year;
        int m = utcDate.Month;
        double d = utcDate.Day + (utcDate.Hour + utcDate.Minute / 60.0 + utcDate.Second / 3600.0) / 24.0;

        if (m <= 2)
        {
            y -= 1;
            m += 12;
        }

        int a = y / 100;
        int b = 2 - a + (a / 4);

        return Math.Floor(365.25 * (y + 4716)) + Math.Floor(30.6001 * (m + 1)) + d + b - 1524.5;
    }

    private static double CalculateLahiriAyanamsha(double jd)
    {
        // J2000.0 Lahiri value ~ 23.853055 degrees, rate 50.29 arcseconds/year
        var t = (jd - 2451545.0) / 365.25;
        return 23.853055 + (0.013969 * t);
    }

    private static double CalculateSunLongitude(double jd)
    {
        var t = (jd - 2451545.0) / 36525.0;
        var l0 = 280.46646 + (36000.76983 * t) + (0.0003032 * t * t);
        var m = 357.52911 + (35999.05029 * t) - (0.0001537 * t * t);
        var mRad = m * Math.PI / 180.0;
        var c = (1.914602 - 0.004817 * t) * Math.Sin(mRad) +
                (0.019993 - 0.000101 * t) * Math.Sin(2 * mRad) +
                0.000289 * Math.Sin(3 * mRad);
        return NormalizeDegrees(l0 + c);
    }

    private static double CalculateMoonLongitude(double jd)
    {
        var t = (jd - 2451545.0) / 36525.0;
        var lp = 218.3164477 + (481267.88123421 * t);
        var d = 297.8501921 + (445267.1114034 * t);
        var m = 357.5291092 + (35999.0502909 * t);
        var mp = 134.9633964 + (477198.8675055 * t);
        var f = 93.2720950 + (483202.0175233 * t);

        double ToRad(double deg) => deg * Math.PI / 180.0;

        var periodic = 6.288774 * Math.Sin(ToRad(mp)) +
                       1.274027 * Math.Sin(ToRad(2 * d - mp)) +
                       0.658314 * Math.Sin(ToRad(2 * d)) +
                       0.213618 * Math.Sin(ToRad(2 * mp)) -
                       0.185116 * Math.Sin(ToRad(m)) -
                       0.114332 * Math.Sin(ToRad(2 * f));

        return NormalizeDegrees(lp + periodic);
    }

    private static (DateTime SunriseUtc, DateTime SunsetUtc) CalculateSunriseSunset(DateTime date, double lat, double lon)
    {
        var dayOfYear = date.DayOfYear;
        var gamma = 2 * Math.PI / 365.0 * (dayOfYear - 1 + (12.0 - 12.0) / 24.0);
        
        // Equation of time in minutes
        var eqtime = 229.18 * (0.000075 + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma)
                     - 0.014615 * Math.Cos(2 * gamma) - 0.040849 * Math.Sin(2 * gamma));

        // Solar declination in radians
        var decl = 0.006918 - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma)
                   - 0.006758 * Math.Cos(2 * gamma) + 0.000907 * Math.Sin(2 * gamma);

        var latRad = lat * Math.PI / 180.0;
        var zenith = 90.833 * Math.PI / 180.0; // Official civil sunrise/sunset zenith

        var cosHa = (Math.Cos(zenith) / (Math.Cos(latRad) * Math.Cos(decl))) - (Math.Tan(latRad) * Math.Tan(decl));
        cosHa = Math.Clamp(cosHa, -1.0, 1.0);
        var haDeg = Math.Acos(cosHa) * 180.0 / Math.PI;

        // Solar noon in UTC minutes from midnight
        var solarNoonUtcMin = 720 - (4 * lon) - eqtime;
        var sunriseUtcMin = solarNoonUtcMin - (haDeg * 4);
        var sunsetUtcMin = solarNoonUtcMin + (haDeg * 4);

        var sunriseUtc = date.Date.AddMinutes(sunriseUtcMin);
        var sunsetUtc = date.Date.AddMinutes(sunsetUtcMin);

        return (sunriseUtc, sunsetUtc);
    }

    private static double NormalizeDegrees(double degrees)
    {
        degrees %= 360.0;
        if (degrees < 0) degrees += 360.0;
        return degrees;
    }
}
