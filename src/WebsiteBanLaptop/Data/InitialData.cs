using Microsoft.AspNetCore.Identity;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Data;

public static class InitialData
{
    public static async Task EnsureRolesAsync(RoleManager<IdentityRole<int>> roles)
    {
        foreach (var name in new[] { "Admin", "Customer" })
        {
            if (await roles.RoleExistsAsync(name)) continue;
            var result = await roles.CreateAsync(new IdentityRole<int>(name));
            if (!result.Succeeded)
                throw new InvalidOperationException("Không thể khởi tạo phân quyền.");
        }
    }
}
