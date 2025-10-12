using Microsoft.EntityFrameworkCore;
using DriveHub.Data;
using DriveHub.Models;
using Microsoft.AspNetCore.Identity;
using DriveHub.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add this line to register the email service
builder.Services.AddScoped<IEmailService, SmtpEmailService>();// Change from SqlServer to Sqlite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add SMS Service
builder.Services.AddScoped<ISmsService, SMSService>();

// Update Email Service to inject SMS service
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Add email service
// Replace the SendGrid registration with:
// Replace any existing email service registration with:
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Create database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // This will create the database and tables if they don't exist
        await context.Database.EnsureCreatedAsync();
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
// Add this before app.Run()
app.MapGet("/test-email", async (IConfiguration config) =>
{
    try
    {
        var apiKey = config["SendGrid:ApiKey"];
        return Results.Json(new
        {
            ApiKeyExists = !string.IsNullOrEmpty(apiKey),
            ApiKeyLength = apiKey?.Length ?? 0,
            Message = "Check if API key is loaded correctly"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { Error = ex.Message });
    }
});
app.Run();