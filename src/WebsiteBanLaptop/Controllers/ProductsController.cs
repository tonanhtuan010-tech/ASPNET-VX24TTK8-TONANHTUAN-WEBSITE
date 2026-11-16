using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Data;

namespace WebsiteBanLaptop.Controllers;
public class ProductsController : Controller
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) { _db = db; }
    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products.Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductID == id);
        return product == null ? NotFound() : View(product);
    }
}
