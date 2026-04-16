using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.DTOs;

public record DokumentListeDto(
    int Id,
    string Titel,
    string? Kurzbeschreibung,
    string KapitelPfad,
    string? VerantwortlicherBereich,
    DokumentStatus Status,
    int AktuelleVersion,
    DateTime GeaendertAm,
    string GeaendertVon,
    DateTime? Pruefterm,
    string? Tags,
    bool Archiviert);

public record DokumentDetailDto(
    int Id,
    string Titel,
    string? Kurzbeschreibung,
    string? InhaltHtml,
    int KapitelId,
    string KapitelPfad,
    int? VerantwortlicherBereichId,
    string? VerantwortlicherBereich,
    DokumentStatus Status,
    DateTime ErstelltAm,
    string ErstelltVon,
    DateTime GeaendertAm,
    string GeaendertVon,
    DateTime? SichtbarAb,
    DateTime? SichtbarBis,
    DateTime? Pruefterm,
    string? Kategorie,
    string? Tags,
    int AktuelleVersion,
    bool Archiviert,
    bool Geloescht,
    FreigabeModus FreigabeModus,
    FreigabeReihenfolge FreigabeReihenfolge,
    bool Druckverbot,
    bool OeffentlichLesbar,
    IReadOnlyList<AnhangDto> Anhaenge,
    IReadOnlyList<DokumentLinkDto> Verlinkungen);

public record AnhangDto(int Id, string Dateiname, string ContentType, long Bytes, string HochgeladenVon, DateTime HochgeladenAm);
public record DokumentLinkDto(int Id, int ZielDokumentId, string ZielTitel, string? Bezeichnung);

public record DokumentErstellenDto(
    string Titel,
    string? Kurzbeschreibung,
    int KapitelId,
    int? VerantwortlicherBereichId,
    string? Kategorie,
    string? Tags,
    DateTime? SichtbarAb,
    DateTime? SichtbarBis,
    DateTime? Pruefterm,
    string? InhaltHtml,
    FreigabeModus FreigabeModus);

public record DokumentBearbeitenDto(
    string Titel,
    string? Kurzbeschreibung,
    int KapitelId,
    int? VerantwortlicherBereichId,
    string? Kategorie,
    string? Tags,
    DateTime? SichtbarAb,
    DateTime? SichtbarBis,
    DateTime? Pruefterm,
    string? InhaltHtml,
    FreigabeModus FreigabeModus,
    FreigabeReihenfolge FreigabeReihenfolge,
    bool Druckverbot,
    bool OeffentlichLesbar);

public record DokumentFilterDto(
    string? Suchbegriff = null,
    int? KapitelId = null,
    DokumentStatus? Status = null,
    string? Kategorie = null,
    bool? NurMitPruefterminAbgelaufen = null,
    bool IncludeArchiviert = false,
    bool NurGeloescht = false);

public record VersionDto(
    int Id,
    int Versionsnummer,
    string? Titel,
    DateTime ErstelltAm,
    string ErstelltVon,
    DokumentStatus StatusZumZeitpunkt,
    string? AenderungsHinweis);

public record AuditDto(
    int Id,
    AuditTyp Typ,
    DateTime Zeitpunkt,
    string Benutzer,
    string? Beschreibung);

// Kapitel

public record KapitelBaumDto(
    int Id,
    string Titel,
    string? Icon,
    int? ElternId,
    int Tiefe,
    int DokumentenAnzahl,
    IReadOnlyList<KapitelBaumDto> Unterkapitel);

public record KapitelDto(int Id, string Titel, string? Beschreibung, string? Icon, int? ElternId);

// Freigabe

public record OffeneFreigabeDto(
    int FreigabeGruppeId,
    int DokumentId,
    string DokumentTitel,
    string GruppenBezeichnung,
    int BenoetigteZustimmungen,
    int BereitsZugestimmt,
    DateTime DokumentGeaendertAm);

public record FreigabeGruppeDto(
    int Id,
    string Bezeichnung,
    int Reihenfolge,
    int BenoetigteZustimmungen,
    IReadOnlyList<string> Mitglieder,
    IReadOnlyList<FreigabeZustimmungDto> Zustimmungen,
    FreigabeEntscheidung Gesamtstatus);

public record FreigabeZustimmungDto(
    int Id,
    string Benutzer,
    FreigabeEntscheidung Entscheidung,
    DateTime? EntschiedenAm,
    string? Kommentar);

// Kenntnisnahmen

public record KenntnisnahmeDto(
    int Id,
    string Ziel,  // "Benutzer: Max Mustermann" oder "Team: IT-Service"
    KenntnisnahmeStatus Status,
    DateTime? Faelligkeit,
    DateTime? BestaetigtAm,
    string? BestaetigtVon);

public record KenntnisnahmeOffenDto(
    int Id,
    int DokumentId,
    string DokumentTitel,
    DateTime? Faelligkeit,
    KenntnisnahmeStatus Status);

// Dashboard

public record DashboardDto(
    int AnzahlDokumente,
    int AnzahlEntwuerfe,
    int AnzahlFreigegeben,
    int AnzahlInFreigabe,
    int MeineOffenenFreigaben,
    int MeineOffenenKenntnisnahmen,
    int UeberfaelligePrueftermine,
    IReadOnlyList<DokumentListeDto> LetzteAenderungen);
