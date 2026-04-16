using OhbPortal.Domain.Enums;

namespace OhbPortal.Domain.Entities;

/// <summary>
/// ACL-artige Berechtigung pro Dokument. Subject kann ein Benutzer, ein Team oder eine globale Rolle sein.
/// Flexibel für spätere Erweiterung: Vererbung entlang Themenbaum kann später ergänzt werden.
/// </summary>
public class DokumentBerechtigung
{
    public int Id { get; set; }
    public int DokumentId { get; set; }
    public Dokument Dokument { get; set; } = null!;

    public int? BenutzerId { get; set; }
    public Benutzer? Benutzer { get; set; }
    public int? TeamId { get; set; }
    public Team? Team { get; set; }
    public Rolle? Rolle { get; set; }

    public BerechtigungsTyp Typ { get; set; } = BerechtigungsTyp.Lesen;
}
