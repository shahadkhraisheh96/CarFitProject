using CarFitProject.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CarFitProject.Models; // <-- Added to reference CarFitDbContext

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 🔥 FOOLPROOF FIX: Automatically append TrustServerCertificate=True to the connection string
// This guarantees that BOTH Identity login and CarFit queries will bypass the SSL chain block.
if (!connectionString.Contains("TrustServerCertificate=", StringComparison.OrdinalIgnoreCase))
{
    if (!connectionString.EndsWith(";")) connectionString += ";";
    connectionString += "TrustServerCertificate=True;";
}

// 1. Configure the Identity Security Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configure the CarFit Application Core Context (Fixes the upcoming Controller DI crash)
builder.Services.AddDbContext<CarFitDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure Identity options to support custom roles explicitly
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddRoles<IdentityRole>() // Enables RoleManager support
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Add custom Seeding to create roles AND a default Admin account instantly on launch
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    // 1. Seed Roles securely
    string[] roleNames = { "Admin", "Seller", "Buyer" };
    foreach (var roleName in roleNames)
    {
        if (!roleManager.RoleExistsAsync(roleName).Result)
        {
            roleManager.CreateAsync(new IdentityRole(roleName)).Wait();
        }
    }

    // 2. Define the Default Administrator credentials
    string adminEmail = "admin@carfit.com";
    string adminPassword = "AdminPassword123!"; // Must pass Identity standard validations

    // Check if the admin user already exists
    var adminUser = userManager.FindByEmailAsync(adminEmail).Result;
    if (adminUser == null)
    {
        var newAdmin = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true // Bypasses email validation rules
        };

        // Create the user container with a hashed password parameter
        IdentityResult createAdminResult = userManager.CreateAsync(newAdmin, adminPassword).Result;

        if (createAdminResult.Succeeded)
        {
            // Assign to the Admin area traffic matrix role
            userManager.AddToRoleAsync(newAdmin, "Admin").Wait();
        }
    }
}

// Configure the HTTP request pipeline.
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

// Add the default routing maps to support custom Areas and fallback home page
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages()
   .WithStaticAssets();

app.Run();