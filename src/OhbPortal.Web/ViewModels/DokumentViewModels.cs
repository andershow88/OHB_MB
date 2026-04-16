using System.ComponentModel.DataAnnotations;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Web.ViewModels;

public class DokumentBearbeitenViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Titel ist erforderlich")]
    [Display(Name = "Titel")]
    public string Titel { get; set; } = string.Empty;

    [Display(Name = "Kurzbeschreibung")]
    public string? Kurzbeschreibung { get; set; }

    [Required]
    [Display(Name = "Kapitel")]
    public int KapitelId { get; set; }

    [Display(Name = "Verantwortlicher Bereich")]
    public int? VerantwortlicherBereichId { get; set; }

    [Display(Name = "Kategorie")]
    public string? Kategorie { get; set; }

    [Display(Name = "Schlagworte (kommagetrennt)")]
    public string? Tags { get; set; }

    [Display(Name = "Sichtbar ab")]
    [DataType(DataType.Date)]
    public DateTime? SichtbarAb { get; set; }

    [Display(Name = "Sichtbar bis")]
    [DataType(DataType.Date)]
    public DateTime? SichtbarBis { get; set; }

    [Display(Name = "Prüftermin")]
    [DataType(DataType.Date)]
    public DateTime? Pruefterm { get; set; }

    [Display(Name = "Inhalt")]
    public string? InhaltHtml { get; set; }

    [Display(Name = "Freigabe-Modus")]
    public FreigabeModus FreigabeModus { get; set; } = FreigabeModus.Keine;

    [Display(Name = "Freigabe-Reihenfolge")]
    public FreigabeReihenfolge FreigabeReihenfolge { get; set; } = FreigabeReihenfolge.Parallel;

    [Display(Name = "Druckverbot")]
    public bool Druckverbot { get; set; }

    [Display(Name = "Öffentlich lesbar")]
    public bool OeffentlichLesbar { get; set; } = true;

    public string? AenderungsHinweis { get; set; }
}
