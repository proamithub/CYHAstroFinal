namespace CYHAstro.Core.DTOs.Panchang;

public sealed record PanchangRequestDto
{
    public DateTime Date { get; init; } = DateTime.UtcNow.Date;
    public decimal Latitude { get; init; } = 28.6139m; // New Delhi
    public decimal Longitude { get; init; } = 77.2090m;
    public decimal TimeZoneOffsetHours { get; init; } = 5.5m; // IST
    public string LocationName { get; init; } = "New Delhi, India";
}

public sealed record PanchangResponseDto
{
    public DateTime Date { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public decimal TimeZoneOffsetHours { get; init; }

    // Vaar (Weekday)
    public string VaarEnglish { get; init; } = string.Empty;
    public string VaarSanskrit { get; init; } = string.Empty;
    public string VaarLord { get; init; } = string.Empty;

    // Sun & Moon Timings
    public string Sunrise { get; init; } = string.Empty;
    public string Sunset { get; init; } = string.Empty;
    public string Moonrise { get; init; } = string.Empty;
    public string Moonset { get; init; } = string.Empty;
    public string SunSign { get; init; } = string.Empty;
    public string MoonSign { get; init; } = string.Empty;

    // Tithi
    public TithiInfoDto Tithi { get; init; } = new();

    // Nakshatra
    public NakshatraInfoDto Nakshatra { get; init; } = new();

    // Yoga
    public YogaInfoDto Yoga { get; init; } = new();

    // Karana
    public KaranaInfoDto Karana { get; init; } = new();

    // Astrological Calendar
    public string VikramSamvat { get; init; } = string.Empty;
    public string ShakaSamvat { get; init; } = string.Empty;
    public string LunarMonthAmanta { get; init; } = string.Empty;
    public string LunarMonthPurnimanta { get; init; } = string.Empty;
    public string Ritu { get; init; } = string.Empty;
    public string Ayana { get; init; } = string.Empty;
    public decimal AyanamshaLahiri { get; init; }

    // Muhuratas
    public MuhuratInfoDto RahuKaal { get; init; } = new();
    public MuhuratInfoDto GulikaKaal { get; init; } = new();
    public MuhuratInfoDto YamagandaKaal { get; init; } = new();
    public MuhuratInfoDto AbhijitMuhurat { get; init; } = new();
    public MuhuratInfoDto BrahmaMuhurat { get; init; } = new();
    public MuhuratInfoDto AmritKaal { get; init; } = new();

    // Choghadiya
    public IReadOnlyList<ChoghadiyaPeriodDto> DayChoghadiya { get; init; } = Array.Empty<ChoghadiyaPeriodDto>();
    public IReadOnlyList<ChoghadiyaPeriodDto> NightChoghadiya { get; init; } = Array.Empty<ChoghadiyaPeriodDto>();
}

public sealed record TithiInfoDto
{
    public int Number { get; init; } // 1 - 30
    public string Name { get; init; } = string.Empty; // e.g., Pratipada, Ekadashi, Purnima, Amavasya
    public string Paksha { get; init; } = string.Empty; // Shukla or Krishna
    public string Deity { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public decimal ProgressPercent { get; init; }
}

public sealed record NakshatraInfoDto
{
    public int Number { get; init; } // 1 - 27
    public string Name { get; init; } = string.Empty; // e.g., Ashwini, Rohini
    public int Pada { get; init; } // 1 - 4
    public string Lord { get; init; } = string.Empty;
    public string Deity { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public decimal ProgressPercent { get; init; }
}

public sealed record YogaInfoDto
{
    public int Number { get; init; } // 1 - 27
    public string Name { get; init; } = string.Empty; // e.g., Vishkambha, Priti, Ayushman
    public string Meaning { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
}

public sealed record KaranaInfoDto
{
    public int Number { get; init; } // 1 - 60
    public string Name { get; init; } = string.Empty; // e.g., Bava, Balava, Kaulava
    public string Nature { get; init; } = string.Empty; // Chara (Moveable) or Sthira (Fixed)
    public string Deity { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
}

public sealed record MuhuratInfoDto
{
    public string Name { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public bool IsAuspicious { get; init; }
    public bool IsCurrentlyActive { get; init; }
}

public sealed record ChoghadiyaPeriodDto
{
    public int PeriodNumber { get; init; }
    public string Name { get; init; } = string.Empty; // Amrit, Shubh, Labh, Char, Rog, Kaal, Udveg
    public string Nature { get; init; } = string.Empty; // Auspicious, Neutral, Inauspicious
    public string ColorBadge { get; init; } = string.Empty; // success, info, warning, danger
    public string RulerPlanet { get; init; } = string.Empty;
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public bool IsDay { get; init; }
    public bool IsCurrentlyActive { get; init; }
}
