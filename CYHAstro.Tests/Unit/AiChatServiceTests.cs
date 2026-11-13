using CYHAstro.Core.DTOs.Ai;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Data;
using CYHAstro.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CYHAstro.Tests.Unit;

public class AiChatServiceTests
{
    private readonly Mock<IAiAstrologyService> _mockAiAstrology = new();
    private readonly Mock<IBirthProfileRepository> _mockBirthRepo = new();
    private readonly Mock<ILogger<AiChatService>> _mockLogger = new();
    private readonly AiChatService _service;

    public AiChatServiceTests()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=CyhAstroTest;Trusted_Connection=True;" }
        }).Build();

        var db = new DbConnectionFactory(config);
        _service = new AiChatService(db, _mockAiAstrology.Object, _mockBirthRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SendMessageAsync_WithEmptyMessage_ShouldFail()
    {
        var req = new SendAiChatMessageRequestDto { Message = "" };
        var res = await _service.SendMessageAsync(10, req);

        res.IsSuccess.Should().BeFalse();
        res.Error.Should().Contain("cannot be empty");
    }
}
