using System.Text;
using CYHAstro.Core.DTOs.Astrology;
using CYHAstro.Infrastructure.Reports;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CYHAstro.Tests.Unit;

public sealed class MatchmakingPdfGeneratorTests
{
    [Fact]
    public async Task GenerateMatchmakingPdfAsync_ShouldReturnValidPdfDocument()
    {
        // Arrange
        var generator = new MatchmakingPdfGenerator(NullLogger<MatchmakingPdfGenerator>.Instance);

        var request = new MatchmakingRequestDto
        {
            MaleName = "Aarav Sharma",
            MaleDob = new DateTime(1994, 5, 12),
            MaleTob = new TimeSpan(9, 30, 0),
            MaleLat = 28.6139m,
            MaleLon = 77.2090m,

            FemaleName = "Pooja Verma",
            FemaleDob = new DateTime(1996, 8, 20),
            FemaleTob = new TimeSpan(15, 45, 0),
            FemaleLat = 19.0760m,
            FemaleLon = 72.8777m
        };

        var result = new MatchmakingResultDto
        {
            TotalScore = 28.5m,
            Conclusion = "Highly Recommended (Shubh Vivah)",
            HasNadiDosha = false,
            HasBhakootDosha = false,
            HasGanaDosha = false,
            AiVerdict = "Both charts exhibit high mutual affinity and planetary friendship, ensuring happiness and mutual growth.",
            GunaBreakdown = new List<GunaMilanItemDto>
            {
                new() { GunaName = "Varna", MaxPoints = 1, ObtainedPoints = 1, Description = "Work and spiritual alignment" },
                new() { GunaName = "Vashya", MaxPoints = 2, ObtainedPoints = 2, Description = "Mutual attraction and dominance" },
                new() { GunaName = "Tara", MaxPoints = 3, ObtainedPoints = 3, Description = "Destiny and health longevity" },
                new() { GunaName = "Yoni", MaxPoints = 4, ObtainedPoints = 3.5m, Description = "Physical and biological harmony" },
                new() { GunaName = "Graha Maitri", MaxPoints = 5, ObtainedPoints = 5, Description = "Psychological and mental friendship" },
                new() { GunaName = "Gana", MaxPoints = 6, ObtainedPoints = 6, Description = "Temperament and behavior alignment" },
                new() { GunaName = "Bhakoot", MaxPoints = 7, ObtainedPoints = 0, Description = "Family prosperity and mutual love" },
                new() { GunaName = "Nadi", MaxPoints = 8, ObtainedPoints = 8, Description = "Genetic, physiological and progeny compatibility" }
            }
        };

        // Act
        var pdfResult = await generator.GenerateMatchmakingPdfAsync(result, request);

        // Assert
        Assert.True(pdfResult.IsSuccess);
        Assert.NotNull(pdfResult.Value);
        Assert.NotEmpty(pdfResult.Value);

        // Verify PDF Magic Bytes (%PDF-)
        var header = Encoding.ASCII.GetString(pdfResult.Value.Take(5).ToArray());
        Assert.Equal("%PDF-", header);
    }
}
