using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Suppress Server Header (A05-2)
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});


builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddScoped<FtpHelper>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".MyApp.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // A02-2
});

// Rate Limiting (A04-1, A04-2)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login_limit", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });
});


builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict; // Improved from Lax
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // A02-2
        options.LoginPath = "/System/Verification";
        options.LogoutPath = "/System/Logout";
        options.AccessDeniedPath = "/System/AccessDenied";
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("sqlConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
// Security Headers Middleware (A05-1a, A05-1b, A05-1c, A08-1)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self'; frame-ancestors 'self';");
    
    // Anti-Cache
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    
    await next();
});

app.UseRouting();
app.UseRateLimiter(); // A04-1, A04-2

app.UseSession();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=System}/{action=Verification}/{id?}");

app.Run();