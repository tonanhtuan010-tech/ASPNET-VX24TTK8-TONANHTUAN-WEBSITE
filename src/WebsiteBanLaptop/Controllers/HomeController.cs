using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Data;

namespace WebsiteBanLaptop.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var products = await _db.Products.AsNoTracking()
            .Include(x => x.Category)
            .OrderBy(x => x.ProductID).ToListAsync();
        return View(products);
    }

    public IActionResult Error() => View();
}
