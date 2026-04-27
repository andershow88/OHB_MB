using OhbPortal.Domain.Enums;

namespace OhbPortal.Domain.Entities;

public class Dokument
{
    public int Id { get; set; }
    public string Titel { get; set; } = string.Empty;
    public string? Kurzbeschreibung { get; set; }
    public string? InhaltHtml { get; set; }

    public int KapitelId { get; set; }
    public Kapitel Kapitel { get; set; } = null!;

    public int? VerantwortlicherBereichId { get; set; }
    public Team? VerantwortlicherBereich { get; set; }

    public DokumentStatus Status { get; set; } = DokumentStatus.Entwurf;

    public DateTime ErstelltAm { get; set; } = DateTime.UtcNow;
    public int ErstelltVonId { get; set; }
    public Benutzer ErstelltVon { get; set; } = null!;
    public DateTime GeaendertAm { get; set; } = DateTime.UtcNow;
    public int GeaendertVonId { get; set; }
    public Benutzer GeaendertVon { get; set; } = null!;

    public DateTime? SichtbarAb { get; set; }
    public DateTime? SichtbarBis { get; set; }
    public DateTime? Pruefterm { get; set; }

    public string? Kategorie { get; set; }
    public string? Tags { get; set; }  // Komma-separiert (Suche/Filter)

    public int AktuelleVersion { get; set; } = 1;

    /// <summary>Sortierung innerhalb des Kapitels (Drag-and-Drop-Reihenfolge).</summary>
    public int Sortierung { get; set; }

    public bool Archiviert { get; set; }
    public DateTime? ArchiviertAm { get; set; }

    public bool Geloescht { get; set; }  // Soft-Delete (Papierkorb)
    public DateTime? GeloeschtAm { get; set; }

    // Freigabe-Konfiguration
    public FreigabeModus FreigabeModus { get; set; } = FreigabeModus.Keine;
    public FreigabeReihenfolge FreigabeReihenfolge { get; set; } = FreigabeReihenfolge.Parallel;

    // Sicherheit
    public bool Druckverbot { get; set; }
    public bool OeffentlichLesbar { get; set; } = true;

    public ICollection<DokumentVersion> Versionen { get; set; } = new List<DokumentVersion>();
    public ICollection<DokumentLink> Verlinkungen { get; set; } = new List<DokumentLink>();
    public ICollection<Anhang> Anhaenge { get; set; } = new List<Anhang>();
    public ICollection<FreigabeGruppe> FreigabeGruppen { get; set; } = new List<FreigabeGruppe>();
    public ICollection<Kenntnisnahme> Kenntnisnahmen { get; set; } = new List<Kenntnisnahme>();
    public ICollection<DokumentBerechtigung> Berechtigungen { get; set; } = new List<DokumentBerechtigung>();
    public ICollection<AuditEintrag> AuditEintraege { get; set; } = new List<AuditEintrag>();
}

public class DokumentVersion
{
    public int Id { get; set; }
    public int DokumentId { get; set; }
    public Dokument Dokument { get; set; } = null!;
    public int Versionsnummer { get; set; }
    public string? Titel { get; set; }
    public string? Kurzbeschreibung { get; set; }
    public string? InhaltHtml { get; set; }
    public DokumentStatus StatusZumZeitpunkt { get; set; }
    public DateTime ErstelltAm { get; set; } = DateTime.UtcNow;
    public int ErstelltVonId { get; set; }
    public Benutzer ErstelltVon { get; set; } = null!;
    public string? AenderungsHinweis { get; set; }
}

public class DokumentLink
{
    public int Id { get; set; }
    public int DokumentId { get; set; }
    public Dokument Dokument { get; set; } = null!;
    public int ZielDokumentId { get; set; }
    public Dokument ZielDokument { get; set; } = null!;
    public string? Bezeichnung { get; set; }
}

public class Anhang
{
    public int Id { get; set; }
    public int DokumentId { get; set; }
    public Dokument Dokument { get; set; } = null!;
    public string Dateiname { get; set; } = string.Empty;
    public string SpeicherSchluessel { get; set; } = string.Empty;  // Pfad im FileStorage
    public string ContentType { get; set; } = string.Empty;
    public long DateigroesseBytes { get; set; }
    public DateTime HochgeladenAm { get; set; } = DateTime.UtcNow;
    public int HochgeladenVonId { get; set; }
    public Benutzer HochgeladenVon { get; set; } = null!;
}
