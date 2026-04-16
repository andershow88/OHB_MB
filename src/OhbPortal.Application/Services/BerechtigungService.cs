using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.DTOs;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Entities;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Services;

public class BerechtigungService : IBerechtigungService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditService _audit;

    public BerechtigungService(IApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IEnumerable<BerechtigungDto>> GetProDokumentAsync(int dokumentId)
    {
        return await _db.Berechtigungen
            .Include(b => b.Benutzer)
            .Include(b => b.Team)
            .Where(b => b.DokumentId == dokumentId)
            .OrderBy(b => b.Typ)
            .Select(b => new BerechtigungDto(
                b.Id,
                b.Benutzer != null ? "Benutzer: " + b.Benutzer.Anzeigename
                    : b.Team != null ? "Team: " + b.Team.Name
                    : b.Rolle != null ? "Rolle: " + b.Rolle.ToString()
                    : "–",
                b.Typ, b.BenutzerId, b.TeamId, b.Rolle))
            .ToListAsync();
    }

    public async Task<int> HinzufuegenAsync(int dokumentId, int? benutzerId, int? teamId, Rolle? rolle,
        BerechtigungsTyp typ, int handelnderBenutzerId)
    {
        var zielAnzahl = (benutzerId.HasValue ? 1 : 0) + (teamId.HasValue ? 1 : 0) + (rolle.HasValue ? 1 : 0);
        if (zielAnzahl != 1)
            throw new InvalidOperationException("Bitte genau einen Zieltyp (Benutzer, Team oder Rolle) wählen.");

        var vorhanden = await _db.Berechtigungen.AnyAsync(b =>
            b.DokumentId == dokumentId
            && b.BenutzerId == benutzerId
            && b.TeamId == teamId
            && b.Rolle == rolle);
        if (vorhanden)
            throw new InvalidOperationException("Diese Berechtigung existiert bereits.");

        var e = new DokumentBerechtigung
        {
            DokumentId = dokumentId,
            BenutzerId = benutzerId,
            TeamId = teamId,
            Rolle = rolle,
            Typ = typ
        };
        _db.Berechtigungen.Add(e);
        await _db.SaveChangesAsync();

        var ziel = benutzerId.HasValue ? $"BenutzerId={benutzerId}"
            : teamId.HasValue ? $"TeamId={teamId}"
            : $"Rolle={rolle}";
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId, dokumentId: dokumentId,
            beschreibung: $"Berechtigung hinzugefügt: {ziel} ({typ})");
        return e.Id;
    }

    public async Task EntfernenAsync(int id, int handelnderBenutzerId)
    {
        var e = await _db.Berechtigungen.FindAsync(id) ?? throw new KeyNotFoundException();
        var dokId = e.DokumentId;
        _db.Berechtigungen.Remove(e);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId, dokumentId: dokId,
            beschreibung: "Berechtigung entfernt");
    }

    public async Task TypAendernAsync(int id, BerechtigungsTyp typ, int handelnderBenutzerId)
    {
        var e = await _db.Berechtigungen.FindAsync(id) ?? throw new KeyNotFoundException();
        e.Typ = typ;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId, dokumentId: e.DokumentId,
            beschreibung: $"Berechtigung geändert auf {typ}");
    }
}
