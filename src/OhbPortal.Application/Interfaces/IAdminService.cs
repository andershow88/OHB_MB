using OhbPortal.Application.DTOs;

namespace OhbPortal.Application.Interfaces;

public interface IAdminService
{
    // Dashboard
    Task<AdminDashboardDto> GetDashboardAsync();

    // Benutzer
    Task<IEnumerable<BenutzerListeDto>> GetBenutzerAsync();
    Task<BenutzerDetailDto?> GetBenutzerDetailAsync(int id);
    Task<int> BenutzerAnlegenAsync(BenutzerAnlegenEingabe dto, int handelnderBenutzerId);
    Task BenutzerAktualisierenAsync(int id, BenutzerBearbeitenEingabe dto, int handelnderBenutzerId);
    Task BenutzerAktivitaetUmschaltenAsync(int id, bool aktiv, int handelnderBenutzerId);
    Task PasswortZuruecksetzenAsync(int id, string neuesPasswort, int handelnderBenutzerId);

    // Teams
    Task<IEnumerable<TeamListeDto>> GetTeamsAsync();
    Task<TeamDetailDto?> GetTeamDetailAsync(int id);
    Task<int> TeamAnlegenAsync(TeamEingabe dto, int handelnderBenutzerId);
    Task TeamAktualisierenAsync(int id, TeamEingabe dto, int handelnderBenutzerId);
    Task TeamLoeschenAsync(int id, int handelnderBenutzerId);
    Task TeamMitgliedHinzufuegenAsync(int teamId, int benutzerId, int handelnderBenutzerId);
    Task TeamMitgliedEntfernenAsync(int teamId, int benutzerId, int handelnderBenutzerId);
}
