using System.ComponentModel.DataAnnotations;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.DTOs;

public record BenutzerListeDto(
    int Id,
    string Benutzername,
    string Anzeigename,
    string EMail,
    Rolle Rolle,
    bool IstAktiv,
    int AnzahlTeams,
    DateTime ErstelltAm);

public record BenutzerDetailDto(
    int Id,
    string Benutzername,
    string Anzeigename,
    string EMail,
    Rolle Rolle,
    bool IstAktiv,
    IReadOnlyList<TeamKurzDto> Teams);

public record TeamKurzDto(int Id, string Name);

public class BenutzerAnlegenEingabe
{
    [Required, MinLength(3), MaxLength(80)]
    [Display(Name = "Benutzername (für Login)")]
    public string Benutzername { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [Display(Name = "Anzeigename")]
    public string Anzeigename { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    [Display(Name = "E-Mail")]
    public string EMail { get; set; } = string.Empty;

    [Required, MinLength(6)]
    [DataType(DataType.Password)]
    [Display(Name = "Initial-Passwort")]
    public string Passwort { get; set; } = string.Empty;

    [Display(Name = "Rolle")]
    public Rolle Rolle { get; set; } = Rolle.Reader;
}

public class BenutzerBearbeitenEingabe
{
    [Required, MaxLength(200)]
    [Display(Name = "Anzeigename")]
    public string Anzeigename { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    [Display(Name = "E-Mail")]
    public string EMail { get; set; } = string.Empty;

    [Display(Name = "Rolle")]
    public Rolle Rolle { get; set; }

    [Display(Name = "Aktiv")]
    public bool IstAktiv { get; set; } = true;
}

public record TeamListeDto(
    int Id,
    string Name,
    string? Beschreibung,
    int AnzahlMitglieder,
    bool IstAktiv);

public record TeamDetailDto(
    int Id,
    string Name,
    string? Beschreibung,
    bool IstAktiv,
    IReadOnlyList<BenutzerKurzDto> Mitglieder);

public record BenutzerKurzDto(int Id, string Anzeigename, Rolle Rolle);

public class TeamEingabe
{
    [Required, MaxLength(150)]
    [Display(Name = "Teamname")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Beschreibung")]
    public string? Beschreibung { get; set; }

    [Display(Name = "Aktiv")]
    public bool IstAktiv { get; set; } = true;
}

public record AdminDashboardDto(
    int AnzahlBenutzer,
    int AnzahlBenutzerAktiv,
    int AnzahlTeams,
    int AnzahlTeamsAktiv,
    int AnzahlDokumente,
    IReadOnlyList<BenutzerListeDto> ZuletztAngelegteBenutzer);
