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

        if (!await db.Dokumente.AnyAsync(d => d.Titel.Contains("Geldwäscheprävention")))
            await SeedDemoDokumenteAsync(db);
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

    private static async Task SeedDemoDokumenteAsync(ApplicationDbContext db)
    {
        var editor = await db.Benutzer.FirstAsync(b => b.Benutzername == "editor");
        var approver = await db.Benutzer.FirstAsync(b => b.Benutzername == "approver");
        var bereich = await db.Benutzer.FirstAsync(b => b.Benutzername == "bereich");
        var admin = await db.Benutzer.FirstAsync(b => b.Benutzername == "admin");

        var pwd = Hash("Demo1234!");
        async Task<Benutzer> Usr(string un, string anz, Rolle r)
        {
            var u = await db.Benutzer.FirstOrDefaultAsync(b => b.Benutzername == un);
            if (u is null) { u = new Benutzer { Benutzername = un, PasswortHash = pwd, Anzeigename = anz, EMail = $"{un}@ohb.local", Rolle = r }; db.Benutzer.Add(u); await db.SaveChangesAsync(); }
            return u;
        }

        var mueller = await Usr("s.mueller", "Sabine Müller", Rolle.Editor);
        var fischer = await Usr("k.fischer", "Klaus Fischer", Rolle.Approver);
        var lang = await Usr("a.lang", "Andrea Lang", Rolle.Editor);
        var wagner = await Usr("m.wagner", "Michael Wagner", Rolle.Bereichsverantwortlicher);

        var kapitel = await db.Kapitel.Include(k => k.ElternKapitel).ToListAsync();
        int Kap(string titel) => kapitel.First(k => k.Titel == titel && k.ElternKapitelId != null).Id;
        int HauptKap(string titel) => kapitel.First(k => k.Titel == titel && k.ElternKapitelId == null).Id;

        var teams = await db.Teams.ToListAsync();
        int? Team(string name) => teams.FirstOrDefault(t => t.Name == name)?.Id;

        async Task<Dokument> Dok(string titel, string kurz, int kapitelId, int? teamId,
            string kategorie, string tags, Benutzer autor, DateTime stand, string inhalt)
        {
            stand = DateTime.SpecifyKind(stand, DateTimeKind.Utc);
            var d = new Dokument
            {
                Titel = titel, Kurzbeschreibung = kurz, InhaltHtml = inhalt,
                KapitelId = kapitelId, VerantwortlicherBereichId = teamId,
                Status = DokumentStatus.Freigegeben,
                ErstelltVonId = autor.Id, GeaendertVonId = autor.Id,
                Kategorie = kategorie, Tags = tags,
                ErstelltAm = stand.AddDays(-7), GeaendertAm = stand,
                AktuelleVersion = 1, FreigabeModus = FreigabeModus.VierAugen,
                OeffentlichLesbar = true, SichtbarAb = stand, Pruefterm = stand.AddYears(1)
            };
            db.Dokumente.Add(d);
            await db.SaveChangesAsync();

            db.DokumentVersionen.Add(new DokumentVersion
            {
                DokumentId = d.Id, Versionsnummer = 1, Titel = d.Titel,
                Kurzbeschreibung = d.Kurzbeschreibung, InhaltHtml = d.InhaltHtml,
                StatusZumZeitpunkt = DokumentStatus.Freigegeben,
                ErstelltAm = stand, ErstelltVonId = autor.Id,
                AenderungsHinweis = "Freigabe erteilt"
            });

            db.AuditEintraege.AddRange(
                new AuditEintrag { DokumentId = d.Id, BenutzerId = autor.Id, Typ = AuditTyp.DokumentErstellt, Beschreibung = titel, Zeitpunkt = stand.AddDays(-7) },
                new AuditEintrag { DokumentId = d.Id, BenutzerId = autor.Id, Typ = AuditTyp.VersionAngelegt, Beschreibung = "Version 1", Zeitpunkt = stand.AddDays(-7) },
                new AuditEintrag { DokumentId = d.Id, BenutzerId = autor.Id, Typ = AuditTyp.FreigabeGestartet, Beschreibung = "4-Augen-Freigabe gestartet", Zeitpunkt = stand.AddDays(-3) },
                new AuditEintrag { DokumentId = d.Id, BenutzerId = fischer.Id, Typ = AuditTyp.FreigabeZugestimmt, Beschreibung = "Freigabe erteilt", Zeitpunkt = stand.AddDays(-1) },
                new AuditEintrag { DokumentId = d.Id, BenutzerId = fischer.Id, Typ = AuditTyp.FreigabeAbgeschlossen, Beschreibung = "Dokument freigegeben", Zeitpunkt = stand.AddDays(-1) }
            );
            await db.SaveChangesAsync();
            return d;
        }

        // === COMPLIANCE ===
        await Dok("Geschäftsanweisung Geldwäscheprävention", "Organisatorische und prozessuale Vorgaben zur Verhinderung von Geldwäsche und Terrorismusfinanzierung gemäß GwG.",
            Kap("Geldwäsche"), Team("Compliance"), "Geschäftsanweisung", "geldwäsche,gwg,kyc,compliance,terrorismusfinanzierung",
            mueller, new DateTime(2025, 9, 15),
            @"<h2>1. Zweck und Geltungsbereich</h2>
<p>Diese Geschäftsanweisung regelt die organisatorischen und prozessualen Maßnahmen der Merkur Privatbank zur Verhinderung von Geldwäsche und Terrorismusfinanzierung gemäß den Anforderungen des Geldwäschegesetzes (GwG).</p>
<p>Die Anweisung gilt für alle Mitarbeiter der Bank, die in direktem oder indirektem Kundenkontakt stehen oder an der Abwicklung von Transaktionen beteiligt sind.</p>

<h2>2. Verantwortlichkeiten</h2>
<h3>2.1 Geldwäschebeauftragter</h3>
<p>Der Geldwäschebeauftragte ist zentrale Ansprechperson für alle Fragen zur Geldwäscheprävention. Er berichtet direkt an die Geschäftsleitung und ist weisungsfrei in der Ausübung seiner Tätigkeit.</p>
<h3>2.2 Mitarbeiter</h3>
<p>Jeder Mitarbeiter ist verpflichtet, verdächtige Transaktionen und Verhaltensweisen unverzüglich dem Geldwäschebeauftragten zu melden.</p>

<h2>3. Know-Your-Customer (KYC)</h2>
<h3>3.1 Identifizierung</h3>
<p>Vor Beginn einer Geschäftsbeziehung ist die Identität des Kunden anhand eines gültigen amtlichen Ausweises zu verifizieren. Bei juristischen Personen sind zusätzlich Handelsregisterauszug und Gesellschafterstruktur zu prüfen.</p>
<h3>3.2 Wirtschaftlich Berechtigte</h3>
<p>Die wirtschaftlich Berechtigten sind zu ermitteln und zu dokumentieren. Eine natürliche Person gilt als wirtschaftlich berechtigt, wenn sie direkt oder indirekt mehr als 25% der Anteile hält oder Kontrolle ausübt.</p>
<h3>3.3 Laufende Überwachung</h3>
<p>Die Geschäftsbeziehung und durchgeführte Transaktionen sind laufend zu überwachen. KYC-Daten sind mindestens alle zwei Jahre zu aktualisieren, bei erhöhtem Risiko jährlich.</p>

<h2>4. Verdachtsmeldung</h2>
<p>Bei Verdacht auf Geldwäsche oder Terrorismusfinanzierung ist unverzüglich eine interne Meldung an den Geldwäschebeauftragten zu erstatten. Dieser prüft den Sachverhalt und entscheidet über die Abgabe einer Verdachtsmeldung an die FIU.</p>
<blockquote><strong>Wichtig:</strong> Das sogenannte Tipping-Off-Verbot ist strikt einzuhalten. Der Kunde darf nicht über die Verdachtsmeldung informiert werden.</blockquote>

<h2>5. Schulung</h2>
<p>Alle Mitarbeiter sind bei Eintritt und danach mindestens jährlich zum Thema Geldwäscheprävention zu schulen. Die Teilnahme ist zu dokumentieren.</p>

<h2>6. Mitgeltende Dokumente</h2>
<ul><li>Risikoanalyse Geldwäsche</li><li>Verfahrensanweisung KYC-Prozess</li><li>Formular Verdachtsmeldung</li></ul>");

        await Dok("Compliance-Richtlinie", "Grundsätze zur Einhaltung regulatorischer und gesetzlicher Anforderungen.",
            Kap("Compliance"), Team("Compliance"), "Richtlinie", "compliance,wphg,mifid,regelkonformität",
            mueller, new DateTime(2025, 6, 1),
            @"<h2>1. Zweck</h2>
<p>Diese Richtlinie definiert den Rahmen für die Compliance-Organisation der Merkur Privatbank. Sie stellt sicher, dass alle geschäftlichen Aktivitäten im Einklang mit den geltenden Gesetzen, Verordnungen und internen Regelungen durchgeführt werden.</p>

<h2>2. Compliance-Funktion</h2>
<p>Die Compliance-Abteilung überwacht die Einhaltung der regulatorischen Anforderungen, insbesondere:</p>
<ul><li>Wertpapierhandelsgesetz (WpHG) und MiFID II</li><li>Kreditwesengesetz (KWG)</li><li>Datenschutz-Grundverordnung (DSGVO)</li><li>Geldwäschegesetz (GwG)</li></ul>

<h2>3. Interessenkonflikte</h2>
<p>Potenzielle Interessenkonflikte sind unverzüglich offenzulegen. Mitarbeiter dürfen keine persönlichen Vorteile aus ihrer beruflichen Stellung ziehen. Geschenke und Zuwendungen über 50 EUR sind genehmigungspflichtig.</p>

<h2>4. Mitarbeitergeschäfte</h2>
<p>Eigengeschäfte in Finanzinstrumenten sind vorab bei der Compliance-Abteilung anzuzeigen. Sperrfristen und Handelsverbote sind strikt einzuhalten.</p>

<h2>5. Meldepflichten</h2>
<p>Verstöße gegen Compliance-Vorgaben sind unverzüglich über den Meldeweg an die Compliance-Abteilung zu melden. Ein Hinweisgebersystem steht allen Mitarbeitern vertraulich zur Verfügung.</p>");

        // === INFORMATIONSSICHERHEIT ===
        await Dok("Informationssicherheitsleitlinie", "Übergeordnete Leitlinie zur Informationssicherheit gemäß ISO 27001 und DORA.",
            Kap("Informationssicherheit"), Team("Informationssicherheit"), "Leitlinie", "informationssicherheit,iso27001,dora,isms,leitlinie",
            lang, new DateTime(2025, 3, 20),
            @"<h2>1. Geltungsbereich</h2>
<p>Diese Leitlinie gilt für alle Organisationseinheiten, Mitarbeiter, externe Dienstleister und Systeme der Merkur Privatbank. Sie bildet die Grundlage des Informationssicherheits-Managementsystems (ISMS).</p>

<h2>2. Sicherheitsziele</h2>
<p>Die Merkur Privatbank verfolgt folgende übergeordnete Sicherheitsziele:</p>
<ul><li><strong>Vertraulichkeit:</strong> Schutz von Informationen vor unbefugtem Zugriff</li>
<li><strong>Integrität:</strong> Sicherstellung der Korrektheit und Vollständigkeit von Informationen</li>
<li><strong>Verfügbarkeit:</strong> Gewährleistung des zeitgerechten Zugriffs auf Informationen und Systeme</li></ul>

<h2>3. Organisation</h2>
<p>Der Informationssicherheitsbeauftragte (ISB) berichtet direkt an die Geschäftsleitung. Er ist verantwortlich für die Weiterentwicklung des ISMS, die Durchführung von Risikoanalysen und die Koordination von Sicherheitsmaßnahmen.</p>

<h2>4. Risikomanagement</h2>
<p>Informationssicherheitsrisiken werden systematisch identifiziert, bewertet und behandelt. Die Risikoanalyse wird mindestens jährlich sowie bei wesentlichen Änderungen der IT-Landschaft durchgeführt.</p>

<h2>5. Vorfallmanagement</h2>
<p>Sicherheitsvorfälle sind unverzüglich an den ISB zu melden. Schwerwiegende Vorfälle werden gemäß DORA-Anforderungen an die zuständige Aufsichtsbehörde gemeldet.</p>

<h2>6. Sensibilisierung</h2>
<p>Alle Mitarbeiter werden mindestens jährlich zu Themen der Informationssicherheit geschult. Neue Mitarbeiter erhalten eine Einführungsschulung innerhalb der ersten vier Wochen.</p>");

        await Dok("Passwort- und Zugangsrichtlinie", "Vorgaben für Passwortstärke, Zugangskontrolle und Berechtigungsmanagement.",
            Kap("Informationssicherheit"), Team("Informationssicherheit"), "Richtlinie", "passwort,zugang,authentifizierung,mfa,berechtigungen",
            lang, new DateTime(2025, 8, 10),
            @"<h2>1. Passwortanforderungen</h2>
<p>Für alle Systeme gelten folgende Mindestanforderungen:</p>
<ul><li>Mindestlänge: 12 Zeichen</li><li>Kombination aus Groß-/Kleinbuchstaben, Ziffern und Sonderzeichen</li><li>Maximale Gültigkeit: 90 Tage</li><li>Passworthistorie: Die letzten 12 Passwörter dürfen nicht wiederverwendet werden</li><li>Kontosperrung nach 5 Fehlversuchen für 30 Minuten</li></ul>

<h2>2. Multi-Faktor-Authentifizierung</h2>
<p>Für den Zugang zu kritischen Systemen und bei Remote-Zugriffen ist eine Multi-Faktor-Authentifizierung (MFA) verpflichtend. Akzeptierte zweite Faktoren sind Hardware-Token und Authenticator-Apps.</p>

<h2>3. Berechtigungsmanagement</h2>
<p>Zugriffsrechte werden nach dem Prinzip der minimalen Berechtigung (Least Privilege) vergeben. Die Vergabe privilegierter Rechte bedarf der Genehmigung durch den Bereichsverantwortlichen und den ISB.</p>
<p>Eine Rezertifizierung aller Berechtigungen erfolgt halbjährlich.</p>

<h2>4. Austritt von Mitarbeitern</h2>
<p>Bei Austritt eines Mitarbeiters sind sämtliche Zugänge am letzten Arbeitstag zu deaktivieren. Die Personalabteilung informiert die IT-Abteilung spätestens drei Werktage vor dem Austritt.</p>");

        // === PERSONAL ===
        await Dok("Geschäftsanweisung Personalwesen", "Regelungen zu Einstellung, Beurteilung, Weiterbildung und Austritt von Mitarbeitern.",
            Kap("Personal"), Team("Personal"), "Geschäftsanweisung", "personal,einstellung,weiterbildung,beurteilung",
            wagner, new DateTime(2025, 1, 15),
            @"<h2>1. Einstellungsprozess</h2>
<p>Jede Einstellung bedarf der Genehmigung durch die Geschäftsleitung. Der Einstellungsprozess umfasst: Stellenausschreibung, Vorauswahl, Vorstellungsgespräch, Referenzprüfung und Vertragsabschluss.</p>
<p>Vor Dienstantritt sind folgende Unterlagen einzuholen: Identitätsnachweis, Führungszeugnis, Qualifikationsnachweise und eine Schufa-Auskunft.</p>

<h2>2. Einarbeitung</h2>
<p>Neue Mitarbeiter durchlaufen ein strukturiertes Onboarding-Programm. In den ersten vier Wochen sind Pflichtschulungen zu absolvieren: Compliance, Geldwäscheprävention, Informationssicherheit und Datenschutz.</p>

<h2>3. Leistungsbeurteilung</h2>
<p>Jährlich wird ein Mitarbeitergespräch geführt und dokumentiert. Die Beurteilung umfasst Zielerreichung, Kompetenzentwicklung und Qualifizierungsbedarf.</p>

<h2>4. Weiterbildung</h2>
<p>Jeder Mitarbeiter hat Anspruch auf mindestens drei Weiterbildungstage pro Jahr. Fachliche Pflichtschulungen (regulatorisch) werden zusätzlich bereitgestellt.</p>

<h2>5. Austritt</h2>
<p>Beim Austritt sind Arbeitsmittel zurückzugeben, Zugänge zu sperren und eine Übergabe durchzuführen. Ein Exit-Interview wird durch die Personalabteilung angeboten.</p>");

        // === KREDIT ===
        await Dok("Kreditrichtlinie", "Grundsätze der Kreditvergabe, Risikoklassifizierung und Überwachung von Kreditengagements.",
            Kap("Kredit"), Team("Kredit"), "Richtlinie", "kredit,kreditvergabe,risiko,sicherheiten,marktfolge",
            fischer, new DateTime(2025, 4, 1),
            @"<h2>1. Kreditgrundsätze</h2>
<p>Die Kreditvergabe erfolgt unter Beachtung der Risikostrategie und der MaRisk-Anforderungen. Jede Kreditentscheidung setzt eine angemessene Bonitätsanalyse voraus.</p>

<h2>2. Kompetenzordnung</h2>
<p>Die Kreditkompetenz ist nach Risikogehalt und Volumen gestaffelt:</p>
<ul><li>Bis 250.000 EUR: Abteilungsleiter Markt + Marktfolge</li><li>250.000 – 1.000.000 EUR: Bereichsleiter + Marktfolge</li><li>Über 1.000.000 EUR: Geschäftsleitung</li></ul>
<p>Die Trennung von Markt und Marktfolge ist stets einzuhalten.</p>

<h2>3. Bonitätsanalyse</h2>
<p>Die Bonitätsprüfung umfasst: Analyse der wirtschaftlichen Verhältnisse, Kapitaldienstfähigkeit, Branchenrisiken und Sicherheitenbewertung. Für gewerbliche Kreditnehmer ist ein Rating durchzuführen.</p>

<h2>4. Sicherheiten</h2>
<p>Zulässige Sicherheiten sind: Grundpfandrechte, Bürgschaften, Verpfändungen von Depots/Konten und Sicherungsübereignungen. Die Bewertung erfolgt nach den Beleihungswertgrundsätzen der Bank.</p>

<h2>5. Laufende Überwachung</h2>
<p>Kreditengagements sind mindestens jährlich zu überprüfen. Bei negativen Entwicklungen ist eine unverzügliche Meldung an die Risikosteuerung erforderlich.</p>");

        // === KUNDE / KONTO ===
        await Dok("Geschäftsanweisung Kontoeröffnung und -führung", "Prozessuale Vorgaben für die Eröffnung, Führung und Schließung von Kundenkonten.",
            Kap("Kunde / Konto"), null, "Geschäftsanweisung", "konto,kontoeröffnung,kundenbeziehung,legitimation",
            mueller, new DateTime(2025, 5, 20),
            @"<h2>1. Kontoeröffnung</h2>
<p>Die Eröffnung eines Kontos setzt die vollständige Legitimation des Kunden gemäß GwG voraus. Erforderliche Unterlagen:</p>
<ul><li>Natürliche Personen: Gültiger Personalausweis oder Reisepass</li><li>Juristische Personen: Handelsregisterauszug, Gesellschaftsvertrag, Legitimation der vertretungsberechtigten Personen</li></ul>
<p>Die wirtschaftlich Berechtigten sind gemäß Geldwäschegesetz zu ermitteln und zu dokumentieren.</p>

<h2>2. Kontoführung</h2>
<p>Änderungen der Stammdaten (Adresse, Verfügungsberechtigte, Firmierung) sind zeitnah zu erfassen und zu dokumentieren. Die Kundendaten sind mindestens alle zwei Jahre auf Aktualität zu prüfen.</p>

<h2>3. Vollmachten</h2>
<p>Kontovollmachten bedürfen der Schriftform. Der Vollmachtgeber ist persönlich zu identifizieren. Die Vollmacht kann jederzeit widerrufen werden.</p>

<h2>4. Kontoschließung</h2>
<p>Bei Kündigung der Geschäftsbeziehung sind alle offenen Salden auszugleichen, Daueraufträge zu löschen und Karten einzuziehen. Die Kontounterlagen sind gemäß den gesetzlichen Aufbewahrungsfristen (10 Jahre) zu archivieren.</p>");

        // === HANDEL ===
        await Dok("Handelsrichtlinie Wertpapiergeschäft", "Vorgaben für die Durchführung und Abwicklung von Wertpapiergeschäften.",
            Kap("Wertpapiergeschäft"), Team("Handel"), "Richtlinie", "handel,wertpapier,mifid,best-execution,orderausführung",
            fischer, new DateTime(2025, 7, 1),
            @"<h2>1. Grundsätze</h2>
<p>Wertpapiergeschäfte sind unter Beachtung der MiFID-II-Anforderungen und der Best-Execution-Policy der Bank durchzuführen. Die Interessen des Kunden haben stets Vorrang.</p>

<h2>2. Orderannahme und -ausführung</h2>
<p>Orders sind zeitnah und zu den bestmöglichen Konditionen auszuführen. Die Auswahl des Handelsplatzes richtet sich nach der Best-Execution-Policy. Limitorders sind unverzüglich an den gewählten Handelsplatz weiterzuleiten.</p>

<h2>3. Anlageberatung</h2>
<p>Vor jeder Anlageberatung ist ein Geeignetheitstest durchzuführen. Die Beratung ist zu protokollieren und dem Kunden unverzüglich zur Verfügung zu stellen. Produkte dürfen nur empfohlen werden, wenn sie dem Risikoprofil des Kunden entsprechen.</p>

<h2>4. Dokumentation</h2>
<p>Alle Geschäftsvorgänge sind lückenlos zu dokumentieren. Telefonische Orderannahmen werden aufgezeichnet und sind mindestens fünf Jahre aufzubewahren.</p>

<h2>5. Reklamationen</h2>
<p>Kundenreklamationen im Wertpapierbereich sind unverzüglich an die Compliance-Abteilung weiterzuleiten und innerhalb von zehn Werktagen zu beantworten.</p>");

        // === REVISION ===
        await Dok("Revisionsrichtlinie", "Organisation, Durchführung und Berichterstattung der internen Revision.",
            Kap("Revision und Kontrollen"), Team("Revision und Kontrollen"), "Richtlinie", "revision,prüfung,audit,kontrollen,marisk",
            wagner, new DateTime(2025, 2, 1),
            @"<h2>1. Aufgaben der Internen Revision</h2>
<p>Die Interne Revision prüft risikoorientiert und prozessunabhängig die Angemessenheit und Wirksamkeit des internen Kontrollsystems, des Risikomanagements und der Governance-Prozesse.</p>

<h2>2. Unabhängigkeit</h2>
<p>Die Interne Revision ist organisatorisch und fachlich unabhängig. Sie untersteht direkt der Geschäftsleitung und hat ein uneingeschränktes Informations- und Prüfungsrecht.</p>

<h2>3. Prüfungsplanung</h2>
<p>Der jährliche Prüfungsplan wird risikobasiert erstellt und von der Geschäftsleitung genehmigt. Alle wesentlichen Aktivitäten und Prozesse werden innerhalb eines Zyklus von drei Jahren geprüft.</p>

<h2>4. Prüfungsdurchführung</h2>
<p>Prüfungen werden anhand eines standardisierten Ablaufs durchgeführt: Prüfungsvorbereitung, Vor-Ort-Prüfung, Berichterstellung und Follow-up. Feststellungen werden nach Schweregrad klassifiziert.</p>

<h2>5. Berichterstattung</h2>
<p>Die Geschäftsleitung und der Aufsichtsrat werden vierteljährlich über den Status der Prüfungen, wesentliche Feststellungen und den Umsetzungsstand von Maßnahmen informiert.</p>");

        // === SICHERHEITSMASSNAHMEN ===
        await Dok("Notfallhandbuch IT", "Maßnahmen zur Aufrechterhaltung des IT-Betriebs bei Störungen und Notfällen.",
            Kap("Sicherheitsmaßnahmen"), Team("IT-Service"), "Handbuch", "notfall,bcm,disaster-recovery,it-betrieb,wiederherstellung",
            lang, new DateTime(2025, 10, 1),
            @"<h2>1. Zweck</h2>
<p>Dieses Notfallhandbuch beschreibt die Maßnahmen zur Wiederherstellung des IT-Betriebs bei Störungen, Ausfällen und Katastrophen. Es ist Bestandteil des Business-Continuity-Managements (BCM) der Bank.</p>

<h2>2. Eskalationsstufen</h2>
<ul><li><strong>Stufe 1 – Störung:</strong> Einzelne Systeme betroffen, Workaround möglich. Behebung innerhalb von 4 Stunden.</li>
<li><strong>Stufe 2 – Schwere Störung:</strong> Geschäftskritische Systeme betroffen. Eskalation an IT-Leitung. Behebung innerhalb von 8 Stunden.</li>
<li><strong>Stufe 3 – Notfall:</strong> Kernbankensystem oder mehrere kritische Systeme ausgefallen. Aktivierung des Krisenstabs.</li></ul>

<h2>3. Wiederanlaufplan</h2>
<p>Die Wiederanlaufreihenfolge richtet sich nach der Kritikalität der Systeme:</p>
<ol><li>Kernbankensystem und Zahlungsverkehr (RTO: 4 Stunden)</li><li>E-Mail und Kommunikation (RTO: 8 Stunden)</li><li>Handelsplattform (RTO: 4 Stunden)</li><li>Kundenportal und Online-Banking (RTO: 8 Stunden)</li><li>Interne Anwendungen (RTO: 24 Stunden)</li></ol>

<h2>4. Notfallübungen</h2>
<p>Mindestens jährlich sind Notfallübungen durchzuführen. Die Ergebnisse werden dokumentiert und Verbesserungsmaßnahmen abgeleitet. Disaster-Recovery-Tests der kritischen Systeme erfolgen halbjährlich.</p>

<h2>5. Kommunikation im Notfall</h2>
<p>Die Kommunikation im Notfall erfolgt über die Eskalationskette. Externe Kommunikation (Kunden, Aufsicht, Presse) erfolgt ausschließlich über die Geschäftsleitung.</p>");

        // === DATENSCHUTZ ===
        await Dok("Datenschutzrichtlinie", "Grundsätze zum Schutz personenbezogener Daten gemäß DSGVO.",
            Kap("Datenverarbeitung"), null, "Richtlinie", "datenschutz,dsgvo,personenbezogene-daten,verarbeitung",
            mueller, new DateTime(2025, 5, 25),
            @"<h2>1. Grundsätze der Datenverarbeitung</h2>
<p>Die Verarbeitung personenbezogener Daten erfolgt unter Beachtung der Grundsätze der DSGVO: Rechtmäßigkeit, Zweckbindung, Datenminimierung, Richtigkeit, Speicherbegrenzung und Vertraulichkeit.</p>

<h2>2. Rechtsgrundlagen</h2>
<p>Personenbezogene Daten werden ausschließlich auf Basis einer Rechtsgrundlage verarbeitet: Vertragserfüllung, rechtliche Verpflichtung, berechtigtes Interesse oder Einwilligung des Betroffenen.</p>

<h2>3. Betroffenenrechte</h2>
<p>Betroffene haben das Recht auf Auskunft, Berichtigung, Löschung, Einschränkung der Verarbeitung, Datenübertragbarkeit und Widerspruch. Anfragen sind innerhalb eines Monats zu beantworten.</p>

<h2>4. Datenschutzbeauftragter</h2>
<p>Der Datenschutzbeauftragte ist Ansprechpartner für alle Datenschutzfragen. Er überwacht die Einhaltung der DSGVO und berät bei der Einführung neuer Verarbeitungstätigkeiten.</p>

<h2>5. Datenschutzverletzungen</h2>
<p>Datenschutzverletzungen sind unverzüglich dem Datenschutzbeauftragten zu melden. Bei Risiko für die Betroffenen ist die Aufsichtsbehörde innerhalb von 72 Stunden zu benachrichtigen.</p>");

        // === PROZESS-/PROJEKTMANAGEMENT ===
        await Dok("Projektmanagement-Richtlinie", "Vorgaben für die Durchführung und Steuerung von Projekten.",
            Kap("Prozess-/Projektmanagement und Organisation"), null, "Richtlinie", "projekt,projektmanagement,steuerung,governance",
            editor, new DateTime(2025, 8, 1),
            @"<h2>1. Projektdefinition</h2>
<p>Ein Projekt liegt vor, wenn ein Vorhaben zeitlich befristet ist, einen definierten Umfang hat und organisationsübergreifende Ressourcen erfordert. Ab einem geschätzten Aufwand von 20 Personentagen ist ein formaler Projektantrag zu stellen.</p>

<h2>2. Projektphasen</h2>
<ol><li><strong>Initiierung:</strong> Projektantrag, Machbarkeitsprüfung, Genehmigung durch Lenkungsausschuss</li>
<li><strong>Planung:</strong> Projektplan, Ressourcenplanung, Risikoanalyse</li>
<li><strong>Durchführung:</strong> Umsetzung, Statusberichte, Qualitätssicherung</li>
<li><strong>Abschluss:</strong> Abnahme, Lessons Learned, Projektabschlussbericht</li></ol>

<h2>3. Projektorganisation</h2>
<p>Jedes Projekt hat einen Projektleiter, einen fachlichen Auftraggeber und bei Bedarf einen Lenkungsausschuss. Die Projektleitung berichtet monatlich an den Auftraggeber.</p>

<h2>4. Risikomanagement</h2>
<p>Projektrisiken sind zu Beginn und laufend zu identifizieren und zu bewerten. Wesentliche Risiken werden im Projektstatusrricht an den Lenkungsausschuss eskaliert.</p>

<h2>5. Änderungsmanagement</h2>
<p>Änderungen am Projektumfang, Budget oder Zeitplan sind formal zu beantragen und vom Auftraggeber zu genehmigen. Die Auswirkungen auf Kosten und Zeitplan sind zu dokumentieren.</p>");

        // === VERWALTUNG ===
        await Dok("Beschaffungsrichtlinie", "Vorgaben für die Beschaffung von Waren und Dienstleistungen.",
            Kap("Verwaltung"), null, "Richtlinie", "beschaffung,einkauf,vergabe,lieferanten",
            wagner, new DateTime(2024, 11, 1),
            @"<h2>1. Grundsätze</h2>
<p>Die Beschaffung von Waren und Dienstleistungen erfolgt wirtschaftlich, transparent und unter Beachtung des Vier-Augen-Prinzips. Ab einem Auftragswert von 5.000 EUR sind mindestens zwei Vergleichsangebote einzuholen.</p>

<h2>2. Zuständigkeiten</h2>
<ul><li>Bis 1.000 EUR: Bereichsleiter</li><li>1.000 – 10.000 EUR: Abteilungsleiter + Verwaltung</li><li>Über 10.000 EUR: Geschäftsleitung</li></ul>

<h2>3. Lieferantenmanagement</h2>
<p>Wesentliche Lieferanten und Dienstleister werden jährlich bewertet. Bei IT-Dienstleistern ist zusätzlich eine Prüfung der Informationssicherheit durchzuführen.</p>

<h2>4. Verträge</h2>
<p>Verträge ab 10.000 EUR Jahresvolumen sind vor Unterzeichnung der Rechtsabteilung zur Prüfung vorzulegen. Bei Auslagerungen gelten zusätzliche MaRisk-Anforderungen.</p>");

        // === INNOVATION ===
        await Dok("Richtlinie Einsatz von Cloud-Diensten", "Anforderungen an die Nutzung von Cloud-Services in der Bank.",
            Kap("Innovation / Digitalisierung"), Team("IT-Service"), "Richtlinie", "cloud,saas,iaas,auslagerung,dora",
            lang, new DateTime(2025, 11, 15),
            @"<h2>1. Grundsätze</h2>
<p>Der Einsatz von Cloud-Diensten ist grundsätzlich zulässig, sofern die regulatorischen Anforderungen (MaRisk, BAIT, DORA) eingehalten werden. Jede Cloud-Nutzung ist vorab durch die Informationssicherheit und Compliance zu genehmigen.</p>

<h2>2. Klassifizierung</h2>
<p>Cloud-Dienste werden nach Kritikalität klassifiziert:</p>
<ul><li><strong>Unkritisch:</strong> Keine bankspezifischen oder personenbezogenen Daten (z.B. allgemeine Collaboration-Tools)</li>
<li><strong>Wesentlich:</strong> Verarbeitung bankspezifischer Daten, keine Kernprozesse</li>
<li><strong>Kritisch:</strong> Kernbankprozesse oder hochsensible Daten – erfordert vollständige Auslagerungsanalyse</li></ul>

<h2>3. Anforderungen an Anbieter</h2>
<p>Cloud-Anbieter müssen: Rechenzentren in der EU betreiben, ISO-27001-zertifiziert sein, vertragliche Audit-Rechte gewähren und ein Sicherheitskonzept nachweisen.</p>

<h2>4. Datenhaltung</h2>
<p>Personenbezogene Daten und bankgeheime Informationen dürfen ausschließlich in Rechenzentren innerhalb der EU/des EWR verarbeitet werden. Eine Verschlüsselung at rest und in transit ist verpflichtend.</p>

<h2>5. Exit-Strategie</h2>
<p>Für jeden Cloud-Dienst ist eine Exit-Strategie zu dokumentieren, die einen geordneten Rückzug zum Alternativanbieter oder in den Eigenbetrieb ermöglicht.</p>");

        // === MARKETING ===
        await Dok("Richtlinie Marketing und Kundenkommunikation", "Vorgaben für Werbung, Social Media und Kundenkommunikation.",
            Kap("Marketing"), null, "Richtlinie", "marketing,werbung,social-media,kommunikation",
            editor, new DateTime(2025, 4, 15),
            @"<h2>1. Grundsätze</h2>
<p>Alle Marketingmaßnahmen und Kundenkommunikation müssen den regulatorischen Anforderungen entsprechen. Werbliche Aussagen zu Finanzprodukten sind stets ausgewogen und nicht irreführend zu gestalten.</p>

<h2>2. Freigabeprozess</h2>
<p>Marketingmaterialien für Finanzprodukte sind vor Veröffentlichung durch die Compliance-Abteilung freizugeben. Dies gilt auch für Social-Media-Beiträge mit Produktbezug.</p>

<h2>3. Social Media</h2>
<p>Die Nutzung von Social-Media-Kanälen für die Bank erfolgt ausschließlich durch autorisierte Mitarbeiter. Private Äußerungen über die Bank in sozialen Medien sind mit Bedacht zu formulieren.</p>

<h2>4. Beschwerdemanagement</h2>
<p>Kundenbeschwerden sind systematisch zu erfassen, zeitnah zu bearbeiten und auszuwerten. Die Bearbeitungsfrist beträgt maximal zehn Werktage.</p>");

        // === VERZEICHNISSE (Hauptkapitel) ===
        await Dok("Verzeichnis der Verarbeitungstätigkeiten", "Dokumentation aller Verarbeitungstätigkeiten gemäß Art. 30 DSGVO.",
            HauptKap("Verzeichnisse"), null, "Verzeichnis", "vvt,dsgvo,verarbeitungstätigkeiten,datenschutz",
            mueller, new DateTime(2025, 6, 1),
            @"<h2>Verarbeitungsverzeichnis gemäß Art. 30 DSGVO</h2>
<p>Dieses Verzeichnis dokumentiert alle Verarbeitungstätigkeiten der Merkur Privatbank, bei denen personenbezogene Daten verarbeitet werden.</p>

<h3>VT-001: Kundenbeziehungsmanagement</h3>
<table class='table table-bordered'><thead><tr><th>Feld</th><th>Beschreibung</th></tr></thead><tbody>
<tr><td>Zweck</td><td>Verwaltung von Kundenbeziehungen, Kontoeröffnung, -führung und -schließung</td></tr>
<tr><td>Betroffene</td><td>Kunden, Bevollmächtigte, wirtschaftlich Berechtigte</td></tr>
<tr><td>Datenkategorien</td><td>Stammdaten, Legitimationsdaten, Kontodaten, Transaktionsdaten</td></tr>
<tr><td>Rechtsgrundlage</td><td>Art. 6 Abs. 1 lit. b DSGVO (Vertragserfüllung)</td></tr>
<tr><td>Löschfrist</td><td>10 Jahre nach Ende der Geschäftsbeziehung</td></tr>
</tbody></table>

<h3>VT-002: Personalverwaltung</h3>
<table class='table table-bordered'><thead><tr><th>Feld</th><th>Beschreibung</th></tr></thead><tbody>
<tr><td>Zweck</td><td>Begründung, Durchführung und Beendigung von Arbeitsverhältnissen</td></tr>
<tr><td>Betroffene</td><td>Mitarbeiter, Bewerber, ehemalige Mitarbeiter</td></tr>
<tr><td>Datenkategorien</td><td>Personalstammdaten, Gehaltsdaten, Beurteilungen, Krankmeldungen</td></tr>
<tr><td>Rechtsgrundlage</td><td>Art. 6 Abs. 1 lit. b DSGVO, § 26 BDSG</td></tr>
<tr><td>Löschfrist</td><td>6 Monate (Bewerber), 10 Jahre (Gehaltsunterlagen), 3 Jahre (Personalakte)</td></tr>
</tbody></table>

<h3>VT-003: Geldwäscheprävention</h3>
<table class='table table-bordered'><thead><tr><th>Feld</th><th>Beschreibung</th></tr></thead><tbody>
<tr><td>Zweck</td><td>Erfüllung der Sorgfaltspflichten nach GwG</td></tr>
<tr><td>Betroffene</td><td>Kunden, wirtschaftlich Berechtigte, Vertragspartner</td></tr>
<tr><td>Datenkategorien</td><td>Identifikationsdaten, PEP-Status, Risikoklassifizierung, Transaktionsmonitoringdaten</td></tr>
<tr><td>Rechtsgrundlage</td><td>Art. 6 Abs. 1 lit. c DSGVO (rechtliche Verpflichtung)</td></tr>
<tr><td>Löschfrist</td><td>5 Jahre nach Ende der Geschäftsbeziehung</td></tr>
</tbody></table>");

        // === FORMULARE (Hauptkapitel) ===
        await Dok("Formular: Antrag auf Zugangsberechtigung", "Standardformular für die Beantragung von IT-Zugangsberechtigungen.",
            HauptKap("Formulare"), Team("IT-Service"), "Formular", "formular,zugang,berechtigung,antrag,it",
            lang, new DateTime(2025, 3, 1),
            @"<h2>Antrag auf Zugangsberechtigung</h2>
<p><em>Dieses Formular ist vollständig auszufüllen und vom Vorgesetzten zu unterzeichnen.</em></p>

<h3>1. Antragsteller</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Name, Vorname</td><td>___________________________</td></tr>
<tr><td>Personalnummer</td><td>___________________________</td></tr>
<tr><td>Abteilung / Team</td><td>___________________________</td></tr>
<tr><td>Vorgesetzter</td><td>___________________________</td></tr>
</tbody></table>

<h3>2. Beantragte Zugangsberechtigungen</h3>
<table class='table table-bordered'><thead><tr><th>System / Anwendung</th><th>Berechtigungsrolle</th><th>Begründung</th></tr></thead><tbody>
<tr><td>___________________</td><td>___________________</td><td>___________________</td></tr>
<tr><td>___________________</td><td>___________________</td><td>___________________</td></tr>
<tr><td>___________________</td><td>___________________</td><td>___________________</td></tr>
</tbody></table>

<h3>3. Genehmigung</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Vorgesetzter</td><td>Datum: ________ Unterschrift: ________________</td></tr>
<tr><td>ISB (bei kritischen Systemen)</td><td>Datum: ________ Unterschrift: ________________</td></tr>
<tr><td>IT-Service (Umsetzung)</td><td>Datum: ________ Unterschrift: ________________</td></tr>
</tbody></table>");
    }
}
