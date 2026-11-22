using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Data;

namespace WebsiteBanLaptop.Controllers;
public class NewsController : Controller
{
    private readonly AppDbContext _db;
    public NewsController(AppDbContext db) { _db = db; }
    public async Task<IActionResult> Index() => View(await _db.News
        .OrderByDescending(x => x.CreatedAt).ToListAsync());
    public async Task<IActionResult> Details(int id)
    {
        var news = await _db.News.FindAsync(id);
        return news == null ? NotFound() : View(news);
    }
}
