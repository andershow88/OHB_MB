using OhbPortal.Domain.Enums;

namespace OhbPortal.Domain.Entities;

public class Benutzer
{
    public int Id { get; set; }
    public string Benutzername { get; set; } = string.Empty;
    public string PasswortHash { get; set; } = string.Empty;
    public string Anzeigename { get; set; } = string.Empty;
    public string EMail { get; set; } = string.Empty;
    public Rolle Rolle { get; set; } = Rolle.Reader;
    public bool IstAktiv { get; set; } = true;
    public DateTime ErstelltAm { get; set; } = DateTime.UtcNow;

    public ICollection<BenutzerTeam> Teams { get; set; } = new List<BenutzerTeam>();
}

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Beschreibung { get; set; }
    public bool IstAktiv { get; set; } = true;

    public ICollection<BenutzerTeam> Mitglieder { get; set; } = new List<BenutzerTeam>();
}

public class BenutzerTeam
{
    public int BenutzerId { get; set; }
    public Benutzer Benutzer { get; set; } = null!;
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
}
