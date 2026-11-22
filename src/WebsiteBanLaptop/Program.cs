using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebsiteBanLaptop.Data;
using WebsiteBanLaptop.Hubs;
using WebsiteBanLaptop.Models;
using WebsiteBanLaptop.Services;

var culture = CultureInfo.GetCultureInfo("vi-VN");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var resetAdminRequested = args.Any(a =>
    string.Equals(a, "--reset-admin-password", StringComparison.OrdinalIgnoreCase));
var webArgs = args.Where(a =>
    !string.Equals(a, "--reset-admin-password", StringComparison.OrdinalIgnoreCase)).ToArray();
var builder = WebApplication.CreateBuilder(webArgs);
if (resetAdminRequested) builder.Logging.ClearProviders();

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CategoryMenuService>();
builder.Services.AddSignalR();
builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection(PaymentOptions.SectionName));
builder.Services.AddScoped<VnPayGateway>();
builder.Services.AddHttpClient<MoMoGateway>();
builder.Services.AddScoped<PaymentReconciliationService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, StoreClaimsPrincipalFactory>();
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Denied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Admin"))
        {
            var requested = context.Request.Path.ToString() + context.Request.QueryString;
            context.Response.Redirect("/Admin/Login?returnUrl=" + Uri.EscapeDataString(requested));
        }
        else context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var created = await DatabaseBootstrapper.PrepareAsync(db, builder.Configuration, app.Environment);
    var expectedDatabase = builder.Configuration["Database:Name"];

    var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    if (resetAdminRequested)
    {
        if (!app.Environment.IsDevelopment())
            throw new InvalidOperationException("Chi tao lai mat khau Admin o Development.");
        await AdminAccountSetup.ResetPasswordAsync(db, users, roles, expectedDatabase);
        return;
    }
    await InitialData.EnsureRolesAsync(roles);
    if (created)
        await CatalogData.RestoreIfEmptyAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapHub<AdminOrdersHub>("/adminHub");
app.Run();
