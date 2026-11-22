using System.Security.Claims;
using System.Text;
using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using WebsiteBanLaptop.Hubs;
using WebsiteBanLaptop.Services;
using WebsiteBanLaptop.Data;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly UserManager<User> _users;
    private readonly SignInManager<User> _signIn;
    private readonly ILogger<AdminController> _logger;
    private readonly CategoryMenuService _categoryMenu;
    private readonly IHubContext<AdminOrdersHub> _adminHub;
    public AdminController(AppDbContext db, IWebHostEnvironment env, UserManager<User> users, SignInManager<User> signIn,
        ILogger<AdminController> logger, CategoryMenuService categoryMenu,
        IHubContext<AdminOrdersHub> adminHub)
    {
        _db = db; _env = env; _users = users; _signIn = signIn; _logger = logger;
        _categoryMenu = categoryMenu;
        _adminHub = adminHub;
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.IsInRole("Admin")) return RedirectToAction(nameof(Index));
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim().ToLowerInvariant();
        var admin = await _users.FindByEmailAsync(email);
        if (admin == null || !await _users.IsInRoleAsync(admin, "Admin"))
        {
            ModelState.AddModelError("", "Tài khoản quản trị hoặc mật khẩu không đúng");
            return View(model);
        }
        var result = await _signIn.PasswordSignInAsync(admin, model.Password,
            isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", result.IsLockedOut
                ? "Tài khoản tạm khóa vì đăng nhập sai nhiều lần."
                : "Tài khoản quản trị hoặc mật khẩu không đúng");
            return View(model);
        }
        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult ChangePassword() => View(new AdminChangePasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ChangePassword(AdminChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var admin = await _users.GetUserAsync(User);
        if (admin == null || !await _users.IsInRoleAsync(admin, "Admin")) return Forbid();

        if (string.Equals(model.CurrentPassword, model.NewPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.NewPassword), "Mật khẩu mới phải khác mật khẩu hiện tại");
            return View(model);
        }

        var result = await _users.ChangePasswordAsync(admin, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
                ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không đúng");
            else if (result.Errors.Any(e => e.Code.StartsWith("Password", StringComparison.Ordinal)))
                ModelState.AddModelError(nameof(model.NewPassword), "Mật khẩu mới chưa đáp ứng yêu cầu bảo mật");
            else
                ModelState.AddModelError("", "Không thể đổi mật khẩu. Vui lòng thử lại");
            return View(model);
        }

        await _signIn.RefreshSignInAsync(admin);
        TempData["Ok"] = "Đã đổi mật khẩu quản trị";
        return RedirectToAction(nameof(ChangePassword));
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalProducts = await _db.Products.CountAsync();
        ViewBag.TotalStock = await _db.Products.SumAsync(x => x.Stock);
        ViewBag.TotalUsers = await _db.Users.CountAsync();
        ViewBag.TotalOrders = await _db.Orders.CountAsync();
        ViewBag.TotalRevenue = await _db.Orders.Where(x => x.Status == "Đã giao" && x.PaymentStatus == "Paid")
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;
        return View(await _db.Orders.AsNoTracking().OrderByDescending(o => o.OrderDate).Take(6).ToListAsync());
    }

    public async Task<IActionResult> Products(string? search)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Name.Contains(search));
        ViewBag.Search = search;
        return View(await query.OrderBy(p => p.ProductID).ToListAsync());
    }

    public async Task<IActionResult> Categories()
    {
        var categories = await _db.Categories.Include(c => c.Products)
            .OrderBy(c => c.CategoryName).ToListAsync();
        return View(categories);
    }

    public IActionResult AddCategory() => View("CategoryForm", new Category());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(Category form)
    {
        if (!ModelState.IsValid) return View("CategoryForm", form);
        var name = form.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.CategoryName == name))
        {
            ModelState.AddModelError(nameof(form.CategoryName), "Hãng này đã có");
            return View("CategoryForm", form);
        }
        _db.Categories.Add(new Category { CategoryName = name });
        await _db.SaveChangesAsync();
        _categoryMenu.Invalidate();
        TempData["Ok"] = "Đã thêm danh mục";
        return RedirectToAction(nameof(Categories));
    }

    public async Task<IActionResult> EditCategory(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        return category == null ? NotFound() : View("CategoryForm", category);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(int id, Category form)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();
        if (!ModelState.IsValid) return View("CategoryForm", form);
        var name = form.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.CategoryID != id && c.CategoryName == name))
        {
            ModelState.AddModelError(nameof(form.CategoryName), "Hãng này đã có");
            return View("CategoryForm", form);
        }
        category.CategoryName = name;
        await _db.SaveChangesAsync();
        _categoryMenu.Invalidate();
        TempData["Ok"] = "Đã sửa danh mục";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return RedirectToAction(nameof(Categories));
        if (await _db.Products.AnyAsync(p => p.CategoryID == id))
        {
            TempData["Error"] = "Hãng đang có sản phẩm nên chưa thể xóa";
            return RedirectToAction(nameof(Categories));
        }
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        _categoryMenu.Invalidate();
        TempData["Ok"] = "Đã xóa danh mục";
        return RedirectToAction(nameof(Categories));
    }

    private async Task LoadCategories()
    {
        ViewBag.Categories = new SelectList(await _db.Categories.ToListAsync(), "CategoryID", "CategoryName");
    }
    public async Task<IActionResult> AddProduct()
    {
        await LoadCategories();
        return View("ProductForm", new Product { Stock = 1, Price = 1000000 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(Product product, IFormFile? photo)
    {
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError("", "Vui lòng kiểm tra các thông tin chưa hợp lệ.");
            await LoadCategories();
            return View("ProductForm", product);
        }
        if (!await _db.Categories.AnyAsync(c => c.CategoryID == product.CategoryID))
        {
            ModelState.AddModelError(nameof(Product.CategoryID), "Hãy chọn hãng laptop có trong danh sách");
            await LoadCategories();
            return View("ProductForm", product);
        }
        try { product.ImageUrl = await GetPhotoPath(photo) ?? CleanImageUrl(product.ImageUrl); }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadCategories();
            return View("ProductForm", product);
        }
        _db.Products.Add(product);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Không thể thêm sản phẩm {ProductName}", product.Name);
            _db.Entry(product).State = EntityState.Detached;
            ModelState.AddModelError("", "Không thể lưu sản phẩm. Vui lòng thử lại.");
            await LoadCategories();
            return View("ProductForm", product);
        }
        TempData["Ok"] = "Đã thêm sản phẩm";
        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> EditProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();
        await LoadCategories();
        return View("ProductForm", product);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(int id, Product form, IFormFile? photo)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();
        if (!ModelState.IsValid)
        {
            ModelState.AddModelError("", "Vui lòng kiểm tra các thông tin chưa hợp lệ.");
            await LoadCategories();
            return View("ProductForm", form);
        }
        if (!await _db.Categories.AnyAsync(c => c.CategoryID == form.CategoryID))
        {
            ModelState.AddModelError(nameof(Product.CategoryID), "Hãy chọn hãng laptop có trong danh sách");
            await LoadCategories();
            return View("ProductForm", form);
        }
        string? newImage;
        try { newImage = await GetPhotoPath(photo); }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadCategories();
            return View("ProductForm", form);
        }
        if (newImage != null) product.ImageUrl = newImage;
        else if (!string.IsNullOrWhiteSpace(form.ImageUrl) && form.ImageUrl != product.ImageUrl)
            product.ImageUrl = CleanImageUrl(form.ImageUrl);
        product.Name = form.Name;
        product.Price = form.Price;
        product.CategoryID = form.CategoryID;
        product.Description = form.Description;
        product.Stock = form.Stock;
        product.Cpu = form.Cpu;
        product.Ram = form.Ram;
        product.Storage = form.Storage;
        product.Graphics = form.Graphics;
        product.Screen = form.Screen;
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đã sửa sản phẩm";
        return RedirectToAction(nameof(Products));
    }

    private async Task<string?> GetPhotoPath(IFormFile? photo)
    {
        if (photo == null || photo.Length == 0) return null;
        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
        if (photo.Length > 3 * 1024 * 1024 ||
            !new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension))
            throw new InvalidOperationException("Chỉ được chọn ảnh JPG, PNG hoặc WEBP dưới 3MB");
        byte[] header = new byte[12];
        await using (var input = photo.OpenReadStream())
        {
            int count = await input.ReadAsync(header.AsMemory(0, 12));
            bool isJpeg = count >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            bool isPng = count >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            bool isWebp = count >= 12 && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
            bool validFile = extension switch
            {
                ".jpg" or ".jpeg" => isJpeg,
                ".png" => isPng,
                ".webp" => isWebp,
                _ => false
            };
            if (!validFile) throw new InvalidOperationException("Tệp được chọn không phải ảnh hợp lệ");
        }
        var folder = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(folder);
        var fileName = Guid.NewGuid().ToString("N") + extension;
        await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
        await photo.CopyToAsync(stream);
        return "/uploads/" + fileName;
    }

    private static string CleanImageUrl(string? image)
    {
        if (string.IsNullOrWhiteSpace(image)) return "/images/laptop-default.svg";
        image = image.Trim();
        if (image.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
            image.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return image;
        return "/images/laptop-default.svg";
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product != null)
        {
            if (await _db.OrderDetails.AnyAsync(d => d.ProductID == id))
                TempData["Error"] = "Sản phẩm đã có đơn hàng, không thể xóa";
            else
            {
                _db.Products.Remove(product);
                await _db.SaveChangesAsync();
                TempData["Ok"] = "Đã xóa sản phẩm";
            }
        }
        return RedirectToAction(nameof(Products));
    }

    public async Task<IActionResult> Users() => View(await _db.Users.OrderBy(u => u.Id).ToListAsync());

    public async Task<IActionResult> EditUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? NotFound() : View(user);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(int id, string fullName, string email,
        string? phone, string? address)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
            fullName.Length > 100 || email.Length > 150 ||
            (phone?.Length ?? 0) > 20 || (address?.Length ?? 0) > 300 ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email.Trim()))
        {
            ModelState.AddModelError("", "Thông tin thành viên chưa đúng");
            return View(user);
        }
        email = email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Id != id && x.Email == email))
        {
            ModelState.AddModelError("", "Email đã được sử dụng");
            return View(user);
        }
        user.FullName = fullName.Trim();
        user.Email = email;
        user.UserName = email;
        user.PhoneNumber = phone?.Trim();
        user.Address = address?.Trim();
        var updateResult = await _users.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            ModelState.AddModelError("", "Không thể cập nhật thành viên. Hãy kiểm tra lại email.");
            return View(user);
        }
        TempData["Ok"] = "Đã sửa thành viên";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        var myId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (user != null && await _users.IsInRoleAsync(user, "Customer") && id != myId &&
            !await _db.Orders.AnyAsync(o => o.UserID == id))
        {
            var result = await _users.DeleteAsync(user);
            TempData[result.Succeeded ? "Ok" : "Error"] = result.Succeeded
                ? "Đã xóa thành viên" : "Không thể xóa thành viên";
        }
        else TempData["Error"] = "Không thể xóa tài khoản này";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> News() => View(await _db.News.OrderByDescending(n => n.CreatedAt).ToListAsync());
    public IActionResult AddNews() => View("NewsForm", new News());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNews(News news)
    {
        if (!ModelState.IsValid) return View("NewsForm", news);
        news.CreatedAt = DateTime.Now;
        _db.News.Add(news);
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đã thêm tin tức";
        return RedirectToAction(nameof(News));
    }

    public async Task<IActionResult> EditNews(int id)
    {
        var news = await _db.News.FindAsync(id);
        return news == null ? NotFound() : View("NewsForm", news);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditNews(int id, News form)
    {
        var news = await _db.News.FindAsync(id);
        if (news == null) return NotFound();
        if (!ModelState.IsValid) return View("NewsForm", form);
        news.Title = form.Title;
        news.Content = form.Content;
        await _db.SaveChangesAsync();
        TempData["Ok"] = "Đã sửa tin tức";
        return RedirectToAction(nameof(News));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNews(int id)
    {
        var news = await _db.News.FindAsync(id);
        if (news != null)
        {
            _db.News.Remove(news);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(News));
    }

    public async Task<IActionResult> Tasks() => View(await _db.StaffTasks
        .OrderByDescending(x => x.CreatedAt).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTask(StaffTask task)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Bạn cần nhập tên công việc và người nhận";
            return RedirectToAction(nameof(Tasks));
        }
        task.CreatedAt = DateTime.Now;
        _db.StaffTasks.Add(task);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Tasks));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTask(int id)
    {
        var task = await _db.StaffTasks.FindAsync(id);
        if (task == null)
        {
            TempData["TaskError"] = "Không tìm thấy công việc để đổi trạng thái.";
        }
        else
        {
            task.IsDone = !task.IsDone;
            await _db.SaveChangesAsync();
            TempData["TaskOk"] = task.IsDone
                ? "Đã chuyển công việc sang trạng thái Đã xong."
                : "Đã chuyển công việc sang trạng thái Chưa xong.";
        }

        return Redirect(Url.Action(nameof(Tasks), "Admin") + "#danh-sach-cong-viec");
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _db.StaffTasks.FindAsync(id);
        if (task != null)
        {
            _db.StaffTasks.Remove(task);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Tasks));
    }

    public async Task<IActionResult> Orders() => View(await _db.Orders
        .Include(o => o.User).OrderByDescending(o => o.OrderDate).ToListAsync());

    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _db.Orders.Include(o => o.OrderDetails)
            .ThenInclude(d => d.Product).FirstOrDefaultAsync(o => o.OrderID == id);
        return order == null ? NotFound() : View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrder(int id, string status)
    {
        string[] allowed = { "Chờ xác nhận", "Đã xác nhận", "Đang giao", "Đã giao", "Đã hủy" };
        if (!allowed.Contains(status)) return BadRequest();
        var order = await _db.Orders.Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.OrderID == id);
        if (order == null) return NotFound();

        if (status == "Đã hủy" && order.PaymentStatus == "Paid" && order.PaymentMethod != "COD")
        {
            TempData["Error"] = "Đơn đã thanh toán trực tuyến. Cần hoàn tiền và đối soát tại cổng thanh toán trước khi hủy.";
            return RedirectToAction(nameof(Orders));
        }
        if (order.PaymentStatus == "Pending" || order.Status == "Chờ thanh toán")
        {
            TempData["Error"] = "Đơn đang chờ xác nhận từ cổng thanh toán.";
            return RedirectToAction(nameof(Orders));
        }
        if (status == order.Status) return RedirectToAction(nameof(Orders));
        bool valid = order.Status switch
        {
            "Chờ xác nhận" => status == "Đã xác nhận" || status == "Đã hủy",
            "Đã xác nhận" => status == "Đang giao" || status == "Đã hủy",
            "Đang giao" => status == "Đã giao" || status == "Đã hủy",
            _ => false
        };
        if (!valid)
        {
            TempData["Error"] = "Không thể chuyển sang trạng thái đã chọn";
            return RedirectToAction(nameof(Orders));
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var updated = await _db.Orders
            .Where(o => o.OrderID == id && o.Status == order.Status)
            .ExecuteUpdateAsync(x => x.SetProperty(o => o.Status, status));
        if (updated != 1)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Đơn hàng đã được cập nhật ở nơi khác, hãy tải lại trang";
            return RedirectToAction(nameof(Orders));
        }
        if (status == "Đã hủy")
        {
            foreach (var item in order.OrderDetails)
            {
                await _db.Products.Where(p => p.ProductID == item.ProductID)
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.Stock, p => p.Stock + item.Quantity));
            }
        }
        if (status == "Đã giao" && order.PaymentMethod == "COD")
        {
            await _db.Orders.Where(o => o.OrderID == id && o.PaymentStatus == "Unpaid")
                .ExecuteUpdateAsync(x => x.SetProperty(o => o.PaymentStatus, "Paid")
                    .SetProperty(o => o.PaymentUpdatedAtUtc, DateTime.UtcNow));
        }
        await transaction.CommitAsync();
        try
        {
            await _adminHub.Clients.All.SendAsync("OrderUpdated", new { id, status });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Khong gui duoc cap nhat don hang {OrderId}", id);
        }
        TempData["Ok"] = "Đã cập nhật đơn hàng";
        return RedirectToAction(nameof(Orders));
    }

    public async Task<IActionResult> Stock()
    {
        var data = await _db.Categories.Select(c => new StockRow
        {
            Name = c.CategoryName,
            Quantity = c.Products.Sum(p => (int?)p.Stock) ?? 0
        }).OrderBy(x => x.Name).ToListAsync();
        ViewBag.Max = Math.Max(1, data.Select(x => x.Quantity).DefaultIfEmpty(0).Max());
        return View(data);
    }

    public async Task<IActionResult> Revenue()
    {
        var data = await _db.Orders.Where(o => o.Status == "Đã giao" && o.PaymentStatus == "Paid")
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new MonthlyRevenueRow
            {
                Year = g.Key.Year, Month = g.Key.Month,
                OrderCount = g.Count(), Total = g.Sum(o => o.TotalAmount)
            }).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ToListAsync();

        var top = await _db.OrderDetails.Where(d => d.Order!.Status == "Đã giao" && d.Order.PaymentStatus == "Paid")
            .GroupBy(d => new { d.ProductID, d.Product!.Name })
            .Select(g => new BestSellerRow
            {
                Name = g.Key.Name,
                Quantity = g.Sum(d => d.Quantity)
            }).OrderByDescending(x => x.Quantity).Take(5).ToListAsync();

        var daily = await _db.Orders.Where(o => o.Status == "Đã giao" && o.PaymentStatus == "Paid")
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month, o.OrderDate.Day })
            .Select(g => new DailyRevenueRow
            {
                Year = g.Key.Year, Month = g.Key.Month, Day = g.Key.Day,
                OrderCount = g.Count(), Total = g.Sum(o => o.TotalAmount)
            }).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
              .ThenByDescending(x => x.Day).ToListAsync();
        ViewBag.DailyRows = daily;
        ViewBag.Total = data.Sum(x => x.Total);
        ViewBag.TopProducts = top;
        return View(data);
    }

    public async Task<IActionResult> ExportRevenueDaily()
    {
        var data = await _db.Orders.Where(o => o.Status == "Đã giao" && o.PaymentStatus == "Paid")
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month, o.OrderDate.Day })
            .Select(g => new DailyRevenueRow
            {
                Year = g.Key.Year, Month = g.Key.Month, Day = g.Key.Day,
                OrderCount = g.Count(), Total = g.Sum(o => o.TotalAmount)
            }).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
              .ThenByDescending(x => x.Day).ToListAsync();
        var csv = new StringBuilder("Ngay,So don,Doanh thu VND\r\n");
        foreach (var row in data)
            csv.AppendLine($"{row.Day:D2}/{row.Month:D2}/{row.Year},{row.OrderCount},{row.Total.ToString(CultureInfo.InvariantCulture)}");
        return File(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv", "doanh-thu-ngay.csv");
    }

    public async Task<IActionResult> ExportRevenue()
    {
        var data = await _db.Orders.Where(o => o.Status == "Đã giao" && o.PaymentStatus == "Paid")
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new MonthlyRevenueRow
            {
                Year = g.Key.Year, Month = g.Key.Month,
                OrderCount = g.Count(), Total = g.Sum(o => o.TotalAmount)
            }).OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ToListAsync();
        var csv = new StringBuilder();
        csv.AppendLine("Thang,So don,Doanh thu VND");
        foreach (var row in data)
            csv.AppendLine($"{row.Month:D2}/{row.Year},{row.OrderCount},{row.Total.ToString(CultureInfo.InvariantCulture)}");
        return File(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            "text/csv", "doanh-thu.csv");
    }

}

public class StockRow
{
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
}
public class MonthlyRevenueRow
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int OrderCount { get; set; }
    public decimal Total { get; set; }
}
public class BestSellerRow
{
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
}

public class DailyRevenueRow
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Day { get; set; }
    public int OrderCount { get; set; }
    public decimal Total { get; set; }
}
