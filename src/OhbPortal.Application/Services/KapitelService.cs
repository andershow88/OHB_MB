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
                k.Icon,
                k.ElternKapitelId,
                DokumenteAnzahl = k.Dokumente.Count(d => !d.Geloescht && !d.Archiviert)
            })
            .ToListAsync();

        IReadOnlyList<KapitelBaumDto> Children(int? parentId, int tiefe) =>
            alle.Where(k => k.ElternKapitelId == parentId)
                .Select(k => new KapitelBaumDto(
                    k.Id, k.Titel, k.Icon, k.ElternKapitelId, tiefe,
                    k.DokumenteAnzahl,
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
}
