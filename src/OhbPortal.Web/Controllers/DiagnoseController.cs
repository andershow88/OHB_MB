using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.Interfaces;

namespace OhbPortal.Web.Controllers;

[AllowAnonymous]
[Route("[controller]")]
public class DiagnoseController : Controller
{
    private readonly IApplicationDbContext _db;

    public DiagnoseController(IApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== OHB Portal Diagnose ===");
        sb.AppendLine();

        try
        {
            var canConnect = await ((DbContext)_db).Database.CanConnectAsync();
            sb.AppendLine($"DB-Verbindung: {(canConnect ? "OK" : "FEHLGESCHLAGEN")}");
            sb.AppendLine($"DB-Provider: {((DbContext)_db).Database.ProviderName}");
            sb.AppendLine();

            if (!canConnect)
                return Content(sb.ToString(), "text/plain");

            var benutzer = await _db.Benutzer.CountAsync();
            var teams = await _db.Teams.CountAsync();
            var kapitel = await _db.Kapitel.CountAsync();
            var dokumente = await _db.Dokumente.CountAsync();
            var anhaenge = await _db.Anhaenge.CountAsync();
            var versionen = await _db.DokumentVersionen.CountAsync();

            sb.AppendLine("--- Tabellen-Zähler ---");
            sb.AppendLine($"Benutzer:    {benutzer}");
            sb.AppendLine($"Teams:       {teams}");
            sb.AppendLine($"Kapitel:     {kapitel}");
            sb.AppendLine($"Dokumente:   {dokumente}");
            sb.AppendLine($"Versionen:   {versionen}");
            sb.AppendLine($"Anhänge:     {anhaenge}");
            sb.AppendLine();

            if (dokumente > 0)
            {
                sb.AppendLine("--- Dokumente ---");
                var docs = await _db.Dokumente
                    .AsNoTracking()
                    .OrderBy(d => d.Id)
                    .Select(d => new { d.Id, d.Titel, d.Status, d.Archiviert, d.Geloescht, d.OeffentlichLesbar })
                    .ToListAsync();
                foreach (var d in docs)
                    sb.AppendLine($"  #{d.Id} | {d.Status} | Arch={d.Archiviert} Del={d.Geloescht} Pub={d.OeffentlichLesbar} | {d.Titel}");
                sb.AppendLine();
            }

            if (benutzer > 0)
            {
                sb.AppendLine("--- Benutzer ---");
                var users = await _db.Benutzer.AsNoTracking().OrderBy(b => b.Id)
                    .Select(b => new { b.Id, b.Benutzername, b.Rolle, b.IstAktiv })
                    .ToListAsync();
                foreach (var u in users)
                    sb.AppendLine($"  #{u.Id} | {u.Benutzername} | {u.Rolle} | Aktiv={u.IstAktiv}");
                sb.AppendLine();
            }

            var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            sb.AppendLine($"DATABASE_URL gesetzt: {!string.IsNullOrWhiteSpace(dbUrl)}");
            sb.AppendLine($"OPENAI_API_KEY gesetzt: {!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))}");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"FEHLER: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                sb.AppendLine($"  Inner: {ex.InnerException.Message}");
        }

        return Content(sb.ToString(), "text/plain");
    }
}
