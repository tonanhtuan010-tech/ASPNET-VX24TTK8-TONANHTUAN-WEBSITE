using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebsiteBanLaptop.Data;

namespace WebsiteBanLaptop.Services;

public sealed record CategoryMenuItem(int CategoryID, string CategoryName);

public sealed class CategoryMenuService
{
    private const string Key = "categories:public-menu";
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public CategoryMenuService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyList<CategoryMenuItem>> GetAsync()
    {
        return await _cache.GetOrCreateAsync(Key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return (IReadOnlyList<CategoryMenuItem>)await _db.Categories.AsNoTracking()
                .OrderBy(c => c.CategoryName)
                .Select(c => new CategoryMenuItem(c.CategoryID, c.CategoryName))
                .ToListAsync();
        }) ?? Array.Empty<CategoryMenuItem>();
    }

    public void Invalidate() => _cache.Remove(Key);
}
