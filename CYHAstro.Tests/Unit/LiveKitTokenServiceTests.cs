using System.IdentityModel.Tokens.Jwt;
using CYHAstro.Core.Configuration;
using CYHAstro.Infrastructure.LiveKit;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CYHAstro.Tests.Unit;

public sealed class LiveKitTokenServiceTests
{
    private readonly LiveKitTokenService _sut;

    public LiveKitTokenServiceTests()
    {
        var options = Options.Create(new LiveKitSettings
        {
            ApiKey = "test_api_key",
            ApiSecret = "super_secure_test_secret_with_32_characters!",
            LiveKitUrl = "wss://test.livekit.cloud"
        });

        _sut = new LiveKitTokenService(options);
    }

    [Fact]
    public void GenerateAccessToken_ShouldProduceValidJwt()
    {
        // Arrange
        const string roomName = "room_sess_abc123";
        const string participantId = "usr_42";
        const string participantName = "Sita Devi";

        // Act
        var token = _sut.GenerateAccessToken(roomName, participantId, participantName, true, true);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3); // JWT 3-segment format

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Issuer.Should().Be("test_api_key");
        jwt.Subject.Should().Be(participantId);
        jwt.Claims.Should().Contain(c => c.Type == "name" && c.Value == participantName);
        jwt.Claims.Should().Contain(c => c.Type == "video" && c.Value.Contains(roomName));
    }
}
