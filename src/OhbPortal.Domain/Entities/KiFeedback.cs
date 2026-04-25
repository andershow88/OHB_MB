namespace OhbPortal.Domain.Entities;

public class KiFeedback
{
    public int Id { get; set; }
    public int? BenutzerId { get; set; }
    public string FrageInitial { get; set; } = "";
    public string AntwortLetzte { get; set; } = "";
    public KiFeedbackBewertung Bewertung { get; set; }
    public DateTime ZeitstempelUtc { get; set; } = DateTime.UtcNow;
    public string? ModellName { get; set; }
}

public enum KiFeedbackBewertung
{
    Negativ = 0,
    Positiv = 1
}
