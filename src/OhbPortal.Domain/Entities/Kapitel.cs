namespace OhbPortal.Domain.Entities;

public class Kapitel
{
    public int Id { get; set; }
    public string Titel { get; set; } = string.Empty;
    public string? Beschreibung { get; set; }
    public int? ElternKapitelId { get; set; }
    public Kapitel? ElternKapitel { get; set; }
    public int Sortierung { get; set; }
    public string? Icon { get; set; }  // Bootstrap-Icon
    public DateTime ErstelltAm { get; set; } = DateTime.UtcNow;
    public DateTime GeaendertAm { get; set; } = DateTime.UtcNow;

    public ICollection<Kapitel> Unterkapitel { get; set; } = new List<Kapitel>();
    public ICollection<Dokument> Dokumente { get; set; } = new List<Dokument>();
}
