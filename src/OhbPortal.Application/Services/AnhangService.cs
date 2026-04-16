using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Entities;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Services;

public class AnhangService : IAnhangService
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IAuditService _audit;

    public AnhangService(IApplicationDbContext db, IFileStorage storage, IAuditService audit)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
    }

    public async Task<int> HochladenAsync(int dokumentId, Stream inhalt, string dateiname, string contentType, long laenge, int benutzerId)
    {
        var key = await _storage.SpeichernAsync(inhalt, dateiname, $"dok_{dokumentId}");
        var a = new Anhang
        {
            DokumentId = dokumentId,
            Dateiname = dateiname,
            SpeicherSchluessel = key,
            ContentType = contentType,
            DateigroesseBytes = laenge,
            HochgeladenAm = DateTime.UtcNow,
            HochgeladenVonId = benutzerId
        };
        _db.Anhaenge.Add(a);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.AnhangHochgeladen, benutzerId, dokumentId: dokumentId, beschreibung: dateiname);
        return a.Id;
    }

    public async Task<(Stream Inhalt, string ContentType, string Dateiname)> HerunterladenAsync(int anhangId)
    {
        var a = await _db.Anhaenge.FindAsync(anhangId) ?? throw new KeyNotFoundException();
        var stream = await _storage.LadenAsync(a.SpeicherSchluessel);
        return (stream, a.ContentType, a.Dateiname);
    }

    public async Task LoeschenAsync(int anhangId, int benutzerId)
    {
        var a = await _db.Anhaenge.FindAsync(anhangId) ?? throw new KeyNotFoundException();
        try { await _storage.LoeschenAsync(a.SpeicherSchluessel); } catch { /* best effort */ }
        _db.Anhaenge.Remove(a);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.AnhangGeloescht, benutzerId, dokumentId: a.DokumentId, beschreibung: a.Dateiname);
    }
}
