using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OhbPortal.Domain.Entities;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.Benutzer.AnyAsync())
            await SeedBenutzerAsync(db);
        if (!await db.Teams.AnyAsync())
            await SeedTeamsAsync(db);
        if (!await db.Kapitel.AnyAsync())
            await SeedKapitelAsync(db);
        if (!await db.Dokumente.AnyAsync())
            await SeedBeispielDokumentAsync(db);
    }

    private static string Hash(string s)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static async Task SeedBenutzerAsync(ApplicationDbContext db)
    {
        var pwd = Hash("Demo1234!");
        var admin = Hash("Admin1234!");
        db.Benutzer.AddRange(
            new Benutzer { Benutzername = "admin", PasswortHash = admin, Anzeigename = "Administrator", EMail = "admin@ohb.local", Rolle = Rolle.Admin },
            new Benutzer { Benutzername = "editor", PasswortHash = pwd, Anzeigename = "Eva Editor", EMail = "editor@ohb.local", Rolle = Rolle.Editor },
            new Benutzer { Benutzername = "approver", PasswortHash = pwd, Anzeigename = "Arne Approver", EMail = "approver@ohb.local", Rolle = Rolle.Approver },
            new Benutzer { Benutzername = "reviewer", PasswortHash = pwd, Anzeigename = "Rita Reviewer", EMail = "reviewer@ohb.local", Rolle = Rolle.Reviewer },
            new Benutzer { Benutzername = "reader", PasswortHash = pwd, Anzeigename = "Rolf Reader", EMail = "reader@ohb.local", Rolle = Rolle.Reader },
            new Benutzer { Benutzername = "bereich", PasswortHash = pwd, Anzeigename = "Bert Bereichsverantwortlicher", EMail = "bereich@ohb.local", Rolle = Rolle.Bereichsverantwortlicher }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedTeamsAsync(ApplicationDbContext db)
    {
        var teams = new[]
        {
            new Team { Name = "IT-Service", Beschreibung = "IT-Betrieb, Infrastruktur, Support" },
            new Team { Name = "Compliance", Beschreibung = "Regulatorische Anforderungen" },
            new Team { Name = "Informationssicherheit" },
            new Team { Name = "Revision und Kontrollen" },
            new Team { Name = "Personal" },
            new Team { Name = "Unternehmensentwicklung" },
            new Team { Name = "Handel" },
            new Team { Name = "Kredit" }
        };
        db.Teams.AddRange(teams);
        await db.SaveChangesAsync();

        // Editor + Bereichsverantwortlicher als Team-Mitglieder IT-Service
        var editor = await db.Benutzer.FirstAsync(b => b.Benutzername == "editor");
        var bereich = await db.Benutzer.FirstAsync(b => b.Benutzername == "bereich");
        var itSvc = teams.First(t => t.Name == "IT-Service");
        db.BenutzerTeams.AddRange(
            new BenutzerTeam { BenutzerId = editor.Id, TeamId = itSvc.Id },
            new BenutzerTeam { BenutzerId = bereich.Id, TeamId = itSvc.Id });
        await db.SaveChangesAsync();
    }

    private static async Task SeedKapitelAsync(ApplicationDbContext db)
    {
        // Hauptkapitel
        var k1 = new Kapitel { Titel = "Unternehmens- und Risikosteuerung", Icon = "bi-diagram-3", Sortierung = 1 };
        var k2 = new Kapitel { Titel = "Sachgebiete / Anweisungen", Icon = "bi-folder2-open", Sortierung = 2 };
        var k3 = new Kapitel { Titel = "Verzeichnisse", Icon = "bi-list-columns", Sortierung = 3 };
        var k4 = new Kapitel { Titel = "Anlagen", Icon = "bi-paperclip", Sortierung = 4 };
        var k5 = new Kapitel { Titel = "Formulare", Icon = "bi-file-earmark-ruled", Sortierung = 5 };

        db.Kapitel.AddRange(k1, k2, k3, k4, k5);
        await db.SaveChangesAsync();

        // Unterkapitel zu "Sachgebiete / Anweisungen"
        var bereiche = new (string Titel, string Icon)[]
        {
            ("Compliance", "bi-shield-check"),
            ("Datenverarbeitung", "bi-database"),
            ("Geldwäsche", "bi-cash-coin"),
            ("Handel", "bi-bar-chart"),
            ("Informationssicherheit", "bi-lock"),
            ("IT-Service", "bi-cpu"),
            ("Kredit", "bi-credit-card"),
            ("Kunde / Konto", "bi-person-badge"),
            ("Marketing", "bi-megaphone"),
            ("Personal", "bi-people"),
            ("Prozess-/Projektmanagement und Organisation", "bi-kanban"),
            ("Revision und Kontrollen", "bi-clipboard-check"),
            ("Sicherheitsmaßnahmen", "bi-shield-lock"),
            ("Unternehmensentwicklung", "bi-graph-up"),
            ("Innovation / Digitalisierung", "bi-lightbulb"),
            ("Verwaltung", "bi-building"),
            ("Wertpapiergeschäft", "bi-coin")
        };
        var sort = 0;
        foreach (var (titel, icon) in bereiche)
        {
            db.Kapitel.Add(new Kapitel { Titel = titel, Icon = icon, Sortierung = sort++, ElternKapitelId = k2.Id });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedBeispielDokumentAsync(ApplicationDbContext db)
    {
        var itSvcKap = await db.Kapitel.FirstOrDefaultAsync(k => k.Titel == "IT-Service");
        if (itSvcKap is null) return;
        var itSvcTeam = await db.Teams.FirstOrDefaultAsync(t => t.Name == "IT-Service");

        var editor = await db.Benutzer.FirstAsync(b => b.Benutzername == "editor");
        var approver = await db.Benutzer.FirstAsync(b => b.Benutzername == "approver");
        var bereich = await db.Benutzer.FirstAsync(b => b.Benutzername == "bereich");
        var reader = await db.Benutzer.FirstAsync(b => b.Benutzername == "reader");

        var inhalt = """
            <h2>Kurzbeschreibung</h2>
            <p>Diese Richtlinie regelt den Einsatz von Fernwartungslösungen zur Unterstützung
            und Administration der Bank-IT durch interne und externe Techniker.</p>

            <h2>Planung und Einsatz von Fernwartungslösungen</h2>
            <ol>
              <li>Jede Fernwartungslösung ist vor dem Einsatz durch die Informationssicherheit freizugeben.</li>
              <li>Es sind ausschließlich von der Bank genehmigte Produkte zulässig.</li>
              <li>Der Einsatz ist zu protokollieren.</li>
            </ol>

            <h2>Bank-interne Nutzung der Fernwartung</h2>
            <p>Die IT-Service-Teams nutzen Fernwartungstools, um Arbeitsplätze und Server zu
            administrieren. Der Zugriff erfordert eine persönliche, namensbasierte Anmeldung und
            Multi-Faktor-Authentifizierung.</p>

            <h2>Externe Nutzung der Fernwartung</h2>
            <p>Externe Dienstleister erhalten Zugriff nur auf Anfrage, zeitlich begrenzt und
            unter Aufsicht eines internen Mitarbeiters. Sessions werden aufgezeichnet.</p>

            <blockquote><strong>Hinweis:</strong> Vor jedem Zugriff ist der Mitarbeiter / Kunde zu
            informieren und seine Zustimmung einzuholen, sofern personenbezogene Daten sichtbar werden.</blockquote>

            <h2>Mitgeltende Dokumente / Links</h2>
            <ul>
              <li>Richtlinie zur Datenverarbeitung</li>
              <li>Richtlinie zur Informationssicherheit</li>
              <li>Policy Multi-Faktor-Authentifizierung</li>
            </ul>
        """;

        var dok = new Dokument
        {
            Titel = "Richtlinie zum Einsatz von Fernwartung",
            Kurzbeschreibung = "Vorgaben für den sicheren Einsatz interner und externer Fernwartungslösungen.",
            InhaltHtml = inhalt,
            KapitelId = itSvcKap.Id,
            VerantwortlicherBereichId = itSvcTeam?.Id,
            Status = DokumentStatus.InFreigabe,
            ErstelltVonId = editor.Id,
            GeaendertVonId = editor.Id,
            Kategorie = "Richtlinie",
            Tags = "fernwartung,it,sicherheit,richtlinie",
            SichtbarAb = DateTime.UtcNow.AddDays(-14),
            Pruefterm = DateTime.UtcNow.AddMonths(12),
            AktuelleVersion = 2,
            FreigabeModus = FreigabeModus.Gruppen,
            FreigabeReihenfolge = FreigabeReihenfolge.Parallel,
            OeffentlichLesbar = true
        };
        db.Dokumente.Add(dok);
        await db.SaveChangesAsync();

        // Versionen
        db.DokumentVersionen.AddRange(
            new DokumentVersion
            {
                DokumentId = dok.Id,
                Versionsnummer = 1,
                Titel = dok.Titel,
                Kurzbeschreibung = dok.Kurzbeschreibung,
                InhaltHtml = "<p>(initiale Version)</p>",
                StatusZumZeitpunkt = DokumentStatus.Entwurf,
                ErstelltAm = DateTime.UtcNow.AddDays(-14),
                ErstelltVonId = editor.Id,
                AenderungsHinweis = "Initiale Fassung"
            },
            new DokumentVersion
            {
                DokumentId = dok.Id,
                Versionsnummer = 2,
                Titel = dok.Titel,
                Kurzbeschreibung = dok.Kurzbeschreibung,
                InhaltHtml = inhalt,
                StatusZumZeitpunkt = DokumentStatus.InFreigabe,
                ErstelltAm = DateTime.UtcNow.AddDays(-1),
                ErstelltVonId = editor.Id,
                AenderungsHinweis = "Kapitel 'Externe Nutzung' praezisiert; Literaturhinweise ergaenzt."
            });
        await db.SaveChangesAsync();

        // Freigabe-Konfiguration: 2 Gruppen, parallel
        var g1 = new FreigabeGruppe
        {
            DokumentId = dok.Id,
            Bezeichnung = "Informationssicherheit",
            Reihenfolge = 1,
            BenoetigteZustimmungen = 1
        };
        var g2 = new FreigabeGruppe
        {
            DokumentId = dok.Id,
            Bezeichnung = "Bereichsverantwortlicher IT",
            Reihenfolge = 1,
            BenoetigteZustimmungen = 1
        };
        db.FreigabeGruppen.AddRange(g1, g2);
        await db.SaveChangesAsync();

        db.FreigabeGruppeMitglieder.AddRange(
            new FreigabeGruppeMitglied { FreigabeGruppeId = g1.Id, BenutzerId = approver.Id },
            new FreigabeGruppeMitglied { FreigabeGruppeId = g2.Id, BenutzerId = bereich.Id });
        await db.SaveChangesAsync();

        // Kenntnisnahmen
        db.Kenntnisnahmen.AddRange(
            new Kenntnisnahme { DokumentId = dok.Id, BenutzerId = reader.Id, Faelligkeit = DateTime.UtcNow.AddDays(14) },
            itSvcTeam is null
                ? new Kenntnisnahme { DokumentId = dok.Id, BenutzerId = editor.Id, Status = KenntnisnahmeStatus.Bestaetigt, BestaetigtAm = DateTime.UtcNow.AddDays(-1), BestaetigtVonId = editor.Id }
                : new Kenntnisnahme { DokumentId = dok.Id, TeamId = itSvcTeam.Id, Faelligkeit = DateTime.UtcNow.AddDays(30) });
        await db.SaveChangesAsync();

        // Audit
        db.AuditEintraege.AddRange(
            new AuditEintrag { DokumentId = dok.Id, BenutzerId = editor.Id, Typ = AuditTyp.DokumentErstellt, Beschreibung = dok.Titel, Zeitpunkt = DateTime.UtcNow.AddDays(-14) },
            new AuditEintrag { DokumentId = dok.Id, BenutzerId = editor.Id, Typ = AuditTyp.VersionAngelegt, Beschreibung = "Version 1", Zeitpunkt = DateTime.UtcNow.AddDays(-14) },
            new AuditEintrag { DokumentId = dok.Id, BenutzerId = editor.Id, Typ = AuditTyp.DokumentBearbeitet, Beschreibung = "Kapitel Externe Nutzung präzisiert", Zeitpunkt = DateTime.UtcNow.AddDays(-1) },
            new AuditEintrag { DokumentId = dok.Id, BenutzerId = editor.Id, Typ = AuditTyp.VersionAngelegt, Beschreibung = "Version 2", Zeitpunkt = DateTime.UtcNow.AddDays(-1) },
            new AuditEintrag { DokumentId = dok.Id, BenutzerId = editor.Id, Typ = AuditTyp.FreigabeGestartet, Beschreibung = "Workflow mit 2 Gruppen gestartet", Zeitpunkt = DateTime.UtcNow.AddHours(-3) });
        await db.SaveChangesAsync();
    }
}
