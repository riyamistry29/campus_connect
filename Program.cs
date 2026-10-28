using CampusConnect.Data;
using CampusConnect.Models;
using CampusConnect.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// =========================================================================
// CampusConnect - College Event Management System
// Entry point: Program.cs
// This file configures the web application, database, security, and routes.
// =========================================================================

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------------------
// 1. DATABASE CONFIGURATION (PostgreSQL + Entity Framework Core)
// We read the connection string from appsettings.json and register DbContext.
// -------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// -------------------------------------------------------------------------
// 2. AUTHENTICATION & IDENTITY (ASP.NET Core Identity)
// Handles user accounts, passwords (PBKDF2 hashing), and roles (Student, Organizer, Admin).
// -------------------------------------------------------------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password rules (beginner friendly & secure)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;

    // Email rules
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// -------------------------------------------------------------------------
// 3. COOKIE SETTINGS
// Controls login redirects and session expiration.
// -------------------------------------------------------------------------
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.LoginPath = "/Account/Login";               // Redirect here if not logged in
    options.AccessDeniedPath = "/Account/AccessDenied"; // Redirect here if role is not allowed
    options.SlidingExpiration = true;
});

// -------------------------------------------------------------------------
// 4. DEPENDENCY INJECTION (Services)
// Register business logic services for Events and Dashboards.
// -------------------------------------------------------------------------
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// -------------------------------------------------------------------------
// 5. MVC CONTROLLERS & VIEWS
// Enables ASP.NET Core Model-View-Controller framework.
// -------------------------------------------------------------------------
builder.Services.AddControllersWithViews();

var app = builder.Build();

// -------------------------------------------------------------------------
// 6. DATABASE SEEDING
// Automatically creates tables and inserts sample categories, events, and demo users.
// -------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Checking database and applying seed data...");
        await DbInitializer.SeedAsync(services);
        logger.LogInformation("Database is ready!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while seeding the database.");
    }
}

// -------------------------------------------------------------------------
// 7. HTTP REQUEST PIPELINE (Middleware)
// -------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Serves css, js, images from wwwroot

app.UseRouting();

// Authentication identifies WHO the user is
app.UseAuthentication();

// Authorization checks WHAT the user is allowed to do
app.UseAuthorization();

// Default route pattern: /Controller/Action/Id
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n=======================================================");
Console.WriteLine("🚀 CampusConnect is LIVE!");
Console.WriteLine("👉 Open your browser at: http://localhost:5000");
Console.WriteLine("=======================================================\n");
Console.ResetColor();

app.Run();

