using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Astrology;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Cache;
using CYHAstro.Infrastructure.Data;
using CYHAstro.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CYHAstro.Tests.Unit;

public class AstrologyCalculationServiceTests
{
    private readonly Mock<IAiAstrologyService> _mockAi = new();
    private readonly Mock<ICacheService> _mockCache = new();
    private readonly Mock<ILogger<AstrologyCalculationService>> _mockLogger = new();
    private readonly AstrologyCalculationService _service;

    public AstrologyCalculationServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=CyhAstroTest;Trusted_Connection=True;" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var db = new DbConnectionFactory(config);

        _mockAi.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("Harmonious planetary alignment today with high success in endeavors."));

        _mockAi.Setup(x => x.ExplainKundliAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("Strong leadership indications and supportive professional trajectory."));

        _mockCache.Setup(x => x.GetAsync<DailyHoroscopeDto>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) =>
            {
                var parts = key.Split(':');
                int signId = int.Parse(parts[1]);
                string[] zodiacs = { "", "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo", "Libra", "Scorpio", "Sagittarius", "Capricorn", "Aquarius", "Pisces" };
                return new DailyHoroscopeDto
                {
                    ZodiacSignId = signId,
                    ZodiacSignName = zodiacs[signId],
                    SanskritName = "Rashi",
                    Date = DateOnly.FromDateTime(DateTime.UtcNow),
                    GeneralText = "Favorable day ahead with positive planetary aspects.",
                    RatingPercentage = 85,
                    LuckyNumber = 7,
                    LuckyColor = "Yellow"
                };
            });

        _service = new AstrologyCalculationService(_mockAi.Object, _mockCache.Object, db, _mockLogger.Object);
    }

    [Theory]
    [InlineData(1, "Aries")]
    [InlineData(5, "Leo")]
    [InlineData(12, "Pisces")]
    public async Task GetDailyHoroscope_ShouldReturnValidForecast_ForValidSigns(int signId, string expectedSignName)
    {
        // Act
        var result = await _service.GetDailyHoroscopeAsync(signId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ZodiacSignName.Should().Be(expectedSignName);
        result.Value.GeneralText.Should().NotBeNullOrWhiteSpace();
        result.Value.RatingPercentage.Should().BeInRange(50, 100);
        result.Value.LuckyNumber.Should().BeInRange(1, 100);
    }

    [Fact]
    public async Task GetDailyHoroscope_ShouldFail_ForInvalidSign()
    {
        // Act
        var result = await _service.GetDailyHoroscopeAsync(13);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(Result.ErrorCodes.ValidationFailed);
    }

    [Fact]
    public async Task GenerateKundli_ShouldComputeAllNinePlanetsAndTwelveHouses()
    {
        // Arrange
        var request = new KundliRequestDto
        {
            Name = "Aarav Sharma",
            Gender = "M",
            DateOfBirth = new DateTime(1995, 8, 15),
            TimeOfBirth = new TimeSpan(14, 30, 0),
            BirthPlace = "New Delhi, India",
            Latitude = 28.6139m,
            Longitude = 77.2090m,
            IncludeAiInterpretation = true
        };

        // Act
        var result = await _service.GenerateKundliAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var chart = result.Value!;
        chart.Name.Should().Be("Aarav Sharma");
        chart.AscendantSign.Should().NotBeNullOrWhiteSpace();
        chart.MoonSign.Should().NotBeNullOrWhiteSpace();
        chart.SunSign.Should().NotBeNullOrWhiteSpace();
        chart.BirthNakshatra.Should().NotBeNullOrWhiteSpace();

        chart.Planets.Should().HaveCount(9);
        chart.Houses.Should().HaveCount(12);
        chart.MangalDoshaStatus.Should().NotBeNullOrWhiteSpace();
        chart.CurrentDasha.Should().NotBeNullOrWhiteSpace();
        chart.AiInterpretation.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CalculateMatchmaking_ShouldCalculateScoreOutOf36_AndProvideBreakdown()
    {
        // Arrange
        var request = new MatchmakingRequestDto
        {
            MaleName = "Rohan",
            MaleDob = new DateTime(1992, 5, 20),
            MaleTob = new TimeSpan(10, 15, 0),
            MaleLat = 28.6139m,
            MaleLon = 77.2090m,

            FemaleName = "Pooja",
            FemaleDob = new DateTime(1994, 9, 12),
            FemaleTob = new TimeSpan(16, 45, 0),
            FemaleLat = 19.0760m,
            FemaleLon = 72.8777m
        };

        // Act
        var result = await _service.CalculateMatchmakingAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var match = result.Value!;
        match.TotalScore.Should().BeInRange(0.0m, 36.0m);
        match.GunaBreakdown.Should().HaveCount(8);
        match.Conclusion.Should().NotBeNullOrWhiteSpace();
        match.AiVerdict.Should().NotBeNullOrWhiteSpace();
    }
}
