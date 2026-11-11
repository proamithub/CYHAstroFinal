using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CYHAstro.Core.Constants;
using CYHAstro.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CYHAstro.Tests.Unit;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private const string TestKey = "TestSuperSecretKeyForJwtTokenServiceMustBeAtLeast32BytesLong!";
    private const string TestIssuer = "CYHAstro.Api";
    private const string TestAudience = "CYHAstro.Clients";

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Key", TestKey },
            { "Jwt:Issuer", TestIssuer },
            { "Jwt:Audience", TestAudience },
            { "Jwt:ExpiryMinutes", "60" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new TokenService(config);
    }

    [Fact]
    public void GenerateAccessToken_ShouldProduceValidToken_WithExpectedClaims()
    {
        // Arrange
        var identityId = "ident-user-123";
        var userId = 42L;
        var publicId = "usr_abc123xyz";
        var email = "astro.seeker@example.com";
        var displayName = "Seeker Rahul";
        var roles = new List<string> { AppConstants.Roles.User, AppConstants.Roles.Astrologer };

        // Act
        var (token, expiresAt) = _tokenService.GenerateAccessToken(identityId, userId, publicId, email, displayName, roles);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTime.UtcNow);

        var principal = _tokenService.GetPrincipalFromToken(token, validateLifetime: true);
        principal.Should().NotBeNull();

        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(identityId);
        principal.FindFirst("userId")?.Value.Should().Be("42");
        principal.FindFirst("publicId")?.Value.Should().Be(publicId);
        principal.FindFirst(ClaimTypes.Email)?.Value.Should().Be(email);
        principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be(displayName);

        var claimsRoles = principal.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
        claimsRoles.Should().Contain(AppConstants.Roles.User);
        claimsRoles.Should().Contain(AppConstants.Roles.Astrologer);
    }

    [Fact]
    public void GenerateRefreshTokenString_ShouldProduce64CharBase64String()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshTokenString();
        var token2 = _tokenService.GenerateRefreshTokenString();

        // Assert
        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void HashToken_ShouldBeDeterministicAndReturn64CharHex()
    {
        // Arrange
        var token = "SampleRefreshTokenString123456789";

        // Act
        var hash1 = _tokenService.HashToken(token);
        var hash2 = _tokenService.HashToken(token);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Length.Should().Be(64); // SHA-256 hex string is 64 characters
    }

    [Fact]
    public void GetPrincipalFromToken_ShouldReturnNull_ForTamperedToken()
    {
        // Arrange
        var (validToken, _) = _tokenService.GenerateAccessToken(
            "id-1", 1, "pub-1", "test@test.com", "Test", new[] { "User" });
        var tamperedToken = validToken + "tampered";

        // Act
        var principal = _tokenService.GetPrincipalFromToken(tamperedToken, validateLifetime: true);

        // Assert
        principal.Should().BeNull();
    }
}
