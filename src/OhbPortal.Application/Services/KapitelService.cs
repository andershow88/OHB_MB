using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.DTOs;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Entities;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Services;

public class KapitelService : IKapitelService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public KapitelService(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IEnumerable<KapitelBaumDto>> GetBaumAsync()
    {
        var alle = await _db.Kapitel
            .OrderBy(k => k.Sortierung).ThenBy(k => k.Titel)
            .Select(k => new
            {
                k.Id,
                k.Titel,
                k.Beschreibung,
                k.Icon,
                k.ElternKapitelId,
                k.Sortierung,
                DokumenteAnzahl = k.Dokumente.Count(d => !d.Geloescht && !d.Archiviert)
            })
            .ToListAsync();

        var dokRows = await _db.Dokumente
            .Where(d => !d.Geloescht)
            .OrderBy(d => d.Titel)
            .Select(d => new { d.Id, d.Titel, d.KapitelId, d.Status, d.Archiviert })
            .ToListAsync();

        var dokumenteByKapitel = dokRows
            .GroupBy(d => d.KapitelId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<KapitelDokumentDto>)g
                    .Select(x => new KapitelDokumentDto(x.Id, x.Titel, x.Status, x.Archiviert))
                    .ToList());

        IReadOnlyList<KapitelDokumentDto> empty = Array.Empty<KapitelDokumentDto>();

        IReadOnlyList<KapitelBaumDto> Children(int? parentId, int tiefe) =>
            alle.Where(k => k.ElternKapitelId == parentId)
                .Select(k => new KapitelBaumDto(
                    k.Id, k.Titel, k.Beschreibung, k.Icon, k.ElternKapitelId,
                    tiefe, k.Sortierung,
                    k.DokumenteAnzahl,
                    dokumenteByKapitel.TryGetValue(k.Id, out var docs) ? docs : empty,
                    Children(k.Id, tiefe + 1)))
                .ToList();

        return Children(null, 0);
    }

    public async Task<KapitelDto?> GetAsync(int id)
    {
        var k = await _db.Kapitel.FindAsync(id);
        return k is null ? null : new KapitelDto(k.Id, k.Titel, k.Beschreibung, k.Icon, k.ElternKapitelId);
    }

    public async Task<int> AnlegenAsync(string titel, int? elternId, string? beschreibung, string? icon, int benutzerId)
    {
        var maxSort = await _db.Kapitel
            .Where(k => k.ElternKapitelId == elternId)
            .Select(k => (int?)k.Sortierung)
            .MaxAsync() ?? -1;

        var k = new Kapitel
        {
            Titel = titel,
            ElternKapitelId = elternId,
            Beschreibung = beschreibung,
            Icon = icon,
            Sortierung = maxSort + 1
        };
        _db.Kapitel.Add(k);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.KapitelErstellt, benutzerId, kapitelId: k.Id, beschreibung: titel);
        return k.Id;
    }

    public async Task AktualisierenAsync(int id, string titel, string? beschreibung, string? icon, int benutzerId)
    {
        var k = await _db.Kapitel.FindAsync(id) ?? throw new KeyNotFoundException();
        k.Titel = titel;
        k.Beschreibung = beschreibung;
        k.Icon = icon;
        k.GeaendertAm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.KapitelGeaendert, benutzerId, kapitelId: id);
    }

    public async Task LoeschenAsync(int id, int benutzerId)
    {
        var k = await _db.Kapitel
            .Include(x => x.Unterkapitel)
            .Include(x => x.Dokumente)
            .FirstOrDefaultAsync(x => x.Id == id) ?? throw new KeyNotFoundException();
        if (k.Unterkapitel.Any() || k.Dokumente.Any(d => !d.Geloescht))
            throw new InvalidOperationException("Kapitel enthält Unterkapitel oder aktive Dokumente.");
        _db.Kapitel.Remove(k);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.KapitelGeloescht, benutzerId, kapitelId: id);
    }

    public Task NachObenVerschiebenAsync(int id, int benutzerId)
        => VerschiebenInternalAsync(id, nachOben: true, benutzerId);

    public Task NachUntenVerschiebenAsync(int id, int benutzerId)
        => VerschiebenInternalAsync(id, nachOben: false, benutzerId);

    private async Task VerschiebenInternalAsync(int id, bool nachOben, int benutzerId)
    {
        var k = await _db.Kapitel.FindAsync(id) ?? throw new KeyNotFoundException();

        var nachbar = nachOben
            ? await _db.Kapitel
                .Where(x => x.ElternKapitelId == k.ElternKapitelId && x.Sortierung < k.Sortierung)
                .OrderByDescending(x => x.Sortierung)
                .FirstOrDefaultAsync()
            : await _db.Kapitel
                .Where(x => x.ElternKapitelId == k.ElternKapitelId && x.Sortierung > k.Sortierung)
                .OrderBy(x => x.Sortierung)
                .FirstOrDefaultAsync();

        if (nachbar is null) return;

        (k.Sortierung, nachbar.Sortierung) = (nachbar.Sortierung, k.Sortierung);
        k.GeaendertAm = DateTime.UtcNow;
        nachbar.GeaendertAm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.KapitelGeaendert, benutzerId, kapitelId: id,
            beschreibung: nachOben ? "Nach oben verschoben" : "Nach unten verschoben");
    }

    public async Task VerschiebenAsync(int id, int zielId, string position, int benutzerId)
    {
        if (id == zielId)
            throw new InvalidOperationException("Kapitel kann nicht auf sich selbst verschoben werden.");

        var k = await _db.Kapitel.FindAsync(id) ?? throw new KeyNotFoundException();
        var ziel = await _db.Kapitel.FindAsync(zielId) ?? throw new KeyNotFoundException();

        int? neuerElternId;
        int neuerIndex;
        switch (position?.ToLowerInvariant())
        {
            case "before":
                neuerElternId = ziel.ElternKapitelId;
                neuerIndex = ziel.Sortierung;
                break;
            case "after":
                neuerElternId = ziel.ElternKapitelId;
                neuerIndex = ziel.Sortierung + 1;
                break;
            case "inside":
                neuerElternId = ziel.Id;
                neuerIndex = int.MaxValue;
                break;
            default:
                throw new InvalidOperationException("Unbekannte Drop-Position.");
        }

        if (neuerElternId.HasValue && await IstNachfahreAsync(id, neuerElternId.Value))
            throw new InvalidOperationException("Kapitel kann nicht in einen seiner Unterkapitel verschoben werden.");

        var alterElternId = k.ElternKapitelId;
        k.ElternKapitelId = neuerElternId;
        k.GeaendertAm = DateTime.UtcNow;

        // Zielgruppe (alle Geschwister im neuen Eltern-Kontext, ohne den Kandidaten)
        var zielgruppe = await _db.Kapitel
            .Where(x => x.ElternKapitelId == neuerElternId && x.Id != id)
            .OrderBy(x => x.Sortierung).ThenBy(x => x.Titel)
            .ToListAsync();

        if (neuerIndex < 0) neuerIndex = 0;
        if (neuerIndex > zielgruppe.Count) neuerIndex = zielgruppe.Count;
        zielgruppe.Insert(neuerIndex, k);

        for (int i = 0; i < zielgruppe.Count; i++)
        {
            if (zielgruppe[i].Sortierung != i)
            {
                zielgruppe[i].Sortierung = i;
                zielgruppe[i].GeaendertAm = DateTime.UtcNow;
            }
        }

        if (alterElternId != neuerElternId)
        {
            var alteGruppe = await _db.Kapitel
                .Where(x => x.ElternKapitelId == alterElternId && x.Id != id)
                .OrderBy(x => x.Sortierung).ThenBy(x => x.Titel)
                .ToListAsync();
            for (int i = 0; i < alteGruppe.Count; i++)
            {
                if (alteGruppe[i].Sortierung != i)
                {
                    alteGruppe[i].Sortierung = i;
                    alteGruppe[i].GeaendertAm = DateTime.UtcNow;
                }
            }
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.KapitelGeaendert, benutzerId, kapitelId: id,
            beschreibung: $"Per Drag-and-Drop verschoben (neuer Eltern: {neuerElternId?.ToString() ?? "Hauptebene"}, Position: {position})");
    }

    private async Task<bool> IstNachfahreAsync(int kapitelId, int kandidatId)
    {
        int? cursor = kandidatId;
        var sicherung = 0;
        while (cursor.HasValue && sicherung++ < 64)
        {
            if (cursor.Value == kapitelId) return true;
            var current = await _db.Kapitel
                .Where(x => x.Id == cursor.Value)
                .Select(x => new { x.ElternKapitelId })
                .FirstOrDefaultAsync();
            cursor = current?.ElternKapitelId;
        }
        return false;
    }
}
