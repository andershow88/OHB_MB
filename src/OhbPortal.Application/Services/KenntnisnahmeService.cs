using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.DTOs;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Entities;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Services;

public class KenntnisnahmeService : IKenntnisnahmeService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public KenntnisnahmeService(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task ZuweisenBenutzerAsync(int dokumentId, int benutzerId, DateTime? faelligkeit)
    {
        _db.Kenntnisnahmen.Add(new Kenntnisnahme
        {
            DokumentId = dokumentId,
            BenutzerId = benutzerId,
            Faelligkeit = faelligkeit
        });
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.KenntnisnahmeZugewiesen, benutzerId, dokumentId: dokumentId);
    }

    public async Task ZuweisenTeamAsync(int dokumentId, int teamId, DateTime? faelligkeit)
    {
        _db.Kenntnisnahmen.Add(new Kenntnisnahme
        {
            DokumentId = dokumentId,
            TeamId = teamId,
            Faelligkeit = faelligkeit
        });
        await _db.SaveChangesAsync();
    }

    public async Task BestaetigenAsync(int kenntnisnahmeId, int benutzerId)
    {
        var k = await _db.Kenntnisnahmen.FindAsync(kenntnisnahmeId) ?? throw new KeyNotFoundException();
        k.Status = KenntnisnahmeStatus.Bestaetigt;
        k.BestaetigtAm = DateTime.UtcNow;
        k.BestaetigtVonId = benutzerId;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.KenntnisnahmeBestaetigt, benutzerId, dokumentId: k.DokumentId);
    }

    public async Task<IEnumerable<KenntnisnahmeDto>> GetProDokumentAsync(int dokumentId)
    {
        return await _db.Kenntnisnahmen
            .Include(k => k.Benutzer)
            .Include(k => k.Team)
            .Include(k => k.BestaetigtVon)
            .Where(k => k.DokumentId == dokumentId)
            .Select(k => new KenntnisnahmeDto(
                k.Id,
                k.Benutzer != null ? "Benutzer: " + k.Benutzer.Anzeigename
                    : k.Team != null ? "Team: " + k.Team.Name : "–",
                k.Status, k.Faelligkeit, k.BestaetigtAm,
                k.BestaetigtVon != null ? k.BestaetigtVon.Anzeigename : null))
            .ToListAsync();
    }

    public async Task<IEnumerable<KenntnisnahmeOffenDto>> GetMeineOffenenAsync(int benutzerId)
    {
        var teamIds = await _db.BenutzerTeams
            .Where(bt => bt.BenutzerId == benutzerId)
            .Select(bt => bt.TeamId).ToListAsync();

        return await _db.Kenntnisnahmen
            .Include(k => k.Dokument)
            .Where(k => (k.BenutzerId == benutzerId || (k.TeamId != null && teamIds.Contains(k.TeamId.Value)))
                     && k.Status != KenntnisnahmeStatus.Bestaetigt)
            .OrderBy(k => k.Faelligkeit)
            .Select(k => new KenntnisnahmeOffenDto(k.Id, k.DokumentId, k.Dokument.Titel,
                k.Faelligkeit, k.Status))
            .ToListAsync();
    }
}
