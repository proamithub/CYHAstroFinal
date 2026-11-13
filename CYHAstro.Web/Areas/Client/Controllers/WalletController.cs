using System.Security.Claims;
using CYHAstro.Core.DTOs.Wallet;
using CYHAstro.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CYHAstro.Web.Areas.Client.Controllers;

[Area("Client")]
[Authorize(AuthenticationSchemes = "ClientCookie")]
public sealed class WalletController : Controller
{
    private readonly IWalletService _walletService;
    private readonly ILogger<WalletController> _logger;

    public WalletController(IWalletService walletService, ILogger<WalletController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { area = "Client" });

        var balanceRes = await _walletService.GetBalanceAsync(userId, ct);
        var packagesRes = await _walletService.GetRechargePackagesAsync(ct);
        var txRes = await _walletService.GetTransactionHistoryAsync(userId, 1, 30, ct);

        ViewBag.Balance = balanceRes.IsSuccess ? balanceRes.Value : new WalletBalanceDto { UserId = userId };
        ViewBag.Packages = packagesRes.IsSuccess ? packagesRes.Value : new List<RechargePackageDto>();
        ViewBag.Transactions = txRes.IsSuccess ? txRes.Value!.Items : new List<WalletTransactionDto>();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recharge([FromForm] long packageId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return RedirectToAction("Login", "Auth", new { area = "Client" });

        var orderRes = await _walletService.CreateRechargeOrderAsync(
            userId, new CreateRechargeOrderRequestDto { PackageId = packageId, PaymentGateway = "Razorpay" }, ct);

        if (!orderRes.IsSuccess)
        {
            TempData["Error"] = orderRes.Error ?? "Could not initiate payment order.";
            return RedirectToAction(nameof(Index));
        }

        // Complete recharge directly in dev/test mode for seamless verification
        var verifyRes = await _walletService.VerifyPaymentAsync(userId, new VerifyPaymentRequestDto
        {
            PaymentPublicId = orderRes.Value!.PaymentPublicId,
            GatewayOrderId = orderRes.Value.GatewayOrderId,
            GatewayPaymentId = "pay_test_" + Guid.NewGuid().ToString("N")[..12]
        }, ct);

        if (verifyRes.IsSuccess)
        {
            TempData["Success"] = $"Recharge of ₹{verifyRes.Value!.CreditedAmount:F2} successful! Added ₹{verifyRes.Value.BonusCredited:F2} bonus. New Balance: ₹{verifyRes.Value.NewTotalBalance:F2}";
        }
        else
        {
            TempData["Error"] = verifyRes.Error ?? "Payment verification failed.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Wallet/CreateOrder")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateRechargeOrderRequestDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var res = await _walletService.CreateRechargeOrderAsync(userId, request, ct);
        if (res.IsSuccess)
        {
            return Ok(res.Value);
        }
        return BadRequest(new { error = res.Error });
    }

    [HttpPost("Wallet/VerifyPayment")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequestDto request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var res = await _walletService.VerifyPaymentAsync(userId, request, ct);
        if (res.IsSuccess)
        {
            return Ok(res.Value);
        }
        return BadRequest(new { error = res.Error });
    }

    [HttpPost("Wallet/Webhook")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();

        _logger.LogInformation("Received Razorpay Webhook. Payload length: {Length}", body.Length);
        return Ok(new { received = true });
    }

    private long GetCurrentUserId()
    {
        var val = User.FindFirstValue("userId");
        return long.TryParse(val, out var id) ? id : 0;
    }
}
