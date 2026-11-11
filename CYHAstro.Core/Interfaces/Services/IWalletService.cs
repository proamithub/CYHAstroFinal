using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Wallet;

namespace CYHAstro.Core.Interfaces.Services;

public interface IWalletService
{
    Task<Result<WalletBalanceDto>> GetBalanceAsync(long userId, CancellationToken ct = default);

    Task<Result<PagedResult<WalletTransactionDto>>> GetTransactionHistoryAsync(
        long userId, int page, int pageSize, CancellationToken ct = default);

    Task<Result<IReadOnlyList<RechargePackageDto>>> GetRechargePackagesAsync(CancellationToken ct = default);

    Task<Result<CreateRechargeOrderResponseDto>> CreateRechargeOrderAsync(
        long userId, CreateRechargeOrderRequestDto request, CancellationToken ct = default);

    Task<Result<VerifyPaymentResponseDto>> VerifyPaymentAsync(
        long userId, VerifyPaymentRequestDto request, CancellationToken ct = default);
}
