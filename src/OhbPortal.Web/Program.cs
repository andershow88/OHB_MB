using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.Interfaces;
using OhbPortal.Application.Services;
using OhbPortal.Infrastructure.Data;
using OhbPortal.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// ── Datenbank: Railway PostgreSQL via DATABASE_URL, sonst SQLite ─────────────
var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrWhiteSpace(dbUrl))
{
    var cs = ParseRailwayPostgresUrl(dbUrl);
    builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(cs));
}
else
{
    var path = Path.Combine(builder.Environment.ContentRootPath, "ohb.db");
    builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlite($"Data Source={path}"));
}

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IKapitelService, KapitelService>();
builder.Services.AddScoped<IDokumentService, DokumentService>();
builder.Services.AddScoped<IFreigabeService, FreigabeService>();
builder.Services.AddScoped<IKenntnisnahmeService, KenntnisnahmeService>();
builder.Services.AddScoped<IAnhangService, AnhangService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

builder.Services.AddHttpClient();

builder.Services.AddAuthentication("OhbAuth")
    .AddCookie("OhbAuth", opt =>
    {
        opt.LoginPath = "/Account/Login";
        opt.LogoutPath = "/Account/Logout";
        opt.AccessDeniedPath = "/Account/Login";
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.SlidingExpiration = true;
        opt.Cookie.Name = "OhbPortal.Auth";
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Dashboard/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try { await DataSeeder.SeedAsync(db); }
    catch (Exception ex) { log.LogError(ex, "DB-Seed fehlgeschlagen"); }
}

app.Run();

static string ParseRailwayPostgresUrl(string url)
{
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':', 2);
    var db = uri.AbsolutePath.TrimStart('/');
    return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Username={userInfo[0]};Password={userInfo.ElementAtOrDefault(1)};Database={db};SSL Mode=Require;Trust Server Certificate=true;";
}
