using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.DTOs;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Entities;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Services;

public class AdminService : IAdminService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuthService _auth;
    private readonly IAuditService _audit;

    public AdminService(IApplicationDbContext db, IAuthService auth, IAuditService audit)
    {
        _db = db;
        _auth = auth;
        _audit = audit;
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        var anzBen = await _db.Benutzer.CountAsync();
        var anzBenAktiv = await _db.Benutzer.CountAsync(b => b.IstAktiv);
        var anzTeams = await _db.Teams.CountAsync();
        var anzTeamsAktiv = await _db.Teams.CountAsync(t => t.IstAktiv);
        var anzDok = await _db.Dokumente.CountAsync(d => !d.Geloescht);

        var letzte = await _db.Benutzer
            .OrderByDescending(b => b.ErstelltAm)
            .Take(5)
            .Select(b => new BenutzerListeDto(b.Id, b.Benutzername, b.Anzeigename, b.EMail,
                b.Rolle, b.IstAktiv, b.Teams.Count, b.ErstelltAm))
            .ToListAsync();

        return new AdminDashboardDto(anzBen, anzBenAktiv, anzTeams, anzTeamsAktiv, anzDok, letzte);
    }

    // ── Benutzer ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<BenutzerListeDto>> GetBenutzerAsync()
    {
        return await _db.Benutzer
            .OrderBy(b => b.Anzeigename)
            .Select(b => new BenutzerListeDto(b.Id, b.Benutzername, b.Anzeigename, b.EMail,
                b.Rolle, b.IstAktiv, b.Teams.Count, b.ErstelltAm))
            .ToListAsync();
    }

    public async Task<BenutzerDetailDto?> GetBenutzerDetailAsync(int id)
    {
        var b = await _db.Benutzer
            .Include(x => x.Teams).ThenInclude(t => t.Team)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return null;
        return new BenutzerDetailDto(b.Id, b.Benutzername, b.Anzeigename, b.EMail,
            b.Rolle, b.IstAktiv,
            b.Teams.Select(bt => new TeamKurzDto(bt.TeamId, bt.Team.Name)).ToList());
    }

    public async Task<int> BenutzerAnlegenAsync(BenutzerAnlegenEingabe dto, int handelnderBenutzerId)
    {
        var benutzername = dto.Benutzername.Trim().ToLowerInvariant();
        if (await _db.Benutzer.AnyAsync(b => b.Benutzername == benutzername))
            throw new InvalidOperationException("Benutzername ist bereits vergeben.");

        var b = new Benutzer
        {
            Benutzername = benutzername,
            Anzeigename = dto.Anzeigename.Trim(),
            EMail = dto.EMail?.Trim() ?? string.Empty,
            PasswortHash = _auth.HashPasswort(dto.Passwort),
            Rolle = dto.Rolle,
            IstAktiv = true,
            ErstelltAm = DateTime.UtcNow
        };
        _db.Benutzer.Add(b);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId,
            beschreibung: $"Benutzer angelegt: {benutzername} ({dto.Rolle})");
        return b.Id;
    }

    public async Task BenutzerAktualisierenAsync(int id, BenutzerBearbeitenEingabe dto, int handelnderBenutzerId)
    {
        var b = await _db.Benutzer.FindAsync(id) ?? throw new KeyNotFoundException();
        b.Anzeigename = dto.Anzeigename.Trim();
        b.EMail = dto.EMail?.Trim() ?? string.Empty;
        var alteRolle = b.Rolle;
        b.Rolle = dto.Rolle;
        b.IstAktiv = dto.IstAktiv;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId,
            beschreibung: $"Benutzer {b.Benutzername} aktualisiert"
                + (alteRolle != dto.Rolle ? $", Rolle {alteRolle} → {dto.Rolle}" : ""));
    }

    public async Task BenutzerAktivitaetUmschaltenAsync(int id, bool aktiv, int handelnderBenutzerId)
    {
        var b = await _db.Benutzer.FindAsync(id) ?? throw new KeyNotFoundException();
        if (id == handelnderBenutzerId && !aktiv)
            throw new InvalidOperationException("Sie können sich nicht selbst deaktivieren.");
        b.IstAktiv = aktiv;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId,
            beschreibung: $"Benutzer {b.Benutzername} {(aktiv ? "aktiviert" : "deaktiviert")}");
    }

    public async Task PasswortZuruecksetzenAsync(int id, string neuesPasswort, int handelnderBenutzerId)
    {
        if (string.IsNullOrWhiteSpace(neuesPasswort) || neuesPasswort.Length < 6)
            throw new InvalidOperationException("Passwort muss mindestens 6 Zeichen haben.");
        var b = await _db.Benutzer.FindAsync(id) ?? throw new KeyNotFoundException();
        b.PasswortHash = _auth.HashPasswort(neuesPasswort);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId,
            beschreibung: $"Passwort zurückgesetzt für {b.Benutzername}");
    }

    // ── Teams ────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<TeamListeDto>> GetTeamsAsync()
    {
        return await _db.Teams
            .OrderBy(t => t.Name)
            .Select(t => new TeamListeDto(t.Id, t.Name, t.Beschreibung, t.Mitglieder.Count, t.IstAktiv))
            .ToListAsync();
    }

    public async Task<TeamDetailDto?> GetTeamDetailAsync(int id)
    {
        var t = await _db.Teams
            .Include(x => x.Mitglieder).ThenInclude(m => m.Benutzer)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return null;
        return new TeamDetailDto(t.Id, t.Name, t.Beschreibung, t.IstAktiv,
            t.Mitglieder.OrderBy(m => m.Benutzer.Anzeigename)
                .Select(m => new BenutzerKurzDto(m.BenutzerId, m.Benutzer.Anzeigename, m.Benutzer.Rolle)).ToList());
    }

    public async Task<int> TeamAnlegenAsync(TeamEingabe dto, int handelnderBenutzerId)
    {
        if (await _db.Teams.AnyAsync(t => t.Name == dto.Name.Trim()))
            throw new InvalidOperationException("Team-Name ist bereits vergeben.");
        var t = new Team
        {
            Name = dto.Name.Trim(),
            Beschreibung = dto.Beschreibung?.Trim(),
            IstAktiv = dto.IstAktiv
        };
        _db.Teams.Add(t);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId,
            beschreibung: $"Team angelegt: {t.Name}");
        return t.Id;
    }

    public async Task TeamAktualisierenAsync(int id, TeamEingabe dto, int handelnderBenutzerId)
    {
        var t = await _db.Teams.FindAsync(id) ?? throw new KeyNotFoundException();
        t.Name = dto.Name.Trim();
        t.Beschreibung = dto.Beschreibung?.Trim();
        t.IstAktiv = dto.IstAktiv;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId,
            beschreibung: $"Team aktualisiert: {t.Name}");
    }

    public async Task TeamLoeschenAsync(int id, int handelnderBenutzerId)
    {
        var t = await _db.Teams.Include(x => x.Mitglieder).FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        // Prüfen: verwenden das Team andere Entitäten noch?
        var hatDokumente = await _db.Dokumente.AnyAsync(d => d.VerantwortlicherBereichId == id);
        var hatKn = await _db.Kenntnisnahmen.AnyAsync(k => k.TeamId == id);
        if (hatDokumente || hatKn)
            throw new InvalidOperationException(
                "Team kann nicht gelöscht werden — es ist noch Verantwortlicher Bereich oder in Kenntnisnahmen zugewiesen. Bitte deaktivieren Sie es stattdessen.");

        var name = t.Name;
        _db.Teams.Remove(t);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId,
            beschreibung: $"Team gelöscht: {name}");
    }

    public async Task TeamMitgliedHinzufuegenAsync(int teamId, int benutzerId, int handelnderBenutzerId)
    {
        var exists = await _db.BenutzerTeams.AnyAsync(bt => bt.TeamId == teamId && bt.BenutzerId == benutzerId);
        if (exists) return;
        _db.BenutzerTeams.Add(new BenutzerTeam { TeamId = teamId, BenutzerId = benutzerId });
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId,
            beschreibung: $"Benutzer {benutzerId} zu Team {teamId} hinzugefügt");
    }

    public async Task TeamMitgliedEntfernenAsync(int teamId, int benutzerId, int handelnderBenutzerId)
    {
        var bt = await _db.BenutzerTeams
            .FirstOrDefaultAsync(x => x.TeamId == teamId && x.BenutzerId == benutzerId)
            ?? throw new KeyNotFoundException();
        _db.BenutzerTeams.Remove(bt);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditTyp.BerechtigungGeaendert, handelnderBenutzerId,
            beschreibung: $"Benutzer {benutzerId} aus Team {teamId} entfernt");
    }
}
