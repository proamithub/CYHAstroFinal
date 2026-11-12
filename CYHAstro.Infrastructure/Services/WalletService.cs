using System.Data;
using System.Security.Cryptography;
using System.Text;
using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Wallet;
using CYHAstro.Core.Entities;
using CYHAstro.Core.Interfaces.Repositories;
using CYHAstro.Core.Interfaces.Services;
using CYHAstro.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CYHAstro.Infrastructure.Services;

public sealed class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepo;
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WalletService> _logger;

    public WalletService(
        IWalletRepository walletRepo,
        IDbConnectionFactory db,
        IConfiguration configuration,
        ILogger<WalletService> logger)
    {
        _walletRepo = walletRepo;
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<WalletBalanceDto>> GetBalanceAsync(long userId, CancellationToken ct = default)
    {
        var wallet = await _walletRepo.GetByUserIdAsync(userId, ct);
        if (wallet == null)
        {
            return Result<WalletBalanceDto>.Failure("Wallet not found.", Result.ErrorCodes.NotFound);
        }

        var dto = new WalletBalanceDto
        {
            UserId = wallet.UserId,
            PaidBalance = wallet.PaidBalance,
            BonusBalance = wallet.BonusBalance,
            CurrencyCode = wallet.CurrencyCode,
            RowVersion = wallet.RowVersion
        };

        return Result<WalletBalanceDto>.Success(dto);
    }

    public async Task<Result<PagedResult<WalletTransactionDto>>> GetTransactionHistoryAsync(
        long userId, int page, int pageSize, CancellationToken ct = default)
    {
        var items = await _walletRepo.GetTransactionHistoryAsync(userId, page, pageSize, ct);
        var totalCount = await _walletRepo.GetTransactionCountAsync(userId, ct);

        var dtos = items.Select(t => new WalletTransactionDto
        {
            Id = t.Id,
            WalletId = t.WalletId,
            UserId = t.UserId,
            TransactionType = t.TransactionType,
            BalanceType = t.BalanceType,
            Amount = t.Amount,
            BalanceBefore = t.BalanceBefore,
            BalanceAfter = t.BalanceAfter,
            ReferenceType = t.ReferenceType,
            ReferenceId = t.ReferenceId,
            Description = t.Description,
            IdempotencyKey = t.IdempotencyKey,
            CreatedAt = t.CreatedAt
        }).ToList();

        return Result<PagedResult<WalletTransactionDto>>.Success(
            PagedResult<WalletTransactionDto>.Create(dtos, page, pageSize, totalCount));
    }

    public async Task<Result<IReadOnlyList<RechargePackageDto>>> GetRechargePackagesAsync(CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT * FROM dbo.RechargePackages 
            WHERE IsActive = 1 
            ORDER BY DisplayOrder ASC, Amount ASC";

        var packages = await conn.QueryAsync<dynamic>(sql);
        var dtos = packages.Select(p => new RechargePackageDto
        {
            Id = p.Id,
            Name = p.Name,
            Amount = p.Amount,
            BonusPercentage = p.BonusPercentage,
            BonusAmount = p.BonusAmount,
            IsPopular = p.IsPopular,
            DisplayOrder = p.DisplayOrder,
            IsActive = p.IsActive
        }).ToList();

        return Result<IReadOnlyList<RechargePackageDto>>.Success(dtos);
    }

    public async Task<Result<CreateRechargeOrderResponseDto>> CreateRechargeOrderAsync(
        long userId, CreateRechargeOrderRequestDto request, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);
        const string pkgSql = "SELECT * FROM dbo.RechargePackages WHERE Id = @Id AND IsActive = 1";
        var package = await conn.QuerySingleOrDefaultAsync<dynamic>(pkgSql, new { Id = request.PackageId });

        if (package == null)
        {
            return Result<CreateRechargeOrderResponseDto>.Failure("Invalid recharge package selected.", Result.ErrorCodes.NotFound);
        }

        decimal amount = (decimal)package.Amount;
        var paymentPublicId = "pay_" + Guid.NewGuid().ToString("N")[..16];
        var orderId = "order_" + Guid.NewGuid().ToString("N")[..16];
        var idempotencyKey = $"recharge_{userId}_{request.PackageId}_{Guid.NewGuid():N}";

        const string insertSql = @"
            INSERT INTO dbo.Payments (
                PublicId, UserId, PackageId, Amount, Currency, PaymentGateway,
                GatewayOrderId, Status, IdempotencyKey, CreatedAt
            )
            VALUES (
                @PublicId, @UserId, @PackageId, @Amount, 'INR', @PaymentGateway,
                @GatewayOrderId, 'INITIATED', @IdempotencyKey, GETUTCDATE()
            )";

        await conn.ExecuteAsync(insertSql, new
        {
            PublicId = paymentPublicId,
            UserId = userId,
            PackageId = request.PackageId,
            Amount = amount,
            PaymentGateway = request.PaymentGateway,
            GatewayOrderId = orderId,
            IdempotencyKey = idempotencyKey
        });

        var razorpayKeyId = _configuration["Razorpay:KeyId"] ?? "rzp_test_placeholder";

        var response = new CreateRechargeOrderResponseDto
        {
            PaymentPublicId = paymentPublicId,
            GatewayOrderId = orderId,
            Amount = amount,
            Currency = "INR",
            PaymentGateway = request.PaymentGateway,
            GatewayKey = razorpayKeyId
        };

        return Result<CreateRechargeOrderResponseDto>.Success(response);
    }

    public async Task<Result<VerifyPaymentResponseDto>> VerifyPaymentAsync(
        long userId, VerifyPaymentRequestDto request, CancellationToken ct = default)
    {
        using var conn = await _db.CreateOpenConnectionAsync(ct);

        // Fetch payment record
        const string paySql = @"
            SELECT p.*, pkg.BonusPercentage, pkg.BonusAmount
            FROM dbo.Payments p
            LEFT JOIN dbo.RechargePackages pkg ON pkg.Id = p.PackageId
            WHERE p.PublicId = @PublicId AND p.UserId = @UserId";

        var payment = await conn.QuerySingleOrDefaultAsync<dynamic>(paySql, new
        {
            PublicId = request.PaymentPublicId,
            UserId = userId
        });

        if (payment == null)
        {
            return Result<VerifyPaymentResponseDto>.Failure("Payment record not found.", Result.ErrorCodes.NotFound);
        }

        if ((string)payment.Status == "SUCCESS")
        {
            var currentWallet = await _walletRepo.GetByUserIdAsync(userId, ct);
            return Result<VerifyPaymentResponseDto>.Success(new VerifyPaymentResponseDto
            {
                IsSuccess = true,
                Message = "Payment already processed and wallet credited.",
                NewTotalBalance = currentWallet?.TotalBalance ?? 0m,
                CreditedAmount = (decimal)payment.Amount,
                BonusCredited = payment.BonusAmount ?? 0m
            });
        }

        // Verify Razorpay signature if configured
        var razorpaySecret = _configuration["Razorpay:KeySecret"];
        if (!string.IsNullOrEmpty(razorpaySecret) && razorpaySecret != "REPLACE_WITH_RAZORPAY_SECRET" && !string.IsNullOrEmpty(request.GatewaySignature))
        {
            var payload = $"{request.GatewayOrderId}|{request.GatewayPaymentId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(razorpaySecret));
            var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLower();
            if (!string.Equals(hash, request.GatewaySignature, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid Razorpay payment signature for payment {PaymentId}", request.PaymentPublicId);
                return Result<VerifyPaymentResponseDto>.Failure("Payment signature verification failed.", Result.ErrorCodes.PaymentFailed);
            }
        }

        decimal amount = (decimal)payment.Amount;
        decimal bonusAmount = payment.BonusAmount != null ? (decimal)payment.BonusAmount : 0.00m;
        string idempotencyKey = (string)payment.IdempotencyKey;

        // Atomically Credit Paid Balance
        var creditResult = await _walletRepo.CreditAsync(
            userId,
            amount,
            "PAID",
            "RECHARGE",
            (long)payment.Id,
            $"Wallet Recharge: ₹{amount:F2}",
            idempotencyKey,
            ct);

        if (!creditResult.IsSuccess)
        {
            return Result<VerifyPaymentResponseDto>.Failure(creditResult.Error!, creditResult.ErrorCode);
        }

        // Credit Bonus Balance if applicable
        if (bonusAmount > 0.00m)
        {
            var bonusKey = idempotencyKey + "_bonus";
            await _walletRepo.CreditAsync(
                userId,
                bonusAmount,
                "BONUS",
                "RECHARGE",
                (long)payment.Id,
                $"Recharge Bonus: ₹{bonusAmount:F2}",
                bonusKey,
                ct);
        }

        // Mark payment as SUCCESS
        const string updatePaymentSql = @"
            UPDATE dbo.Payments
            SET Status = 'SUCCESS',
                GatewayPaymentId = @PaymentId,
                GatewaySignature = @Signature,
                PaidAt = GETUTCDATE(),
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id";

        await conn.ExecuteAsync(updatePaymentSql, new
        {
            Id = (long)payment.Id,
            PaymentId = request.GatewayPaymentId,
            Signature = request.GatewaySignature
        });

        var updatedWallet = await _walletRepo.GetByUserIdAsync(userId, ct);

        return Result<VerifyPaymentResponseDto>.Success(new VerifyPaymentResponseDto
        {
            IsSuccess = true,
            Message = "Payment verified successfully. Wallet has been credited.",
            NewTotalBalance = updatedWallet?.TotalBalance ?? (amount + bonusAmount),
            CreditedAmount = amount,
            BonusCredited = bonusAmount
        });
    }
}
