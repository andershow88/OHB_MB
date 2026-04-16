using OhbPortal.Domain.Enums;

namespace OhbPortal.Domain.Entities;

public class Kenntnisnahme
{
    public int Id { get; set; }
    public int DokumentId { get; set; }
    public Dokument Dokument { get; set; } = null!;

    /// <summary>Ein Kenntnisnahme-Eintrag kann entweder einem einzelnen Benutzer ODER einem Team zugeordnet sein.</summary>
    public int? BenutzerId { get; set; }
    public Benutzer? Benutzer { get; set; }
    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public KenntnisnahmeStatus Status { get; set; } = KenntnisnahmeStatus.Offen;
    public DateTime ZugewiesenAm { get; set; } = DateTime.UtcNow;
    public DateTime? BestaetigtAm { get; set; }
    public int? BestaetigtVonId { get; set; }
    public Benutzer? BestaetigtVon { get; set; }
    public DateTime? Faelligkeit { get; set; }
}
