using OhbPortal.Domain.Enums;

namespace OhbPortal.Domain.Entities;

public class AuditEintrag
{
    public int Id { get; set; }
    public AuditTyp Typ { get; set; }
    public DateTime Zeitpunkt { get; set; } = DateTime.UtcNow;

    public int? DokumentId { get; set; }
    public Dokument? Dokument { get; set; }

    public int? KapitelId { get; set; }
    public Kapitel? Kapitel { get; set; }

    public int BenutzerId { get; set; }
    public Benutzer Benutzer { get; set; } = null!;

    public string? Beschreibung { get; set; }
}
