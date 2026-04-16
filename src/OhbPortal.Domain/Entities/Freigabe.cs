using OhbPortal.Domain.Enums;

namespace OhbPortal.Domain.Entities;

/// <summary>
/// Freigabegruppe pro Dokument. Pro Gruppe kann festgelegt werden, wie viele
/// Zustimmungen aus den zugewiesenen Benutzern benötigt werden.
/// </summary>
public class FreigabeGruppe
{
    public int Id { get; set; }
    public int DokumentId { get; set; }
    public Dokument Dokument { get; set; } = null!;

    public string Bezeichnung { get; set; } = string.Empty;
    public int Reihenfolge { get; set; }  // für sequentielle Freigaben
    public int BenoetigteZustimmungen { get; set; } = 1;

    public ICollection<FreigabeGruppeMitglied> Mitglieder { get; set; } = new List<FreigabeGruppeMitglied>();
    public ICollection<FreigabeZustimmung> Zustimmungen { get; set; } = new List<FreigabeZustimmung>();
}

public class FreigabeGruppeMitglied
{
    public int Id { get; set; }
    public int FreigabeGruppeId { get; set; }
    public FreigabeGruppe FreigabeGruppe { get; set; } = null!;
    public int BenutzerId { get; set; }
    public Benutzer Benutzer { get; set; } = null!;
}

public class FreigabeZustimmung
{
    public int Id { get; set; }
    public int FreigabeGruppeId { get; set; }
    public FreigabeGruppe FreigabeGruppe { get; set; } = null!;
    public int BenutzerId { get; set; }
    public Benutzer Benutzer { get; set; } = null!;
    public FreigabeEntscheidung Entscheidung { get; set; } = FreigabeEntscheidung.Ausstehend;
    public DateTime? EntschiedenAm { get; set; }
    public string? Kommentar { get; set; }
}
