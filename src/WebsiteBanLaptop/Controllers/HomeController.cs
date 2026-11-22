using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Data;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) { _db = db; }

    private IQueryable<Product> Filter(string? search, int? categoryId, string? priceRange,
        string? usage, string? sort, string? cpu, string? ram)
    {
        var query = _db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search.Trim()));
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryID == categoryId.Value);

        if (priceRange == "under20") query = query.Where(p => p.Price < 20000000);
        if (priceRange == "20-25") query = query.Where(p => p.Price >= 20000000 && p.Price < 25000000);
        if (priceRange == "25-30") query = query.Where(p => p.Price >= 25000000 && p.Price < 30000000);
        if (priceRange == "30-40") query = query.Where(p => p.Price >= 30000000 && p.Price < 40000000);
        if (priceRange == "over40") query = query.Where(p => p.Price >= 40000000);

        if (cpu == "intel") query = query.Where(p => p.Cpu.Contains("Intel"));
        if (cpu == "amd") query = query.Where(p => p.Cpu.Contains("AMD"));
        if (cpu == "apple") query = query.Where(p => p.Cpu.Contains("Apple"));
        if (ram == "8") query = query.Where(p => p.Ram.StartsWith("8GB"));
        if (ram == "16") query = query.Where(p => p.Ram.StartsWith("16GB"));
        if (ram == "32") query = query.Where(p => p.Ram.StartsWith("32GB"));

        if (usage == "gaming")
            query = query.Where(p => p.Name.Contains("Gaming") || p.Name.Contains("LOQ") || p.Name.Contains("TUF") || p.Graphics.Contains("RTX"));
        if (usage == "office")
            query = query.Where(p => !p.Name.Contains("Gaming") && !p.Name.Contains("LOQ") && !p.Name.Contains("TUF") && !p.Graphics.Contains("RTX"));
        if (usage == "study")
            query = query.Where(p => p.Price < 25000000 && !p.Graphics.Contains("RTX"));

        return sort switch
        {
            "low" => query.OrderBy(p => p.Price),
            "high" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderBy(p => p.ProductID)
        };
    }

    private async Task LoadFilters(string? search, int? categoryId, string? priceRange,
        string? usage, string? sort, string? cpu, string? ram)
    {
        ViewBag.Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync();
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.PriceRange = priceRange;
        ViewBag.Usage = usage;
        ViewBag.Sort = sort;
        ViewBag.Cpu = cpu;
        ViewBag.Ram = ram;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId, string? priceRange,
        string? usage, string? sort, string? cpu, string? ram)
    {
        await LoadFilters(search, categoryId, priceRange, usage, sort, cpu, ram);
        return View(await Filter(search, categoryId, priceRange, usage, sort, cpu, ram).ToListAsync());
    }

    public async Task<IActionResult> Categories(string? search, int? categoryId, string? priceRange,
        string? usage, string? sort, string? cpu, string? ram)
    {
        await LoadFilters(search, categoryId, priceRange, usage, sort, cpu, ram);
        return View(await Filter(search, categoryId, priceRange, usage, sort, cpu, ram).ToListAsync());
    }

    public IActionResult Error() => View();
}
