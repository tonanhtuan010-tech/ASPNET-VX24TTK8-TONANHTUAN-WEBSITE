using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Data;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<User> _users;
    private readonly SignInManager<User> _signIn;

    public AccountController(AppDbContext db, UserManager<User> users, SignInManager<User> signIn)
    {
        _db = db;
        _users = users;
        _signIn = signIn;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim().ToLowerInvariant();
        if (await _users.FindByEmailAsync(email) != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được đăng ký");
            return View(model);
        }
        var user = new User
        {
            FullName = model.FullName.Trim(), UserName = email, Email = email,
            Role = "Customer"
        };
        var result = await _users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Không thể đăng ký. Vui lòng kiểm tra lại thông tin.");
            return View(model);
        }

        var roleResult = await _users.AddToRoleAsync(user, "Customer");
        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);
            ModelState.AddModelError("", "Không thể hoàn tất đăng ký, vui lòng thử lại.");
            return View(model);
        }
        return RedirectToAction(nameof(RegisterSuccess));
    }

    [HttpGet]
    public IActionResult RegisterSuccess() => View();

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.FindByEmailAsync(model.Email.Trim().ToLowerInvariant());
        if (user == null)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
            return View(model);
        }
        var result = await _signIn.PasswordSignInAsync(user, model.Password,
            isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.IsLockedOut
                ? "Tài khoản tạm khóa vì đăng nhập sai nhiều lần. Hãy thử lại sau."
                : "Email hoặc mật khẩu không đúng");
            return View(model);
        }
        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return View(await _db.Users.FindAsync(id));
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(string fullName, string? phone, string? address)
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100 ||
            (phone?.Length ?? 0) > 20 || (address?.Length ?? 0) > 300 ||
            (!string.IsNullOrWhiteSpace(phone) &&
             !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[0-9+ .-]{9,20}$")))
        {
            TempData["Error"] = "Thông tin tài khoản chưa hợp lệ";
            return RedirectToAction(nameof(Profile));
        }
        user.FullName = fullName.Trim();
        user.PhoneNumber = phone?.Trim();
        user.Address = address?.Trim();
        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = "Không thể lưu thông tin tài khoản";
            return RedirectToAction(nameof(Profile));
        }
        await _signIn.RefreshSignInAsync(user);
        TempData["Ok"] = "Đã lưu thông tin";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Orders()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return View(await _db.Orders.Where(o => o.UserID == id)
            .OrderByDescending(o => o.OrderDate).ToListAsync());
    }

    [Authorize, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.Include(o => o.OrderDetails)
            .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);
        return order == null ? NotFound() : View(order);
    }

    public IActionResult Denied() => View();
}
