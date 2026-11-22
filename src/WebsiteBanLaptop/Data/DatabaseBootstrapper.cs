using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Data;

public static class DatabaseBootstrapper
{
    public static async Task<bool> PrepareAsync(AppDbContext db, IConfiguration configuration,
        IHostEnvironment environment)
    {
        var actualDatabase = db.Database.GetDbConnection().Database;
        var expectedDatabase = configuration["Database:Name"];
        if (string.IsNullOrWhiteSpace(expectedDatabase) ||
            !string.Equals(actualDatabase, expectedDatabase, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Tên cơ sở dữ liệu không khớp với cấu hình ứng dụng.");

        bool created = false;
        if (environment.IsDevelopment())
        {
            try
            {
                created = await db.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Không thể chuẩn bị cơ sở dữ liệu. Hãy kiểm tra SQL Server LocalDB và chuỗi kết nối.", ex);
            }
        }
        else if (!await db.Database.CanConnectAsync())
        {
            throw new InvalidOperationException(
                "Chưa kết nối được cơ sở dữ liệu. Khi chạy trên máy chủ, cần tạo database trước.");
        }

        try
        {
            await db.Categories.AsNoTracking().Take(1).ToListAsync();
            await db.Products.AsNoTracking().Take(1).ToListAsync();
            await db.Users.AsNoTracking().Take(1).ToListAsync();
            await db.Roles.AsNoTracking().Take(1).ToListAsync();
            await db.Orders.AsNoTracking().Take(1).ToListAsync();
            await db.OrderDetails.AsNoTracking().Take(1).ToListAsync();
            await db.News.AsNoTracking().Take(1).ToListAsync();
            await db.StaffTasks.AsNoTracking().Take(1).ToListAsync();
            await db.ProductReviews.AsNoTracking().Take(1).ToListAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Database đã có nhưng cấu trúc bảng không khớp với mã nguồn. " +
                "Website không xóa hay tạo lại bảng, hãy kiểm tra lại hoặc dùng database khác.", ex);
        }

        return created;
    }
}
