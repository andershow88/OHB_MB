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

        // Sequentielle Reihenfolge durchsetzen
        if (gruppe.Dokument.FreigabeReihenfolge == FreigabeReihenfolge.Sequentiell)
        {
            var vorherGruppen = await _db.FreigabeGruppen
                .Include(g => g.Zustimmungen)
                .Where(g => g.DokumentId == gruppe.DokumentId && g.Reihenfolge < gruppe.Reihenfolge)
                .ToListAsync();
            var nochOffen = vorherGruppen.FirstOrDefault(g =>
                g.Zustimmungen.Count(z => z.Entscheidung == FreigabeEntscheidung.Zugestimmt) < g.BenoetigteZustimmungen);
            if (nochOffen is not null)
                throw new InvalidOperationException(
                    $"Sequentieller Workflow: Gruppe '{nochOffen.Bezeichnung}' (Stufe {nochOffen.Reihenfolge}) muss zuerst abgeschlossen werden.");
        }

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
        var kandidaten = await _db.FreigabeGruppen
            .Where(g => g.Mitglieder.Any(m => m.BenutzerId == benutzerId)
                     && g.Dokument.Status == DokumentStatus.InFreigabe
                     && !g.Zustimmungen.Any(z => z.BenutzerId == benutzerId
                         && z.Entscheidung != FreigabeEntscheidung.Ausstehend))
            .OrderByDescending(g => g.Dokument.GeaendertAm)
            .Select(g => new
            {
                g.Id,
                g.DokumentId,
                DokumentTitel = g.Dokument.Titel,
                g.Bezeichnung,
                g.BenoetigteZustimmungen,
                g.Reihenfolge,
                Zugestimmt = g.Zustimmungen.Count(z => z.Entscheidung == FreigabeEntscheidung.Zugestimmt),
                DokumentGeaendertAm = g.Dokument.GeaendertAm,
                DokumentReihenfolgeModus = g.Dokument.FreigabeReihenfolge
            })
            .ToListAsync();

        // Sequentielle: nur aktuelle aktive Stufe pro Dokument anzeigen
        var dokumenteSeq = kandidaten
            .Where(k => k.DokumentReihenfolgeModus == FreigabeReihenfolge.Sequentiell)
            .Select(k => k.DokumentId)
            .Distinct()
            .ToList();

        var aktiveStufeProDok = new Dictionary<int, int>();
        foreach (var dokId in dokumenteSeq)
        {
            var alleGruppen = await _db.FreigabeGruppen
                .Include(g => g.Zustimmungen)
                .Where(g => g.DokumentId == dokId)
                .OrderBy(g => g.Reihenfolge)
                .ToListAsync();
            var erste = alleGruppen.FirstOrDefault(g =>
                g.Zustimmungen.Count(z => z.Entscheidung == FreigabeEntscheidung.Zugestimmt) < g.BenoetigteZustimmungen);
            if (erste is not null) aktiveStufeProDok[dokId] = erste.Reihenfolge;
        }

        return kandidaten
            .Where(k => k.DokumentReihenfolgeModus == FreigabeReihenfolge.Parallel
                        || !aktiveStufeProDok.ContainsKey(k.DokumentId)
                        || aktiveStufeProDok[k.DokumentId] == k.Reihenfolge)
            .Select(k => new OffeneFreigabeDto(
                k.Id, k.DokumentId, k.DokumentTitel,
                k.Bezeichnung, k.BenoetigteZustimmungen, k.Zugestimmt, k.DokumentGeaendertAm))
            .ToList();
    }

    // ── Pflege der Gruppen-Konfiguration ────────────────────────────────────

    public async Task<int> GruppeAnlegenAsync(int dokumentId, string bezeichnung, int reihenfolge, int benoetigteZustimmungen, int benutzerId)
    {
        var dok = await _db.Dokumente.FindAsync(dokumentId) ?? throw new KeyNotFoundException();
        KonfigurationErlaubtOderWerfen(dok);

        var g = new FreigabeGruppe
        {
            DokumentId = dokumentId,
            Bezeichnung = bezeichnung.Trim(),
            Reihenfolge = reihenfolge < 1 ? 1 : reihenfolge,
            BenoetigteZustimmungen = benoetigteZustimmungen < 1 ? 1 : benoetigteZustimmungen
        };
        _db.FreigabeGruppen.Add(g);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, benutzerId, dokumentId: dokumentId,
            beschreibung: $"Freigabegruppe angelegt: {g.Bezeichnung} (Stufe {g.Reihenfolge}, Quorum {g.BenoetigteZustimmungen})");
        return g.Id;
    }

    public async Task GruppeBearbeitenAsync(int gruppeId, string bezeichnung, int reihenfolge, int benoetigteZustimmungen, int benutzerId)
    {
        var g = await _db.FreigabeGruppen.Include(x => x.Dokument).FirstOrDefaultAsync(x => x.Id == gruppeId)
            ?? throw new KeyNotFoundException();
        KonfigurationErlaubtOderWerfen(g.Dokument);

        g.Bezeichnung = bezeichnung.Trim();
        g.Reihenfolge = reihenfolge < 1 ? 1 : reihenfolge;
        g.BenoetigteZustimmungen = benoetigteZustimmungen < 1 ? 1 : benoetigteZustimmungen;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, benutzerId, dokumentId: g.DokumentId,
            beschreibung: $"Freigabegruppe geändert: {g.Bezeichnung}");
    }

    public async Task GruppeLoeschenAsync(int gruppeId, int benutzerId)
    {
        var g = await _db.FreigabeGruppen.Include(x => x.Dokument).FirstOrDefaultAsync(x => x.Id == gruppeId)
            ?? throw new KeyNotFoundException();
        KonfigurationErlaubtOderWerfen(g.Dokument);
        var dokId = g.DokumentId;
        var name = g.Bezeichnung;
        _db.FreigabeGruppen.Remove(g);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, benutzerId, dokumentId: dokId,
            beschreibung: $"Freigabegruppe gelöscht: {name}");
    }

    public async Task MitgliedHinzufuegenAsync(int gruppeId, int zugewiesenerBenutzerId, int handelnderBenutzerId)
    {
        var g = await _db.FreigabeGruppen.Include(x => x.Dokument).Include(x => x.Mitglieder)
            .FirstOrDefaultAsync(x => x.Id == gruppeId) ?? throw new KeyNotFoundException();
        KonfigurationErlaubtOderWerfen(g.Dokument);

        if (g.Mitglieder.Any(m => m.BenutzerId == zugewiesenerBenutzerId)) return;
        _db.FreigabeGruppeMitglieder.Add(new FreigabeGruppeMitglied
        {
            FreigabeGruppeId = gruppeId,
            BenutzerId = zugewiesenerBenutzerId
        });
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId, dokumentId: g.DokumentId,
            beschreibung: $"Mitglied zu '{g.Bezeichnung}' hinzugefügt (BenutzerId={zugewiesenerBenutzerId})");
    }

    public async Task MitgliedEntfernenAsync(int mitgliedId, int handelnderBenutzerId)
    {
        var m = await _db.FreigabeGruppeMitglieder
            .Include(x => x.FreigabeGruppe).ThenInclude(g => g.Dokument)
            .FirstOrDefaultAsync(x => x.Id == mitgliedId) ?? throw new KeyNotFoundException();
        KonfigurationErlaubtOderWerfen(m.FreigabeGruppe.Dokument);
        var dokId = m.FreigabeGruppe.DokumentId;
        var bez = m.FreigabeGruppe.Bezeichnung;
        var userId = m.BenutzerId;
        _db.FreigabeGruppeMitglieder.Remove(m);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId, dokumentId: dokId,
            beschreibung: $"Mitglied aus '{bez}' entfernt (BenutzerId={userId})");
    }

    private static void KonfigurationErlaubtOderWerfen(Dokument d)
    {
        if (d.Status is DokumentStatus.InFreigabe or DokumentStatus.Freigegeben)
            throw new InvalidOperationException(
                "Freigabekonfiguration kann nicht mehr geändert werden, solange das Dokument in Freigabe oder bereits freigegeben ist.");
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
                g.Mitglieder.Select(m => new FreigabeMitgliedDto(m.Id, m.BenutzerId, m.Benutzer.Anzeigename)).ToList(),
                g.Zustimmungen.Select(z => new FreigabeZustimmungDto(z.Id,
                    z.Benutzer.Anzeigename, z.Entscheidung, z.EntschiedenAm, z.Kommentar)).ToList(),
                gesamt);
        }).ToList();
    }
}
