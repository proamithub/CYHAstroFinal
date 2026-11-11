using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.User;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Infrastructure.Data;
using CYHAstro.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CYHAstro.Tests.Unit;

public class ProfileServiceTests
{
    private readonly Mock<IBirthProfileRepository> _mockBirthRepo = new();
    private readonly Mock<ILogger<ProfileService>> _mockLogger = new();
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=CyhAstroTest;Trusted_Connection=True;" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var db = new DbConnectionFactory(config);

        _profileService = new ProfileService(db, _mockBirthRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetBirthProfiles_ShouldReturnListOfMappedProfiles()
    {
        // Arrange
        var userId = 25L;
        var profiles = new List<BirthProfile>
        {
            new()
            {
                Id = 1,
                UserId = userId,
                PublicId = "bpf_test1",
                Name = "Priya Sharma",
                Gender = "F",
                DateOfBirth = new DateOnly(1998, 3, 25),
                TimeOfBirth = new TimeOnly(9, 15, 0),
                BirthPlace = "Jaipur, Rajasthan",
                Latitude = 26.9124m,
                Longitude = 75.7873m,
                IsPrimary = true,
                RelationLabel = "Self",
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockBirthRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profiles);

        // Act
        var result = await _profileService.GetBirthProfilesAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Name.Should().Be("Priya Sharma");
        result.Value[0].Gender.Should().Be("F");
        result.Value[0].IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBirthProfile_ShouldCallRepoCreate_AndReturnCreatedDto()
    {
        // Arrange
        var userId = 30L;
        var request = new CreateBirthProfileDto
        {
            Name = "Vikram",
            Gender = "M",
            DateOfBirth = new DateTime(2000, 1, 1),
            TimeOfBirth = new TimeSpan(11, 0, 0),
            BirthPlace = "Varanasi, UP",
            Latitude = 25.3176m,
            Longitude = 82.9739m,
            IsPrimary = true,
            RelationLabel = "Self"
        };

        _mockBirthRepo.Setup(r => r.CreateAsync(It.IsAny<BirthProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(77L);

        // Act
        var result = await _profileService.CreateBirthProfileAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(77L);
        result.Value.Name.Should().Be("Vikram");
        result.Value.PublicId.Should().StartWith("bpf_");
    }

    [Fact]
    public async Task DeleteBirthProfile_ShouldDelegateToRepo()
    {
        // Arrange
        _mockBirthRepo.Setup(r => r.DeleteAsync(10L, 5L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _profileService.DeleteBirthProfileAsync(10L, 5L);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
