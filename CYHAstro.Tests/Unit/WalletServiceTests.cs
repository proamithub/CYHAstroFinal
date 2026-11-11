using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Wallet;
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

public class WalletServiceTests
{
    private readonly Mock<IWalletRepository> _mockWalletRepo = new();
    private readonly Mock<ILogger<WalletService>> _mockLogger = new();
    private readonly WalletService _walletService;

    public WalletServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=CyhAstroTest;Trusted_Connection=True;" },
            { "Razorpay:KeyId", "rzp_test_mock_key" },
            { "Razorpay:KeySecret", "rzp_test_secret" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var db = new DbConnectionFactory(config);

        _walletService = new WalletService(_mockWalletRepo.Object, db, config, _mockLogger.Object);
    }

    [Fact]
    public async Task GetBalance_ShouldReturnTotalBalance_CalculatedFromPaidAndBonus()
    {
        // Arrange
        var userId = 100L;
        var wallet = new Wallet
        {
            UserId = userId,
            PaidBalance = 350.50m,
            BonusBalance = 150.00m,
            CurrencyCode = "INR",
            RowVersion = 3
        };

        _mockWalletRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        // Act
        var result = await _walletService.GetBalanceAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var balance = result.Value!;
        balance.PaidBalance.Should().Be(350.50m);
        balance.BonusBalance.Should().Be(150.00m);
        balance.TotalBalance.Should().Be(500.50m);
        balance.CurrencyCode.Should().Be("INR");
    }

    [Fact]
    public async Task GetBalance_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        // Arrange
        _mockWalletRepo.Setup(r => r.GetByUserIdAsync(999L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        // Act
        var result = await _walletService.GetBalanceAsync(999L);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(Result.ErrorCodes.NotFound);
    }

    [Fact]
    public async Task GetTransactionHistory_ShouldMapTransactionsProperly()
    {
        // Arrange
        var userId = 50L;
        var transactions = new List<WalletTransaction>
        {
            new()
            {
                Id = 1,
                WalletId = 10,
                UserId = userId,
                TransactionType = "CREDIT",
                BalanceType = "PAID",
                Amount = 500m,
                BalanceBefore = 0m,
                BalanceAfter = 500m,
                ReferenceType = "RECHARGE",
                Description = "Recharge ₹500",
                IdempotencyKey = "tx_001",
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockWalletRepo.Setup(r => r.GetTransactionHistoryAsync(userId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);
        _mockWalletRepo.Setup(r => r.GetTransactionCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _walletService.GetTransactionHistoryAsync(userId, 1, 20);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items[0].Amount.Should().Be(500m);
        result.Value.Items[0].TransactionType.Should().Be("CREDIT");
    }
}
