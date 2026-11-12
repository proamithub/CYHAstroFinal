using System.Text;
using CYHAstro.Core.DTOs.Astrology;
using CYHAstro.Infrastructure.Reports;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CYHAstro.Tests.Unit;

public sealed class KundliPdfGeneratorTests
{
    [Fact]
    public async Task GenerateKundliPdfAsync_ShouldReturnValidPdfDocument()
    {
        // Arrange
        var generator = new KundliPdfGenerator(NullLogger<KundliPdfGenerator>.Instance);

        var request = new KundliRequestDto
        {
            Name = "Aarav Sharma",
            Gender = "M",
            DateOfBirth = new DateTime(1995, 8, 15),
            TimeOfBirth = new TimeSpan(14, 30, 0),
            BirthPlace = "New Delhi, India",
            Latitude = 28.6139m,
            Longitude = 77.2090m,
            ChartStyle = "NorthIndian",
            Ayanamsha = "Lahiri"
        };

        var chart = new KundliChartDto
        {
            PublicId = "knd_test12345",
            Name = "Aarav Sharma",
            AscendantSign = "Scorpio",
            AscendantSignId = 8,
            MoonSign = "Aries",
            SunSign = "Leo",
            BirthNakshatra = "Ashwini",
            BirthPada = 1,
            MangalDoshaStatus = "No (Anshik)",
            KaalSarpDoshaStatus = "No",
            SadeSatiStatus = "None",
            CurrentDasha = "Jupiter / Saturn",
            AiInterpretation = "The native possesses strong leadership capabilities and keen insight.",
            Planets = new List<KundliPlanetaryPositionDto>
            {
                new() { PlanetName = "Sun", SanskritName = "Surya", Longitude = 122.5m, SignId = 5, SignName = "Leo", House = 10, Nakshatra = "Magha", Pada = 1, IsRetrograde = false },
                new() { PlanetName = "Moon", SanskritName = "Chandra", Longitude = 10.2m, SignId = 1, SignName = "Aries", House = 6, Nakshatra = "Ashwini", Pada = 4, IsRetrograde = false },
                new() { PlanetName = "Mars", SanskritName = "Mangal", Longitude = 205.8m, SignId = 7, SignName = "Libra", House = 12, Nakshatra = "Vishakha", Pada = 2, IsRetrograde = false },
                new() { PlanetName = "Mercury", SanskritName = "Budha", Longitude = 135.1m, SignId = 5, SignName = "Leo", House = 10, Nakshatra = "Purva Phalguni", Pada = 1, IsRetrograde = true },
                new() { PlanetName = "Jupiter", SanskritName = "Guru", Longitude = 240.4m, SignId = 9, SignName = "Sagittarius", House = 2, Nakshatra = "Mula", Pada = 1, IsRetrograde = false },
                new() { PlanetName = "Venus", SanskritName = "Shukra", Longitude = 150.9m, SignId = 6, SignName = "Virgo", House = 11, Nakshatra = "Uttara Phalguni", Pada = 3, IsRetrograde = false },
                new() { PlanetName = "Saturn", SanskritName = "Shani", Longitude = 330.6m, SignId = 12, SignName = "Pisces", House = 5, Nakshatra = "Uttara Bhadrapada", Pada = 2, IsRetrograde = true },
                new() { PlanetName = "Rahu", SanskritName = "Rahu", Longitude = 195.0m, SignId = 7, SignName = "Libra", House = 12, Nakshatra = "Swati", Pada = 3, IsRetrograde = true },
                new() { PlanetName = "Ketu", SanskritName = "Ketu", Longitude = 15.0m, SignId = 1, SignName = "Aries", House = 6, Nakshatra = "Bharani", Pada = 1, IsRetrograde = true }
            },
            Houses = Enumerable.Range(1, 12).Select(i => new KundliHouseDto
            {
                HouseNumber = i,
                SignId = ((7 + i - 1) % 12) + 1,
                SignName = "Sign" + i,
                PlanetsInHouse = i == 10 ? new List<string> { "Sun", "Mercury" } : new List<string>()
            }).ToList()
        };

        // Act
        var result = await generator.GenerateKundliPdfAsync(chart, request);

        // Assert
        Assert.True(result.IsSuccess, result.Error ?? "Unknown error");
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Length > 1000);

        // Verify PDF Magic Header bytes "%PDF"
        var header = Encoding.ASCII.GetString(result.Value.Take(4).ToArray());
        Assert.Equal("%PDF", header);
    }
}
