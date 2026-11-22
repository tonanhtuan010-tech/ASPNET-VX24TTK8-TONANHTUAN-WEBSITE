using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WebsiteBanLaptop.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connection = config.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Thiếu cấu hình SQL Server.");
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection);
        var expected = config["Database:Name"];
        if (string.IsNullOrWhiteSpace(expected) ||
            !string.Equals(builder.InitialCatalog, expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Tên database trong chuỗi kết nối không khớp với mục Database:Name.");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection).Options;
        return new AppDbContext(options);
    }
}
