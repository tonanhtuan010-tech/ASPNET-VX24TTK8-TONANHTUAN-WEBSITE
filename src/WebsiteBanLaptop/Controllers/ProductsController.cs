using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Data;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Controllers;
public class ProductsController : Controller
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products.AsNoTracking().Include(p => p.Category)
            .Include(p => p.Reviews).FirstOrDefaultAsync(p => p.ProductID == id);
        if (product == null) return NotFound();
        var reviews = product.Reviews.OrderByDescending(r => r.CreatedAtUtc).ToList();
        ViewBag.AverageRating = reviews.Count == 0 ? (double?)null : reviews.Average(r => r.Rating);
        ViewBag.CanReview = false;
        if (User.Identity?.IsAuthenticated == true &&
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            var purchased = await _db.OrderDetails.AnyAsync(d => d.ProductID == id &&
                d.Order!.UserID == userId && d.Order.Status == "Đã giao" && d.Order.PaymentStatus == "Paid");
            ViewBag.CanReview = purchased && !reviews.Any(r => r.UserID == userId);
        }
        return View(product);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int productId, int rating, string? comment)
    {
        if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(comment) ||
            comment.Trim().Length is < 10 or > 800)
        {
            TempData["Error"] = "Đánh giá cần 1–5 sao và nội dung từ 10 đến 800 ký tự.";
            return RedirectToAction(nameof(Details), new { id = productId });
        }
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var purchased = await _db.OrderDetails.AnyAsync(d => d.ProductID == productId &&
            d.Order!.UserID == userId && d.Order.Status == "Đã giao" && d.Order.PaymentStatus == "Paid");
        if (!purchased) return Forbid();
        if (await _db.ProductReviews.AnyAsync(x => x.ProductID == productId && x.UserID == userId))
        {
            TempData["Error"] = "Bạn đã đánh giá sản phẩm này.";
            return RedirectToAction(nameof(Details), new { id = productId });
        }
        _db.ProductReviews.Add(new ProductReview
        {
            ProductID = productId, UserID = userId,
            Rating = rating, Comment = comment.Trim()
        });
        try
        {
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Đã ghi nhận đánh giá của bạn.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Không thể lưu đánh giá. Hãy kiểm tra lại.";
        }
        return RedirectToAction(nameof(Details), new { id = productId });
    }
}
