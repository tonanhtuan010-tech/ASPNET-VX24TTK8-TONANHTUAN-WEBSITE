using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebsiteBanLaptop.Services;
using WebsiteBanLaptop.Hubs;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Data;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Controllers;
public class CartController : Controller
{
    private readonly AppDbContext _db;
    private readonly IHubContext<AdminOrdersHub> _adminHub;
    private readonly ILogger<CartController> _logger;
    private readonly PaymentOptions _payments;
    public CartController(AppDbContext db, IHubContext<AdminOrdersHub> adminHub,
        ILogger<CartController> logger, IOptions<PaymentOptions> payments)
    {
        _db = db;
        _adminHub = adminHub;
        _logger = logger;
        _payments = payments.Value;
    }

    private async Task<List<CartLine>> GetLines()
    {
        var cart = CartStore.Get(HttpContext.Session);
        var ids = cart.Select(c => c.ProductID).ToList();
        var products = await _db.Products.Where(x => ids.Contains(x.ProductID)).ToListAsync();
        return cart.Join(products, c => c.ProductID, p => p.ProductID,
            (c, p) => new CartLine { Product = p, Quantity = Math.Clamp(c.Quantity, 1, 10) })
            .ToList();
    }
    public async Task<IActionResult> Index() => View(await GetLines());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return NotFound();
        if (product.Stock < 1) { TempData["Error"] = "Sản phẩm đã hết hàng"; return RedirectToAction("Details", "Products", new { id = productId }); }
        var cart = CartStore.Get(HttpContext.Session);
        var item = cart.FirstOrDefault(c => c.ProductID == productId);
        quantity = Math.Clamp(quantity, 1, 10);
        if (item == null) cart.Add(new CartItem { ProductID = productId, Quantity = Math.Min(product.Stock, quantity) });
        else item.Quantity = Math.Min(Math.Min(product.Stock, 10), item.Quantity + quantity);
        CartStore.Save(HttpContext.Session, cart);
        TempData["Ok"] = "Đã thêm sản phẩm vào giỏ hàng";
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        var cart = CartStore.Get(HttpContext.Session);
        var item = cart.FirstOrDefault(c => c.ProductID == productId);
        if (item != null)
        {
            var stock = await _db.Products.Where(p => p.ProductID == productId)
                .Select(p => p.Stock).FirstOrDefaultAsync();
            if (quantity <= 0 || stock <= 0) cart.Remove(item);
            else item.Quantity = Math.Min(Math.Min(quantity, 10), stock);
            CartStore.Save(HttpContext.Session, cart);
        }
        if (IsAjaxRequest()) return await CartPartialAsync();
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        var cart = CartStore.Get(HttpContext.Session);
        cart.RemoveAll(c => c.ProductID == productId);
        CartStore.Save(HttpContext.Session, cart);
        if (IsAjaxRequest()) return await CartPartialAsync();
        return RedirectToAction("Index");
    }

    private bool IsAjaxRequest() => string.Equals(
        Request.Headers["X-Requested-With"].ToString(), "XMLHttpRequest",
        StringComparison.OrdinalIgnoreCase);

    private async Task<IActionResult> CartPartialAsync()
    {
        var lines = await GetLines();
        Response.Headers["X-Cart-Count"] = lines.Sum(x => x.Quantity).ToString();
        return PartialView("_CartBody", lines);
    }

    [Authorize]
    public async Task<IActionResult> Checkout()
    {
        if (!(await GetLines()).Any()) return RedirectToAction("Index");
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(id);
        ViewBag.Lines = await GetLines();
        return View(new CheckoutViewModel { CustomerName = user?.FullName ?? "",
            Phone = user?.PhoneNumber ?? "", Address = user?.Address ?? "" });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var lines = await GetLines();
        if (!lines.Any()) return RedirectToAction("Index");
        ViewBag.Lines = lines;
        if (!ModelState.IsValid) return View(model);
        if ((model.PaymentMethod == "VNPAY" && !_payments.CanUseVnPay) ||
            (model.PaymentMethod == "MOMO" && !_payments.CanUseMoMo))
        {
            ModelState.AddModelError(nameof(model.PaymentMethod),
                "Hình thức thanh toán này hiện không khả dụng. Vui lòng chọn hình thức khác.");
            return View(model);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var order = new Order
        {
            UserID = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            CustomerName = model.CustomerName.Trim(), Phone = model.Phone.Trim(),
            Address = model.Address.Trim(), Note = model.Note?.Trim(),
            TotalAmount = lines.Sum(x => x.Amount),
            Status = model.PaymentMethod == "COD" ? "Chờ xác nhận" : "Chờ thanh toán",
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = model.PaymentMethod == "COD" ? "Unpaid" : "Pending",
            PaymentReference = model.PaymentMethod == "COD" ? null : Guid.NewGuid().ToString("N"),
            PaymentExpiresAtUtc = model.PaymentMethod == "COD" ? null : DateTime.UtcNow.AddMinutes(20)
        };
        foreach (var line in lines)
        {
            var valid = await _db.Products.Where(p => p.ProductID == line.Product.ProductID
                && p.Stock >= line.Quantity && p.Price == line.Product.Price)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - line.Quantity)) == 1;
            if (!valid)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Giá hoặc số lượng sản phẩm đã thay đổi. Bạn hãy quay lại giỏ hàng và kiểm tra.");
                return View(model);
            }
            order.OrderDetails.Add(new OrderDetail
            {
                ProductID = line.Product.ProductID,
                Quantity = line.Quantity, UnitPrice = line.Product.Price
            });
        }
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        CartStore.Save(HttpContext.Session, new List<CartItem>());
        try
        {
            await _adminHub.Clients.All.SendAsync("OrderCreated", new
            {
                id = order.OrderID,
                status = order.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong gui duoc thong bao don hang {OrderId}", order.OrderID);
        }
        if (model.PaymentMethod is "VNPAY" or "MOMO")
            return RedirectToAction("Pay", "Payments", new { id = order.OrderID });
        return RedirectToAction(nameof(Success), new { id = order.OrderID });
    }

    [Authorize]
    public async Task<IActionResult> Success(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);
        return order == null ? NotFound() : View(order);
    }
}
