using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Data;

public static class AdminAccountSetup
{
    public static async Task ResetPasswordAsync(AppDbContext db,
        UserManager<User> users, RoleManager<IdentityRole<int>> roles, string? expectedDatabase)
    {
        var currentDatabase = db.Database.GetDbConnection().Database;
        if (string.IsNullOrWhiteSpace(expectedDatabase) ||
            !string.Equals(currentDatabase, expectedDatabase, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ đặt lại mật khẩu Admin trên database của website.");

        const string email = "admin@laptop.local";
        if (!await roles.RoleExistsAsync("Admin"))
        {
            var created = await roles.CreateAsync(new IdentityRole<int>("Admin"));
            if (!created.Succeeded) throw new InvalidOperationException("Không thể tạo vai trò Admin.");
        }
        var admin = await users.FindByEmailAsync(email);
        if (admin != null && !await users.IsInRoleAsync(admin, "Admin"))
            throw new InvalidOperationException("Email admin@laptop.local đã được dùng cho một tài khoản khác.");

        var password = GeneratePassword();
        IdentityResult result;
        if (admin == null)
        {
            admin = new User { FullName = "Quản trị viên", Email = email,
                UserName = email, Role = "Admin" };
            result = await users.CreateAsync(admin, password);
            if (!result.Succeeded) throw new InvalidOperationException("Không thể tạo tài khoản Admin.");
            result = await users.AddToRoleAsync(admin, "Admin");
        }
        else
        {
            var token = await users.GeneratePasswordResetTokenAsync(admin);
            result = await users.ResetPasswordAsync(admin, token, password);
        }
        if (!result.Succeeded) throw new InvalidOperationException("Không thể cập nhật mật khẩu Admin.");
        result = await users.SetLockoutEndDateAsync(admin, null);
        if (!result.Succeeded) throw new InvalidOperationException("Không thể mở khóa Admin.");
        result = await users.ResetAccessFailedCountAsync(admin);
        if (!result.Succeeded) throw new InvalidOperationException("Không thể làm mới số lần đăng nhập sai.");

        if (!Console.IsOutputRedirected)
        {
            try { Console.Clear(); }
            catch (IOException) { }
        }
        Console.WriteLine("Ten dang nhap: " + email);
        Console.WriteLine("Mat khau: " + password);
        Console.WriteLine("Link truy cap: https://localhost:7296/Admin/Login");
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
