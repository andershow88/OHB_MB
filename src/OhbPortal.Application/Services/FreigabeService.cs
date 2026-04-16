using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.DTOs;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Entities;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Services;

public class FreigabeService : IFreigabeService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public FreigabeService(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task FreigabeStartenAsync(int dokumentId, int benutzerId)
    {
        var d = await _db.Dokumente.FindAsync(dokumentId) ?? throw new KeyNotFoundException();
        if (d.FreigabeModus == FreigabeModus.Keine)
            throw new InvalidOperationException("Dokument hat keinen Freigabe-Workflow konfiguriert.");
        d.Status = DokumentStatus.InFreigabe;
        d.GeaendertAm = DateTime.UtcNow;
        d.GeaendertVonId = benutzerId;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.FreigabeGestartet, benutzerId, dokumentId: dokumentId);
    }

    public async Task ZustimmenAsync(int freigabeGruppeId, int benutzerId, string? kommentar = null)
        => await EntscheidenAsync(freigabeGruppeId, benutzerId, FreigabeEntscheidung.Zugestimmt, kommentar);

    public async Task AblehnenAsync(int freigabeGruppeId, int benutzerId, string? kommentar = null)
        => await EntscheidenAsync(freigabeGruppeId, benutzerId, FreigabeEntscheidung.Abgelehnt, kommentar);

    private async Task EntscheidenAsync(int freigabeGruppeId, int benutzerId, FreigabeEntscheidung entscheidung, string? kommentar)
    {
        var gruppe = await _db.FreigabeGruppen
            .Include(g => g.Dokument)
            .Include(g => g.Mitglieder)
            .Include(g => g.Zustimmungen)
            .FirstOrDefaultAsync(g => g.Id == freigabeGruppeId) ?? throw new KeyNotFoundException();

        if (!gruppe.Mitglieder.Any(m => m.BenutzerId == benutzerId))
            throw new UnauthorizedAccessException("Benutzer ist kein Mitglied dieser Freigabegruppe.");

        var vorhanden = gruppe.Zustimmungen.FirstOrDefault(z => z.BenutzerId == benutzerId);
        if (vorhanden is null)
        {
            _db.FreigabeZustimmungen.Add(new FreigabeZustimmung
            {
                FreigabeGruppeId = freigabeGruppeId,
                BenutzerId = benutzerId,
                Entscheidung = entscheidung,
                EntschiedenAm = DateTime.UtcNow,
                Kommentar = kommentar
            });
        }
        else
        {
            vorhanden.Entscheidung = entscheidung;
            vorhanden.EntschiedenAm = DateTime.UtcNow;
            vorhanden.Kommentar = kommentar;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(entscheidung == FreigabeEntscheidung.Zugestimmt
                ? AuditTyp.FreigabeZugestimmt : AuditTyp.FreigabeAbgelehnt,
            benutzerId, dokumentId: gruppe.DokumentId, beschreibung: kommentar);

        await GesamtstatusPruefenAsync(gruppe.DokumentId, benutzerId);
    }

    private async Task GesamtstatusPruefenAsync(int dokumentId, int benutzerId)
    {
        var dok = await _db.Dokumente
            .Include(d => d.FreigabeGruppen).ThenInclude(g => g.Zustimmungen)
            .FirstOrDefaultAsync(d => d.Id == dokumentId);
        if (dok is null) return;

        bool irgendwoAbgelehnt = dok.FreigabeGruppen
            .SelectMany(g => g.Zustimmungen)
            .Any(z => z.Entscheidung == FreigabeEntscheidung.Abgelehnt);

        if (irgendwoAbgelehnt)
        {
            dok.Status = DokumentStatus.Abgelehnt;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(AuditTyp.FreigabeAbgeschlossen, benutzerId, dokumentId: dokumentId,
                beschreibung: "Ergebnis: Abgelehnt");
            return;
        }

        bool alleGruppenErfuellt = dok.FreigabeGruppen.All(g =>
            g.Zustimmungen.Count(z => z.Entscheidung == FreigabeEntscheidung.Zugestimmt)
                >= g.BenoetigteZustimmungen);

        if (alleGruppenErfuellt && dok.FreigabeGruppen.Any())
        {
            dok.Status = DokumentStatus.Freigegeben;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(AuditTyp.FreigabeAbgeschlossen, benutzerId, dokumentId: dokumentId,
                beschreibung: "Ergebnis: Freigegeben");
        }
    }

    public async Task<IEnumerable<OffeneFreigabeDto>> GetMeineOffenenAsync(int benutzerId)
    {
        return await _db.FreigabeGruppen
            .Include(g => g.Dokument)
            .Include(g => g.Mitglieder)
            .Include(g => g.Zustimmungen)
            .Where(g => g.Mitglieder.Any(m => m.BenutzerId == benutzerId)
                     && g.Dokument.Status == DokumentStatus.InFreigabe
                     && !g.Zustimmungen.Any(z => z.BenutzerId == benutzerId
                         && z.Entscheidung != FreigabeEntscheidung.Ausstehend))
            .Select(g => new OffeneFreigabeDto(
                g.Id, g.DokumentId, g.Dokument.Titel,
                g.Bezeichnung, g.BenoetigteZustimmungen,
                g.Zustimmungen.Count(z => z.Entscheidung == FreigabeEntscheidung.Zugestimmt),
                g.Dokument.GeaendertAm))
            .OrderByDescending(o => o.DokumentGeaendertAm)
            .ToListAsync();
    }

    public async Task<IEnumerable<FreigabeGruppeDto>> GetGruppenAsync(int dokumentId)
    {
        var gruppen = await _db.FreigabeGruppen
            .Include(g => g.Mitglieder).ThenInclude(m => m.Benutzer)
            .Include(g => g.Zustimmungen).ThenInclude(z => z.Benutzer)
            .Where(g => g.DokumentId == dokumentId)
            .OrderBy(g => g.Reihenfolge).ThenBy(g => g.Bezeichnung)
            .ToListAsync();

        return gruppen.Select(g =>
        {
            var zugestimmt = g.Zustimmungen.Count(z => z.Entscheidung == FreigabeEntscheidung.Zugestimmt);
            var abgelehnt = g.Zustimmungen.Any(z => z.Entscheidung == FreigabeEntscheidung.Abgelehnt);
            var gesamt = abgelehnt ? FreigabeEntscheidung.Abgelehnt
                : zugestimmt >= g.BenoetigteZustimmungen ? FreigabeEntscheidung.Zugestimmt
                : FreigabeEntscheidung.Ausstehend;

            return new FreigabeGruppeDto(
                g.Id, g.Bezeichnung, g.Reihenfolge, g.BenoetigteZustimmungen,
                g.Mitglieder.Select(m => m.Benutzer.Anzeigename).ToList(),
                g.Zustimmungen.Select(z => new FreigabeZustimmungDto(z.Id,
                    z.Benutzer.Anzeigename, z.Entscheidung, z.EntschiedenAm, z.Kommentar)).ToList(),
                gesamt);
        }).ToList();
    }
}
