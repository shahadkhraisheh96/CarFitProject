using CarFitProject.Data;
using CarFitProject.Models;
using CarFitProject.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Trust the dev SQL Server self-signed cert in Development only.
if (builder.Environment.IsDevelopment()
    && !connectionString.Contains("TrustServerCertificate=", StringComparison.OrdinalIgnoreCase))
{
    if (!connectionString.EndsWith(";")) connectionString += ";";
    connectionString += "TrustServerCertificate=True;";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<CarFitDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 10;
    options.Password.RequiredUniqueChars = 4;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Swap Identity's default PBKDF2 hasher for BCrypt cost 12.
builder.Services.AddScoped<IPasswordHasher<IdentityUser>, BCryptPasswordHasher<IdentityUser>>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddScoped<IRecommendationService, RecommendationService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

await SeedIdentityAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static async Task SeedIdentityAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var config = services.GetRequiredService<IConfiguration>();
    var env = services.GetRequiredService<IHostEnvironment>();

    string[] roleNames = { "Admin", "Dealer", "Buyer" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var adminEmail = config["AdminSeed:Email"];
    var adminPassword = config["AdminSeed:Password"];

    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        if (!env.IsDevelopment())
        {
            throw new InvalidOperationException(
                "AdminSeed:Email and AdminSeed:Password must be configured (user secrets or environment variables) in non-Development environments.");
        }
        return;
    }

    if (await userManager.FindByEmailAsync(adminEmail) != null)
    {
        return;
    }

    var newAdmin = new IdentityUser
    {
        UserName = adminEmail,
        Email = adminEmail,
        EmailConfirmed = true
    };

    var createResult = await userManager.CreateAsync(newAdmin, adminPassword);
    if (createResult.Succeeded)
    {
        await userManager.AddToRoleAsync(newAdmin, "Admin");
    }
    else
    {
        var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
        throw new InvalidOperationException($"Failed to seed admin user: {errors}");
    }
}
