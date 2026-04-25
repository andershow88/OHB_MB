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
builder.Services.AddScoped<IBerechtigungService, BerechtigungService>();
builder.Services.AddScoped<IAnhangService, AnhangService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

builder.Services.AddScoped<OhbPortal.Web.Services.SmartSearchService>();
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

// ── AUTO-LOGIN: Zum Wiederherstellen der Login-Pflicht diese Zeile auskommentieren ──
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated != true)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new("BenutzerId", "1"),
            new(System.Security.Claims.ClaimTypes.Name, "admin"),
            new(System.Security.Claims.ClaimTypes.GivenName, "Administrator"),
            new(System.Security.Claims.ClaimTypes.Role, "Admin")
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "OhbAuth");
        context.User = new System.Security.Claims.ClaimsPrincipal(identity);
    }
    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DataSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        log.LogWarning(ex, "Seed fehlgeschlagen – DB wird neu erstellt (Schema-Mismatch?)");
        try
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAsync(db);
            log.LogInformation("DB neu erstellt und erfolgreich geseeded");
        }
        catch (Exception ex2) { log.LogError(ex2, "DB-Seed auch nach Neuanlage fehlgeschlagen"); }
    }

    // Sicherstellen, dass die KiFeedback-Tabelle in bestehenden DBs existiert
    // (EnsureCreatedAsync legt nur beim ersten Anlegen Tabellen an, nicht bei Schema-Änderungen)
    try
    {
        var providerName = db.Database.ProviderName ?? string.Empty;
        var ddl = providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase)
            ? @"CREATE TABLE IF NOT EXISTS ""KiFeedbacks"" (
                    ""Id"" INTEGER PRIMARY KEY AUTOINCREMENT,
                    ""BenutzerId"" INTEGER NULL,
                    ""FrageInitial"" TEXT NOT NULL,
                    ""AntwortLetzte"" TEXT NOT NULL,
                    ""Bewertung"" INTEGER NOT NULL,
                    ""ZeitstempelUtc"" TEXT NOT NULL,
                    ""ModellName"" TEXT NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_KiFeedbacks_ZeitstempelUtc"" ON ""KiFeedbacks""(""ZeitstempelUtc"");
                CREATE INDEX IF NOT EXISTS ""IX_KiFeedbacks_Bewertung"" ON ""KiFeedbacks""(""Bewertung"");"
            : @"CREATE TABLE IF NOT EXISTS ""KiFeedbacks"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""BenutzerId"" INTEGER NULL,
                    ""FrageInitial"" TEXT NOT NULL,
                    ""AntwortLetzte"" TEXT NOT NULL,
                    ""Bewertung"" INTEGER NOT NULL,
                    ""ZeitstempelUtc"" TIMESTAMP NOT NULL,
                    ""ModellName"" VARCHAR(100) NULL
                );
                CREATE INDEX IF NOT EXISTS ""IX_KiFeedbacks_ZeitstempelUtc"" ON ""KiFeedbacks""(""ZeitstempelUtc"");
                CREATE INDEX IF NOT EXISTS ""IX_KiFeedbacks_Bewertung"" ON ""KiFeedbacks""(""Bewertung"");";
        await db.Database.ExecuteSqlRawAsync(ddl);
    }
    catch (Exception ex)
    {
        log.LogWarning(ex, "KiFeedbacks-Tabelle konnte nicht angelegt/geprüft werden");
    }
}

app.Run();

static string ParseRailwayPostgresUrl(string url)
{
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':', 2);
    var db = uri.AbsolutePath.TrimStart('/');
    return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Username={userInfo[0]};Password={userInfo.ElementAtOrDefault(1)};Database={db};SSL Mode=Require;Trust Server Certificate=true;";
}
