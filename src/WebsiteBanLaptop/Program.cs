using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Data;
using WebsiteBanLaptop.Models;

var culture = CultureInfo.GetCultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var resetAdmin = args.Any(a => string.Equals(a, "--reset-admin-password", StringComparison.OrdinalIgnoreCase));
var webArgs = args.Where(a => !string.Equals(a, "--reset-admin-password", StringComparison.OrdinalIgnoreCase)).ToArray();
var builder = WebApplication.CreateBuilder(webArgs);
if (resetAdmin) builder.Logging.ClearProviders();
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.AccessDeniedPath = "/Admin/Denied";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events.OnRedirectToLogin = context =>
        {
            var path = context.Request.Path.ToString() + context.Request.QueryString.ToString();
            if (context.Request.Path.StartsWithSegments("/Admin"))
                context.Response.Redirect("/Admin/Login?returnUrl=" + Uri.EscapeDataString(path));
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
if (resetAdmin)
{
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("Chi cho phep tao tai khoan Admin trong moi truong Development.");
        Environment.ExitCode = 1;
        return;
    }
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        await AdminAccountSetup.ResetPasswordAsync(db, hasher);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Khong the tao mat khau Admin: " + ex.Message);
        Environment.ExitCode = 1;
    }
    return;
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
