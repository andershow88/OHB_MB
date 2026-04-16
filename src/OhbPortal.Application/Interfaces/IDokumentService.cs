using OhbPortal.Application.DTOs;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Interfaces;

public interface IDokumentService
{
    Task<IEnumerable<DokumentListeDto>> GetAlleAsync(DokumentFilterDto filter);
    Task<DokumentDetailDto?> GetDetailAsync(int id);
    Task<int> ErstellenAsync(DokumentErstellenDto dto, int benutzerId);
    Task AktualisierenAsync(int id, DokumentBearbeitenDto dto, int benutzerId, string? aenderungshinweis = null);
    Task StatusAendernAsync(int id, DokumentStatus neuerStatus, int benutzerId, string? notiz = null);
    Task ArchivierenAsync(int id, int benutzerId);
    Task WiederherstellenAsync(int id, int benutzerId);
    Task InPapierkorbVerschiebenAsync(int id, int benutzerId);
    Task EndgueltigLoeschenAsync(int id, int benutzerId);

    // Versionierung
    Task<IEnumerable<VersionDto>> GetVersionenAsync(int dokumentId);
    // Audit
    Task<IEnumerable<AuditDto>> GetAuditAsync(int dokumentId);
}

public interface IKapitelService
{
    Task<IEnumerable<KapitelBaumDto>> GetBaumAsync();
    Task<KapitelDto?> GetAsync(int id);
    Task<int> AnlegenAsync(string titel, int? elternId, string? beschreibung, string? icon, int benutzerId);
    Task AktualisierenAsync(int id, string titel, string? beschreibung, string? icon, int benutzerId);
    Task LoeschenAsync(int id, int benutzerId);
}

public interface IFreigabeService
{
    Task FreigabeStartenAsync(int dokumentId, int benutzerId);
    Task ZustimmenAsync(int freigabeGruppeId, int benutzerId, string? kommentar = null);
    Task AblehnenAsync(int freigabeGruppeId, int benutzerId, string? kommentar = null);
    Task<IEnumerable<OffeneFreigabeDto>> GetMeineOffenenAsync(int benutzerId);
    Task<IEnumerable<FreigabeGruppeDto>> GetGruppenAsync(int dokumentId);

    // Pflege der Gruppen-Konfiguration
    Task<int> GruppeAnlegenAsync(int dokumentId, string bezeichnung, int reihenfolge, int benoetigteZustimmungen, int benutzerId);
    Task GruppeBearbeitenAsync(int gruppeId, string bezeichnung, int reihenfolge, int benoetigteZustimmungen, int benutzerId);
    Task GruppeLoeschenAsync(int gruppeId, int benutzerId);
    Task MitgliedHinzufuegenAsync(int gruppeId, int zugewiesenerBenutzerId, int handelnderBenutzerId);
    Task MitgliedEntfernenAsync(int mitgliedId, int handelnderBenutzerId);
}

public interface IKenntnisnahmeService
{
    Task ZuweisenBenutzerAsync(int dokumentId, int benutzerId, DateTime? faelligkeit, int handelnderBenutzerId);
    Task ZuweisenTeamAsync(int dokumentId, int teamId, DateTime? faelligkeit, int handelnderBenutzerId);
    Task BestaetigenAsync(int kenntnisnahmeId, int benutzerId);
    Task LoeschenAsync(int kenntnisnahmeId, int handelnderBenutzerId);
    Task<IEnumerable<KenntnisnahmeDto>> GetProDokumentAsync(int dokumentId);
    Task<IEnumerable<KenntnisnahmeOffenDto>> GetMeineOffenenAsync(int benutzerId);
}

public interface IAnhangService
{
    Task<int> HochladenAsync(int dokumentId, Stream inhalt, string dateiname, string contentType, long laenge, int benutzerId);
    Task<(Stream Inhalt, string ContentType, string Dateiname)> HerunterladenAsync(int anhangId);
    Task LoeschenAsync(int anhangId, int benutzerId);
}

public interface IAuditService
{
    Task LogAsync(AuditTyp typ, int benutzerId, int? dokumentId = null, int? kapitelId = null, string? beschreibung = null);
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(int benutzerId);
}
