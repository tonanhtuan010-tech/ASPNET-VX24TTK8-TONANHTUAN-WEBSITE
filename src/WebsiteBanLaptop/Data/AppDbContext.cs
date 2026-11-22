using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Models;

namespace WebsiteBanLaptop.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<News> News => Set<News>();
    public DbSet<StaffTask> StaffTasks => Set<StaffTask>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<User>().Property(x => x.Id).HasColumnName("UserID");
        modelBuilder.Entity<User>().Property(x => x.FullName).HasMaxLength(100).IsRequired();
        modelBuilder.Entity<User>().Property(x => x.Email).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<User>().Property(x => x.UserName).HasMaxLength(150).IsRequired();
        modelBuilder.Entity<User>().Property(x => x.Role).HasMaxLength(20).IsRequired();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();

        modelBuilder.Entity<Order>().HasIndex(x => new { x.Status, x.OrderDate });
        modelBuilder.Entity<Order>().HasIndex(x => x.PaymentReference)
            .IsUnique().HasFilter("[PaymentReference] IS NOT NULL");
        modelBuilder.Entity<News>().HasIndex(x => x.CreatedAt);
        modelBuilder.Entity<ProductReview>().HasIndex(x => new { x.UserID, x.ProductID }).IsUnique();
        modelBuilder.Entity<ProductReview>().HasOne(x => x.Product).WithMany(x => x.Reviews)
            .HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductReview>().HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserID).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Category>().HasIndex(x => x.CategoryName).IsUnique();
        modelBuilder.Entity<Product>().Property(x => x.Price).HasPrecision(18, 0);
        modelBuilder.Entity<Order>().Property(x => x.TotalAmount).HasPrecision(18, 0);
        modelBuilder.Entity<OrderDetail>().Property(x => x.UnitPrice).HasPrecision(18, 0);

        modelBuilder.Entity<Product>().HasOne(x => x.Category)
            .WithMany(x => x.Products).HasForeignKey(x => x.CategoryID)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>().HasOne(x => x.User)
            .WithMany(x => x.Orders).HasForeignKey(x => x.UserID)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrderDetail>().HasOne(x => x.Order)
            .WithMany(x => x.OrderDetails).HasForeignKey(x => x.OrderID)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrderDetail>().HasOne(x => x.Product)
            .WithMany(x => x.OrderDetails).HasForeignKey(x => x.ProductID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
