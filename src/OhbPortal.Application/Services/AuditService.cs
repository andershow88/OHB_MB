using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Entities;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _db;

    public AuditService(IApplicationDbContext db) => _db = db;

    public async Task LogAsync(AuditTyp typ, int benutzerId, int? dokumentId = null, int? kapitelId = null, string? beschreibung = null)
    {
        _db.AuditEintraege.Add(new AuditEintrag
        {
            Typ = typ,
            Zeitpunkt = DateTime.UtcNow,
            BenutzerId = benutzerId,
            DokumentId = dokumentId,
            KapitelId = kapitelId,
            Beschreibung = beschreibung
        });
        await _db.SaveChangesAsync();
    }
}
