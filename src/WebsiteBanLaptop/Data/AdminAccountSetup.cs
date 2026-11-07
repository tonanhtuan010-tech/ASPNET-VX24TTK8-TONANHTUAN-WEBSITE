using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Data;

// Chạy bằng lệnh --reset-admin-password, KHÔNG phải một URL.
public static class AdminAccountSetup
{
    public static async Task ResetPasswordAsync(AppDbContext db, IPasswordHasher<User> hasher)
    {
        const string database = "WebsiteBanLaptopStage01";
        var currentDatabase = db.Database.GetDbConnection().Database;
        if (!string.Equals(currentDatabase, database, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Chỉ được tạo lại Admin trên database {database}. Hiện tại: {currentDatabase}.");

        await db.Database.EnsureCreatedAsync();
        const string email = "admin@laptop.local";
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (admin != null && admin.Role != "Admin")
            throw new InvalidOperationException(
                "Email admin@laptop.local đang thuộc tài khoản khách hàng. " +
                "Không tự chuyển khách hàng thành Admin. Hãy kiểm tra tài khoản trước.");

        if (admin == null)
        {
            admin = new User
            {
                FullName = "Quản trị viên",
                Email = email,
                Role = "Admin"
            };
            db.Users.Add(admin);
        }

        var newPassword = GeneratePassword();
        admin.PasswordHash = hasher.HashPassword(admin, newPassword);
        await db.SaveChangesAsync();

        // Xoa thong tin bien dich, log va cac thong bao cu khoi cua so lenh.
        // Khong clear neu stdout dang duoc chuyen huong sang tep/pipe.
        if (!Console.IsOutputRedirected)
        {
            try { Console.Clear(); }
            catch (IOException) { /* Console khong ho tro thao tac xoa man hinh. */ }
        }

        Console.WriteLine("Ten dang nhap: " + email);
        Console.WriteLine("Mat khau: " + newPassword);
        Console.WriteLine("Link truy cap: https://localhost:7281/Admin/Login");
    }

    private static string GeneratePassword()
    {
        const string allowed = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var chars = new char[17];
        for (var i = 0; i < chars.Length - 1; i++)
            chars[i] = allowed[RandomNumberGenerator.GetInt32(allowed.Length)];
        chars[^1] = '!';
        return new string(chars);
    }
}
