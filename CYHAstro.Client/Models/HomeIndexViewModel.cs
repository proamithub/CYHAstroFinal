using CYHAstro.Core.DTOs.Astrologer;
using CYHAstro.Core.DTOs.Astrology;

namespace CYHAstro.Client.Models;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<AstrologerListingDto> TopAstrologers { get; init; } = new List<AstrologerListingDto>();
    public DailyHoroscopeDto? FeaturedHoroscope { get; init; }
    public IReadOnlyList<string> Specialties { get; init; } = new List<string>();
}
