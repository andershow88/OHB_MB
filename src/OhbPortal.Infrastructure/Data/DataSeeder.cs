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

        // Nachträglich einspielbare Richtlinien/Strategie aus importierten PDFs
        // (Personendaten anonymisiert). Nur wenn noch nicht vorhanden.
        if (!await db.Dokumente.AnyAsync(d => d.Titel.Contains("Minimales-Risiko")))
            await SeedImportierteRichtlinienAsync(db);
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

    private static async Task SeedImportierteRichtlinienAsync(ApplicationDbContext db)
    {
        var pwd = Hash("Demo1234!");

        async Task<Benutzer> EnsureUser(string un, string anz, Rolle r)
        {
            var u = await db.Benutzer.FirstOrDefaultAsync(b => b.Benutzername == un);
            if (u is null)
            {
                u = new Benutzer
                {
                    Benutzername = un,
                    PasswortHash = pwd,
                    Anzeigename = anz,
                    EMail = $"{un}@ohb.local",
                    Rolle = r
                };
                db.Benutzer.Add(u);
                await db.SaveChangesAsync();
            }
            return u;
        }

        var mustermann = await EnsureUser("m.mustermann", "Max Mustermann", Rolle.Editor);
        var schmidt = await EnsureUser("h.schmidt", "Hans Schmidt", Rolle.Bereichsverantwortlicher);
        var weber = await EnsureUser("t.weber", "Thomas Weber", Rolle.Editor);

        var risikoSteuerung = await db.Kapitel
            .FirstAsync(k => k.Titel == "Unternehmens- und Risikosteuerung" && k.ElternKapitelId == null);
        var strategien = await db.Kapitel
            .FirstOrDefaultAsync(k => k.Titel == "Strategien" && k.ElternKapitelId == risikoSteuerung.Id);
        if (strategien is null)
        {
            strategien = new Kapitel
            {
                Titel = "Strategien",
                Icon = "bi-compass",
                Sortierung = 0,
                ElternKapitelId = risikoSteuerung.Id
            };
            db.Kapitel.Add(strategien);
            await db.SaveChangesAsync();
        }

        var unternehmensentwicklung = await db.Kapitel
            .Include(k => k.ElternKapitel)
            .FirstAsync(k => k.Titel == "Unternehmensentwicklung"
                && k.ElternKapitel != null
                && k.ElternKapitel.Titel == "Sachgebiete / Anweisungen");

        var teamUE = await db.Teams.FirstOrDefaultAsync(t => t.Name == "Unternehmensentwicklung");

        async Task Anlegen(
            string titel,
            string? kurz,
            int kapitelId,
            int? bereichId,
            string kategorie,
            string tags,
            Benutzer autor,
            DateTime stand,
            string? aenderungsNotiz,
            string inhaltHtml)
        {
            stand = DateTime.SpecifyKind(stand, DateTimeKind.Utc);
            var d = new Dokument
            {
                Titel = titel,
                Kurzbeschreibung = kurz,
                InhaltHtml = inhaltHtml,
                KapitelId = kapitelId,
                VerantwortlicherBereichId = bereichId,
                Status = DokumentStatus.Freigegeben,
                ErstelltVonId = autor.Id,
                GeaendertVonId = autor.Id,
                Kategorie = kategorie,
                Tags = tags,
                ErstelltAm = stand.AddDays(-1),
                GeaendertAm = stand,
                AktuelleVersion = 1,
                FreigabeModus = FreigabeModus.Keine,
                OeffentlichLesbar = true,
                SichtbarAb = stand.AddDays(-1),
                Pruefterm = stand.AddYears(1)
            };
            db.Dokumente.Add(d);
            await db.SaveChangesAsync();

            db.DokumentVersionen.Add(new DokumentVersion
            {
                DokumentId = d.Id,
                Versionsnummer = 1,
                Titel = d.Titel,
                Kurzbeschreibung = d.Kurzbeschreibung,
                InhaltHtml = d.InhaltHtml,
                StatusZumZeitpunkt = d.Status,
                ErstelltAm = stand,
                ErstelltVonId = autor.Id,
                AenderungsHinweis = aenderungsNotiz
            });

            db.AuditEintraege.Add(new AuditEintrag
            {
                DokumentId = d.Id,
                BenutzerId = autor.Id,
                Typ = AuditTyp.DokumentErstellt,
                Beschreibung = "Import aus PDF (anonymisiert)",
                Zeitpunkt = stand.AddDays(-1)
            });
            db.AuditEintraege.Add(new AuditEintrag
            {
                DokumentId = d.Id,
                BenutzerId = autor.Id,
                Typ = AuditTyp.VersionAngelegt,
                Beschreibung = "Version 1",
                Zeitpunkt = stand.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        await Anlegen(
            "Richtlinie für den Einsatz von Künstlicher Intelligenz (KI) – Minimales-Risiko-Anwendungsfälle",
            "Grundsätze für den Einsatz von Minimales-Risiko-KI-Systemen gemäß EU AI Act.",
            unternehmensentwicklung.Id,
            teamUE?.Id,
            "Richtlinie",
            "ki,ai,minimales-risiko,eu-ai-act,richtlinie",
            mustermann,
            new DateTime(2025, 4, 14),
            "Neuerstellung im Rahmen des LeasiNetWeb-Projekts",
            ImportierteRichtlinienDaten.KiRichtlinieHtml);

        await Anlegen(
            "IT-/DOR-Strategie",
            "Strategische Ausrichtung der IT und der digitalen operationalen Resilienz gemäß DORA und MaRisk.",
            strategien.Id,
            teamUE?.Id,
            "Strategie",
            "it,dor,strategie,dora,marisk,resilienz",
            schmidt,
            new DateTime(2026, 1, 28),
            "Tz 3.1: Erweiterungen IT-Kompetenzen · Tz 3.4: erweiterte Budgets DOR · Tz 6.1: Ergänzungen Risiko-/Auswirkungstoleranz",
            ImportierteRichtlinienDaten.ItDorStrategieHtml);

        await Anlegen(
            "Richtlinie für Eigenprogrammierungen",
            "Vorgaben zu Anforderungsprozess, Entwicklung, Test, Abnahme und Betrieb von Eigenanwendungen.",
            unternehmensentwicklung.Id,
            teamUE?.Id,
            "Richtlinie",
            "eigenprogrammierung,entwicklung,release,test,abnahme",
            mustermann,
            new DateTime(2024, 10, 14),
            "zu 1.2.1 Packages/nuget · zu 1.3.1 Tests/Testdokumentation · zu 1.6 Sonstiges neu aufgenommen",
            ImportierteRichtlinienDaten.EigenprogrammierungenHtml);

        await Anlegen(
            "Richtlinie für RPA Entwicklungen",
            "Vorgaben für die Umsetzung von Robotic-Process-Automation-Prozessen mit UiPath.",
            unternehmensentwicklung.Id,
            teamUE?.Id,
            "Richtlinie",
            "rpa,uipath,robotic-process-automation,automatisierung",
            weber,
            new DateTime(2022, 12, 14),
            "Neuerstellung der Geschäftsanweisung",
            ImportierteRichtlinienDaten.RpaRichtlinieHtml);

        await Anlegen(
            "Richtlinie für Entwicklung und Hosting von Anwendungen durch externe Dritte",
            "Anforderungen an Beauftragung, Entwicklung, Hosting, Test, Abnahme und Betrieb durch externe Dienstleister.",
            unternehmensentwicklung.Id,
            teamUE?.Id,
            "Richtlinie",
            "externe-entwicklung,hosting,dienstleister,auslagerung",
            mustermann,
            new DateTime(2023, 7, 7),
            "Neuerstellung",
            ImportierteRichtlinienDaten.ExterneEntwicklungHtml);
    }
}
