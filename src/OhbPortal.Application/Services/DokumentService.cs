using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.DTOs;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Entities;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Services;

public class DokumentService : IDokumentService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public DokumentService(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IEnumerable<DokumentListeDto>> GetAlleAsync(DokumentFilterDto filter)
    {
        var query = _db.Dokumente
            .Include(d => d.Kapitel).ThenInclude(k => k.ElternKapitel)
            .Include(d => d.VerantwortlicherBereich)
            .Include(d => d.GeaendertVon)
            .AsQueryable();

        if (filter.NurGeloescht)
            query = query.Where(d => d.Geloescht);
        else
            query = query.Where(d => !d.Geloescht);

        if (!filter.IncludeArchiviert && !filter.NurGeloescht)
            query = query.Where(d => !d.Archiviert);

        if (filter.KapitelId.HasValue)
            query = query.Where(d => d.KapitelId == filter.KapitelId.Value);
        if (filter.Status.HasValue)
            query = query.Where(d => d.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.Kategorie))
            query = query.Where(d => d.Kategorie == filter.Kategorie);
        if (!string.IsNullOrWhiteSpace(filter.Suchbegriff))
        {
            var term = filter.Suchbegriff.Trim();
            query = query.Where(d =>
                d.Titel.Contains(term) ||
                (d.Kurzbeschreibung != null && d.Kurzbeschreibung.Contains(term)) ||
                (d.Tags != null && d.Tags.Contains(term)) ||
                (d.InhaltHtml != null && d.InhaltHtml.Contains(term)));
        }
        if (filter.NurMitPruefterminAbgelaufen == true)
            query = query.Where(d => d.Pruefterm.HasValue && d.Pruefterm.Value < DateTime.UtcNow);

        return await query
            .OrderByDescending(d => d.GeaendertAm)
            .Take(300)
            .Select(d => new DokumentListeDto(
                d.Id, d.Titel, d.Kurzbeschreibung,
                (d.Kapitel.ElternKapitel != null ? d.Kapitel.ElternKapitel.Titel + " › " : "") + d.Kapitel.Titel,
                d.VerantwortlicherBereich != null ? d.VerantwortlicherBereich.Name : null,
                d.Status, d.AktuelleVersion, d.GeaendertAm,
                d.GeaendertVon.Anzeigename, d.Pruefterm, d.Tags, d.Archiviert))
            .ToListAsync();
    }

    public async Task<DokumentDetailDto?> GetDetailAsync(int id)
    {
        var d = await _db.Dokumente
            .Include(x => x.Kapitel).ThenInclude(k => k.ElternKapitel)
            .Include(x => x.VerantwortlicherBereich)
            .Include(x => x.ErstelltVon)
            .Include(x => x.GeaendertVon)
            .Include(x => x.Anhaenge).ThenInclude(a => a.HochgeladenVon)
            .Include(x => x.Verlinkungen).ThenInclude(l => l.ZielDokument)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return null;

        return new DokumentDetailDto(
            d.Id, d.Titel, d.Kurzbeschreibung, d.InhaltHtml,
            d.KapitelId,
            (d.Kapitel.ElternKapitel != null ? d.Kapitel.ElternKapitel.Titel + " › " : "") + d.Kapitel.Titel,
            d.VerantwortlicherBereichId,
            d.VerantwortlicherBereich?.Name,
            d.Status, d.ErstelltAm, d.ErstelltVon.Anzeigename,
            d.GeaendertAm, d.GeaendertVon.Anzeigename,
            d.SichtbarAb, d.SichtbarBis, d.Pruefterm,
            d.Kategorie, d.Tags, d.AktuelleVersion,
            d.Archiviert, d.Geloescht,
            d.FreigabeModus, d.FreigabeReihenfolge,
            d.Druckverbot, d.OeffentlichLesbar,
            d.Anhaenge.Select(a => new AnhangDto(a.Id, a.Dateiname, a.ContentType, a.DateigroesseBytes,
                a.HochgeladenVon.Anzeigename, a.HochgeladenAm)).ToList(),
            d.Verlinkungen.Select(l => new DokumentLinkDto(l.Id, l.ZielDokumentId, l.ZielDokument.Titel, l.Bezeichnung)).ToList());
    }

    public async Task<int> ErstellenAsync(DokumentErstellenDto dto, int benutzerId)
    {
        var d = new Dokument
        {
            Titel = dto.Titel,
            Kurzbeschreibung = dto.Kurzbeschreibung,
            KapitelId = dto.KapitelId,
            VerantwortlicherBereichId = dto.VerantwortlicherBereichId,
            Kategorie = dto.Kategorie,
            Tags = dto.Tags,
            SichtbarAb = dto.SichtbarAb,
            SichtbarBis = dto.SichtbarBis,
            Pruefterm = dto.Pruefterm,
            InhaltHtml = dto.InhaltHtml,
            FreigabeModus = dto.FreigabeModus,
            Status = DokumentStatus.Entwurf,
            ErstelltAm = DateTime.UtcNow,
            GeaendertAm = DateTime.UtcNow,
            ErstelltVonId = benutzerId,
            GeaendertVonId = benutzerId,
            AktuelleVersion = 1
        };
        _db.Dokumente.Add(d);
        await _db.SaveChangesAsync();

        // Erste Version als Snapshot
        _db.DokumentVersionen.Add(new DokumentVersion
        {
            DokumentId = d.Id,
            Versionsnummer = 1,
            Titel = d.Titel,
            Kurzbeschreibung = d.Kurzbeschreibung,
            InhaltHtml = d.InhaltHtml,
            StatusZumZeitpunkt = d.Status,
            ErstelltAm = d.ErstelltAm,
            ErstelltVonId = benutzerId,
            AenderungsHinweis = "Initiale Version"
        });
        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditTyp.DokumentErstellt, benutzerId, dokumentId: d.Id, beschreibung: d.Titel);
        return d.Id;
    }

    public async Task AktualisierenAsync(int id, DokumentBearbeitenDto dto, int benutzerId, string? aenderungshinweis = null)
    {
        var d = await _db.Dokumente.FindAsync(id) ?? throw new KeyNotFoundException();

        d.Titel = dto.Titel;
        d.Kurzbeschreibung = dto.Kurzbeschreibung;
        d.KapitelId = dto.KapitelId;
        d.VerantwortlicherBereichId = dto.VerantwortlicherBereichId;
        d.Kategorie = dto.Kategorie;
        d.Tags = dto.Tags;
        d.SichtbarAb = dto.SichtbarAb;
        d.SichtbarBis = dto.SichtbarBis;
        d.Pruefterm = dto.Pruefterm;
        d.InhaltHtml = dto.InhaltHtml;
        d.FreigabeModus = dto.FreigabeModus;
        d.FreigabeReihenfolge = dto.FreigabeReihenfolge;
        d.Druckverbot = dto.Druckverbot;
        d.OeffentlichLesbar = dto.OeffentlichLesbar;
        d.GeaendertAm = DateTime.UtcNow;
        d.GeaendertVonId = benutzerId;
        d.AktuelleVersion += 1;

        _db.DokumentVersionen.Add(new DokumentVersion
        {
            DokumentId = d.Id,
            Versionsnummer = d.AktuelleVersion,
            Titel = d.Titel,
            Kurzbeschreibung = d.Kurzbeschreibung,
            InhaltHtml = d.InhaltHtml,
            StatusZumZeitpunkt = d.Status,
            ErstelltAm = DateTime.UtcNow,
            ErstelltVonId = benutzerId,
            AenderungsHinweis = aenderungshinweis
        });
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.DokumentBearbeitet, benutzerId, dokumentId: id,
            beschreibung: aenderungshinweis);
        await _audit.LogAsync(AuditTyp.VersionAngelegt, benutzerId, dokumentId: id,
            beschreibung: $"Version {d.AktuelleVersion}");
    }

    public async Task StatusAendernAsync(int id, DokumentStatus neuerStatus, int benutzerId, string? notiz = null)
    {
        var d = await _db.Dokumente.FindAsync(id) ?? throw new KeyNotFoundException();
        d.Status = neuerStatus;
        d.GeaendertAm = DateTime.UtcNow;
        d.GeaendertVonId = benutzerId;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.DokumentBearbeitet, benutzerId, dokumentId: id,
            beschreibung: $"Status: {neuerStatus}{(notiz is null ? "" : " – " + notiz)}");
    }

    public async Task ArchivierenAsync(int id, int benutzerId)
    {
        var d = await _db.Dokumente.FindAsync(id) ?? throw new KeyNotFoundException();
        d.Archiviert = true;
        d.ArchiviertAm = DateTime.UtcNow;
        d.Status = DokumentStatus.Archiviert;
        d.GeaendertAm = DateTime.UtcNow;
        d.GeaendertVonId = benutzerId;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.DokumentArchiviert, benutzerId, dokumentId: id);
    }

    public async Task WiederherstellenAsync(int id, int benutzerId)
    {
        var d = await _db.Dokumente.FindAsync(id) ?? throw new KeyNotFoundException();
        d.Archiviert = false;
        d.ArchiviertAm = null;
        d.Geloescht = false;
        d.GeloeschtAm = null;
        d.Status = DokumentStatus.Entwurf;
        d.GeaendertAm = DateTime.UtcNow;
        d.GeaendertVonId = benutzerId;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.DokumentWiederhergestellt, benutzerId, dokumentId: id);
    }

    public async Task InPapierkorbVerschiebenAsync(int id, int benutzerId)
    {
        var d = await _db.Dokumente.FindAsync(id) ?? throw new KeyNotFoundException();
        d.Geloescht = true;
        d.GeloeschtAm = DateTime.UtcNow;
        d.GeaendertAm = DateTime.UtcNow;
        d.GeaendertVonId = benutzerId;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.DokumentGeloescht, benutzerId, dokumentId: id,
            beschreibung: "In Papierkorb verschoben");
    }

    public async Task EndgueltigLoeschenAsync(int id, int benutzerId)
    {
        var d = await _db.Dokumente.FindAsync(id) ?? throw new KeyNotFoundException();
        _db.Dokumente.Remove(d);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<VersionDto>> GetVersionenAsync(int dokumentId)
        => await _db.DokumentVersionen
            .Include(v => v.ErstelltVon)
            .Where(v => v.DokumentId == dokumentId)
            .OrderByDescending(v => v.Versionsnummer)
            .Select(v => new VersionDto(v.Id, v.Versionsnummer, v.Titel, v.ErstelltAm,
                v.ErstelltVon.Anzeigename, v.StatusZumZeitpunkt, v.AenderungsHinweis))
            .ToListAsync();

    public async Task<IEnumerable<AuditDto>> GetAuditAsync(int dokumentId)
        => await _db.AuditEintraege
            .Include(a => a.Benutzer)
            .Where(a => a.DokumentId == dokumentId)
            .OrderByDescending(a => a.Zeitpunkt)
            .Take(200)
            .Select(a => new AuditDto(a.Id, a.Typ, a.Zeitpunkt, a.Benutzer.Anzeigename, a.Beschreibung))
            .ToListAsync();
}
