using CYHAstro.Core.DTOs.Panchang;
using CYHAstro.Infrastructure.Panchang;
using Xunit;

namespace CYHAstro.Tests.Unit;

public sealed class VedicPanchangCalculatorTests
{
    [Fact]
    public void Calculate_ShouldReturnCompleteVedicPanchang()
    {
        // Arrange
        var request = new PanchangRequestDto
        {
            Date = new DateTime(2026, 11, 12),
            Latitude = 28.6139m,
            Longitude = 77.2090m,
            TimeZoneOffsetHours = 5.5m,
            LocationName = "New Delhi, India"
        };

        // Act
        var result = VedicPanchangCalculator.Calculate(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Delhi, India", result.LocationName);

        // Weekday (Thursday / Guruvar)
        Assert.Equal("Thursday", result.VaarEnglish);
        Assert.Equal("Guruvar", result.VaarSanskrit);
        Assert.Equal("Jupiter", result.VaarLord);

        // Tithi
        Assert.NotNull(result.Tithi);
        Assert.InRange(result.Tithi.Number, 1, 30);
        Assert.False(string.IsNullOrWhiteSpace(result.Tithi.Name));
        Assert.Contains(result.Tithi.Paksha, new[] { "Shukla Paksha", "Krishna Paksha" });

        // Nakshatra
        Assert.NotNull(result.Nakshatra);
        Assert.InRange(result.Nakshatra.Number, 1, 27);
        Assert.InRange(result.Nakshatra.Pada, 1, 4);
        Assert.False(string.IsNullOrWhiteSpace(result.Nakshatra.Name));

        // Yoga & Karana
        Assert.NotNull(result.Yoga);
        Assert.InRange(result.Yoga.Number, 1, 27);
        Assert.NotNull(result.Karana);
        Assert.InRange(result.Karana.Number, 1, 60);

        // Muhurats
        Assert.NotNull(result.RahuKaal);
        Assert.False(string.IsNullOrWhiteSpace(result.RahuKaal.StartTime));
        Assert.False(string.IsNullOrWhiteSpace(result.RahuKaal.EndTime));
        Assert.False(result.RahuKaal.IsAuspicious);

        Assert.NotNull(result.AbhijitMuhurat);
        Assert.True(result.AbhijitMuhurat.IsAuspicious);

        // Choghadiyas (8 day + 8 night)
        Assert.Equal(8, result.DayChoghadiya.Count);
        Assert.Equal(8, result.NightChoghadiya.Count);
        Assert.All(result.DayChoghadiya, c => Assert.True(c.IsDay));
        Assert.All(result.NightChoghadiya, c => Assert.False(c.IsDay));
    }
}
