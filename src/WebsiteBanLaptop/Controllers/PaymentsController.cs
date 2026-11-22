using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebsiteBanLaptop.Data;
using WebsiteBanLaptop.Services;
using System.Security.Claims;

namespace WebsiteBanLaptop.Controllers;

public sealed class PaymentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly PaymentOptions _options;
    private readonly VnPayGateway _vnpay;
    private readonly MoMoGateway _momo;
    private readonly PaymentReconciliationService _reconcile;
    private readonly ILogger<PaymentsController> _logger;
    public PaymentsController(AppDbContext db, IOptions<PaymentOptions> options,
        VnPayGateway vnpay, MoMoGateway momo, PaymentReconciliationService reconcile,
        ILogger<PaymentsController> logger)
    {
        _db = db; _options = options.Value; _vnpay = vnpay; _momo = momo;
        _reconcile = reconcile; _logger = logger;
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Pay(int id, CancellationToken token)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderID == id && x.UserID == userId, token);
        if (order is null) return NotFound();
        if (order.PaymentStatus != "Pending" || order.Status != "Chờ thanh toán")
            return RedirectToAction("OrderDetails", "Account", new { id });
        return View(order);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PayNow(int id, CancellationToken token)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.OrderID == id && x.UserID == userId, token);
        if (order is null) return NotFound();
        if (order.PaymentStatus != "Pending" || order.Status != "Chờ thanh toán")
            return RedirectToAction("OrderDetails", "Account", new { id });
        if ((order.PaymentMethod == "VNPAY" && !_options.CanUseVnPay) ||
            (order.PaymentMethod == "MOMO" && !_options.CanUseMoMo))
        {
            TempData["Error"] = "Hình thức thanh toán hiện không khả dụng. Vui lòng liên hệ cửa hàng.";
            return RedirectToAction("OrderDetails", "Account", new { id });
        }
        if (order.PaymentExpiresAtUtc <= DateTime.UtcNow)
        {
            TempData["Error"] = "Phiên thanh toán đã hết hạn. Hãy liên hệ cửa hàng để kiểm tra đơn hàng.";
            return RedirectToAction("OrderDetails", "Account", new { id });
        }
        try
        {
            string url = order.PaymentMethod switch
            {
                "VNPAY" => _vnpay.CreateUrl(order, HttpContext.Connection.RemoteIpAddress?.ToString()),
                "MOMO" => await _momo.CreateUrlAsync(order, token),
                _ => throw new InvalidOperationException("Phương thức thanh toán không hợp lệ.")
            };
            return Redirect(url);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "Không thể tạo liên kết thanh toán cho đơn {OrderId}", id);
            TempData["Error"] = "Chưa thể kết nối cổng thanh toán. Đơn hàng đang chờ thanh toán; không đặt thêm đơn mới.";
            return RedirectToAction("OrderDetails", "Account", new { id });
        }
    }

    [Authorize, HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Result(string reference, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(reference)) return BadRequest();
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x =>
            x.PaymentReference == reference && x.UserID == userId, token);
        return order == null ? NotFound() : View(order);
    }

    [AllowAnonymous, HttpGet, IgnoreAntiforgeryToken]
    public async Task<IActionResult> VnPayIpn(CancellationToken token)
    {
        if (!_vnpay.TryVerify(Request.Query, out var proof) || proof == null)
            return VnPayReply("97", "Invalid signature");
        var result = await _reconcile.ApplyAsync(proof, token);
        return result switch
        {
            PaymentApplyResult.Applied => VnPayReply("00", "Confirm Success"),
            PaymentApplyResult.Duplicate => VnPayReply("02", "Already confirmed"),
            PaymentApplyResult.AwaitingCapture => VnPayReply("99", "Payment not finalized"),
            PaymentApplyResult.NotFound => VnPayReply("01", "Order not found"),
            PaymentApplyResult.WrongAmount => VnPayReply("04", "Invalid amount"),
            _ => VnPayReply("99", "Conflicting status")
        };
    }

    private static IActionResult VnPayReply(string code, string message) =>
        new JsonResult(new Dictionary<string,string> { ["RspCode"] = code, ["Message"] = message })
        {
            SerializerSettings = new JsonSerializerOptions { PropertyNamingPolicy = null, DictionaryKeyPolicy = null }
        };

    [AllowAnonymous, HttpPost, IgnoreAntiforgeryToken, RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> MoMoIpn(CancellationToken token)
    {
        try
        {
            using var body = await JsonDocument.ParseAsync(Request.Body, cancellationToken: token);
            if (!_momo.TryVerify(body.RootElement, out var proof) || proof == null)
                return Unauthorized();
            var result = await _reconcile.ApplyAsync(proof, token);
            return result is PaymentApplyResult.Applied or PaymentApplyResult.Duplicate or PaymentApplyResult.AwaitingCapture ? NoContent() : BadRequest();
        }
        catch (JsonException)
        {
            return BadRequest();
        }
    }
}
