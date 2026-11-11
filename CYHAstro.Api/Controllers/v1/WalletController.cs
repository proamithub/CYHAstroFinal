using System.Security.Claims;
using CYHAstro.Core.Common;
using CYHAstro.Core.DTOs.Wallet;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Api.Controllers.v1;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ILogger<WalletController> _logger;

    public WalletController(IWalletService walletService, ILogger<WalletController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    /// <summary>
    /// Get current authenticated user's active wallet balance (Paid, Bonus, and Total).
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(WalletBalanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _walletService.GetBalanceAsync(userId, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => NotFound(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Get paginated transaction history from the immutable wallet ledger.
    /// </summary>
    [HttpGet("transactions")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<WalletTransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _walletService.GetTransactionHistoryAsync(userId, page, pageSize, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// List all active recharge packages with available promotional bonus percentages.
    /// </summary>
    [HttpGet("packages")]
    [ProducesResponseType(typeof(IReadOnlyList<RechargePackageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPackages(CancellationToken ct)
    {
        var result = await _walletService.GetRechargePackagesAsync(ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Initiate a wallet recharge payment order with the payment gateway (Razorpay / Cashfree).
    /// </summary>
    [HttpPost("create-order")]
    [Authorize]
    [ProducesResponseType(typeof(CreateRechargeOrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateRechargeOrderRequestDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _walletService.CreateRechargeOrderAsync(userId, request, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    /// <summary>
    /// Verify payment gateway callback and atomically credit the user's wallet.
    /// Idempotency guarantees that multiple calls with the same order ID will never credit twice.
    /// </summary>
    [HttpPost("verify-payment")]
    [Authorize]
    [ProducesResponseType(typeof(VerifyPaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyPayment(
        [FromBody] VerifyPaymentRequestDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _walletService.VerifyPaymentAsync(userId, request, ct);
        return result.Match<IActionResult>(
            success => Ok(success),
            error => BadRequest(new { error, code = result.ErrorCode }));
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
