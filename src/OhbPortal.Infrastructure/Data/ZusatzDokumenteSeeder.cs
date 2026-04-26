using Microsoft.EntityFrameworkCore;
using OhbPortal.Domain.Entities;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Infrastructure.Data;

/// <summary>
/// Zusatzpaket: ~100 freigegebene Demo-Dokumente, verteilt über alle Sachgebiete
/// und über die Hauptkapitel Verzeichnisse, Anlagen, Formulare.
/// Idempotent über Tag-Marker "seed-pack-2".
/// </summary>
public static class ZusatzDokumenteSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (await db.Dokumente.AnyAsync(d => d.Tags != null && d.Tags.Contains("seed-pack-2")))
            return;

        var editor = await db.Benutzer.FirstAsync(b => b.Benutzername == "editor");
        var bereich = await db.Benutzer.FirstAsync(b => b.Benutzername == "bereich");
        var approver = await db.Benutzer.FirstAsync(b => b.Benutzername == "approver");
        var mueller = await db.Benutzer.FirstOrDefaultAsync(b => b.Benutzername == "s.mueller") ?? editor;
        var fischer = await db.Benutzer.FirstOrDefaultAsync(b => b.Benutzername == "k.fischer") ?? approver;
        var lang = await db.Benutzer.FirstOrDefaultAsync(b => b.Benutzername == "a.lang") ?? editor;
        var wagner = await db.Benutzer.FirstOrDefaultAsync(b => b.Benutzername == "m.wagner") ?? bereich;

        var kapitel = await db.Kapitel.Include(k => k.ElternKapitel).ToListAsync();
        int Kap(string titel) => kapitel.First(k => k.Titel == titel && k.ElternKapitelId != null).Id;
        int HauptKap(string titel) => kapitel.First(k => k.Titel == titel && k.ElternKapitelId == null).Id;

        var teams = await db.Teams.ToListAsync();
        int? Team(string name) => teams.FirstOrDefault(t => t.Name == name)?.Id;

        async Task Dok(string titel, string kurz, int kapitelId, int? teamId,
            string kategorie, string tags, Benutzer autor, DateTime stand, string inhalt)
        {
            stand = DateTime.SpecifyKind(stand, DateTimeKind.Utc);
            var d = new Dokument
            {
                Titel = titel,
                Kurzbeschreibung = kurz,
                InhaltHtml = inhalt,
                KapitelId = kapitelId,
                VerantwortlicherBereichId = teamId,
                Status = DokumentStatus.Freigegeben,
                ErstelltVonId = autor.Id,
                GeaendertVonId = autor.Id,
                Kategorie = kategorie,
                Tags = tags + ",seed-pack-2",
                ErstelltAm = stand.AddDays(-7),
                GeaendertAm = stand,
                AktuelleVersion = 1,
                FreigabeModus = FreigabeModus.VierAugen,
                OeffentlichLesbar = true,
                SichtbarAb = stand,
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
                StatusZumZeitpunkt = DokumentStatus.Freigegeben,
                ErstelltAm = stand,
                ErstelltVonId = autor.Id,
                AenderungsHinweis = "Freigabe erteilt"
            });
            db.AuditEintraege.AddRange(
                new AuditEintrag { DokumentId = d.Id, BenutzerId = autor.Id, Typ = AuditTyp.DokumentErstellt, Beschreibung = titel, Zeitpunkt = stand.AddDays(-7) },
                new AuditEintrag { DokumentId = d.Id, BenutzerId = autor.Id, Typ = AuditTyp.VersionAngelegt, Beschreibung = "Version 1", Zeitpunkt = stand.AddDays(-7) },
                new AuditEintrag { DokumentId = d.Id, BenutzerId = autor.Id, Typ = AuditTyp.FreigabeGestartet, Beschreibung = "4-Augen-Freigabe gestartet", Zeitpunkt = stand.AddDays(-3) },
                new AuditEintrag { DokumentId = d.Id, BenutzerId = autor.Id, Typ = AuditTyp.FreigabeAbgeschlossen, Beschreibung = "Dokument freigegeben", Zeitpunkt = stand.AddDays(-1) }
            );
            await db.SaveChangesAsync();
        }

        // ===================== COMPLIANCE =====================
        await Dok("Verfahrensanweisung MaRisk-Compliance",
            "Operative Umsetzung der MaRisk-Compliance-Funktion gemäß AT 4.4.2.",
            Kap("Compliance"), Team("Compliance"), "Verfahrensanweisung",
            "marisk,compliance,at4.4.2,monitoring",
            mueller, new DateTime(2025, 9, 12),
            @"<h2>1. Auftrag der MaRisk-Compliance-Funktion</h2>
<p>Die MaRisk-Compliance-Funktion identifiziert die für das Institut wesentlichen rechtlichen Regelungen und Vorgaben, deren Nichteinhaltung den Vermögens-, Ertrags- oder Liquiditätslagen der Bank gefährden könnte, und überwacht deren Einhaltung.</p>
<h2>2. Stellung in der Organisation</h2>
<p>Die MaRisk-Compliance-Funktion ist organisatorisch und fachlich unabhängig. Sie berichtet quartalsweise direkt an die Geschäftsleitung sowie jährlich an den Aufsichtsrat.</p>
<h2>3. Compliance-Risiko-Inventur</h2>
<p>Mindestens jährlich wird eine vollständige Inventur der relevanten Rechtsgebiete durchgeführt. Wesentliche Änderungen der regulatorischen Anforderungen werden anlassbezogen ergänzt.</p>
<h3>3.1 Bewertungsdimensionen</h3>
<ul><li>Eintrittswahrscheinlichkeit</li><li>Schadenshöhe</li><li>Reputationsrisiko</li><li>Wirksamkeit bestehender Kontrollen</li></ul>
<h2>4. Compliance-Monitoring</h2>
<p>Die Überwachung erfolgt über stichprobenhafte Prüfungen, Auswertung von Schlüsselindikatoren (KCI) und enge Abstimmung mit den Fachbereichen. Festgestellte Mängel werden nachverfolgt bis zur vollständigen Beseitigung.</p>
<h2>5. Berichterstattung</h2>
<p>Der jährliche Compliance-Bericht enthält den Status der Risikoinventur, durchgeführte Prüfungshandlungen, festgestellte Schwachstellen und den Umsetzungsstand abgeleiteter Maßnahmen.</p>");

        await Dok("Verfahrensanweisung WpHG-Compliance",
            "Aufgaben und Prozesse der WpHG-Compliance gemäß § 80 WpHG.",
            Kap("Compliance"), Team("Compliance"), "Verfahrensanweisung",
            "wphg,compliance,wertpapierdienstleistung,marktmissbrauch",
            mueller, new DateTime(2025, 7, 22),
            @"<h2>1. Auftrag</h2>
<p>Die WpHG-Compliance-Funktion stellt die Einhaltung der wertpapierhandelsrechtlichen Anforderungen sicher, insbesondere § 80 WpHG, MaComp und MAR.</p>
<h2>2. Mitarbeitergeschäfte</h2>
<p>Sämtliche Mitarbeitergeschäfte in Finanzinstrumenten sind über die Compliance-Plattform vorab anzuzeigen. Sperrfristen und Watch-/Restricted-Lists werden zentral gepflegt.</p>
<h2>3. Marktmissbrauchsüberwachung</h2>
<p>Die laufende Überwachung von Auffälligkeiten erfolgt über das Trade-Surveillance-System. Verdachtsfälle werden formal dokumentiert und bei Bestätigung an die BaFin gemeldet (STOR).</p>
<h2>4. Interessenkonflikte</h2>
<p>Potenzielle Interessenkonflikte werden im Konfliktregister erfasst. Bei nicht vermeidbaren Konflikten erfolgt eine Offenlegung gegenüber dem Kunden vor Vertragsabschluss.</p>
<h2>5. Mitarbeiterschulungen</h2>
<p>Alle Mitarbeiter im Wertpapiergeschäft werden mindestens jährlich zu MAR, MiFID II und Best Execution geschult. Die Teilnahme wird im Schulungssystem dokumentiert.</p>");

        await Dok("Richtlinie Hinweisgebersystem (Whistleblowing)",
            "Vertrauliche Meldewege für Hinweise auf Verstöße gegen Recht und interne Regelungen.",
            Kap("Compliance"), Team("Compliance"), "Richtlinie",
            "whistleblowing,hinweisgeber,hinschg,vertraulich",
            mueller, new DateTime(2025, 4, 18),
            @"<h2>1. Zielsetzung</h2>
<p>Die Bank stellt allen Beschäftigten und externen Dritten ein vertrauliches Hinweisgebersystem im Sinne des Hinweisgeberschutzgesetzes (HinSchG) zur Verfügung.</p>
<h2>2. Meldewege</h2>
<ul><li>Interne digitale Meldeplattform (anonym möglich)</li><li>Postalisch an den Compliance-Beauftragten</li><li>Externe Meldestelle des BfJ</li></ul>
<h2>3. Schutz vor Repressalien</h2>
<p>Hinweisgeber, die in gutem Glauben einen Verstoß melden, werden umfassend vor Benachteiligung geschützt. Verstöße gegen das Repressalienverbot werden disziplinarisch verfolgt.</p>
<h2>4. Bearbeitungsfristen</h2>
<p>Eingangsbestätigung innerhalb von 7 Tagen, Rückmeldung über ergriffene Maßnahmen spätestens nach 3 Monaten.</p>
<h2>5. Vertraulichkeit</h2>
<p>Die Identität des Hinweisgebers wird streng vertraulich behandelt. Nur autorisierte Personen der Compliance-Funktion haben Zugang zu den Falldaten.</p>");

        await Dok("Verhaltenskodex (Code of Conduct)",
            "Grundsätze ethischen und integren Verhaltens für alle Mitarbeiter der Bank.",
            Kap("Compliance"), Team("Compliance"), "Leitlinie",
            "verhaltenskodex,coc,ethik,integrität",
            mueller, new DateTime(2025, 1, 8),
            @"<h2>1. Präambel</h2>
<p>Vertrauen ist die Grundlage unseres Geschäfts. Dieser Verhaltenskodex beschreibt die Werte und Verhaltensregeln, die für alle Beschäftigten verbindlich sind.</p>
<h2>2. Integrität</h2>
<p>Wir handeln ehrlich, fair und gesetzestreu. Korruption, Bestechung und das Annehmen unangemessener Vorteile sind strikt untersagt.</p>
<h2>3. Umgang mit Kollegen und Kunden</h2>
<p>Wir begegnen einander mit Respekt und Wertschätzung. Diskriminierung und Belästigung jeglicher Art werden nicht toleriert.</p>
<h2>4. Umgang mit Informationen</h2>
<p>Vertrauliche Informationen werden geschützt. Insiderinformationen dürfen nicht für eigene oder fremde Zwecke verwendet werden.</p>
<h2>5. Geschenke und Einladungen</h2>
<p>Geschenke über 50 EUR sowie Einladungen über 100 EUR sind genehmigungspflichtig. Bargeldgeschenke sind grundsätzlich nicht zulässig.</p>
<h2>6. Meldung von Verstößen</h2>
<p>Verstöße gegen den Kodex sind über das Hinweisgebersystem zu melden. Eine Meldung in gutem Glauben führt nicht zu Nachteilen.</p>");

        await Dok("Richtlinie zum Umgang mit Sanktionen und Embargos",
            "Vorgaben zum Sanktions- und Embargo-Screening sowie zur Behandlung von Verstößen.",
            Kap("Compliance"), Team("Compliance"), "Richtlinie",
            "sanktionen,embargo,screening,ofac,eu-sanktionen",
            mueller, new DateTime(2025, 6, 30),
            @"<h2>1. Geltungsbereich</h2>
<p>Diese Richtlinie gilt für alle Geschäftsbeziehungen, Transaktionen und Auslagerungen der Bank. Maßgeblich sind insbesondere die Sanktionslisten der EU, der UN, der US-OFAC sowie nationale Listen.</p>
<h2>2. Screening</h2>
<p>Das Screening neuer Geschäftsbeziehungen erfolgt vor Aufnahme. Ein Bestandsabgleich der gesamten Kundenbasis erfolgt täglich gegen aktualisierte Listen.</p>
<h2>3. Treffer-Bearbeitung</h2>
<ol><li>L1-Sichtung durch Sanctions-Team innerhalb von 4 Stunden</li><li>L2-Eskalation bei unklarer Einordnung an Compliance-Leitung</li><li>L3-Eskalation bei bestätigtem Treffer an Geschäftsleitung</li></ol>
<h2>4. Transaktionssperren</h2>
<p>Bei einem bestätigten Sanktionstreffer wird die Transaktion umgehend gesperrt. Eine Freigabe erfolgt nur nach Genehmigung durch BaFin und Bundesbank.</p>
<h2>5. Berichtspflichten</h2>
<p>Bestätigte Sanktionsverstöße werden unverzüglich der zuständigen Aufsicht und gegebenenfalls dem BAFA gemeldet.</p>");

        await Dok("Verfahrensanweisung MiFID-II-Geeignetheitsprüfung",
            "Ablauf der Geeignetheits- und Angemessenheitsprüfung bei der Anlageberatung.",
            Kap("Compliance"), Team("Compliance"), "Verfahrensanweisung",
            "mifid,geeignetheit,anlageberatung,zielmarkt,kunde",
            mueller, new DateTime(2025, 11, 5),
            @"<h2>1. Zweck</h2>
<p>Die Geeignetheitsprüfung stellt sicher, dass dem Kunden nur Finanzinstrumente empfohlen werden, die seinen Kenntnissen, Erfahrungen, finanziellen Verhältnissen und Anlagezielen entsprechen.</p>
<h2>2. Erhebung des Kundenprofils</h2>
<ul><li>Anlageziel und Anlagehorizont</li><li>Risikobereitschaft und Verlusttragfähigkeit</li><li>Kenntnisse und Erfahrungen mit Finanzinstrumenten</li><li>Vermögens- und Einkommenssituation</li></ul>
<h2>3. Zielmarktabgleich</h2>
<p>Vor jeder Empfehlung wird der Zielmarkt des Produkts mit dem Kundenprofil abgeglichen. Negativabweichungen werden dokumentiert und gegenüber dem Kunden offengelegt.</p>
<h2>4. Geeignetheitserklärung</h2>
<p>Die Geeignetheitserklärung wird dem Kunden vor Abschluss in dauerhafter Form übermittelt. Sie enthält die empfohlenen Produkte, die Begründung der Geeignetheit und die zugrunde liegenden Daten.</p>
<h2>5. Reassessment</h2>
<p>Bei laufenden Beratungsverhältnissen erfolgt mindestens jährlich eine Aktualisierung der Kundeninformationen.</p>");

        // ===================== DATENVERARBEITUNG =====================
        await Dok("Verfahrensanweisung Datenlöschung und Aufbewahrung",
            "Löschkonzept und Aufbewahrungsfristen für personenbezogene und geschäftliche Daten.",
            Kap("Datenverarbeitung"), null, "Verfahrensanweisung",
            "löschung,aufbewahrung,dsgvo,löschkonzept,fristen",
            lang, new DateTime(2025, 7, 5),
            @"<h2>1. Grundsätze</h2>
<p>Personenbezogene Daten werden nur so lange aufbewahrt, wie es für den Zweck der Verarbeitung erforderlich oder gesetzlich vorgeschrieben ist (Art. 5 Abs. 1 lit. e DSGVO).</p>
<h2>2. Aufbewahrungsfristen (Auszug)</h2>
<table class='table table-bordered'><thead><tr><th>Datenkategorie</th><th>Frist</th><th>Grundlage</th></tr></thead><tbody>
<tr><td>Buchungsbelege, Bilanzen</td><td>10 Jahre</td><td>§ 257 HGB</td></tr>
<tr><td>Geschäftsbriefe</td><td>6 Jahre</td><td>§ 257 HGB</td></tr>
<tr><td>Personalakten</td><td>3 Jahre nach Austritt</td><td>BGB / BDSG</td></tr>
<tr><td>Bewerberdaten</td><td>6 Monate</td><td>AGG</td></tr>
<tr><td>Bonitätsanfragen</td><td>1 Jahr</td><td>BDSG</td></tr>
</tbody></table>
<h2>3. Löschprozess</h2>
<p>Quartalsweise wird ein automatischer Löschlauf durchgeführt. Die Ergebnisse werden im Löschprotokoll dokumentiert und vom Datenschutzbeauftragten freigegeben.</p>
<h2>4. Sperrung</h2>
<p>Können Daten aus rechtlichen Gründen nicht gelöscht werden, werden sie für die weitere Verarbeitung gesperrt und nur noch für den verbleibenden Zweck genutzt.</p>
<h2>5. Backups</h2>
<p>Daten in Backup-Medien werden im Rahmen der Rotation überschrieben. Eine vorzeitige selektive Löschung ist nur in begründeten Ausnahmefällen vorgesehen.</p>");

        await Dok("Richtlinie Auftragsverarbeitung (Art. 28 DSGVO)",
            "Anforderungen an die Auswahl, Beauftragung und Überwachung von Auftragsverarbeitern.",
            Kap("Datenverarbeitung"), null, "Richtlinie",
            "auftragsverarbeitung,art28,dsgvo,dienstleister,av-vertrag",
            lang, new DateTime(2025, 5, 10),
            @"<h2>1. Anwendungsbereich</h2>
<p>Diese Richtlinie regelt die Auswahl, Beauftragung und laufende Überwachung von Auftragsverarbeitern, die personenbezogene Daten im Auftrag der Bank verarbeiten.</p>
<h2>2. Auswahlkriterien</h2>
<p>Die Auswahl erfolgt risikoorientiert. Geprüft werden technische und organisatorische Maßnahmen (TOM), Standort der Datenverarbeitung, Subdienstleister und Zertifizierungen (z.B. ISO 27001, SOC 2).</p>
<h2>3. AV-Vertrag</h2>
<p>Vor Beginn der Verarbeitung wird ein Auftragsverarbeitungsvertrag gemäß Art. 28 DSGVO geschlossen. Mindestinhalte:</p>
<ul><li>Gegenstand, Dauer, Art und Zweck der Verarbeitung</li><li>Kategorien betroffener Personen und Daten</li><li>Pflichten und Rechte des Verantwortlichen</li><li>Pflichten des Auftragsverarbeiters (Verschwiegenheit, TOM, Subdienstleister)</li></ul>
<h2>4. Drittlandsübermittlung</h2>
<p>Bei Verarbeitung außerhalb der EU sind Standardvertragsklauseln (SCC) inklusive Transfer Impact Assessment (TIA) erforderlich.</p>
<h2>5. Audits</h2>
<p>Wesentliche Auftragsverarbeiter werden mindestens alle 24 Monate auditiert (Vor-Ort oder remote). Kritische Mängel führen zur Eskalation an die Geschäftsleitung.</p>");

        await Dok("Richtlinie Datenschutz-Folgenabschätzung",
            "Vorgehen zur Durchführung von Datenschutz-Folgenabschätzungen (DSFA) gemäß Art. 35 DSGVO.",
            Kap("Datenverarbeitung"), null, "Richtlinie",
            "dsfa,art35,dsgvo,risiko,bewertung",
            lang, new DateTime(2025, 8, 14),
            @"<h2>1. Wann ist eine DSFA erforderlich?</h2>
<p>Eine DSFA ist erforderlich, wenn die Verarbeitung voraussichtlich ein hohes Risiko für die Rechte und Freiheiten natürlicher Personen birgt, insbesondere:</p>
<ul><li>Systematische und umfassende Bewertung persönlicher Aspekte (Profiling)</li><li>Umfangreiche Verarbeitung besonderer Kategorien personenbezogener Daten</li><li>Systematische umfangreiche Überwachung öffentlich zugänglicher Bereiche</li></ul>
<h2>2. Methodik</h2>
<p>Die DSFA folgt der vom Datenschutzbeauftragten bereitgestellten Vorlage und umfasst Beschreibung, Notwendigkeitsbewertung, Risikoanalyse und Maßnahmenplan.</p>
<h2>3. Zuständigkeiten</h2>
<p>Verantwortlich für die Durchführung ist der Fachbereich, der die Verarbeitung initiiert. Der Datenschutzbeauftragte berät und gibt das Ergebnis frei.</p>
<h2>4. Konsultation der Aufsicht</h2>
<p>Verbleibt nach Ergreifung der Maßnahmen ein hohes Risiko, ist die zuständige Aufsichtsbehörde gemäß Art. 36 DSGVO zu konsultieren.</p>
<h2>5. Aktualisierung</h2>
<p>Bei wesentlichen Änderungen der Verarbeitung ist die DSFA zu überprüfen und bei Bedarf zu aktualisieren.</p>");

        await Dok("Verfahrensanweisung Betroffenenanfragen",
            "Einheitlicher Prozess zur Bearbeitung von Auskunfts-, Berichtigungs- und Löschanträgen.",
            Kap("Datenverarbeitung"), null, "Verfahrensanweisung",
            "betroffenenrechte,auskunft,löschung,art15,art17",
            lang, new DateTime(2025, 3, 1),
            @"<h2>1. Eingangskanäle</h2>
<p>Anfragen können über das Kundenportal, per E-Mail (datenschutz@), per Brief oder telefonisch gestellt werden. Der Eingang wird zentral im Datenschutz-Ticket-System erfasst.</p>
<h2>2. Identitätsprüfung</h2>
<p>Vor inhaltlicher Beantwortung wird die Identität des Antragstellers verifiziert (Ausweiskopie, qualifizierte elektronische Signatur oder Online-Banking-Login).</p>
<h2>3. Recherche</h2>
<p>Auf Basis des hinterlegten Datenkatalogs werden alle relevanten Systeme abgefragt. Die Bearbeitungsfrist beträgt einen Monat (verlängerbar um zwei Monate bei komplexen Fällen).</p>
<h2>4. Bearbeitung nach Antragsart</h2>
<ul><li><strong>Auskunft:</strong> Strukturierte Datenauszüge, Zwecke, Empfänger, Speicherfristen</li><li><strong>Berichtigung:</strong> Korrektur in den Quellsystemen, Information aller Empfänger</li><li><strong>Löschung:</strong> Prüfung Aufbewahrungsfristen, ggf. Sperrung statt Löschung</li><li><strong>Datenübertragbarkeit:</strong> Bereitstellung im strukturierten Format</li></ul>
<h2>5. Eskalation</h2>
<p>Bei Ablehnung oder unklaren Anträgen erfolgt Eskalation an den Datenschutzbeauftragten.</p>");

        await Dok("Richtlinie zur Pseudonymisierung und Anonymisierung",
            "Verfahren zur Reduktion personenbeziehbarer Daten in Test-, Analyse- und Reportingumgebungen.",
            Kap("Datenverarbeitung"), null, "Richtlinie",
            "pseudonymisierung,anonymisierung,testdaten,dsgvo",
            lang, new DateTime(2025, 10, 22),
            @"<h2>1. Begriffsabgrenzung</h2>
<p><strong>Pseudonymisierung</strong> ersetzt direkte Identifikatoren durch Pseudonyme (Re-Identifikation mit Zusatzinformation möglich). <strong>Anonymisierung</strong> entfernt jeden Personenbezug irreversibel.</p>
<h2>2. Einsatzbereiche</h2>
<ul><li>Testumgebungen (Pseudonymisierung Pflicht)</li><li>BI-/Analyse-Sandboxen (Anonymisierung bevorzugt)</li><li>Externe Reports und Veröffentlichungen (Anonymisierung Pflicht)</li></ul>
<h2>3. Verfahren</h2>
<p>Eingesetzt werden hashbasierte Pseudonyme mit Salt, Tokenisierung kritischer Felder und K-Anonymität bei Aggregaten (k≥5).</p>
<h2>4. Schlüsselverwaltung</h2>
<p>Pseudonymisierungs-Schlüssel werden in einem dedizierten Hardware-Security-Modul (HSM) gespeichert. Zugriff hat ausschließlich der Datenschutzbeauftragte und sein Stellvertreter.</p>
<h2>5. Prüfung der Anonymisierung</h2>
<p>Vor Veröffentlichung wird die Reidentifikationswahrscheinlichkeit anhand der ENISA-Leitlinien geprüft.</p>");

        await Dok("Verfahrensanweisung internationale Datenübermittlung",
            "Anforderungen für die Übermittlung personenbezogener Daten in Drittländer.",
            Kap("Datenverarbeitung"), null, "Verfahrensanweisung",
            "drittland,scc,tia,dsgvo,internationale-übermittlung",
            lang, new DateTime(2025, 9, 28),
            @"<h2>1. Geltungsbereich</h2>
<p>Diese Anweisung regelt jede Übermittlung personenbezogener Daten an Empfänger außerhalb des EWR.</p>
<h2>2. Prüfreihenfolge</h2>
<ol><li>Liegt ein Angemessenheitsbeschluss der EU-Kommission vor?</li><li>Wenn nein: Standardvertragsklauseln (SCC) abschließen</li><li>Transfer Impact Assessment (TIA) durchführen</li><li>Bei hohem Risiko: zusätzliche Maßnahmen (Verschlüsselung, Pseudonymisierung)</li></ol>
<h2>3. Transfer Impact Assessment</h2>
<p>Im TIA werden Rechtslage des Drittlandes (Zugriffe durch Behörden), die Art der Daten und die ergriffenen Schutzmaßnahmen bewertet. Das Ergebnis ist zu dokumentieren.</p>
<h2>4. Dokumentation</h2>
<p>Jede Drittlandsübermittlung wird im Verarbeitungsverzeichnis erfasst inklusive Rechtsgrundlage, Empfänger, Datenkategorien und Schutzmaßnahmen.</p>
<h2>5. Eskalation</h2>
<p>Bei Behördenanfragen aus Drittländern erfolgt sofortige Eskalation an die Rechts- und Datenschutzabteilung. Eine Herausgabe ohne Rechtshilfeabkommen ist unzulässig.</p>");

        // ===================== GELDWÄSCHE =====================
        await Dok("Verfahrensanweisung Transaktionsmonitoring",
            "Regelbasierte und szenarienorientierte Überwachung von Transaktionen.",
            Kap("Geldwäsche"), Team("Compliance"), "Verfahrensanweisung",
            "transaktionsmonitoring,gwg,szenarien,alarme",
            mueller, new DateTime(2025, 8, 8),
            @"<h2>1. Zweck</h2>
<p>Das Transaktionsmonitoring identifiziert Transaktionen, die im Hinblick auf Geldwäsche, Terrorismusfinanzierung oder andere strafbare Handlungen auffällig erscheinen.</p>
<h2>2. Szenarien</h2>
<ul><li>Strukturierung (Smurfing) bei Bargeldgeschäften</li><li>Hohe Transaktionsvolumina ohne wirtschaftliche Plausibilität</li><li>Transaktionen mit Hochrisikoländern</li><li>Kreislauftransaktionen zwischen verbundenen Konten</li><li>Auffällige Auslandsüberweisungen</li></ul>
<h2>3. Alarmbearbeitung</h2>
<p>Alarme werden nach SLA bearbeitet: L1-Sichtung innerhalb 24 Stunden, Klärung mit Fachbereich/Kundenberater innerhalb 5 Werktagen, Abschluss innerhalb 30 Tagen.</p>
<h2>4. Dokumentation</h2>
<p>Jeder Alarm und jede Entscheidung wird im Geldwäsche-Tool revisionssicher dokumentiert. Schließungsgründe werden standardisiert erfasst.</p>
<h2>5. Modellpflege</h2>
<p>Schwellenwerte und Szenarien werden mindestens jährlich auf Wirksamkeit überprüft. Bei zu hoher Falsch-Positiv-Rate werden Modelle nachjustiert.</p>");

        await Dok("Risikoanalyse Geldwäsche und Terrorismusfinanzierung",
            "Institutsweite Bewertung der Geldwäsche- und Terrorismusfinanzierungsrisiken.",
            Kap("Geldwäsche"), Team("Compliance"), "Analyse",
            "risikoanalyse,gwg,terrorismusfinanzierung,kunden,produkte,länder",
            mueller, new DateTime(2025, 11, 30),
            @"<h2>1. Methodik</h2>
<p>Die Risikoanalyse bewertet die Bank ganzheitlich anhand der Dimensionen Kunde, Produkt, Vertriebsweg, geographisches Risiko und Branchenrisiko.</p>
<h2>2. Risikofaktoren Kunde</h2>
<ul><li>Politisch exponierte Personen (PEP)</li><li>Kunden mit komplexen Strukturen oder Trusts</li><li>Branchen mit hohem Bargeldanteil</li><li>Kunden aus Hochrisikoländern</li></ul>
<h2>3. Risikofaktoren Produkt</h2>
<p>Anonymisierungsfähige Produkte (z.B. Prepaid, Edelmetalle) und Produkte mit hoher Liquidität werden als erhöht eingestuft. Klassische Sparprodukte gelten als gering.</p>
<h2>4. Geographisches Risiko</h2>
<p>Maßgeblich sind die FATF-Listen, EU-Drittstaatenliste sowie eigene länderbezogene Bewertungen. Hochrisikoländer führen automatisch zur verstärkten Sorgfalt.</p>
<h2>5. Schlussfolgerungen</h2>
<p>Aus der Risikoanalyse werden präventive Maßnahmen abgeleitet: Anpassung der KYC-Tiefe, Schwellenwerte des Monitorings, Schulungsschwerpunkte.</p>");

        await Dok("Verfahrensanweisung Verdachtsmeldungen (FIU)",
            "Prozess zur Erstellung und Übermittlung von Verdachtsmeldungen an die Financial Intelligence Unit.",
            Kap("Geldwäsche"), Team("Compliance"), "Verfahrensanweisung",
            "fiu,verdachtsmeldung,gwg,goaml,tipping-off",
            mueller, new DateTime(2025, 6, 15),
            @"<h2>1. Auslöser</h2>
<p>Eine Verdachtsmeldung ist abzugeben, wenn Tatsachen vorliegen, die darauf hindeuten, dass ein Vermögenswert mit einer Straftat in Zusammenhang steht oder der Geldwäsche dient.</p>
<h2>2. Interne Meldung</h2>
<p>Mitarbeiter melden Verdachtsmomente unverzüglich an den Geldwäschebeauftragten. Eine Meldung an den Kunden (Tipping-Off) ist strafbar.</p>
<h2>3. Bewertung durch GwB</h2>
<p>Der Geldwäschebeauftragte prüft die Meldung, holt ergänzende Informationen ein und entscheidet über die externe Meldung an die FIU.</p>
<h2>4. Übermittlung über goAML</h2>
<p>Die externe Meldung erfolgt elektronisch über das System goAML der FIU. Die Meldung wird unter Wahrung der gesetzlichen Fristen abgegeben.</p>
<h2>5. Transaktionsbearbeitung</h2>
<p>Die gemeldete Transaktion darf grundsätzlich erst nach Zustimmung der FIU oder nach Ablauf von drei Werktagen ausgeführt werden, sofern die FIU nicht widerspricht.</p>");

        await Dok("Richtlinie Embargo- und PEP-Screening",
            "Screening neuer und bestehender Geschäftsbeziehungen gegen Sanktions- und PEP-Listen.",
            Kap("Geldwäsche"), Team("Compliance"), "Richtlinie",
            "pep,embargo,screening,sanktionen,kunde",
            mueller, new DateTime(2025, 2, 14),
            @"<h2>1. Zielsetzung</h2>
<p>Vermeidung von Geschäftsbeziehungen zu sanktionierten Personen und identifikation politisch exponierter Personen (PEP), die einer verstärkten Sorgfalt unterliegen.</p>
<h2>2. Eingesetzte Listen</h2>
<ul><li>EU-Sanktionsliste (Konsolidierte Liste)</li><li>UN-Sanktionsliste</li><li>OFAC-Liste (USA)</li><li>PEP-Datenbank eines spezialisierten Anbieters</li></ul>
<h2>3. Screening-Zeitpunkte</h2>
<ul><li>Vor Aufnahme einer Geschäftsbeziehung</li><li>Bei Aktualisierung der Sanktionslisten (täglich)</li><li>Bei jeder Auslandsüberweisung</li></ul>
<h2>4. Trefferbewertung</h2>
<p>Treffer werden in einem zweistufigen Verfahren bewertet (L1: Operatives Sanctions-Team, L2: Compliance-Leitung). False Positives werden dokumentiert, um die Modelle zu kalibrieren.</p>
<h2>5. PEP-Behandlung</h2>
<p>Bei bestätigten PEPs ist eine Genehmigung durch die Geschäftsleitung erforderlich. Die Geschäftsbeziehung unterliegt verstärkter laufender Überwachung.</p>");

        await Dok("Verfahrensanweisung Verstärkte Sorgfaltspflichten",
            "Maßnahmen zur Erfüllung verstärkter Sorgfaltspflichten gemäß § 15 GwG.",
            Kap("Geldwäsche"), Team("Compliance"), "Verfahrensanweisung",
            "verstärkte-sorgfalt,gwg,§15,hochrisiko,kyc",
            mueller, new DateTime(2025, 5, 4),
            @"<h2>1. Anwendungsfälle</h2>
<ul><li>PEP-Geschäftsbeziehungen</li><li>Kunden aus Drittstaaten mit erhöhtem Risiko</li><li>Komplexe oder ungewöhnlich hohe Transaktionen</li><li>Korrespondenzbankbeziehungen</li></ul>
<h2>2. Zusätzliche Maßnahmen</h2>
<p>Über die normalen Sorgfaltspflichten hinaus werden eingeholt:</p>
<ul><li>Quelle des Vermögens und der Mittel</li><li>Ausführliche Geschäftsmodellbeschreibung</li><li>Genehmigung durch Mitglied der Geschäftsleitung</li></ul>
<h2>3. Laufende Überwachung</h2>
<p>Verstärkt überwachte Konten werden mindestens jährlich überprüft. Schwellenwerte des Transaktionsmonitorings sind reduziert.</p>
<h2>4. Dokumentation</h2>
<p>Sämtliche Bewertungen, Genehmigungen und Maßnahmen werden im KYC-System revisionssicher dokumentiert.</p>
<h2>5. Risikoaktualisierung</h2>
<p>Bei wesentlichen Änderungen (Eigentümerwechsel, Geschäftsmodellwechsel, neue Sanktionierung) wird die Bewertung sofort aktualisiert.</p>");

        // ===================== HANDEL =====================
        await Dok("Best-Execution-Policy",
            "Grundsätze der bestmöglichen Auftragsausführung gemäß MiFID II.",
            Kap("Handel"), Team("Handel"), "Policy",
            "best-execution,mifid,ausführung,handelsplatz",
            fischer, new DateTime(2025, 4, 22),
            @"<h2>1. Grundsatz</h2>
<p>Aufträge von Privatkunden werden zu den im Hinblick auf den Gesamtgegenwert bestmöglichen Bedingungen ausgeführt.</p>
<h2>2. Ausführungsfaktoren</h2>
<ul><li>Preis</li><li>Kosten (explizit und implizit)</li><li>Schnelligkeit</li><li>Wahrscheinlichkeit der Ausführung und Abwicklung</li><li>Auftragsumfang und -art</li></ul>
<h2>3. Ausführungsplätze</h2>
<p>Die Bank nutzt regulierte Märkte, MTFs und systematische Internalisierer (SI). Eine vollständige Liste der Ausführungsplätze wird im Anhang geführt und mindestens jährlich aktualisiert.</p>
<h2>4. Berichterstattung</h2>
<p>Quartalsweise wird der Top-5-Ausführungsplatz-Bericht (RTS 28) veröffentlicht. Eine Wirksamkeitsprüfung der Policy erfolgt jährlich.</p>
<h2>5. Kundenanweisungen</h2>
<p>Spezifische Kundenanweisungen haben Vorrang. Der Kunde wird darauf hingewiesen, dass dies eine Best-Execution-Verpflichtung der Bank für die abweichenden Aspekte einschränken kann.</p>");

        await Dok("Verfahrensanweisung Eigenhandel",
            "Vorgaben für den Eigenhandel der Bank in Wertpapieren und Derivaten.",
            Kap("Handel"), Team("Handel"), "Verfahrensanweisung",
            "eigenhandel,nostro,limit,positionsführung",
            fischer, new DateTime(2025, 9, 18),
            @"<h2>1. Genehmigte Produkte</h2>
<p>Eigenhandel erfolgt ausschließlich in Produkten, die im Produktkatalog freigegeben sind. Neue Produkte durchlaufen den Produktfreigabeprozess (NPP).</p>
<h2>2. Limitsystem</h2>
<p>Pro Händler, Produkt und Marktrisikofaktor existieren tägliche und Intraday-Limits. Limitverletzungen führen zur sofortigen Glattstellung und werden eskaliert.</p>
<h2>3. Aufgabentrennung</h2>
<p>Front-Office (Handel), Middle-Office (Risikocontrolling) und Back-Office (Abwicklung) sind organisatorisch und systemisch getrennt.</p>
<h2>4. Stop-Loss-Regelungen</h2>
<p>Auf Positions- und Buchebene gelten Stop-Loss-Schwellen. Bei Erreichen erfolgt die Glattstellung gemäß definierten Eskalationsstufen.</p>
<h2>5. Marktanomalien</h2>
<p>Bei außergewöhnlichen Marktbedingungen (Liquiditätskrise, extreme Volatilität) kann der Handel durch die Geschäftsleitung kurzfristig eingeschränkt oder eingestellt werden.</p>");

        await Dok("Richtlinie Mitarbeitergeschäfte",
            "Vorgaben für persönliche Geschäfte von Mitarbeitern in Finanzinstrumenten.",
            Kap("Handel"), Team("Handel"), "Richtlinie",
            "mitarbeitergeschäfte,wphg,sperrfrist,watchlist",
            fischer, new DateTime(2025, 7, 10),
            @"<h2>1. Anwendungsbereich</h2>
<p>Diese Richtlinie gilt für alle Mitarbeiter, die im Rahmen ihrer Tätigkeit Zugang zu kursrelevanten Informationen haben oder selbst im Wertpapiergeschäft tätig sind.</p>
<h2>2. Vorabgenehmigung</h2>
<p>Käufe und Verkäufe von Einzelaktien sowie Derivaten sind vor Auftragserteilung über das Compliance-Tool freizugeben. Die Genehmigung gilt 48 Stunden.</p>
<h2>3. Sperrfristen</h2>
<ul><li>Closed Periods rund um die Veröffentlichung von Quartals- und Jahresergebnissen</li><li>Watch- und Restricted Lists für laufende Mandate</li><li>Mindesthaltefrist 4 Wochen für Einzelwerte</li></ul>
<h2>4. Verbote</h2>
<p>Insiderhandel, Frontrunning sowie spekulative Leerverkäufe sind verboten.</p>
<h2>5. Berichterstattung</h2>
<p>Die persönlichen Depots werden quartalsweise an die Compliance-Abteilung zur Kontrolle übermittelt.</p>");

        await Dok("Verfahrensanweisung Trade-Surveillance",
            "Maßnahmen zur Überwachung des Wertpapierhandels auf Marktmissbrauch.",
            Kap("Handel"), Team("Compliance"), "Verfahrensanweisung",
            "trade-surveillance,mar,marktmissbrauch,stor",
            fischer, new DateTime(2025, 10, 5),
            @"<h2>1. Aufgabe</h2>
<p>Erkennung von Marktmissbrauch im Sinne der Marktmissbrauchsverordnung (MAR) - insbesondere Insiderhandel und Marktmanipulation.</p>
<h2>2. Eingesetzte Modelle</h2>
<ul><li>Front Running / Pre-Hedging</li><li>Spoofing und Layering</li><li>Wash Trades</li><li>Auffällige Preisbeeinflussung</li><li>Auffällige Volumenmuster</li></ul>
<h2>3. Alarmbearbeitung</h2>
<p>Alarme werden täglich durch die Surveillance-Spezialisten gesichtet. Bestätigte Verdachtsfälle werden eskaliert und ggf. als STOR an die BaFin gemeldet.</p>
<h2>4. Modellvalidierung</h2>
<p>Die Modelle werden mindestens jährlich validiert. Backtesting und False-Positive-Analysen werden dokumentiert.</p>
<h2>5. Schulung der Mitarbeiter</h2>
<p>Händler und Vertriebsmitarbeiter werden jährlich zu MAR sensibilisiert. Inhaltlich werden typische Verstöße sowie Meldepflichten geschult.</p>");

        await Dok("Richtlinie Auftragsausführung Wertpapiere",
            "Operative Vorgaben für die Auftragsannahme, -weiterleitung und -bestätigung im Wertpapiergeschäft.",
            Kap("Handel"), Team("Handel"), "Richtlinie",
            "orderausführung,auftrag,bestätigung,wertpapiere",
            fischer, new DateTime(2025, 6, 8),
            @"<h2>1. Auftragsannahme</h2>
<p>Aufträge werden über die freigegebenen Kanäle (Online-Banking, Order-Telefon, persönlich) entgegengenommen. Telefonische Aufträge werden aufgezeichnet.</p>
<h2>2. Plausibilitätsprüfung</h2>
<p>Vor Weiterleitung erfolgen automatische Prüfungen auf Bestand, Limit, Zielmarktkonformität, Sanktionen und Vorhandensein eines Anlageprofils.</p>
<h2>3. Routing</h2>
<p>Die Routing-Entscheidung folgt der Best-Execution-Policy. Aufträge mit Sonderbedingungen (Limit, Stop) werden dokumentiert weitergeleitet.</p>
<h2>4. Auftragsbestätigung</h2>
<p>Der Kunde erhält am gleichen Werktag eine elektronische Auftragsbestätigung. Die Wertpapierabrechnung folgt am Ausführungstag.</p>
<h2>5. Reklamationsbearbeitung</h2>
<p>Reklamationen werden innerhalb von 5 Werktagen bearbeitet. Bei vermuteter Falschausführung erfolgt eine sofortige Eskalation an die Compliance.</p>");

        await Dok("Verfahrensanweisung Conflicts-of-Interest im Handel",
            "Identifikation und Behandlung von Interessenkonflikten in handelsbezogenen Geschäften.",
            Kap("Handel"), Team("Compliance"), "Verfahrensanweisung",
            "interessenkonflikte,handel,offenlegung,chinese-walls",
            fischer, new DateTime(2025, 3, 27),
            @"<h2>1. Identifikation</h2>
<p>Interessenkonflikte werden anhand definierter Konflikttypen identifiziert: Konflikt zwischen Bank und Kunde, zwischen Kunden, zwischen Mitarbeiter und Bank.</p>
<h2>2. Vermeidung</h2>
<ul><li>Funktionentrennung (Chinese Walls)</li><li>Räumliche und systemische Trennung</li><li>Eskalation an die Compliance bei Grenzfällen</li></ul>
<h2>3. Offenlegung</h2>
<p>Lassen sich Konflikte nicht vermeiden, werden sie dem Kunden vor Vertragsabschluss in dauerhafter Form offengelegt.</p>
<h2>4. Konfliktregister</h2>
<p>Alle bekannten Interessenkonflikte werden im zentralen Register erfasst. Die Compliance prüft das Register quartalsweise.</p>
<h2>5. Schulung</h2>
<p>Mitarbeiter im Handel und in der Anlageberatung werden jährlich zur Erkennung und Behandlung von Konflikten geschult.</p>");

        // ===================== INFORMATIONSSICHERHEIT =====================
        await Dok("Richtlinie Schutzbedarfsanalyse",
            "Methodik zur Ermittlung des Schutzbedarfs von Informationen und Systemen.",
            Kap("Informationssicherheit"), Team("Informationssicherheit"), "Richtlinie",
            "schutzbedarf,bsi,vertraulichkeit,integrität,verfügbarkeit",
            lang, new DateTime(2025, 4, 14),
            @"<h2>1. Methodik</h2>
<p>Die Schutzbedarfsanalyse orientiert sich an BSI IT-Grundschutz und bewertet pro Asset die drei Schutzziele Vertraulichkeit, Integrität und Verfügbarkeit.</p>
<h2>2. Schutzbedarfsklassen</h2>
<table class='table table-bordered'><thead><tr><th>Klasse</th><th>Beschreibung</th></tr></thead><tbody>
<tr><td>Normal</td><td>Begrenzte Auswirkungen, lokal beherrschbar</td></tr>
<tr><td>Hoch</td><td>Erhebliche Auswirkungen, Reputationsschäden möglich</td></tr>
<tr><td>Sehr hoch</td><td>Existenzbedrohende Auswirkungen, regulatorische Verstöße</td></tr>
</tbody></table>
<h2>3. Vorgehen</h2>
<p>Der Asset-Eigentümer führt die Erstbewertung durch, der ISB validiert. Bei Abweichungen entscheidet der Risiko-Ausschuss.</p>
<h2>4. Vererbung</h2>
<p>Der Schutzbedarf vererbt sich auf abhängige Systeme nach Maximumprinzip, Kumulationsprinzip oder Verteilungsprinzip - je nach Konstellation.</p>
<h2>5. Aktualisierung</h2>
<p>Die Schutzbedarfsanalyse wird mindestens jährlich sowie bei wesentlichen Änderungen (neue Anwendung, Re-Architektur) überprüft.</p>");

        await Dok("Verfahrensanweisung Sicherheitsvorfallmanagement",
            "Erkennung, Behandlung und Nachbereitung von Informationssicherheitsvorfällen.",
            Kap("Informationssicherheit"), Team("Informationssicherheit"), "Verfahrensanweisung",
            "incident,sicherheitsvorfall,csirt,dora,meldung",
            lang, new DateTime(2025, 8, 1),
            @"<h2>1. Definition</h2>
<p>Ein Sicherheitsvorfall ist ein Ereignis, das die Vertraulichkeit, Integrität oder Verfügbarkeit von Informationen oder Systemen tatsächlich oder potenziell verletzt.</p>
<h2>2. Meldewege</h2>
<p>Mitarbeiter melden Vorfälle unverzüglich über das ITSM-Ticket-System oder den Sicherheits-Hotline. Die Meldung kann auch anonym erfolgen.</p>
<h2>3. Klassifikation</h2>
<ul><li><strong>Niedrig:</strong> Lokal begrenzt, kein Datenabfluss</li><li><strong>Mittel:</strong> Mehrere Systeme betroffen</li><li><strong>Hoch:</strong> Kritische Daten oder Systeme betroffen, Aktivierung CSIRT</li><li><strong>Kritisch:</strong> Großflächiger Vorfall, Aktivierung Krisenstab</li></ul>
<h2>4. Reaktion</h2>
<p>Maßnahmen umfassen Eindämmung, Beweissicherung (Forensik), Wiederherstellung und Lessons Learned.</p>
<h2>5. Meldepflichten</h2>
<p>DORA-meldepflichtige Vorfälle werden innerhalb der Fristen an die zuständige Aufsicht gemeldet. Datenschutzvorfälle gemäß Art. 33/34 DSGVO innerhalb 72 Stunden.</p>");

        await Dok("Richtlinie Kryptografie und Verschlüsselung",
            "Vorgaben zur Verwendung kryptografischer Verfahren und Schlüssel.",
            Kap("Informationssicherheit"), Team("Informationssicherheit"), "Richtlinie",
            "kryptografie,verschlüsselung,tls,hsm,schlüsselmanagement",
            lang, new DateTime(2025, 5, 20),
            @"<h2>1. Grundsatz</h2>
<p>Sensible Daten werden sowohl bei der Übertragung (in transit) als auch bei der Speicherung (at rest) verschlüsselt. Eingesetzt werden ausschließlich freigegebene, etablierte Verfahren.</p>
<h2>2. Empfohlene Algorithmen</h2>
<ul><li>Symmetrisch: AES-256-GCM</li><li>Asymmetrisch: RSA-3072 oder ECDSA P-256</li><li>Hashing: SHA-256, SHA-384, SHA-512</li><li>Key Derivation: PBKDF2, Argon2id</li></ul>
<h2>3. Transportverschlüsselung</h2>
<p>Externe Kommunikation erfolgt mindestens über TLS 1.2 (TLS 1.3 bevorzugt). Interne sensible Verbindungen sind ebenfalls TLS-verschlüsselt.</p>
<h2>4. Schlüsselmanagement</h2>
<p>Kryptografische Schlüssel werden in einem zentralen HSM verwaltet. Schlüsselrotation erfolgt risikobasiert, mindestens alle 24 Monate.</p>
<h2>5. Verbotene Verfahren</h2>
<p>MD5, SHA-1, DES und 3DES sind für neue Anwendungen nicht zulässig. Bestehende Nutzungen werden zeitnah migriert.</p>");

        await Dok("Verfahrensanweisung Schwachstellenmanagement",
            "Identifikation, Bewertung und Behebung technischer Schwachstellen.",
            Kap("Informationssicherheit"), Team("Informationssicherheit"), "Verfahrensanweisung",
            "schwachstellen,cve,patch,vulnerability,scan",
            lang, new DateTime(2025, 9, 25),
            @"<h2>1. Quellen</h2>
<p>Schwachstellen werden aus folgenden Quellen identifiziert: CVE/NVD, Hersteller-Bulletins, BSI-Warnungen, eigene Vulnerability-Scans, Pentests.</p>
<h2>2. Bewertung</h2>
<p>Jede Schwachstelle wird anhand CVSS-Score und Kontextfaktoren (Erreichbarkeit, Datenklassifikation, kompensierende Maßnahmen) bewertet.</p>
<h2>3. SLA für Behebung</h2>
<table class='table table-bordered'><thead><tr><th>Kritikalität</th><th>Frist</th></tr></thead><tbody>
<tr><td>Kritisch</td><td>72 Stunden</td></tr>
<tr><td>Hoch</td><td>14 Tage</td></tr>
<tr><td>Mittel</td><td>30 Tage</td></tr>
<tr><td>Niedrig</td><td>90 Tage</td></tr>
</tbody></table>
<h2>4. Ausnahmen</h2>
<p>Lassen sich Patches nicht in der Frist umsetzen, sind kompensierende Maßnahmen zu definieren und der Risikoakzept des ISB einzuholen.</p>
<h2>5. Reporting</h2>
<p>Monatliches Reporting an die IT-Leitung und ISB inklusive Trendanalyse, offene Findings und Patch-Status.</p>");

        await Dok("Richtlinie Mobile Geräte (BYOD/COPE)",
            "Sicherheitsvorgaben für mobile Endgeräte im Unternehmenseinsatz.",
            Kap("Informationssicherheit"), Team("Informationssicherheit"), "Richtlinie",
            "byod,cope,mobile,mdm,smartphone,tablet",
            lang, new DateTime(2025, 7, 17),
            @"<h2>1. Geräteklassen</h2>
<ul><li><strong>Corporate Owned Personally Enabled (COPE):</strong> Bankgerät mit eingeschränkter Privatnutzung</li><li><strong>Bring Your Own Device (BYOD):</strong> Privates Gerät mit Container-Lösung</li></ul>
<h2>2. Mindestanforderungen</h2>
<ul><li>Aktuelles Betriebssystem (max. 1 Hauptversion alt)</li><li>Geräteverschlüsselung aktiv</li><li>Bildschirmsperre nach 5 Minuten Inaktivität</li><li>Mindestens 6-stelliges Passwort oder biometrische Authentifizierung</li></ul>
<h2>3. Mobile Device Management (MDM)</h2>
<p>Alle Geräte mit Zugriff auf bankliche Daten werden in das zentrale MDM eingebunden. Sicherheitsrichtlinien werden technisch durchgesetzt.</p>
<h2>4. Verlust und Diebstahl</h2>
<p>Bei Verlust oder Diebstahl ist der Service Desk unverzüglich zu informieren. Die Bank kann das Gerät remote sperren bzw. löschen.</p>
<h2>5. Verbotene Apps</h2>
<p>Eine Liste verbotener Anwendungen (insb. mit nicht vertrauenswürdigem Cloud-Backend) wird zentral gepflegt und durchgesetzt.</p>");

        await Dok("Verfahrensanweisung Penetrationstests und Red-Teaming",
            "Planung und Durchführung von Penetrationstests und Red-Team-Übungen.",
            Kap("Informationssicherheit"), Team("Informationssicherheit"), "Verfahrensanweisung",
            "pentest,red-team,tibre,sicherheitstest,offensiv",
            lang, new DateTime(2025, 11, 12),
            @"<h2>1. Geltungsbereich</h2>
<p>Diese Anweisung regelt interne und externe Sicherheitstests an Anwendungen, Infrastruktur und Geschäftsprozessen.</p>
<h2>2. Frequenzen</h2>
<ul><li>Externe Pentests aller kritischen Anwendungen: jährlich</li><li>Pentests interner Anwendungen: alle 24 Monate</li><li>TIBRE-konformes Red-Teaming: alle 3 Jahre (DORA-Anforderung)</li></ul>
<h2>3. Beauftragung</h2>
<p>Externe Anbieter werden anhand einer Whitelist ausgewählt. Verträge enthalten NDA, Verhaltensregeln und Beweissicherungspflicht.</p>
<h2>4. Durchführung</h2>
<p>Ein Test-Approver definiert Scope und Stop-Kriterien. Während des Tests gilt eine 24/7-Bereitschaft des CSIRT.</p>
<h2>5. Berichtswesen</h2>
<p>Findings werden im Schwachstellenmanagement aufgenommen. Die Geschäftsleitung erhält eine Zusammenfassung mit Ampelbewertung.</p>");

        // ===================== IT-SERVICE =====================
        await Dok("Richtlinie Change-Management",
            "Strukturierter Umgang mit Änderungen in Produktivsystemen.",
            Kap("IT-Service"), Team("IT-Service"), "Richtlinie",
            "change,itil,cab,produktiv,release",
            lang, new DateTime(2025, 6, 4),
            @"<h2>1. Zielsetzung</h2>
<p>Änderungen an produktiven IT-Systemen werden geplant, bewertet, freigegeben und nachvollziehbar umgesetzt.</p>
<h2>2. Change-Kategorien</h2>
<ul><li><strong>Standard-Change:</strong> Vorab freigegeben, geringes Risiko</li><li><strong>Normaler Change:</strong> Einzelfallprüfung im CAB</li><li><strong>Notfall-Change:</strong> Außerhalb regulärer Termine, nachträgliche Dokumentation</li></ul>
<h2>3. Change Advisory Board</h2>
<p>Das CAB tagt wöchentlich. Stimmberechtigt sind IT-Betrieb, Anwendungsentwicklung, ISB und Vertreter der Fachbereiche.</p>
<h2>4. Risikobewertung</h2>
<p>Jeder Change wird anhand der Auswirkungen, der Komplexität und der Reversibilität bewertet. Kritische Changes erfordern eine Geschäftsleitungsinformation.</p>
<h2>5. Notfall-Rollback</h2>
<p>Vor jedem Change wird ein Rollback-Plan dokumentiert. Bei kritischen Störungen wird automatisiert auf den Vor-Zustand zurückgesetzt.</p>");

        await Dok("Verfahrensanweisung Incident-Management",
            "Bearbeitung von Störungen im IT-Betrieb gemäß ITIL.",
            Kap("IT-Service"), Team("IT-Service"), "Verfahrensanweisung",
            "incident,itil,ticketsystem,sla,major-incident",
            lang, new DateTime(2025, 3, 18),
            @"<h2>1. Tickethaltung</h2>
<p>Sämtliche Störungen werden im zentralen ITSM-Tool erfasst. Self-Service-Portale ermöglichen den Anwendern eine direkte Eröffnung.</p>
<h2>2. Klassifizierung</h2>
<table class='table table-bordered'><thead><tr><th>Priorität</th><th>Reaktionszeit</th><th>Lösungszeit</th></tr></thead><tbody>
<tr><td>P1</td><td>15 Min</td><td>4 Std</td></tr>
<tr><td>P2</td><td>1 Std</td><td>8 Std</td></tr>
<tr><td>P3</td><td>4 Std</td><td>3 Werktage</td></tr>
<tr><td>P4</td><td>1 Werktag</td><td>10 Werktage</td></tr>
</tbody></table>
<h2>3. Eskalation</h2>
<p>Werden SLA-Fristen überschritten oder ist absehbar, dass sie nicht eingehalten werden können, erfolgt automatische Eskalation an die nächsthöhere Stufe.</p>
<h2>4. Major Incident</h2>
<p>Bei P1-Vorfällen mit Geschäftsauswirkung wird ein Major-Incident-Manager bestellt. Dieser koordiniert Wiederherstellung und Kommunikation.</p>
<h2>5. Lessons Learned</h2>
<p>Major Incidents werden in einem Post-Incident-Review aufgearbeitet. Maßnahmen fließen in das Problem-Management.</p>");

        await Dok("Richtlinie Patch- und Updatemanagement",
            "Verfahren zur regelmäßigen Aktualisierung von Software und Systemen.",
            Kap("IT-Service"), Team("IT-Service"), "Richtlinie",
            "patch,update,microsoft,linux,zero-day",
            lang, new DateTime(2025, 8, 26),
            @"<h2>1. Ziel</h2>
<p>Aktualisierung von Betriebssystemen, Middleware und Anwendungen, um Sicherheitslücken zu schließen und Stabilität zu wahren.</p>
<h2>2. Patchzyklen</h2>
<ul><li>Workstations: monatlich (Patch Tuesday + 7 Tage Test)</li><li>Server unkritisch: monatlich</li><li>Server kritisch: nach Release Notes und Risikoabwägung</li><li>Sicherheitskritische Patches: 72 Stunden</li></ul>
<h2>3. Test- und Freigabeprozess</h2>
<p>Patches durchlaufen Test- und Stage-Umgebungen. Erst nach Tests werden sie produktiv ausgerollt. Bei kritischen Sicherheitslücken kann ein verkürzter Prozess angewendet werden.</p>
<h2>4. Reporting</h2>
<p>Patch-Compliance wird wöchentlich an IT-Leitung und ISB berichtet. Soll-Wert: 95% innerhalb 30 Tagen.</p>
<h2>5. Ausnahmen</h2>
<p>Nicht patchbare Systeme erhalten kompensierende Maßnahmen (Netzwerksegmentierung, Monitoring). Risikoakzeptanz durch ISB.</p>");

        await Dok("Verfahrensanweisung Backup und Wiederherstellung",
            "Sicherung und Wiederherstellung von Daten und Systemen.",
            Kap("IT-Service"), Team("IT-Service"), "Verfahrensanweisung",
            "backup,restore,3-2-1,offsite,disaster",
            lang, new DateTime(2025, 4, 8),
            @"<h2>1. Backup-Strategie</h2>
<p>Es gilt das 3-2-1-Prinzip: 3 Kopien, 2 Medien, 1 Offsite-Kopie. Die Sicherungen werden zusätzlich gegen Manipulation (Immutable Storage) geschützt.</p>
<h2>2. Sicherungszyklen</h2>
<ul><li>Voll-Backup: wöchentlich</li><li>Inkrementell: täglich</li><li>Datenbank-Logs: kontinuierlich (≤ 15 Min)</li></ul>
<h2>3. Aufbewahrung</h2>
<p>Tägliche Sicherungen 30 Tage, monatliche 12 Monate, jährliche 10 Jahre. Aufbewahrung gemäß gesetzlicher Vorgaben.</p>
<h2>4. Restore-Tests</h2>
<p>Restore-Tests erfolgen pro System mindestens einmal pro Quartal. Ergebnisse werden im Backup-Bericht dokumentiert.</p>
<h2>5. Notfallwiederherstellung</h2>
<p>Im Disaster-Fall werden definierte Wiederherstellungspläne (DR-Plans) abgearbeitet. RTO und RPO sind pro System dokumentiert.</p>");

        await Dok("Richtlinie Software-Asset-Management",
            "Verwaltung von Software-Lizenzen und Software-Inventar.",
            Kap("IT-Service"), Team("IT-Service"), "Richtlinie",
            "sam,lizenz,software,inventar,audit",
            lang, new DateTime(2025, 2, 26),
            @"<h2>1. Zielsetzung</h2>
<p>Effizienter und rechtssicherer Einsatz von Software durch zentrale Lizenzverwaltung und Inventarisierung.</p>
<h2>2. Software-Whitelist</h2>
<p>Nur Software, die im zentralen Katalog freigegeben ist, darf installiert werden. Beantragung neuer Software erfolgt über das Self-Service-Portal.</p>
<h2>3. Lizenzpflege</h2>
<p>Lizenzen werden zentral erfasst und mit installierten Versionen abgeglichen. Über- und Unterlizenzierung werden monatlich ausgewertet.</p>
<h2>4. Audit-Vorbereitung</h2>
<p>Halbjährlich erfolgt ein Selbst-Audit auf Lizenzkonformität. Ergebnisse werden mit Maßnahmenplan an IT-Leitung berichtet.</p>
<h2>5. End of Life</h2>
<p>Software außerhalb des Hersteller-Supports darf nicht ohne ausdrückliche Risikoakzeptanz produktiv eingesetzt werden.</p>");

        await Dok("Verfahrensanweisung Netzwerksicherheit",
            "Sicherheitsanforderungen an die Netzwerk-Infrastruktur.",
            Kap("IT-Service"), Team("IT-Service"), "Verfahrensanweisung",
            "netzwerk,segmentierung,firewall,ids,ips",
            lang, new DateTime(2025, 10, 15),
            @"<h2>1. Segmentierung</h2>
<p>Das Unternehmensnetzwerk ist in Sicherheitszonen segmentiert: DMZ, Workplace, Server, Verwaltung, Test/Dev. Übergänge sind durch Firewalls kontrolliert.</p>
<h2>2. Firewall-Regelwerk</h2>
<p>Default-Deny gilt für alle Übergänge. Freigaben werden im Change-Prozess beantragt, technisch dokumentiert und mindestens jährlich rezertifiziert.</p>
<h2>3. IDS/IPS</h2>
<p>Intrusion-Detection- und Prevention-Systeme überwachen den Datenverkehr. Auffälligkeiten werden ans SIEM gemeldet und durch das CSIRT gesichtet.</p>
<h2>4. Remote-Zugänge</h2>
<p>Externe Zugänge erfolgen ausschließlich über VPN mit MFA. Privilegierte Zugänge nutzen zusätzlich Privileged Access Management (PAM).</p>
<h2>5. WLAN</h2>
<p>Bank-WLANs sind WPA3-Enterprise-gesichert. Gäste-WLAN ist vom Bank-Netz getrennt und auf Internetzugang beschränkt.</p>");

        // ===================== KREDIT =====================
        await Dok("Verfahrensanweisung Kreditbeschluss",
            "Ablauf der Kreditentscheidung und Dokumentation des Beschlusses.",
            Kap("Kredit"), Team("Kredit"), "Verfahrensanweisung",
            "kreditbeschluss,markt,marktfolge,dokumentation",
            fischer, new DateTime(2025, 5, 14),
            @"<h2>1. Antragsannahme</h2>
<p>Kreditanträge werden durch den Markt aufgenommen. Pflichtinformationen umfassen Verwendungszweck, Höhe, Laufzeit, Sicherheiten und wirtschaftliche Verhältnisse.</p>
<h2>2. Bonitätsprüfung</h2>
<p>Die Marktfolge prüft die Bonität anhand des Rating-Verfahrens, der Kapitaldienstfähigkeit und der Sicherheiten. Bei Auslandskunden zusätzlich Länderrisiko.</p>
<h2>3. Beschlussfassung</h2>
<p>Die Beschlussfassung erfolgt im Vier-Augen-Prinzip durch Markt und Marktfolge. Bei abweichenden Voten entscheidet die nächsthöhere Kompetenzstufe.</p>
<h2>4. Beschlussdokument</h2>
<p>Im Kreditbeschluss werden Konditionen, Auflagen, Sicherheiten, Voten und Genehmigungsfreigaben festgehalten. Ein einheitliches Template ist verbindlich.</p>
<h2>5. Auszahlung</h2>
<p>Die Auszahlung erfolgt erst nach Erfüllung aller Auszahlungsvoraussetzungen (z.B. Bestellung der Sicherheiten, Vorlage von Belegen).</p>");

        await Dok("Richtlinie Sicherheitenbewertung",
            "Bewertungsmethoden für gestellte Kreditsicherheiten.",
            Kap("Kredit"), Team("Kredit"), "Richtlinie",
            "sicherheiten,bewertung,beleihungswert,grundpfand",
            fischer, new DateTime(2025, 7, 25),
            @"<h2>1. Anwendungsbereich</h2>
<p>Diese Richtlinie regelt die Bewertung aller Sicherheiten, die im Kreditgeschäft hereingenommen werden.</p>
<h2>2. Wertarten</h2>
<ul><li>Marktwert (aktueller Verkehrswert)</li><li>Beleihungswert (langfristig nachhaltig erzielbarer Wert, abzgl. Sicherheitsabschlag)</li><li>Realisationswert (im Verwertungsfall erzielbar)</li></ul>
<h2>3. Bewertungsmethoden</h2>
<p>Immobilien werden je nach Klasse über Vergleichswert-, Ertragswert- oder Sachwertverfahren bewertet. Bei Immobilien über 3 Mio. EUR ist ein externes Gutachten erforderlich.</p>
<h2>4. Wiedervorlage</h2>
<p>Sicherheiten werden mindestens alle 36 Monate, bei kritischer Bonität jährlich, neu bewertet.</p>
<h2>5. Sicherheitenabschläge</h2>
<table class='table table-bordered'><thead><tr><th>Sicherheit</th><th>Abschlag</th></tr></thead><tbody>
<tr><td>Wohnimmobilien</td><td>20%</td></tr>
<tr><td>Gewerbeimmobilien</td><td>30-40%</td></tr>
<tr><td>Wertpapierdepot (Blue Chips)</td><td>30%</td></tr>
<tr><td>Bürgschaften (Privat)</td><td>50%</td></tr>
</tbody></table>");

        await Dok("Verfahrensanweisung Risikorelevante Kreditentscheidungen",
            "Sondervorschriften für risikorelevante Engagements gemäß MaRisk BTO 1.",
            Kap("Kredit"), Team("Kredit"), "Verfahrensanweisung",
            "risikorelevant,marisk,bto1,kreditentscheidung,votum",
            fischer, new DateTime(2025, 9, 30),
            @"<h2>1. Definition</h2>
<p>Als risikorelevant gelten Kreditengagements, die aufgrund ihres Volumens, ihrer Komplexität oder ihrer Bonität ein erhöhtes Risiko für die Bank darstellen.</p>
<h2>2. Schwellenwerte</h2>
<ul><li>Volumen größer 5 Mio. EUR (gewerblich)</li><li>Volumen größer 1 Mio. EUR bei nachrangigem Rating</li><li>Komplexe Strukturen (Konsortialkredite, Mezzanine)</li></ul>
<h2>3. Doppeltes Votum</h2>
<p>Bei risikorelevanten Engagements ist ein eindeutiges Votum von Markt und Marktfolge erforderlich. Beide Voten werden im System dokumentiert.</p>
<h2>4. Eskalation</h2>
<p>Bei Voten-Differenzen entscheidet die Geschäftsleitung. Die Eskalation wird begründet und nachvollziehbar dokumentiert.</p>
<h2>5. Engmaschige Überwachung</h2>
<p>Risikorelevante Engagements unterliegen mindestens halbjährlicher Überwachung mit erweitertem Reporting an Risikosteuerung und Geschäftsleitung.</p>");

        await Dok("Richtlinie Problemkreditbearbeitung",
            "Erkennung, Bearbeitung und Verwertung von Problemengagements.",
            Kap("Kredit"), Team("Kredit"), "Richtlinie",
            "problemkredit,intensivbetreuung,sanierung,verwertung",
            fischer, new DateTime(2025, 6, 24),
            @"<h2>1. Frühwarnindikatoren</h2>
<p>Frühwarnsignale werden aus quantitativen (Kontoumsätze, Limitausnutzung, Zahlungsstörungen) und qualitativen (Branchenentwicklung, Managementwechsel) Quellen abgeleitet.</p>
<h2>2. Intensivbetreuung</h2>
<p>Bei mittlerem Risiko erfolgt der Wechsel in die Intensivbetreuung. Hier finden engmaschige Bonitäts- und Sicherheitenprüfungen statt.</p>
<h2>3. Sanierung</h2>
<p>Bei Restrukturierungsbedarf wird in Abstimmung mit dem Kunden ein Sanierungskonzept entwickelt. Die Bank kann Forderungsverzichte oder Stundungen gewähren.</p>
<h2>4. Verwertung</h2>
<p>Sind Sanierungsbemühungen erfolglos, beginnt die Verwertung. Der Verwertungsplan wird dokumentiert und durch die Geschäftsleitung freigegeben.</p>
<h2>5. Wertberichtigungen</h2>
<p>Risikovorsorgen werden gemäß IFRS 9 nach erwarteten Verlusten berechnet. Einzelwertberichtigungen werden vom Kreditausschuss beschlossen.</p>");

        await Dok("Verfahrensanweisung Kreditakteführung",
            "Aufbau und Pflege der Kreditakte gemäß MaRisk.",
            Kap("Kredit"), Team("Kredit"), "Verfahrensanweisung",
            "kreditakte,dokumentation,marisk,vollständigkeit",
            fischer, new DateTime(2025, 4, 2),
            @"<h2>1. Anforderungen</h2>
<p>Jede Kreditakte ist vollständig, aktuell und nachvollziehbar zu führen. Sie umfasst Antrag, Bonitätsanalyse, Beschluss, Verträge, Sicherheiten und laufende Überwachung.</p>
<h2>2. Elektronische Akte</h2>
<p>Akten werden ausschließlich elektronisch im Kreditsystem geführt. Originaldokumente werden gescannt und revisionssicher archiviert.</p>
<h2>3. Pflichtinhalte</h2>
<ul><li>Antragsunterlagen</li><li>Wirtschaftliche Unterlagen (Bilanzen, BWA, Steuerbescheide)</li><li>Beschlussvorlage und Beschlussdokumentation</li><li>Verträge und Nebenabreden</li><li>Sicherheitenverträge und Bewertungen</li><li>Korrespondenz und Aktenvermerke</li></ul>
<h2>4. Aktualisierung</h2>
<p>Wirtschaftliche Unterlagen werden mindestens jährlich, bei größeren Engagements halbjährlich aktualisiert.</p>
<h2>5. Aufbewahrung</h2>
<p>Kreditakten werden 10 Jahre nach Beendigung des Engagements gemäß § 257 HGB aufbewahrt.</p>");

        await Dok("Richtlinie Kreditderivate und Kreditrisikominderung",
            "Einsatz von Kreditderivaten und anderen Instrumenten zur Risikominderung.",
            Kap("Kredit"), Team("Kredit"), "Richtlinie",
            "kreditderivate,cds,risikominderung,crm,basel",
            fischer, new DateTime(2025, 11, 20),
            @"<h2>1. Zielsetzung</h2>
<p>Kreditderivate und andere Instrumente zur Kreditrisikominderung dürfen eingesetzt werden, um Klumpenrisiken zu reduzieren oder Kapitalanforderungen zu optimieren.</p>
<h2>2. Zugelassene Instrumente</h2>
<ul><li>Credit Default Swaps (CDS)</li><li>Garantien und Bürgschaften</li><li>Kreditverbriefungen mit signifikantem Risikotransfer</li></ul>
<h2>3. Anerkennung</h2>
<p>Eine Anerkennung als Risikominderung im Sinne von CRR erfolgt nur bei Erfüllung der Mindeststandards (Bonität des Sicherungsgebers, rechtliche Wirksamkeit, Laufzeitkongruenz).</p>
<h2>4. Counterparty-Risiko</h2>
<p>Kontrahentenrisiken werden gesondert überwacht und durch Limits begrenzt. Collaterals und Variation Margin werden täglich abgestimmt.</p>
<h2>5. Reporting</h2>
<p>Quartalsweises Reporting an die Risikosteuerung und Geschäftsleitung über Volumen, Effekte auf RWA und Ausgleichsleistungen.</p>");

        // ===================== KUNDE / KONTO =====================
        await Dok("Verfahrensanweisung Kundenklassifizierung",
            "Einstufung von Kunden in MiFID-II-Klassen sowie GwG-Risikoklassen.",
            Kap("Kunde / Konto"), null, "Verfahrensanweisung",
            "kundenklassifizierung,mifid,kleinanleger,professionell,risikoklasse",
            mueller, new DateTime(2025, 5, 28),
            @"<h2>1. MiFID-II-Klassen</h2>
<ul><li><strong>Kleinanleger:</strong> Höchster Schutz</li><li><strong>Professioneller Kunde:</strong> Reduzierter Schutz, bei Bedarf herabstufbar</li><li><strong>Geeignete Gegenpartei:</strong> Eingeschränkter Schutz, nur für institutionelle Kunden</li></ul>
<h2>2. Wechsel der Klassifizierung</h2>
<p>Ein Kunde kann auf eigenen Wunsch heraufstufen lassen, sofern die quantitativen und qualitativen Voraussetzungen vorliegen (Vermögen, Erfahrung, Berufstätigkeit).</p>
<h2>3. GwG-Risikoklassifizierung</h2>
<p>Parallel werden Kunden anhand des Geldwäscherisikos in die Klassen niedrig, mittel, hoch eingestuft. Faktoren: Branche, Land, Produkt, persönliches Risiko.</p>
<h2>4. Dokumentation</h2>
<p>Die Einstufung wird im Kundenstamm dokumentiert. Eine Aktualisierung erfolgt anlassbezogen sowie mindestens alle zwei Jahre.</p>
<h2>5. Auswirkungen</h2>
<p>Die Klassifizierung steuert KYC-Tiefe, Sorgfaltsumfang, Reportingpflichten und Produktangebote.</p>");

        await Dok("Richtlinie zur Kontoinaktivität und Nachlassbearbeitung",
            "Behandlung inaktiver Konten und Konten verstorbener Kunden.",
            Kap("Kunde / Konto"), null, "Richtlinie",
            "inaktiv,nachlass,erbschein,§37-kwg,nachlassgericht",
            mueller, new DateTime(2025, 8, 19),
            @"<h2>1. Inaktive Konten</h2>
<p>Konten ohne Kundenaktivität von mindestens 24 Monaten werden als inaktiv markiert. Der Kunde wird schriftlich kontaktiert und um Bestätigung gebeten.</p>
<h2>2. Schließung</h2>
<p>Bleiben Reaktionen aus, kann die Bank das Konto nach Ablauf zusätzlicher 6 Monate schließen. Restbeträge werden gemäß § 372 BGB hinterlegt.</p>
<h2>3. Nachlassbearbeitung</h2>
<p>Bei Tod des Kontoinhabers werden Verfügungen ausschließlich auf Vorlage eines Erbscheins, eines Testaments mit Eröffnungsprotokoll oder einer notariellen Vollmacht zugelassen.</p>
<h2>4. Daueraufträge</h2>
<p>Daueraufträge und Lastschriftmandate werden zum Zeitpunkt des Todes überprüft und ggf. ausgesetzt. Lebensnotwendige Zahlungen (Miete, Strom) können fortgeführt werden.</p>
<h2>5. Mitteilungspflichten</h2>
<p>Erbschaftsteuerlich relevante Bestände werden gemäß § 33 ErbStG dem zuständigen Finanzamt gemeldet.</p>");

        await Dok("Verfahrensanweisung SEPA-Zahlungsverkehr",
            "Abwicklung von SEPA-Überweisungen, -Lastschriften und Echtzeitzahlungen.",
            Kap("Kunde / Konto"), null, "Verfahrensanweisung",
            "sepa,instant,iban,zahlungsverkehr,sct,sdd",
            mueller, new DateTime(2025, 6, 18),
            @"<h2>1. Anwendungsbereich</h2>
<p>Diese Anweisung gilt für SEPA Credit Transfer (SCT), SEPA Direct Debit (SDD) und SEPA Instant Credit Transfer (SCT Inst).</p>
<h2>2. Plausibilitätsprüfungen</h2>
<ul><li>IBAN-Prüfziffer</li><li>BIC-Validierung (sofern verpflichtend)</li><li>Sanktionsscreening</li><li>Limit- und Bestandsprüfung</li></ul>
<h2>3. Ausführungsfristen</h2>
<p>SCT D+1, Eilüberweisung D+0, Instant Payment innerhalb von 10 Sekunden mit 24/7-Verfügbarkeit.</p>
<h2>4. Rückgaben</h2>
<p>Lastschrift-Rückgaben sind innerhalb von 8 Wochen ohne Begründung möglich, bei nicht autorisierten Lastschriften innerhalb von 13 Monaten.</p>
<h2>5. Reklamationen</h2>
<p>Beschwerden zu falsch ausgeführten Zahlungen werden innerhalb von 5 Werktagen geprüft. Im Schadensfall greift die haftungsrechtliche Regelung des § 675y BGB.</p>");

        await Dok("Richtlinie zum Kontowechselservice",
            "Unterstützung von Kunden beim Wechsel des Zahlungskontos.",
            Kap("Kunde / Konto"), null, "Richtlinie",
            "kontowechsel,zkg,§20,zahlungskonto",
            mueller, new DateTime(2025, 3, 12),
            @"<h2>1. Gesetzlicher Rahmen</h2>
<p>Der Kontowechselservice ist nach § 20 Zahlungskontengesetz (ZKG) verpflichtend. Der Kunde wählt zwischen kostenpflichtiger Komfort- und kostenfreier Basisleistung.</p>
<h2>2. Ablauf</h2>
<ol><li>Erteilung der Kontowechselermächtigung durch den Kunden</li><li>Anforderung der wiederkehrenden Zahlungen bei der bisherigen Bank (12 Monate)</li><li>Information der Zahlungsempfänger und -pflichtigen</li><li>Abschluss innerhalb von 12 Werktagen</li></ol>
<h2>3. Pflichten der bisherigen Bank</h2>
<p>Die bisherige Bank ist verpflichtet, alle wiederkehrenden Zahlungen sowie Saldostände innerhalb von 5 Werktagen bereitzustellen.</p>
<h2>4. Schadensersatz</h2>
<p>Schäden, die durch Pflichtverletzung im Wechselprozess entstehen, werden gemäß ZKG ersetzt.</p>
<h2>5. Dokumentation</h2>
<p>Der Wechselprozess wird vollständig im System dokumentiert und 5 Jahre aufbewahrt.</p>");

        await Dok("Verfahrensanweisung Pfändungs- und P-Konto-Bearbeitung",
            "Behandlung von Pfändungen und Pfändungsschutzkonten.",
            Kap("Kunde / Konto"), null, "Verfahrensanweisung",
            "pfändung,p-konto,§850k,zpo,bescheinigung",
            mueller, new DateTime(2025, 10, 8),
            @"<h2>1. Eingang von Pfändungen</h2>
<p>Pfändungs- und Überweisungsbeschlüsse werden zentral durch die Pfändungsstelle bearbeitet. Eingang und Wirksamkeit werden taggleich erfasst.</p>
<h2>2. P-Konto-Umwandlung</h2>
<p>Auf Verlangen des Kunden wird das Konto innerhalb von 4 Werktagen in ein Pfändungsschutzkonto (P-Konto) umgewandelt. Es kann nur ein P-Konto pro Kunde geführt werden.</p>
<h2>3. Freibeträge</h2>
<p>Der Grundfreibetrag wird ohne Bescheinigung gewährt. Erhöhungen für Unterhaltsverpflichtete oder Sozialleistungen werden auf Vorlage einer Bescheinigung berücksichtigt.</p>
<h2>4. Drittschuldnererklärung</h2>
<p>Innerhalb von zwei Wochen nach Pfändungseingang wird die Drittschuldnererklärung gegenüber dem Gläubiger abgegeben.</p>
<h2>5. Mitteilung an SCHUFA</h2>
<p>Pfändungsmaßnahmen werden datenschutzkonform an die Auskunfteien übermittelt.</p>");

        // ===================== MARKETING =====================
        await Dok("Richtlinie Werbung für Finanzprodukte",
            "Vorgaben für werbliche Aussagen zu Finanzprodukten.",
            Kap("Marketing"), null, "Richtlinie",
            "werbung,finanzprodukt,wphg,kid,prospekt",
            editor, new DateTime(2025, 5, 6),
            @"<h2>1. Grundsätze</h2>
<p>Werbliche Aussagen zu Finanzprodukten sind eindeutig, fair und nicht irreführend. Hinweise auf Risiken und Kosten dürfen nicht weniger auffällig dargestellt werden als Vorteile.</p>
<h2>2. Pflichtangaben</h2>
<ul><li>Warnhinweise auf Risiken und mögliche Verluste</li><li>Hinweis auf Beratungspflicht und Geeignetheitsprüfung</li><li>Verweis auf Basisinformationsblatt (KID/PRIIP)</li></ul>
<h2>3. Performance-Angaben</h2>
<p>Vergangenheitsbezogene Angaben werden mit dem Hinweis versehen, dass sie kein verlässlicher Indikator für die zukünftige Wertentwicklung sind.</p>
<h2>4. Freigabe</h2>
<p>Werbematerialien werden vor Veröffentlichung von der Compliance-Abteilung freigegeben. Die Freigabe wird im Marketing-Workflow dokumentiert.</p>
<h2>5. Archivierung</h2>
<p>Alle veröffentlichten Werbemittel werden 5 Jahre archiviert (regulatorische Anforderung).</p>");

        await Dok("Verfahrensanweisung Newsletter und E-Mail-Marketing",
            "Rechtssichere Versendung kommerzieller E-Mails und Newsletter.",
            Kap("Marketing"), null, "Verfahrensanweisung",
            "newsletter,email,double-opt-in,uwg,abmeldung",
            editor, new DateTime(2025, 7, 1),
            @"<h2>1. Einwilligung</h2>
<p>Der Versand von Marketing-E-Mails ist nur mit ausdrücklicher Einwilligung des Empfängers zulässig (Double-Opt-In).</p>
<h2>2. Bestandskunden</h2>
<p>Eine Werbung an Bestandskunden ist im Rahmen des § 7 Abs. 3 UWG für ähnliche Produkte zulässig, sofern bei Erhebung und in jeder E-Mail eine Widerspruchsmöglichkeit angeboten wird.</p>
<h2>3. Pflichtangaben</h2>
<ul><li>Vollständiges Impressum</li><li>Eindeutige Absenderkennung (kein Spoofing)</li><li>Abmeldelink in jedem Newsletter</li><li>Datenschutzerklärung</li></ul>
<h2>4. Tracking</h2>
<p>Tracking-Pixel und Click-Tracking sind nur mit Einwilligung gemäß TTDSG zulässig. Datensparsamkeit ist zu wahren.</p>
<h2>5. Bounces und Sperrlisten</h2>
<p>Hard Bounces werden sofort, Soft Bounces nach drei Versuchen aus dem Verteiler entfernt. Sperrlisten werden zentral gepflegt.</p>");

        await Dok("Richtlinie Sponsoring und Spenden",
            "Vorgaben für die Vergabe von Sponsorings und Spenden.",
            Kap("Marketing"), null, "Richtlinie",
            "sponsoring,spenden,gemeinnützigkeit,korruption",
            editor, new DateTime(2025, 9, 2),
            @"<h2>1. Zielsetzung</h2>
<p>Sponsoring und Spenden unterstützen gesellschaftliches Engagement der Bank ohne ungebührliche Vorteilsgewährung.</p>
<h2>2. Genehmigungsstufen</h2>
<ul><li>Bis 1.000 EUR: Bereichsleiter</li><li>1.000 - 5.000 EUR: Marketing-Leitung + Compliance</li><li>Über 5.000 EUR: Geschäftsleitung</li></ul>
<h2>3. Ausschlüsse</h2>
<p>Spenden an politische Parteien sind ausgeschlossen. Sponsorings an Empfänger mit Bezug zu Geschäftsentscheidungen der Bank sind unzulässig.</p>
<h2>4. Vertragliche Grundlage</h2>
<p>Jedes Sponsoring/Spenden wird vertraglich vereinbart. Bei Spenden ist eine Spendenbescheinigung anzufordern.</p>
<h2>5. Transparenz</h2>
<p>Eine jährliche Übersicht aller Sponsorings und Spenden wird der Geschäftsleitung und der Compliance vorgelegt.</p>");

        await Dok("Verfahrensanweisung Pressearbeit und Krisenkommunikation",
            "Externe Kommunikation in Routine- und Krisensituationen.",
            Kap("Marketing"), null, "Verfahrensanweisung",
            "presse,krise,kommunikation,statement,sprecher",
            editor, new DateTime(2025, 2, 10),
            @"<h2>1. Routine-Pressearbeit</h2>
<p>Pressemitteilungen werden ausschließlich durch die Pressesprecher der Bank veröffentlicht. Mitarbeiter dürfen ohne Genehmigung keine Stellungnahmen abgeben.</p>
<h2>2. Krisenkommunikation</h2>
<p>Im Krisenfall (z.B. Cyberangriff, Reputationsereignis) wird der Krisenstab aktiviert. Alle externen Aussagen werden zentral gesteuert.</p>
<h2>3. Sprachregelung</h2>
<p>Die zentrale Sprachregelung wird zwischen Geschäftsleitung, Pressestelle, Recht und Compliance abgestimmt und allen Sprechern bereitgestellt.</p>
<h2>4. Social Media</h2>
<p>Im Krisenfall wird Social-Media-Monitoring aktiviert. Inkorrekte Behauptungen werden zeitnah, sachlich und faktenbasiert richtiggestellt.</p>
<h2>5. Nachbereitung</h2>
<p>Nach Krisen wird eine Lessons-Learned-Analyse durchgeführt und das Krisenhandbuch aktualisiert.</p>");

        // ===================== PERSONAL =====================
        await Dok("Richtlinie Arbeitszeit und mobiles Arbeiten",
            "Regelungen zu Arbeitszeit, Pausen, Vertrauensarbeitszeit und mobilem Arbeiten.",
            Kap("Personal"), Team("Personal"), "Richtlinie",
            "arbeitszeit,homeoffice,mobiles-arbeiten,vertrauensarbeitszeit",
            wagner, new DateTime(2025, 3, 5),
            @"<h2>1. Geltungsbereich</h2>
<p>Diese Richtlinie regelt die Arbeitszeit aller Beschäftigten und die Möglichkeit des mobilen Arbeitens außerhalb der Büroräume.</p>
<h2>2. Arbeitszeitmodelle</h2>
<ul><li>Vertrauensarbeitszeit für Führungskräfte und definierte Funktionen</li><li>Gleitzeit für die Mehrheit der Mitarbeiter (Kernzeit 9:00 - 15:00 Uhr)</li><li>Schichtmodelle für ausgewählte IT-Funktionen</li></ul>
<h2>3. Mobiles Arbeiten</h2>
<p>Mobiles Arbeiten bis zu 60% der monatlichen Arbeitszeit ist möglich, sofern fachliche Tätigkeit dies zulässt und eine Vereinbarung mit dem Vorgesetzten besteht.</p>
<h2>4. Datenschutz und Informationssicherheit</h2>
<p>Beim mobilen Arbeiten sind Bildschirmsperre, Kopfhörer für vertrauliche Gespräche und sichere Internetverbindungen zu verwenden. Drucken außerhalb der Bank ist nicht zulässig.</p>
<h2>5. Erreichbarkeit</h2>
<p>Erreichbarkeitserwartungen außerhalb der regulären Arbeitszeit sind ausdrücklich nicht vorgesehen. Eingehende Mails außerhalb der Kernzeit müssen nicht beantwortet werden.</p>");

        await Dok("Verfahrensanweisung Onboarding neuer Mitarbeiter",
            "Strukturierter Onboarding-Prozess für neue Beschäftigte.",
            Kap("Personal"), Team("Personal"), "Verfahrensanweisung",
            "onboarding,einarbeitung,checkliste,patenmodell",
            wagner, new DateTime(2025, 6, 10),
            @"<h2>1. Vor Eintritt</h2>
<ul><li>Vertragsunterzeichnung und Personalakte angelegt</li><li>Arbeitsplatz, IT-Equipment und Zugänge bestellt</li><li>Pate / Mentor zugewiesen</li><li>Onboarding-Plan an neue Mitarbeitende und Vorgesetzte verteilt</li></ul>
<h2>2. Erster Tag</h2>
<p>Begrüßung durch HR, Übergabe der Arbeitsmittel, Einführung in die Bank, Vorstellung im Team.</p>
<h2>3. Erste Wochen - Pflichtschulungen</h2>
<ul><li>Compliance und Code of Conduct</li><li>Geldwäscheprävention</li><li>Informationssicherheit und Datenschutz</li><li>IT-Grundkenntnisse (Tools, Sicherheitsregeln)</li></ul>
<h2>4. 100-Tage-Plan</h2>
<p>Innerhalb der ersten 100 Tage finden strukturierte Feedbackgespräche statt (30, 60, 100 Tage). Lernziele und Integrationsfortschritte werden besprochen.</p>
<h2>5. Probezeitende</h2>
<p>Vor Ablauf der Probezeit erfolgt ein gemeinsames Resümee von Mitarbeiter, Vorgesetztem und HR. Entscheidung über Festanstellung wird dokumentiert.</p>");

        await Dok("Richtlinie Vergütung und variable Gehaltsbestandteile",
            "Grundsätze der fixen und variablen Vergütung gemäß InstitutsVergV.",
            Kap("Personal"), Team("Personal"), "Richtlinie",
            "vergütung,bonus,instv,risk-taker,malus",
            wagner, new DateTime(2025, 4, 25),
            @"<h2>1. Vergütungsgrundsätze</h2>
<p>Die Vergütungssysteme der Bank fördern nachhaltige Leistungen und stehen im Einklang mit der Geschäfts- und Risikostrategie sowie der InstitutsVergV.</p>
<h2>2. Fixe Vergütung</h2>
<p>Die fixe Vergütung deckt das Tätigkeitsprofil ab und ist marktgerecht. Sie wird jährlich auf Marktangemessenheit überprüft.</p>
<h2>3. Variable Vergütung</h2>
<p>Variable Vergütung berücksichtigt Unternehmens-, Bereichs- und individuelle Ziele und ist auf maximal 100% der fixen Vergütung begrenzt (Bonus Cap).</p>
<h2>4. Risk-Taker</h2>
<p>Für identifizierte Risk-Taker gelten spezielle Anforderungen: Aufschub von 40-60% über mindestens 5 Jahre, Malus- und Clawback-Regelungen, Auszahlung in nicht-bar Instrumenten möglich.</p>
<h2>5. Vergütungsausschuss</h2>
<p>Der Vergütungsausschuss überwacht die Angemessenheit der Vergütungssysteme und genehmigt grundlegende Änderungen.</p>");

        await Dok("Verfahrensanweisung Schulungs- und Weiterbildungsmanagement",
            "Planung, Durchführung und Dokumentation von Pflicht- und Wahlschulungen.",
            Kap("Personal"), Team("Personal"), "Verfahrensanweisung",
            "schulung,weiterbildung,lms,pflichtschulung",
            wagner, new DateTime(2025, 9, 8),
            @"<h2>1. Bedarfsermittlung</h2>
<p>Schulungsbedarf wird jährlich aus regulatorischen Anforderungen, Mitarbeitergesprächen und strategischen Zielen abgeleitet.</p>
<h2>2. Pflichtschulungen</h2>
<p>Jährliche Pflichtschulungen umfassen: Compliance, Geldwäscheprävention, Informationssicherheit, Datenschutz, MAR, MiFID II (für betroffene Mitarbeiter).</p>
<h2>3. Lernplattform</h2>
<p>Schulungen werden über das zentrale Lern-Management-System (LMS) ausgerollt. Teilnahme und Abschluss werden automatisiert dokumentiert.</p>
<h2>4. Eskalation</h2>
<p>Bei Nichtteilnahme an Pflichtschulungen erfolgt eine erste Erinnerung nach 14 Tagen, eine zweite nach 30 Tagen. Anschließend wird der Vorgesetzte informiert.</p>
<h2>5. Wirksamkeitsmessung</h2>
<p>Die Wirksamkeit von Schulungen wird mit Tests, Umfragen und Stichproben geprüft. Ergebnisse fließen in die Schulungsplanung des Folgejahres ein.</p>");

        await Dok("Richtlinie Antidiskriminierung und Diversität",
            "Gleichbehandlung, Diversität und Inklusion am Arbeitsplatz.",
            Kap("Personal"), Team("Personal"), "Richtlinie",
            "antidiskriminierung,agg,diversität,inklusion,gleichbehandlung",
            wagner, new DateTime(2025, 7, 19),
            @"<h2>1. Selbstverpflichtung</h2>
<p>Die Bank fördert Vielfalt und Chancengleichheit. Diskriminierung aufgrund von Alter, Geschlecht, ethnischer Herkunft, Religion, Behinderung, sexueller Orientierung oder Identität wird nicht toleriert.</p>
<h2>2. AGG-Beschwerdestelle</h2>
<p>Die Beschwerdestelle nach § 13 AGG ist zentral angesiedelt. Beschwerden werden vertraulich und zeitnah bearbeitet.</p>
<h2>3. Diversitätsziele</h2>
<ul><li>Erhöhung des Frauenanteils in Führungspositionen</li><li>Förderung von altersdiversen Teams</li><li>Inklusion schwerbehinderter Mitarbeiter (mind. 5%)</li></ul>
<h2>4. Maßnahmen</h2>
<p>Diversitätstrainings, Mentoring-Programme, anonyme Bewerbungsverfahren und barrierefreie Arbeitsplätze sind etabliert.</p>
<h2>5. Berichterstattung</h2>
<p>Die Geschäftsleitung erhält jährlich einen Bericht zu Diversitätskennzahlen und ergriffenen Maßnahmen.</p>");

        // ===================== PROZESS-/PROJEKTMANAGEMENT =====================
        await Dok("Richtlinie Prozessmanagement",
            "Lebenszyklus von Geschäftsprozessen - Identifikation, Modellierung, Verbesserung.",
            Kap("Prozess-/Projektmanagement und Organisation"), null, "Richtlinie",
            "prozessmanagement,bpm,bpmn,kvp,prozesslandkarte",
            editor, new DateTime(2025, 3, 22),
            @"<h2>1. Prozesslandkarte</h2>
<p>Die Bank pflegt eine zentrale Prozesslandkarte mit Steuerungs-, Kern- und Unterstützungsprozessen. Verantwortliche Prozessowner sind benannt.</p>
<h2>2. Modellierungsstandard</h2>
<p>Geschäftsprozesse werden in BPMN 2.0 modelliert. Modelle werden im zentralen Prozessrepository gepflegt und versioniert.</p>
<h2>3. Prozessfreigabe</h2>
<p>Neue oder wesentlich geänderte Prozesse werden vom Prozessowner zusammen mit Compliance, Datenschutz und ISB freigegeben.</p>
<h2>4. Kontinuierliche Verbesserung</h2>
<p>Mitarbeiter können Verbesserungsvorschläge im KVP-Tool einreichen. Bewährte Vorschläge werden umgesetzt und prämiert.</p>
<h2>5. Prozesskennzahlen</h2>
<p>Pro Kernprozess werden KPI definiert (Durchlaufzeit, Fehlerquote, Kundenzufriedenheit). Reporting an die Geschäftsleitung erfolgt quartalsweise.</p>");

        await Dok("Verfahrensanweisung Geschäftsmodellanalyse",
            "Methodisches Vorgehen zur Bewertung der Geschäftsmodellrentabilität (BMA).",
            Kap("Prozess-/Projektmanagement und Organisation"), null, "Verfahrensanweisung",
            "bma,geschäftsmodell,marisk,strategie,rentabilität",
            editor, new DateTime(2025, 8, 5),
            @"<h2>1. Zweck</h2>
<p>Die Geschäftsmodellanalyse bewertet die Tragfähigkeit des Geschäftsmodells unter Stress- und Normalszenarien gemäß MaRisk.</p>
<h2>2. Bestandteile</h2>
<ul><li>Marktumfeld- und Wettbewerbsanalyse</li><li>Geschäftsfeldportfolio</li><li>Profitabilitäts- und Effizienzanalyse</li><li>Strategische Roadmap</li></ul>
<h2>3. Frequenz</h2>
<p>Die BMA wird mindestens jährlich durchgeführt sowie anlassbezogen bei wesentlichen Änderungen des Geschäftsmodells oder des Marktumfelds.</p>
<h2>4. Stresstesting</h2>
<p>Im Rahmen der BMA werden mindestens drei Szenarien geprüft: Basis, mild adverse, severely adverse. Auswirkungen auf RoE, GuV und Liquidität werden bewertet.</p>
<h2>5. Berichterstattung</h2>
<p>Die Ergebnisse werden der Geschäftsleitung und dem Aufsichtsrat berichtet und fließen in die strategische Planung ein.</p>");

        await Dok("Richtlinie Auslagerungsmanagement",
            "Steuerung von Auslagerungen gemäß DORA und MaRisk AT 9.",
            Kap("Prozess-/Projektmanagement und Organisation"), null, "Richtlinie",
            "auslagerung,dora,marisk,at9,dienstleister,exit",
            editor, new DateTime(2025, 10, 28),
            @"<h2>1. Definition</h2>
<p>Eine Auslagerung liegt vor, wenn ein anderer Anbieter Tätigkeiten erbringt, die ansonsten von der Bank selbst erbracht würden.</p>
<h2>2. Klassifikation</h2>
<ul><li><strong>Wesentliche Auslagerung:</strong> Tätigkeiten, deren Ausfall die Geschäftstätigkeit beeinträchtigen könnte</li><li><strong>Nicht-wesentlich:</strong> Geringe Bedeutung</li></ul>
<h2>3. Auslagerungsregister</h2>
<p>Sämtliche Auslagerungen werden in einem zentralen Register erfasst und gemäß DORA-Vorgaben dokumentiert.</p>
<h2>4. Risikobewertung und Due Diligence</h2>
<p>Vor Vertragsschluss wird eine Risikoanalyse und Due Diligence (finanziell, technisch, operativ) durchgeführt. Wesentliche Auslagerungen erfordern Geschäftsleitungsentscheidung.</p>
<h2>5. Exit-Strategie</h2>
<p>Für jede wesentliche Auslagerung ist eine Exit-Strategie zu dokumentieren: Rücknahme in Eigenbetrieb oder Wechsel des Dienstleisters innerhalb definierter Zeit.</p>");

        await Dok("Verfahrensanweisung Lessons Learned",
            "Strukturierte Auswertung von Projekten und Vorfällen zur Wissenssicherung.",
            Kap("Prozess-/Projektmanagement und Organisation"), null, "Verfahrensanweisung",
            "lessons-learned,projektabschluss,wissensmanagement,kvp",
            editor, new DateTime(2025, 5, 30),
            @"<h2>1. Anwendungsfälle</h2>
<p>Lessons-Learned-Sessions werden nach Abschluss aller Projekte ab 30 PT, nach Major Incidents und nach Audits durchgeführt.</p>
<h2>2. Methodik</h2>
<ul><li>What worked well? - Erfolgsfaktoren</li><li>What didn't work? - Hindernisse und Probleme</li><li>What can be improved? - Verbesserungsvorschläge</li><li>Action items - konkrete Maßnahmen mit Verantwortlichen</li></ul>
<h2>3. Teilnehmer</h2>
<p>Projektteam, Steering Committee, ggf. Stakeholder. Bei Incidents zusätzlich CSIRT und betroffene Fachbereiche.</p>
<h2>4. Wissensdatenbank</h2>
<p>Lessons werden zentral in der Wissensdatenbank dokumentiert und nach Themen und Stichworten verschlagwortet.</p>
<h2>5. Wirksamkeitskontrolle</h2>
<p>Die Umsetzung der Maßnahmen wird im PMO nachgehalten. Wiederkehrende Probleme führen zur Anpassung von Prozessen.</p>");

        await Dok("Richtlinie Lenkungsausschuss-Reporting",
            "Reporting-Standard für Lenkungsausschüsse von Projekten und Initiativen.",
            Kap("Prozess-/Projektmanagement und Organisation"), null, "Richtlinie",
            "steco,lenkungsausschuss,projektreport,ampelstatus",
            editor, new DateTime(2025, 11, 8),
            @"<h2>1. Tagungsfrequenz</h2>
<p>Lenkungsausschüsse tagen mindestens monatlich. Bei kritischen Statusänderungen erfolgen Ad-hoc-Sitzungen.</p>
<h2>2. Statusbericht</h2>
<p>Vor jeder Sitzung wird ein einheitlicher Statusbericht erstellt:</p>
<ul><li>Ampel: Status (Zeit, Budget, Qualität)</li><li>Erreichte Meilensteine seit letzter Sitzung</li><li>Risiken und Maßnahmen</li><li>Anstehende Entscheidungsvorlagen</li></ul>
<h2>3. Eskalation</h2>
<p>Bei Ampelstatus rot werden Eskalationspfade aktiviert. Eskalationen werden unverzüglich der Geschäftsleitung berichtet.</p>
<h2>4. Entscheidungsvorlagen</h2>
<p>Vorlagen werden mindestens 5 Werktage vor der Sitzung verteilt. Sie enthalten Hintergrund, Optionen, Empfehlung und Auswirkungen.</p>
<h2>5. Protokollierung</h2>
<p>Beschlüsse werden im Protokoll mit Verantwortlichen und Fristen festgehalten und im PMO-System nachverfolgt.</p>");

        // ===================== REVISION =====================
        await Dok("Verfahrensanweisung Revisionsprüfung",
            "Ablauf einer Prüfung der internen Revision.",
            Kap("Revision und Kontrollen"), Team("Revision und Kontrollen"), "Verfahrensanweisung",
            "revision,prüfung,bp,marisk,follow-up",
            wagner, new DateTime(2025, 6, 28),
            @"<h2>1. Auftragserteilung</h2>
<p>Die Prüfungen werden im Jahresprüfplan festgelegt. Sonderprüfungen werden anlassbezogen durch die Geschäftsleitung beauftragt.</p>
<h2>2. Prüfungsvorbereitung</h2>
<p>In der Vorbereitung werden Prüfziele, Prüfungsumfang, Risikoschwerpunkte und Datenanforderungen definiert. Eine Auftaktbesprechung mit dem geprüften Bereich erfolgt vor Beginn.</p>
<h2>3. Prüfungsdurchführung</h2>
<p>Prüfungshandlungen umfassen Datenanalysen, Stichproben, Interviews und Walkthrough-Tests. Ergebnisse werden in Arbeitspapieren revisionssicher dokumentiert.</p>
<h2>4. Berichterstellung</h2>
<p>Der Bericht enthält Sachverhalte, Bewertungen, Empfehlungen und vereinbarte Maßnahmen. Feststellungen werden nach Schweregrad (gering, mittel, schwer) klassifiziert.</p>
<h2>5. Follow-up</h2>
<p>Die Umsetzung vereinbarter Maßnahmen wird quartalsweise nachverfolgt und an die Geschäftsleitung berichtet.</p>");

        await Dok("Richtlinie Internes Kontrollsystem (IKS)",
            "Aufbau und Pflege des internen Kontrollsystems.",
            Kap("Revision und Kontrollen"), Team("Revision und Kontrollen"), "Richtlinie",
            "iks,kontrollen,coso,3lod,verteidigungslinie",
            wagner, new DateTime(2025, 1, 30),
            @"<h2>1. Three Lines of Defense</h2>
<p>Das IKS folgt dem Three-Lines-of-Defense-Modell:</p>
<ul><li>1st Line: Operative Bereiche, eigene Kontrollen</li><li>2nd Line: Compliance, Risikocontrolling</li><li>3rd Line: Interne Revision</li></ul>
<h2>2. Kontrollarten</h2>
<ul><li>Präventive Kontrollen (Vier-Augen-Prinzip, Funktionentrennung)</li><li>Detektive Kontrollen (Abgleiche, Auffälligkeitsanalysen)</li><li>Korrigierende Kontrollen (Abstimmung, Eskalation)</li></ul>
<h2>3. Kontrollkatalog</h2>
<p>Pro Prozess werden Schlüsselkontrollen identifiziert und im zentralen IKS-Tool erfasst. Zuständigkeiten und Frequenzen sind definiert.</p>
<h2>4. Wirksamkeitsprüfung</h2>
<p>Die Wirksamkeit der Kontrollen wird mindestens jährlich durch die 2nd und 3rd Line bewertet.</p>
<h2>5. Reporting</h2>
<p>IKS-Berichte werden quartalsweise an die Geschäftsleitung und jährlich an den Aufsichtsrat übergeben.</p>");

        await Dok("Verfahrensanweisung Kontroll-Selbstbewertung",
            "Periodische Selbstbewertung der Kontrollwirksamkeit durch die Fachbereiche.",
            Kap("Revision und Kontrollen"), Team("Revision und Kontrollen"), "Verfahrensanweisung",
            "csa,selbstbewertung,kontrolle,fachbereich",
            wagner, new DateTime(2025, 7, 11),
            @"<h2>1. Zielsetzung</h2>
<p>Die Control Self Assessment (CSA) versetzt die Fachbereiche in die Lage, die Wirksamkeit ihrer Kontrollen periodisch zu prüfen und zu dokumentieren.</p>
<h2>2. Frequenz</h2>
<p>CSA werden mindestens jährlich durchgeführt. Bei wesentlichen Prozessänderungen anlassbezogen.</p>
<h2>3. Methodik</h2>
<ul><li>Durchsicht der definierten Schlüsselkontrollen</li><li>Bewertung von Design und Wirksamkeit</li><li>Stichprobenartige Tests</li><li>Maßnahmenplan bei Mängeln</li></ul>
<h2>4. Eskalation</h2>
<p>Mängel werden im IKS-Tool erfasst und nachverfolgt. Schwere Mängel werden direkt an die 2nd Line eskaliert.</p>
<h2>5. Qualitätssicherung</h2>
<p>Die 2nd Line führt stichprobenartige Validierungen durch und bewertet die Qualität der CSA.</p>");

        await Dok("Richtlinie Follow-up Maßnahmen",
            "Nachverfolgung der Umsetzung von Prüfungs- und Auditfeststellungen.",
            Kap("Revision und Kontrollen"), Team("Revision und Kontrollen"), "Richtlinie",
            "follow-up,umsetzung,maßnahmen,audit",
            wagner, new DateTime(2025, 9, 14),
            @"<h2>1. Maßnahmenkatalog</h2>
<p>Sämtliche aus Prüfungen, Audits und Sonderprüfungen abgeleiteten Maßnahmen werden zentral im Maßnahmenkatalog erfasst.</p>
<h2>2. Pflichtangaben</h2>
<ul><li>Beschreibung der Maßnahme</li><li>Verantwortlicher Bereich und Person</li><li>Zieltermin</li><li>Zwischenstände</li></ul>
<h2>3. Statusverfolgung</h2>
<p>Status wird monatlich aktualisiert (offen, in Umsetzung, umgesetzt, überfällig). Überfällige Maßnahmen werden eskaliert.</p>
<h2>4. Wirksamkeitskontrolle</h2>
<p>Vor Schließung einer Maßnahme prüft die Revision die Wirksamkeit. Stichproben oder erneute Prüfung erfolgen risikobasiert.</p>
<h2>5. Reporting</h2>
<p>Quartalsweise Berichterstattung an Geschäftsleitung und Aufsichtsrat über Status und überfällige Maßnahmen.</p>");

        await Dok("Verfahrensanweisung Sonderprüfungen",
            "Anlassbezogene Sonderprüfungen außerhalb des Jahresprüfplans.",
            Kap("Revision und Kontrollen"), Team("Revision und Kontrollen"), "Verfahrensanweisung",
            "sonderprüfung,ad-hoc,vorfall,untersuchung",
            wagner, new DateTime(2025, 4, 18),
            @"<h2>1. Auslöser</h2>
<p>Sonderprüfungen werden initiiert bei: Verdacht auf doloses Handeln, Aufsichtsanforderungen, Großschadenereignissen, vermuteten Compliance-Verstößen.</p>
<h2>2. Beauftragung</h2>
<p>Beauftragung erfolgt durch die Geschäftsleitung oder den Aufsichtsrat. Bei Verdacht auf Mitwirkung der Geschäftsleitung erfolgt die Beauftragung direkt durch den Aufsichtsrat.</p>
<h2>3. Vertraulichkeit</h2>
<p>Sonderprüfungen unterliegen erhöhter Vertraulichkeit. Der Personenkreis mit Kenntnis ist auf das absolute Minimum zu beschränken.</p>
<h2>4. Externe Unterstützung</h2>
<p>Bei komplexen Sachverhalten oder Befangenheit interner Stellen können externe Forensiker oder Wirtschaftsprüfer beauftragt werden.</p>
<h2>5. Berichterstattung</h2>
<p>Berichtsempfänger ist der Auftraggeber. Eine angemessene Information weiterer Stellen (Aufsicht, Strafverfolgung) wird in Abstimmung mit Recht und Compliance entschieden.</p>");

        // ===================== SICHERHEITSMASSNAHMEN =====================
        await Dok("Richtlinie Zutrittskontrolle und Gebäudesicherheit",
            "Schutz der Bankgebäude vor unbefugtem Zutritt und physischen Bedrohungen.",
            Kap("Sicherheitsmaßnahmen"), null, "Richtlinie",
            "zutritt,gebäude,physische-sicherheit,kamera,wachschutz",
            lang, new DateTime(2025, 5, 22),
            @"<h2>1. Schutzzonen</h2>
<p>Die Bankgebäude sind in Schutzzonen eingeteilt: Öffentlich, Halböffentlich (Kunde), Sicherheitsbereich (Mitarbeiter), Hochsicherheitsbereich (Tresor, RZ).</p>
<h2>2. Zutrittsmedien</h2>
<p>Zutritte erfolgen über personalisierte Ausweise mit RFID. In Hochsicherheitsbereichen kommt MFA (Ausweis + PIN/Biometrie) zum Einsatz.</p>
<h2>3. Besucherregelung</h2>
<p>Externe Besucher werden am Empfang registriert und erhalten Besucherausweise. Sie sind innerhalb der Bank zu begleiten.</p>
<h2>4. Videoüberwachung</h2>
<p>Außenbereiche und sicherheitsrelevante Innenbereiche werden videoüberwacht. Aufzeichnungen werden 30 Tage gespeichert (DSGVO-konform).</p>
<h2>5. Wachschutz und Alarm</h2>
<p>Wachschutz und Sicherheitstechnik werden 24/7 betrieben. Alarme werden zur Polizei und an den Krisenstab eskaliert.</p>");

        await Dok("Verfahrensanweisung Schlüssel- und Ausweismanagement",
            "Ausgabe, Rückgabe und Sperrung von Schlüsseln und Ausweisen.",
            Kap("Sicherheitsmaßnahmen"), null, "Verfahrensanweisung",
            "schlüssel,ausweis,sperrung,ausgabe",
            lang, new DateTime(2025, 8, 30),
            @"<h2>1. Ausgabe</h2>
<p>Schlüssel und Ausweise werden zentral durch die Sicherheitsabteilung ausgegeben. Empfang wird per Unterschrift dokumentiert.</p>
<h2>2. Berechtigungssystematik</h2>
<p>Berechtigungen werden rollenbasiert vergeben (Need-to-have). Eine Schlüsselhierarchie minimiert die Risiken bei Verlust.</p>
<h2>3. Verlustmeldung</h2>
<p>Bei Verlust ist unverzüglich die Sicherheitsabteilung zu informieren. Der Ausweis wird sofort gesperrt, ggf. Schlösser ausgetauscht.</p>
<h2>4. Inventur</h2>
<p>Halbjährlich erfolgt eine Inventur sämtlicher Schlüssel und Ausweise. Differenzen werden dokumentiert und untersucht.</p>
<h2>5. Austritt</h2>
<p>Bei Austritt eines Mitarbeiters werden alle Schlüssel und Ausweise am letzten Arbeitstag eingezogen. Die Personalabteilung informiert die Sicherheitsabteilung mindestens 3 Werktage im Voraus.</p>");

        await Dok("Richtlinie Brandschutz und Evakuierung",
            "Maßnahmen zur Brandverhütung und Evakuierung der Gebäude.",
            Kap("Sicherheitsmaßnahmen"), null, "Richtlinie",
            "brandschutz,evakuierung,fluchtwege,brandschutzhelfer",
            lang, new DateTime(2025, 2, 18),
            @"<h2>1. Brandverhütung</h2>
<p>Offenes Feuer und Rauchen sind in den Bankgebäuden untersagt. Elektrogeräte werden regelmäßig nach DGUV V3 geprüft.</p>
<h2>2. Brandschutzeinrichtungen</h2>
<ul><li>Rauchmelder in allen Räumen</li><li>Sprinkleranlagen in IT-Bereichen mit Inertgas-Löschung</li><li>Feuerlöscher gemäß ASR A2.2</li><li>Feststellanlagen für Brandschutztüren</li></ul>
<h2>3. Brandschutzhelfer</h2>
<p>In jedem Geschoss sind mindestens zwei ausgebildete Brandschutzhelfer im Einsatz.</p>
<h2>4. Evakuierung</h2>
<p>Im Brandfall werden alle Mitarbeiter über Sirenen und Lautsprecheransagen alarmiert. Evakuierung erfolgt über die ausgeschilderten Fluchtwege.</p>
<h2>5. Übungen</h2>
<p>Mindestens jährlich werden Räumungsübungen durchgeführt. Erkenntnisse fließen in die Aktualisierung der Pläne ein.</p>");

        await Dok("Verfahrensanweisung Bargeldhandhabung",
            "Sichere Annahme, Aufbewahrung und Transport von Bargeld.",
            Kap("Sicherheitsmaßnahmen"), null, "Verfahrensanweisung",
            "bargeld,kasse,tresor,wert-transport",
            lang, new DateTime(2025, 6, 12),
            @"<h2>1. Annahme und Auszahlung</h2>
<p>Bargeldannahme und -auszahlung erfolgen ausschließlich an dafür eingerichteten Kassen. Doppelte Zählung ist verpflichtend ab definierten Schwellenwerten.</p>
<h2>2. Tresorbestände</h2>
<p>Tresorbestände werden täglich nach Geschäftsschluss inventarisiert und dokumentiert. Differenzen werden sofort untersucht.</p>
<h2>3. Werttransport</h2>
<p>Der Werttransport erfolgt durch zertifizierte Dienstleister. Transporte werden organisatorisch und technisch gesichert (versiegelte Container, GPS-Tracking).</p>
<h2>4. Falschgeld</h2>
<p>Erkanntes Falschgeld wird nicht weiterverwendet. Es wird gesondert dokumentiert und an die Bundesbank gemeldet.</p>
<h2>5. Notfälle</h2>
<p>Bei Überfällen oder Bedrohungslagen ist die Sicherheit der Mitarbeiter und Kunden oberstes Gebot. Anweisungen der Täter sind zur Vermeidung von Eskalation zu befolgen.</p>");

        await Dok("Richtlinie Reisesicherheit",
            "Sicherheitsanforderungen für Dienstreisen, insbesondere ins Ausland.",
            Kap("Sicherheitsmaßnahmen"), null, "Richtlinie",
            "reise,dienstreise,ausland,risiko,reisemanagement",
            lang, new DateTime(2025, 10, 30),
            @"<h2>1. Risikoeinstufung</h2>
<p>Reiseziele werden anhand der Auswärtigen-Amts-Bewertung in vier Klassen eingeteilt: niedrig, erhöht, hoch, kritisch.</p>
<h2>2. Genehmigungspflicht</h2>
<ul><li>Niedrig: Reguläre Genehmigung des Vorgesetzten</li><li>Erhöht: Zusätzliche Genehmigung der Sicherheitsabteilung</li><li>Hoch: Geschäftsleitungsentscheidung</li><li>Kritisch: Reisen grundsätzlich untersagt</li></ul>
<h2>3. Vorbereitung</h2>
<p>Reiseantritt ist nur nach absolvierten Briefings (Sicherheit, Kultur, Datenschutz) zulässig. Notfallkontakte und Versicherungsschutz werden geprüft.</p>
<h2>4. IT-Sicherheit auf Reisen</h2>
<p>Mitarbeiter erhalten gehärtete Reisegeräte ohne sensible Bankdaten. Öffentliche WLAN dürfen nur mit VPN genutzt werden.</p>
<h2>5. Verhalten in Krisen</h2>
<p>Bei Sicherheitsvorfällen ist umgehend Kontakt mit dem Krisenstab aufzunehmen. Die Bank koordiniert ggf. Evakuierungen.</p>");

        // ===================== UNTERNEHMENSENTWICKLUNG =====================
        await Dok("Richtlinie Geschäftsmodellweiterentwicklung",
            "Methodisches Vorgehen zur kontinuierlichen Weiterentwicklung des Geschäftsmodells.",
            Kap("Unternehmensentwicklung"), Team("Unternehmensentwicklung"), "Richtlinie",
            "geschäftsmodell,strategie,roadmap,unternehmensentwicklung",
            wagner, new DateTime(2025, 4, 9),
            @"<h2>1. Strategischer Rahmen</h2>
<p>Die Geschäftsmodellweiterentwicklung folgt der mittelfristigen Strategie und der jährlichen Geschäftsmodellanalyse.</p>
<h2>2. Initiativenpipeline</h2>
<p>Strategische Initiativen werden in einer zentralen Pipeline bewertet anhand strategischer Passung, Wirtschaftlichkeit, Risiko und Umsetzbarkeit.</p>
<h2>3. Priorisierung</h2>
<p>Priorisierung erfolgt mindestens halbjährlich im strategischen Steering. Initiativen erhalten Owner, Budget und Meilensteine.</p>
<h2>4. Umsetzung</h2>
<p>Umsetzungen erfolgen über das Projekt-Portfolio-Management. Quartalsweise Status-Reports an die Geschäftsleitung.</p>
<h2>5. Wirksamkeitsmessung</h2>
<p>Pro Initiative werden Erfolgs-KPI definiert (Ertrag, Effizienz, Kundenzufriedenheit). Nach 12 Monaten wird ein Wirksamkeitscheck durchgeführt.</p>");

        await Dok("Verfahrensanweisung Markt- und Wettbewerbsanalyse",
            "Beobachtung des Markt- und Wettbewerbsumfelds.",
            Kap("Unternehmensentwicklung"), Team("Unternehmensentwicklung"), "Verfahrensanweisung",
            "marktanalyse,wettbewerb,benchmark,trends",
            wagner, new DateTime(2025, 7, 28),
            @"<h2>1. Beobachtungsfelder</h2>
<ul><li>Wettbewerber (klassische Banken, Neo-Banken, Fintechs)</li><li>Regulatorische Entwicklungen</li><li>Technologie-Trends</li><li>Kundenbedürfnisse und Marktforschung</li></ul>
<h2>2. Datenquellen</h2>
<p>Branchenstudien, Geschäftsberichte, Konferenzen, Beraterstudien, Kundenbefragungen, Mystery Shopping.</p>
<h2>3. Frequenz</h2>
<p>Quartalsweises Update zu wichtigen Entwicklungen, jährlicher Strategie-Review.</p>
<h2>4. Adressaten</h2>
<p>Reports gehen an Geschäftsleitung, Bereichsleitungen und ausgewählte Stabsfunktionen. Sensible Wettbewerbsdaten sind eingeschränkt zugänglich.</p>
<h2>5. Ableitungen</h2>
<p>Erkenntnisse fließen in die Geschäftsmodellanalyse, Strategiearbeit und Initiativenpipeline ein.</p>");

        await Dok("Richtlinie Strategieprozess",
            "Jährlicher Prozess zur Strategieentwicklung und -fortschreibung.",
            Kap("Unternehmensentwicklung"), Team("Unternehmensentwicklung"), "Richtlinie",
            "strategie,prozess,kalender,planung",
            wagner, new DateTime(2025, 1, 22),
            @"<h2>1. Zeitliche Struktur</h2>
<p>Der Strategieprozess folgt einem festen Jahreskalender mit definierten Meilensteinen.</p>
<h2>2. Beteiligte</h2>
<ul><li>Geschäftsleitung als Eigner</li><li>Bereichsleitung mit Inhaltsbeiträgen</li><li>Strategiebereich als Methoden- und Prozessverantwortliche</li><li>Aufsichtsrat zur Ratifizierung</li></ul>
<h2>3. Phasen</h2>
<ol><li>Q1: Strategischer Review (Ist-Analyse, Geschäftsmodellanalyse)</li><li>Q2: Strategieentwicklung (Workshops, Szenarien)</li><li>Q3: Strategieformulierung (Ziele, Maßnahmen, KPI)</li><li>Q4: Verabschiedung und Kommunikation</li></ol>
<h2>4. Outputs</h2>
<p>Mittelfristplanung, Strategie-Memo, Maßnahmen-Roadmap, Strategie-Cockpit.</p>
<h2>5. Aktualisierung</h2>
<p>Bei wesentlichen Änderungen des Marktumfelds wird die Strategie unterjährig angepasst.</p>");

        await Dok("Verfahrensanweisung Beteiligungsmanagement",
            "Steuerung von Tochtergesellschaften und Beteiligungen.",
            Kap("Unternehmensentwicklung"), Team("Unternehmensentwicklung"), "Verfahrensanweisung",
            "beteiligungen,tochter,governance,reporting",
            wagner, new DateTime(2025, 9, 17),
            @"<h2>1. Beteiligungsstrategie</h2>
<p>Beteiligungen werden ausschließlich gehalten, wenn sie der Strategie der Bank dienen oder regulatorisch erforderlich sind. Bestand wird jährlich überprüft.</p>
<h2>2. Governance</h2>
<p>Pro Beteiligung werden ein Aufsichtsorgan, Reportinglinien und Steuerungsinstrumente festgelegt. Vertretung in Aufsichtsorganen erfolgt durch nominierte Personen.</p>
<h2>3. Reporting</h2>
<p>Quartalsweises Reporting der Beteiligungen an die Geschäftsleitung. Inhalte: Finanzkennzahlen, Risiken, strategische Themen, Compliance.</p>
<h2>4. Ankündigungspflichten</h2>
<p>Wesentliche Vorgänge in Beteiligungen (Akquisitionen, Verkäufe, Großprojekte) werden frühzeitig der Geschäftsleitung angezeigt.</p>
<h2>5. Exit-Entscheidungen</h2>
<p>Verkauf oder Auflösung von Beteiligungen werden nach abgestuftem Genehmigungsprozess entschieden.</p>");

        // ===================== INNOVATION / DIGITALISIERUNG =====================
        await Dok("Richtlinie Einsatz Künstlicher Intelligenz (Hochrisiko)",
            "Erweiterte Anforderungen an Hochrisiko-KI-Systeme gemäß EU AI Act.",
            Kap("Innovation / Digitalisierung"), Team("Unternehmensentwicklung"), "Richtlinie",
            "ki,ai-act,hochrisiko,erklärbarkeit,governance",
            wagner, new DateTime(2025, 11, 18),
            @"<h2>1. Klassifizierung</h2>
<p>Diese Richtlinie ergänzt die Minimales-Risiko-Richtlinie und gilt für KI-Systeme der Hochrisikoklasse gemäß EU AI Act, etwa Kreditscoring oder Bewerber-Tools.</p>
<h2>2. Pflichten vor Inbetriebnahme</h2>
<ul><li>Risikomanagementsystem für die KI</li><li>Datenqualitäts- und Repräsentativitätsanalysen</li><li>Technische Dokumentation</li><li>Konformitätsbewertung</li></ul>
<h2>3. Erklärbarkeit</h2>
<p>Hochrisiko-KI muss erklärbar sein. Eingesetzt werden Verfahren wie SHAP, LIME oder Counterfactual Explanations. Kunden haben Anspruch auf nachvollziehbare Begründungen.</p>
<h2>4. Menschliche Aufsicht</h2>
<p>Entscheidungen mit erheblichen Auswirkungen werden durch Menschen kontrolliert (Human-in-the-Loop). Voll-automatisierte Ablehnungen sind ausgeschlossen.</p>
<h2>5. Monitoring</h2>
<p>Modelle werden auf Drift, Bias und Performance überwacht. Auffälligkeiten führen zur Modell-Revision.</p>");

        await Dok("Verfahrensanweisung Innovation Sandbox",
            "Geschützter Erprobungsraum für neue Technologien und Geschäftsmodelle.",
            Kap("Innovation / Digitalisierung"), Team("Unternehmensentwicklung"), "Verfahrensanweisung",
            "innovation,sandbox,poc,prototyp",
            wagner, new DateTime(2025, 6, 7),
            @"<h2>1. Zweck</h2>
<p>Die Innovation Sandbox erlaubt schnelle, risikoarme Erprobung neuer Technologien außerhalb der Produktivumgebung.</p>
<h2>2. Voraussetzungen</h2>
<ul><li>Schriftlicher Antrag mit Use Case</li><li>Datenschutz-Vorprüfung (i.d.R. anonymisierte Daten)</li><li>Genehmigung durch IT-Leitung und ISB</li></ul>
<h2>3. Technische Trennung</h2>
<p>Die Sandbox ist netzwerkseitig vom Produktivnetz getrennt. Synthetische oder anonymisierte Daten werden bevorzugt.</p>
<h2>4. Begrenzung</h2>
<p>Sandbox-Projekte sind auf 6 Monate begrenzt. Bei erfolgreichem PoC erfolgt die Überführung in den regulären Produktivierungsprozess.</p>
<h2>5. Wissenstransfer</h2>
<p>Ergebnisse werden in einem Demoday präsentiert. Auch gescheiterte PoCs werden offen kommuniziert (Failure-Sharing).</p>");

        await Dok("Richtlinie API-Management und Open Banking",
            "Bereitstellung und Konsumierung von APIs unter PSD2 und Open-Banking-Standards.",
            Kap("Innovation / Digitalisierung"), Team("IT-Service"), "Richtlinie",
            "api,psd2,open-banking,sca,oauth",
            wagner, new DateTime(2025, 3, 30),
            @"<h2>1. Zielsetzung</h2>
<p>APIs werden als strategischer Schnittstellen-Standard für interne und externe Integrationen genutzt.</p>
<h2>2. PSD2-Schnittstellen</h2>
<p>Die nach PSD2 verpflichtenden Schnittstellen (AISP, PISP, CBPII) werden gemäß Berlin-Group-Standard bereitgestellt. Verfügbarkeit und Performance werden überwacht (≥99,5%).</p>
<h2>3. Authentifizierung</h2>
<p>API-Authentifizierung erfolgt über OAuth 2.0 / OpenID Connect. Für Zahlungen wird SCA umgesetzt (Wissen, Besitz, Inhärenz).</p>
<h2>4. Lifecycle</h2>
<p>APIs durchlaufen den Lifecycle Design > Build > Test > Publish > Deprecate. Versionen werden parallel mindestens 12 Monate gepflegt.</p>
<h2>5. Monitoring</h2>
<p>API-Performance, Fehlerquoten und Sicherheitsmetriken werden auf einem zentralen Dashboard überwacht.</p>");

        await Dok("Verfahrensanweisung Proof-of-Concept-Bewertung",
            "Strukturiertes Vorgehen zur Bewertung und Auswahl von PoC-Ergebnissen.",
            Kap("Innovation / Digitalisierung"), Team("Unternehmensentwicklung"), "Verfahrensanweisung",
            "poc,bewertung,scorecard,produktivierung",
            wagner, new DateTime(2025, 8, 22),
            @"<h2>1. PoC-Ziele</h2>
<p>Vor jedem PoC werden Ziele, Hypothesen und Erfolgsmetriken (KPI) festgelegt. Time-Box typischerweise 6-12 Wochen.</p>
<h2>2. Bewertungs-Scorecard</h2>
<table class='table table-bordered'><thead><tr><th>Dimension</th><th>Gewicht</th></tr></thead><tbody>
<tr><td>Strategischer Fit</td><td>20%</td></tr>
<tr><td>Wirtschaftlichkeit</td><td>25%</td></tr>
<tr><td>Technische Machbarkeit</td><td>20%</td></tr>
<tr><td>Risiko und Compliance</td><td>20%</td></tr>
<tr><td>Time-to-Market</td><td>15%</td></tr>
</tbody></table>
<h2>3. Entscheidungsweg</h2>
<p>Die Bewertung wird im Innovation Board vorgestellt. Optionen: Stop, weiter erproben, in Produktivierung überführen.</p>
<h2>4. Übergabe</h2>
<p>Bei Übergabe an die Linie werden Anforderungen, Architektur, Integrationsbedarfe und Betriebskonzepte dokumentiert.</p>
<h2>5. Lessons</h2>
<p>Pro PoC werden Lessons Learned gehalten. Auch Misserfolge generieren Learnings.</p>");

        await Dok("Richtlinie Digital Workplace",
            "Modernes, sicheres und produktives Arbeiten mit digitalen Werkzeugen.",
            Kap("Innovation / Digitalisierung"), Team("IT-Service"), "Richtlinie",
            "digital-workplace,collaboration,m365,zero-trust",
            wagner, new DateTime(2025, 2, 28),
            @"<h2>1. Zielbild</h2>
<p>Der Digital Workplace stellt Mitarbeitern moderne Werkzeuge zur Kommunikation, Kollaboration und Wissensarbeit zur Verfügung.</p>
<h2>2. Standardausstattung</h2>
<ul><li>Laptop mit Vollverschlüsselung und EDR</li><li>Headset und Mobilgerät</li><li>Microsoft 365 (E-Mail, Teams, OneDrive)</li><li>Zentrale Single-Sign-On</li></ul>
<h2>3. Sicherheitsprinzipien</h2>
<p>Zero-Trust-Architektur: Jede Anfrage wird authentifiziert und autorisiert. Geräte werden vor dem Zugriff auf Compliance geprüft (Conditional Access).</p>
<h2>4. Datenklassifizierung</h2>
<p>Dokumente werden nach Schutzbedarf klassifiziert. Vertrauliche Inhalte werden automatisch verschlüsselt und gegen Weiterleitung geschützt (DLP).</p>
<h2>5. Schulung</h2>
<p>Fortlaufende Schulungen und Tipps zur effizienten Nutzung der Werkzeuge sowie zur Sicherheit am digitalen Arbeitsplatz.</p>");

        // ===================== VERWALTUNG =====================
        await Dok("Richtlinie Reisekostenabrechnung",
            "Vorgaben zur Abrechnung von Dienstreisen und Spesen.",
            Kap("Verwaltung"), null, "Richtlinie",
            "reisekosten,spesen,abrechnung,bundesreisekostengesetz",
            wagner, new DateTime(2025, 4, 20),
            @"<h2>1. Anwendungsbereich</h2>
<p>Diese Richtlinie gilt für alle Dienstreisen, einschließlich Schulungs- und Konferenzbesuchen.</p>
<h2>2. Genehmigung</h2>
<p>Dienstreisen werden vor Antritt durch den Vorgesetzten genehmigt. Auslandsreisen erfordern zusätzlich Sicherheits-Briefing.</p>
<h2>3. Erstattungsfähige Kosten</h2>
<ul><li>Fahrtkosten (Bahn 1. Klasse, Flug Economy, Mietwagen Mittelklasse)</li><li>Übernachtungskosten in marktüblichem Rahmen</li><li>Verpflegungspauschalen gemäß steuerlicher Vorschriften</li><li>Notwendige Nebenkosten (Parken, Maut)</li></ul>
<h2>4. Abrechnung</h2>
<p>Reisekosten werden binnen 30 Tagen elektronisch eingereicht. Belege werden hochgeladen oder per App erfasst.</p>
<h2>5. Kontrollen</h2>
<p>Stichprobenartige Prüfung erfolgt durch die Verwaltung. Auffälligkeiten werden mit dem Antragsteller geklärt.</p>");

        await Dok("Verfahrensanweisung Vertragsmanagement",
            "Erfassung, Pflege und Überwachung sämtlicher Verträge der Bank.",
            Kap("Verwaltung"), null, "Verfahrensanweisung",
            "vertrag,vertragsmanagement,clm,wiedervorlage",
            wagner, new DateTime(2025, 7, 14),
            @"<h2>1. Vertragsregister</h2>
<p>Alle wesentlichen Verträge werden im zentralen Contract-Lifecycle-Management-Tool erfasst.</p>
<h2>2. Pflichtangaben</h2>
<ul><li>Vertragspartei und Inhalt</li><li>Laufzeit, Kündigungsfristen</li><li>Verlängerungsoptionen</li><li>Vertragsverantwortlicher</li><li>Wertgrenzen und SLAs</li></ul>
<h2>3. Wiedervorlagen</h2>
<p>Verträge werden 6 Monate vor Ende automatisch dem Verantwortlichen vorgelegt. Verlängerungen oder Neuverhandlungen werden rechtzeitig initiiert.</p>
<h2>4. Vier-Augen-Prinzip</h2>
<p>Verträge werden ab definierten Wertgrenzen im Vier-Augen-Prinzip unterzeichnet. Bei Werten über 100.000 EUR ist die Geschäftsleitung beteiligt.</p>
<h2>5. Vertragsänderungen</h2>
<p>Änderungen werden formal vereinbart und im Tool dokumentiert. Mündliche Nebenabreden sind unzulässig.</p>");

        await Dok("Richtlinie Bewirtungs- und Repräsentationskosten",
            "Erstattung und Dokumentation von Bewirtungen und Repräsentationsausgaben.",
            Kap("Verwaltung"), null, "Richtlinie",
            "bewirtung,repräsentation,steuer,beleg",
            wagner, new DateTime(2025, 6, 22),
            @"<h2>1. Geschäftliche Veranlassung</h2>
<p>Bewirtungen sind nur erstattungsfähig, wenn ein klarer geschäftlicher Anlass vorliegt. Bewirtungen unter Mitarbeitern sind grundsätzlich ausgeschlossen.</p>
<h2>2. Belegpflichten</h2>
<p>Originalbelege müssen Datum, Ort, Bewirtungspersonen, Anlass und Kostenaufstellung enthalten. Trinkgelder werden gesondert ausgewiesen.</p>
<h2>3. Höchstgrenzen</h2>
<ul><li>Mittagessen: 60 EUR pro Person</li><li>Abendessen: 100 EUR pro Person</li><li>Kunde + Begleitung: zusätzliche Genehmigung erforderlich</li></ul>
<h2>4. Genehmigung</h2>
<p>Bewirtungen ab 250 EUR werden vorab durch den Bereichsleiter genehmigt.</p>
<h2>5. Dokumentation</h2>
<p>Bewirtungen werden im Bewirtungstool erfasst. Steuerlich nicht abzugsfähige Anteile werden separat ausgewiesen.</p>");

        await Dok("Verfahrensanweisung Aktenführung und Archivierung",
            "Anforderungen an die ordnungsgemäße Aufbewahrung geschäftlicher Unterlagen.",
            Kap("Verwaltung"), null, "Verfahrensanweisung",
            "akte,archiv,gobd,aufbewahrungsfrist",
            wagner, new DateTime(2025, 5, 16),
            @"<h2>1. Grundsatz</h2>
<p>Geschäftsunterlagen werden gemäß GoBD ordnungsgemäß, vollständig, unveränderlich und zeitnah erfasst und aufbewahrt.</p>
<h2>2. Elektronische Archivierung</h2>
<p>Eingehende Belege werden ersetzend gescannt. Originale werden nach erfolgreicher Erfassung gemäß Verfahrensdokumentation vernichtet.</p>
<h2>3. Aufbewahrungsfristen</h2>
<table class='table table-bordered'><thead><tr><th>Unterlagenart</th><th>Frist</th></tr></thead><tbody>
<tr><td>Buchungsbelege</td><td>10 Jahre</td></tr>
<tr><td>Geschäftsbriefe</td><td>6 Jahre</td></tr>
<tr><td>Gehaltsunterlagen</td><td>10 Jahre</td></tr>
<tr><td>Verträge</td><td>10 Jahre nach Beendigung</td></tr>
</tbody></table>
<h2>4. Zugriffsregelung</h2>
<p>Zugriffe auf Archivsysteme werden protokolliert. Berechtigungen werden rollenbasiert vergeben und regelmäßig rezertifiziert.</p>
<h2>5. Vernichtung</h2>
<p>Nach Ablauf der Aufbewahrungsfrist werden Daten datenschutzgerecht und nachvollziehbar vernichtet.</p>");

        await Dok("Richtlinie Postdienste und Kommunikation",
            "Eingangs- und Ausgangspost sowie sichere Kommunikation.",
            Kap("Verwaltung"), null, "Richtlinie",
            "post,kommunikation,kuvert,vertraulich,einschreiben",
            wagner, new DateTime(2025, 9, 6),
            @"<h2>1. Eingangspost</h2>
<p>Zentrale Post wird täglich am Eingang sortiert und digitalisiert. Vertrauliche Post wird ungeöffnet an den Empfänger weitergeleitet.</p>
<h2>2. Ausgangspost</h2>
<p>Standardpost wird zentral frankiert und versandt. Bei sensiblen Inhalten wird Einschreiben mit Rückschein verwendet.</p>
<h2>3. Datenschutz</h2>
<p>Bei Versand personenbezogener Daten an Externe ist auf adressrichtige und vertraulichkeitswahrende Versendung zu achten (Sichtfenster, Doppelkuvertierung).</p>
<h2>4. Elektronische Kommunikation</h2>
<p>Vertrauliche Inhalte werden ausschließlich verschlüsselt versendet. Hierfür stehen S/MIME und ein sicheres Kunden-Postfach zur Verfügung.</p>
<h2>5. Aufbewahrung</h2>
<p>Eingangspost wird systemseitig zugeordnet und gemäß Aufbewahrungsfristen archiviert.</p>");

        // ===================== WERTPAPIERGESCHÄFT =====================
        await Dok("Richtlinie Anlageberatung und Geeignetheitsprüfung",
            "Operative Vorgaben zur Anlageberatung im Sinne von MiFID II.",
            Kap("Wertpapiergeschäft"), Team("Handel"), "Richtlinie",
            "anlageberatung,mifid,geeignetheit,beratungsprotokoll",
            fischer, new DateTime(2025, 5, 12),
            @"<h2>1. Anwendungsbereich</h2>
<p>Diese Richtlinie regelt die Beratung von Privatkunden im Wertpapiergeschäft.</p>
<h2>2. Beratungsschritte</h2>
<ol><li>Erhebung des Anlageprofils</li><li>Definition Anlageziel und Risikobereitschaft</li><li>Produktauswahl unter Beachtung des Zielmarkts</li><li>Geeignetheitserklärung</li><li>Dokumentation und Übergabe</li></ol>
<h2>3. Beratungsprotokoll</h2>
<p>Pro Beratungsgespräch wird ein vollständiges Protokoll erstellt und dem Kunden ausgehändigt.</p>
<h2>4. Telefonische Beratung</h2>
<p>Telefonische Beratungen werden aufgezeichnet und 5 Jahre archiviert.</p>
<h2>5. Schulung der Berater</h2>
<p>Berater verfügen über die erforderliche Sachkunde gemäß WpHG-MaAnzV. Fortbildungen erfolgen jährlich.</p>");

        await Dok("Verfahrensanweisung Anlagenvermittlung",
            "Vermittlung von Wertpapieren ohne Anlageberatung.",
            Kap("Wertpapiergeschäft"), Team("Handel"), "Verfahrensanweisung",
            "anlagevermittlung,beratungsfrei,execution-only",
            fischer, new DateTime(2025, 8, 16),
            @"<h2>1. Abgrenzung</h2>
<p>Anlagenvermittlung erfolgt ohne Beratungsleistung. Der Kunde entscheidet eigenständig über das Geschäft.</p>
<h2>2. Angemessenheitsprüfung</h2>
<p>Bei nicht-execution-only-Geschäften wird eine Angemessenheitsprüfung anhand Kenntnisse und Erfahrungen des Kunden durchgeführt.</p>
<h2>3. Execution-Only</h2>
<p>Bei einfachen Finanzinstrumenten und auf Initiative des Kunden ist Execution-Only ohne Angemessenheitsprüfung zulässig (mit Hinweisen).</p>
<h2>4. Risiko-Hinweise</h2>
<p>Bei nicht angemessenen oder komplexen Produkten erfolgt ein Risikohinweis. Der Kunde bestätigt Kenntnisnahme.</p>
<h2>5. Dokumentation</h2>
<p>Auch im vermittelten Geschäft werden Auftragsannahme, Hinweise und ggf. Angemessenheitsprüfung revisionssicher dokumentiert.</p>");

        await Dok("Richtlinie Vermögensverwaltung",
            "Erbringung der Finanzportfolioverwaltung gemäß WpHG.",
            Kap("Wertpapiergeschäft"), Team("Handel"), "Richtlinie",
            "vermögensverwaltung,portfolio,strategie,benchmark",
            fischer, new DateTime(2025, 11, 25),
            @"<h2>1. Mandat</h2>
<p>Vermögensverwaltungsmandate basieren auf einem schriftlichen Vertrag mit Anlagezielen, Anlagestrategie, Anlagegrenzen und Vergütungsmodell.</p>
<h2>2. Anlagestrategien</h2>
<ul><li>Defensiv (≥70% Anleihen)</li><li>Ausgewogen</li><li>Wachstum (≥60% Aktien)</li><li>Nachhaltig (ESG-Filter)</li></ul>
<h2>3. Compliance-Grenzen</h2>
<p>Pro Strategie werden Grenzen für Einzelpositionen, Branchen, Regionen und Bonität definiert. Verstöße werden automatisch eskaliert.</p>
<h2>4. Reporting an den Kunden</h2>
<p>Kunden erhalten quartalsweise Reportings mit Wertentwicklung, Allokation, größten Positionen und Markt­kommentar.</p>
<h2>5. Performancemessung</h2>
<p>Die Performance wird gegen Benchmarks gemessen und gemäß GIPS-Standards berechnet und veröffentlicht.</p>");

        await Dok("Verfahrensanweisung Depotführung",
            "Verwahrung von Wertpapieren und Abwicklung depotbezogener Vorgänge.",
            Kap("Wertpapiergeschäft"), Team("Handel"), "Verfahrensanweisung",
            "depot,verwahrung,kapitalmaßnahme,corporate-action",
            fischer, new DateTime(2025, 3, 24),
            @"<h2>1. Depotmodelle</h2>
<p>Die Bank bietet Wertpapierdepots als Eigenbestand- und Kundendepots. Drittverwahrung erfolgt über Clearstream und ausgewählte Subverwahrer.</p>
<h2>2. Kapitalmaßnahmen</h2>
<p>Kapitalmaßnahmen (Dividenden, Splits, Bezugsrechte) werden zentral gepflegt und zeitgerecht im Kundendepot gebucht.</p>
<h2>3. Steuerliche Behandlung</h2>
<p>Steuerabzug erfolgt im Rahmen der Kapitalertragsteuer-Logik (Freistellungsauftrag, NV-Bescheinigung). Jahressteuerbescheinigung wird zum Jahresende erstellt.</p>
<h2>4. Depotüberträge</h2>
<p>Eingehende und ausgehende Depotüberträge werden binnen 15 Werktagen abgewickelt. Kosten werden gemäß Preisverzeichnis berechnet.</p>
<h2>5. Abstimmung</h2>
<p>Bestände werden täglich mit Verwahrstellen und Kundendepots abgestimmt. Differenzen werden binnen 24 Stunden geklärt.</p>");

        await Dok("Richtlinie Produktfreigabeprozess",
            "New-Product-Process (NPP) für Wertpapier- und Bankprodukte.",
            Kap("Wertpapiergeschäft"), Team("Handel"), "Richtlinie",
            "npp,produktfreigabe,zielmarkt,produktgovernance",
            fischer, new DateTime(2025, 10, 20),
            @"<h2>1. Anwendungsbereich</h2>
<p>Vor Vertrieb eines neuen Produkts wird der Produktfreigabeprozess durchlaufen. Auch wesentliche Änderungen bestehender Produkte werden geprüft.</p>
<h2>2. Beteiligte Bereiche</h2>
<ul><li>Produktmanagement (Antrag)</li><li>Risikocontrolling, Compliance, Recht, ISB</li><li>Operations und IT</li><li>Geschäftsleitung (finale Freigabe)</li></ul>
<h2>3. Pflichtinhalte</h2>
<p>Produktbeschreibung, Zielmarkt (positiv/negativ), Risiken, regulatorische Einordnung, Auswirkungen auf IT/Operations, Konditionen, Vertriebskanäle.</p>
<h2>4. Zielmarkt</h2>
<p>Pro Produkt werden positiver und negativer Zielmarkt definiert. Vertriebsstellen erhalten Vorgaben zur Anwendung im Beratungsprozess.</p>
<h2>5. Produktreview</h2>
<p>Mindestens jährlich erfolgt ein Produkt-Review (Volumen, Beschwerden, Performance, regulatorische Änderungen).</p>");

        // ===================== VERZEICHNISSE (Hauptkapitel) =====================
        await Dok("Verzeichnis der Auslagerungen (MaRisk AT 9)",
            "Übersicht aller wesentlichen und nicht wesentlichen Auslagerungen.",
            HauptKap("Verzeichnisse"), null, "Verzeichnis",
            "auslagerung,marisk,at9,dora,register",
            wagner, new DateTime(2025, 11, 12),
            @"<h2>Auslagerungsverzeichnis</h2>
<p>Dieses Verzeichnis dokumentiert alle Auslagerungen der Bank gemäß MaRisk AT 9 und DORA Art. 28.</p>
<h3>Wesentliche Auslagerungen</h3>
<table class='table table-bordered'><thead><tr><th>ID</th><th>Dienstleister</th><th>Leistung</th><th>Vertragsbeginn</th><th>Risikoklasse</th></tr></thead><tbody>
<tr><td>AL-001</td><td>RZ-Dienstleister GmbH</td><td>Rechenzentrumsbetrieb Kernbankensystem</td><td>2018-01-01</td><td>Hoch</td></tr>
<tr><td>AL-002</td><td>Cloud-Anbieter EU</td><td>SaaS für Collaboration und E-Mail</td><td>2022-07-01</td><td>Mittel</td></tr>
<tr><td>AL-003</td><td>Wertpapier-Verwahrer AG</td><td>Wertpapierabwicklung und -verwahrung</td><td>2010-01-01</td><td>Hoch</td></tr>
<tr><td>AL-004</td><td>Druck- und Versanddienstleister</td><td>Druck- und Versand Kontoauszüge</td><td>2017-04-01</td><td>Mittel</td></tr>
</tbody></table>
<h3>Nicht-wesentliche Auslagerungen</h3>
<p>Die Liste umfasst weitere 27 nicht-wesentliche Auslagerungen, deren Details im Auslagerungstool gepflegt werden.</p>
<h3>Pflege</h3>
<p>Das Verzeichnis wird mindestens jährlich, bei Änderungen anlassbezogen aktualisiert. Owner: Auslagerungs­beauftragter.</p>");

        await Dok("Verzeichnis der wesentlichen IT-Dienstleister (DORA)",
            "Detailliertes Register kritischer IKT-Drittparteien gemäß DORA.",
            HauptKap("Verzeichnisse"), Team("IT-Service"), "Verzeichnis",
            "dora,ikt,drittpartei,kritisch,register",
            lang, new DateTime(2026, 1, 12),
            @"<h2>IKT-Drittparteienregister</h2>
<p>Das Register erfasst alle IKT-Drittparteien gemäß DORA Art. 28. Vorstand und ESA können jederzeit auf das Register zugreifen.</p>
<h3>Pflichtangaben pro Eintrag</h3>
<ul><li>Eindeutige Vertrags-ID und LEI</li><li>Funktion und Kritikalität</li><li>Vertragsbeginn und Beendigungsmodalitäten</li><li>Subunternehmer (Multi-Vendor-Stack)</li><li>Geographische Konzentration</li><li>Exit-Plan</li></ul>
<h3>Auszug</h3>
<table class='table table-bordered'><thead><tr><th>ID</th><th>Funktion</th><th>Kritikalität</th><th>Subunternehmer</th></tr></thead><tbody>
<tr><td>IKT-101</td><td>Kernbankensystem</td><td>Kritisch</td><td>RZ-Subunternehmer (DE), Software-Hersteller (DE)</td></tr>
<tr><td>IKT-102</td><td>Collaboration Cloud</td><td>Wichtig</td><td>Hyperscaler (EU)</td></tr>
<tr><td>IKT-103</td><td>SIEM</td><td>Wichtig</td><td>Threat-Intelligence-Anbieter</td></tr>
<tr><td>IKT-104</td><td>HR-Cloud</td><td>Wichtig</td><td>Hyperscaler, Lokalisierungs-Subunternehmer</td></tr>
</tbody></table>
<h3>Konzentrationsrisiko</h3>
<p>Die Konzentrationsanalyse erfolgt halbjährlich. Bei Risikokonzentrationen werden Mitigationen geprüft (Multi-Vendor, Multi-Region).</p>");

        await Dok("Verzeichnis der Schlüsselfunktionen",
            "Übersicht der Schlüsselfunktionen und ihrer Inhaber.",
            HauptKap("Verzeichnisse"), null, "Verzeichnis",
            "schlüsselfunktion,kwg,marisk,verantwortlich",
            wagner, new DateTime(2025, 8, 8),
            @"<h2>Schlüsselfunktionen gemäß KWG und MaRisk</h2>
<p>Schlüsselfunktionen sind Funktionen, deren Inhaber maßgeblichen Einfluss auf das Geschäft der Bank haben.</p>
<table class='table table-bordered'><thead><tr><th>Funktion</th><th>Inhaber</th><th>Berichtsweg</th></tr></thead><tbody>
<tr><td>Geldwäschebeauftragter</td><td>Stelle 1</td><td>Geschäftsleitung</td></tr>
<tr><td>MaRisk-Compliance-Beauftragter</td><td>Stelle 2</td><td>Geschäftsleitung</td></tr>
<tr><td>WpHG-Compliance-Beauftragter</td><td>Stelle 3</td><td>Geschäftsleitung</td></tr>
<tr><td>Informationssicherheitsbeauftragter</td><td>Stelle 4</td><td>Geschäftsleitung</td></tr>
<tr><td>Datenschutzbeauftragter</td><td>Stelle 5</td><td>Geschäftsleitung</td></tr>
<tr><td>Auslagerungsbeauftragter</td><td>Stelle 6</td><td>Geschäftsleitung</td></tr>
<tr><td>Leiter Risikocontrolling</td><td>Stelle 7</td><td>Geschäftsleitung</td></tr>
<tr><td>Leiter Interne Revision</td><td>Stelle 8</td><td>Geschäftsleitung / Aufsichtsrat</td></tr>
</tbody></table>
<h3>Stellvertreterregelung</h3>
<p>Pro Schlüsselfunktion ist ein Stellvertreter benannt. Vertretungen werden im Personalsystem dokumentiert.</p>
<h3>Anzeige bei der Aufsicht</h3>
<p>Wechsel in Schlüsselfunktionen werden BaFin/EZB gemäß § 24 KWG angezeigt.</p>");

        await Dok("Verzeichnis der Notfallpläne",
            "Übersicht aller Notfall-, Wiederanlauf- und Krisenpläne.",
            HauptKap("Verzeichnisse"), Team("IT-Service"), "Verzeichnis",
            "notfallplan,bcm,disaster-recovery,bcp",
            lang, new DateTime(2025, 9, 22),
            @"<h2>Notfallplanverzeichnis</h2>
<p>Übersicht aller relevanten Notfall- und Krisenpläne der Bank, einschließlich Geltungsbereich und Owner.</p>
<table class='table table-bordered'><thead><tr><th>Plan-ID</th><th>Bezeichnung</th><th>Owner</th><th>Letzte Übung</th></tr></thead><tbody>
<tr><td>BCP-001</td><td>BCM-Rahmenwerk</td><td>BCM-Beauftragter</td><td>2025-09</td></tr>
<tr><td>BCP-010</td><td>Notfallhandbuch IT</td><td>IT-Leitung</td><td>2025-10</td></tr>
<tr><td>BCP-011</td><td>Disaster-Recovery-Plan Kernbank</td><td>IT-Leitung</td><td>2025-04</td></tr>
<tr><td>BCP-020</td><td>Krisenkommunikationsplan</td><td>Pressestelle</td><td>2025-03</td></tr>
<tr><td>BCP-030</td><td>Personalausfall-Plan</td><td>Personalleitung</td><td>2025-06</td></tr>
<tr><td>BCP-040</td><td>Pandemieplan</td><td>Krisenstab</td><td>2024-11</td></tr>
</tbody></table>
<h3>Aktualisierung</h3>
<p>Alle Pläne werden mindestens jährlich aktualisiert sowie nach Übungen oder realen Vorfällen.</p>
<h3>Verfügbarkeit</h3>
<p>Pläne sind im Bank-Intranet sowie in einer Offline-Kopie an den Notfallarbeitsplätzen verfügbar.</p>");

        await Dok("Verzeichnis der Aufbewahrungsfristen",
            "Konsolidierte Übersicht über Aufbewahrungsfristen unterschiedlicher Datenarten.",
            HauptKap("Verzeichnisse"), null, "Verzeichnis",
            "aufbewahrungsfristen,gobd,hgb,ao,bdsg",
            wagner, new DateTime(2025, 6, 30),
            @"<h2>Aufbewahrungsfristen</h2>
<p>Diese Übersicht fasst die wesentlichen Aufbewahrungsfristen der Bank zusammen. Maßgeblich sind HGB, AO, BDSG, GwG sowie weitere Spezialgesetze.</p>
<table class='table table-bordered'><thead><tr><th>Datenkategorie</th><th>Frist</th><th>Rechtsgrundlage</th></tr></thead><tbody>
<tr><td>Buchungsbelege, Bilanzen, Inventare</td><td>10 Jahre</td><td>§ 257 HGB, § 147 AO</td></tr>
<tr><td>Geschäftsbriefe</td><td>6 Jahre</td><td>§ 257 HGB</td></tr>
<tr><td>Identifizierungsunterlagen GwG</td><td>5 Jahre nach Geschäftsbeziehung</td><td>§ 8 GwG</td></tr>
<tr><td>Telefonaufzeichnungen Wertpapier</td><td>5 Jahre</td><td>MiFID II</td></tr>
<tr><td>Personalstammdaten</td><td>3 Jahre nach Austritt</td><td>BDSG</td></tr>
<tr><td>Bewerberdaten</td><td>6 Monate</td><td>AGG</td></tr>
<tr><td>Kreditakten</td><td>10 Jahre nach Beendigung</td><td>§ 257 HGB</td></tr>
<tr><td>SCHUFA-Anfragen</td><td>1 Jahr</td><td>BDSG</td></tr>
</tbody></table>
<h3>Sperrung statt Löschung</h3>
<p>Soweit eine Löschung nicht möglich ist (Pendenz, laufendes Verfahren), erfolgt eine Sperrung des weiteren Zugriffs.</p>");

        // ===================== ANLAGEN (Hauptkapitel) =====================
        await Dok("Anlage 1 - Organigramm der Bank",
            "Aktuelles Organigramm der Aufbauorganisation.",
            HauptKap("Anlagen"), null, "Anlage",
            "organigramm,aufbauorganisation,struktur",
            wagner, new DateTime(2025, 4, 12),
            @"<h2>Organigramm</h2>
<p>Diese Anlage stellt die Aufbauorganisation der Bank zum Stichtag dar. Änderungen werden anlassbezogen veröffentlicht.</p>
<h3>Vorstandsebene</h3>
<ul><li>Vorstandsvorsitz</li><li>Vorstand Markt</li><li>Vorstand Marktfolge / Risiko</li><li>Vorstand IT/Operations</li></ul>
<h3>Bereiche unter dem Vorstand</h3>
<table class='table table-bordered'><thead><tr><th>Vorstandsressort</th><th>Bereiche</th></tr></thead><tbody>
<tr><td>Vorstandsvorsitz</td><td>Stab, Recht, Compliance, Personal, Unternehmensentwicklung</td></tr>
<tr><td>Vorstand Markt</td><td>Privatkunden, Firmenkunden, Wertpapiergeschäft</td></tr>
<tr><td>Vorstand Marktfolge / Risiko</td><td>Marktfolge, Kreditrisiko, Risikocontrolling, Treasury</td></tr>
<tr><td>Vorstand IT/Operations</td><td>IT-Service, Anwendungsentwicklung, Operations, Verwaltung</td></tr>
</tbody></table>
<h3>Stabsfunktionen</h3>
<p>Direkt der Geschäftsleitung zugeordnet: Interne Revision, Datenschutzbeauftragter, Geldwäschebeauftragter, ISB, Auslagerungsbeauftragter.</p>");

        await Dok("Anlage 2 - Funktionendiagramm",
            "Funktionendiagramm zur Visualisierung von Verantwortlichkeiten.",
            HauptKap("Anlagen"), null, "Anlage",
            "funktionendiagramm,raci,verantwortlichkeit",
            wagner, new DateTime(2025, 5, 18),
            @"<h2>Funktionendiagramm</h2>
<p>Das Funktionendiagramm visualisiert nach RACI-Logik, wer in den zentralen Prozessen verantwortlich (R), rechenschaftspflichtig (A), beratend (C) oder informiert (I) ist.</p>
<h3>Auszug zentraler Prozesse</h3>
<table class='table table-bordered'><thead><tr><th>Prozess</th><th>R</th><th>A</th><th>C</th><th>I</th></tr></thead><tbody>
<tr><td>Kreditvergabe Privatkunde</td><td>Markt</td><td>Marktfolge</td><td>Risikocontrolling</td><td>Geschäftsleitung</td></tr>
<tr><td>Wertpapieranlageberatung</td><td>Berater</td><td>Bereichsleitung</td><td>Compliance</td><td>Geschäftsleitung</td></tr>
<tr><td>Kontoeröffnung</td><td>Berater</td><td>Backoffice Konto</td><td>Compliance / GwB</td><td>Bereichsleitung</td></tr>
<tr><td>IT-Change</td><td>Anwendungsentwicklung</td><td>IT-Leitung</td><td>ISB / Compliance</td><td>Fachbereich</td></tr>
<tr><td>Verdachtsmeldung GwG</td><td>Mitarbeiter / GwB</td><td>GwB</td><td>Compliance</td><td>Geschäftsleitung</td></tr>
</tbody></table>
<h3>Pflege</h3>
<p>Das Funktionendiagramm wird mit dem Prozessmanagement abgestimmt und mindestens jährlich aktualisiert.</p>");

        await Dok("Anlage 3 - Kompetenzordnung",
            "Übersicht aller Kompetenzregelungen für Beschlussfassung und Genehmigung.",
            HauptKap("Anlagen"), null, "Anlage",
            "kompetenz,beschluss,genehmigung,wertgrenze",
            wagner, new DateTime(2025, 6, 15),
            @"<h2>Kompetenzordnung</h2>
<p>Diese Kompetenzordnung ergänzt die Geschäftsordnung der Geschäftsleitung und regelt die Befugnisse für Beschluss-, Vergabe- und Vertragsentscheidungen.</p>
<h3>Investitionen und Beschaffung</h3>
<table class='table table-bordered'><thead><tr><th>Wert</th><th>Kompetenz</th></tr></thead><tbody>
<tr><td>bis 1.000 EUR</td><td>Bereichsleiter</td></tr>
<tr><td>1.000 - 10.000 EUR</td><td>Abteilungsleiter + Verwaltung</td></tr>
<tr><td>10.000 - 50.000 EUR</td><td>Bereichsvorstand</td></tr>
<tr><td>über 50.000 EUR</td><td>Gesamtgeschäftsleitung</td></tr>
</tbody></table>
<h3>Kreditkompetenzen</h3>
<table class='table table-bordered'><thead><tr><th>Volumen</th><th>Kompetenz</th></tr></thead><tbody>
<tr><td>bis 250.000 EUR</td><td>Abteilungsleiter Markt + Marktfolge</td></tr>
<tr><td>250.000 - 1.000.000 EUR</td><td>Bereichsleiter</td></tr>
<tr><td>über 1.000.000 EUR</td><td>Gesamtgeschäftsleitung</td></tr>
</tbody></table>
<h3>Sonderfälle</h3>
<p>Geschäfte mit Organkrediten, PEPs oder verbundenen Unternehmen unterliegen unabhängig vom Volumen der Geschäftsleitung.</p>");

        await Dok("Anlage 4 - Eskalationsmatrix Sicherheitsvorfälle",
            "Eskalationsstufen und Verantwortlichkeiten bei IT- und Sicherheitsvorfällen.",
            HauptKap("Anlagen"), Team("Informationssicherheit"), "Anlage",
            "eskalation,vorfall,csirt,krise",
            lang, new DateTime(2025, 7, 26),
            @"<h2>Eskalationsmatrix Sicherheitsvorfälle</h2>
<p>Die Matrix definiert für definierte Vorfallklassen die Eskalationsstufen, Reaktionszeiten und Informationsempfänger.</p>
<table class='table table-bordered'><thead><tr><th>Stufe</th><th>Auslöser</th><th>Reaktion</th><th>Information an</th></tr></thead><tbody>
<tr><td>S1 - niedrig</td><td>Einzelner Endpoint betroffen, keine Datenkompromittierung</td><td>L1-Bearbeitung im SOC</td><td>ISB im Tagesreport</td></tr>
<tr><td>S2 - mittel</td><td>Mehrere Endpoints, kein kritisches System</td><td>L2 + ISB</td><td>IT-Leitung</td></tr>
<tr><td>S3 - hoch</td><td>Kritisches System oder Datenabfluss</td><td>CSIRT, IT-Leitung, ISB</td><td>Geschäftsleitung, Compliance, Datenschutz</td></tr>
<tr><td>S4 - kritisch</td><td>Großflächiger Vorfall, Geschäftsbetrieb beeinträchtigt</td><td>Aktivierung Krisenstab</td><td>Geschäftsleitung, Aufsicht, Presse</td></tr>
</tbody></table>
<h3>Erreichbarkeit</h3>
<p>24/7-Hotline des SOC. Eskalationen erfolgen über das ITSM-Tool sowie redundant per Telefon.</p>
<h3>Übungen</h3>
<p>Die Eskalationsmatrix wird halbjährlich im Rahmen von Tabletop-Übungen getestet.</p>");

        await Dok("Anlage 5 - Glossar regulatorischer Begriffe",
            "Begriffsdefinitionen aus den wichtigsten regulatorischen Rahmenwerken.",
            HauptKap("Anlagen"), null, "Anlage",
            "glossar,marisk,dora,mifid,gwg",
            wagner, new DateTime(2025, 9, 9),
            @"<h2>Glossar</h2>
<p>Dieses Glossar erläutert in der Bank verwendete Begriffe aus den wichtigsten regulatorischen Rahmenwerken.</p>
<table class='table table-bordered'><thead><tr><th>Begriff</th><th>Bedeutung</th></tr></thead><tbody>
<tr><td>BAIT</td><td>Bankaufsichtliche Anforderungen an die IT</td></tr>
<tr><td>DORA</td><td>Digital Operational Resilience Act (EU 2022/2554)</td></tr>
<tr><td>DSGVO</td><td>Datenschutz-Grundverordnung der EU</td></tr>
<tr><td>FIU</td><td>Financial Intelligence Unit (Zentralstelle für Finanztransaktionsuntersuchungen)</td></tr>
<tr><td>GwG</td><td>Geldwäschegesetz</td></tr>
<tr><td>InstitutsVergV</td><td>Institutsvergütungsverordnung</td></tr>
<tr><td>KWG</td><td>Kreditwesengesetz</td></tr>
<tr><td>MAR</td><td>Market Abuse Regulation</td></tr>
<tr><td>MaRisk</td><td>Mindestanforderungen an das Risikomanagement</td></tr>
<tr><td>MiFID II</td><td>Markets in Financial Instruments Directive II</td></tr>
<tr><td>NPP</td><td>New Product Process</td></tr>
<tr><td>PEP</td><td>Politisch exponierte Person</td></tr>
<tr><td>RTO/RPO</td><td>Recovery Time / Recovery Point Objective</td></tr>
<tr><td>SCC</td><td>Standardvertragsklauseln (EU)</td></tr>
<tr><td>SCT/SDD</td><td>SEPA Credit Transfer / SEPA Direct Debit</td></tr>
<tr><td>STOR</td><td>Suspicious Transaction or Order Report (MAR)</td></tr>
<tr><td>WpHG</td><td>Wertpapierhandelsgesetz</td></tr>
</tbody></table>");

        // ===================== FORMULARE (Hauptkapitel) =====================
        await Dok("Formular: Meldung Sicherheitsvorfall",
            "Standardformular zur Meldung von IT- und Informationssicherheitsvorfällen.",
            HauptKap("Formulare"), Team("Informationssicherheit"), "Formular",
            "formular,vorfall,meldung,it",
            lang, new DateTime(2025, 5, 1),
            @"<h2>Meldung Sicherheitsvorfall</h2>
<p><em>Bitte vollständig ausfüllen und an security@ohb.local senden bzw. über das Self-Service-Portal eröffnen.</em></p>
<h3>1. Meldende Person</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Name</td><td>___________________________</td></tr>
<tr><td>Abteilung</td><td>___________________________</td></tr>
<tr><td>Telefon</td><td>___________________________</td></tr>
<tr><td>Datum / Uhrzeit der Beobachtung</td><td>___________________________</td></tr>
</tbody></table>
<h3>2. Betroffenes System / Daten</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>System / Anwendung</td><td>___________________________</td></tr>
<tr><td>Datenklassifikation</td><td>[ ] Öffentlich [ ] Intern [ ] Vertraulich [ ] Streng vertraulich</td></tr>
<tr><td>Anzahl betroffener Personen</td><td>___________________________</td></tr>
</tbody></table>
<h3>3. Beschreibung</h3>
<p>__________________________________________________________________<br>__________________________________________________________________</p>
<h3>4. Sofortmaßnahmen ergriffen?</h3>
<p>[ ] Ja - welche: _____________________ &nbsp; [ ] Nein</p>
<h3>5. Weitere Informationsempfänger</h3>
<p>[ ] Vorgesetzter [ ] Datenschutz [ ] Kunde [ ] Aufsicht</p>");

        await Dok("Formular: Antrag auf Auslagerung",
            "Antragsformular zur Vorbereitung einer geplanten Auslagerung.",
            HauptKap("Formulare"), null, "Formular",
            "formular,auslagerung,antrag,marisk,dora",
            wagner, new DateTime(2025, 6, 25),
            @"<h2>Antrag auf Auslagerung</h2>
<p><em>Vor jeder Auslagerung ist dieser Antrag vollständig auszufüllen und der Geschäftsleitung zur Genehmigung vorzulegen.</em></p>
<h3>1. Antragsteller</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Bereich / Abteilung</td><td>___________________________</td></tr>
<tr><td>Antragsteller</td><td>___________________________</td></tr>
</tbody></table>
<h3>2. Geplante Auslagerung</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Dienstleister</td><td>___________________________</td></tr>
<tr><td>Beschreibung der Leistung</td><td>___________________________</td></tr>
<tr><td>Wesentlich i.S.v. AT 9?</td><td>[ ] Ja [ ] Nein</td></tr>
<tr><td>Geplanter Vertragsbeginn</td><td>___________________________</td></tr>
<tr><td>Volumen p.a. (EUR)</td><td>___________________________</td></tr>
</tbody></table>
<h3>3. Risikoanalyse</h3>
<p>Bewertung der Risiken in den Dimensionen Operativ, Compliance, Datenschutz, Konzentrationsrisiko: __________</p>
<h3>4. Stellungnahmen</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Auslagerungsbeauftragter</td><td>Datum: ________ Unterschrift: ________________</td></tr>
<tr><td>ISB</td><td>Datum: ________ Unterschrift: ________________</td></tr>
<tr><td>Compliance</td><td>Datum: ________ Unterschrift: ________________</td></tr>
<tr><td>Datenschutz</td><td>Datum: ________ Unterschrift: ________________</td></tr>
<tr><td>Geschäftsleitung</td><td>Datum: ________ Unterschrift: ________________</td></tr>
</tbody></table>");

        await Dok("Formular: Verdachtsmeldung Geldwäsche",
            "Internes Formular zur Erfassung einer Verdachtsmeldung an den Geldwäschebeauftragten.",
            HauptKap("Formulare"), Team("Compliance"), "Formular",
            "formular,verdachtsmeldung,gwg,gwb",
            mueller, new DateTime(2025, 7, 8),
            @"<h2>Verdachtsmeldung an den Geldwäschebeauftragten</h2>
<p><em>Vertraulich - bitte ausschließlich an gwb@ohb.local übermitteln. Tipping-Off ist strafbar.</em></p>
<h3>1. Meldende Person</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Name</td><td>___________________________</td></tr>
<tr><td>Abteilung</td><td>___________________________</td></tr>
<tr><td>Datum / Uhrzeit</td><td>___________________________</td></tr>
</tbody></table>
<h3>2. Betroffene Person / Konto</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Name / Firmierung</td><td>___________________________</td></tr>
<tr><td>Kontonummer / IBAN</td><td>___________________________</td></tr>
<tr><td>Geschäftsbeziehung seit</td><td>___________________________</td></tr>
</tbody></table>
<h3>3. Sachverhalt</h3>
<p>__________________________________________________________________<br>__________________________________________________________________</p>
<h3>4. Auffällige Transaktionen</h3>
<table class='table table-bordered'><thead><tr><th>Datum</th><th>Betrag</th><th>Verwendungszweck</th><th>Auffälligkeit</th></tr></thead><tbody>
<tr><td>__________</td><td>__________</td><td>__________</td><td>__________</td></tr>
<tr><td>__________</td><td>__________</td><td>__________</td><td>__________</td></tr>
</tbody></table>
<h3>5. Weitere Informationen</h3>
<p>__________________________________________________________________</p>");

        await Dok("Formular: Anlageprofil-Erfassung MiFID II",
            "Erhebung des Anlageprofils im Rahmen der Geeignetheitsprüfung.",
            HauptKap("Formulare"), Team("Handel"), "Formular",
            "formular,anlageprofil,mifid,geeignetheit",
            fischer, new DateTime(2025, 8, 12),
            @"<h2>Anlageprofil</h2>
<p><em>Dieses Formular dient der Erhebung des Anlageprofils gemäß MiFID II. Die Angaben werden vertraulich behandelt.</em></p>
<h3>1. Persönliche Angaben</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Name, Vorname</td><td>___________________________</td></tr>
<tr><td>Geburtsdatum</td><td>___________________________</td></tr>
<tr><td>Beruf</td><td>___________________________</td></tr>
</tbody></table>
<h3>2. Finanzielle Verhältnisse</h3>
<table class='table table-bordered'><tbody>
<tr><td>Monatliches Nettoeinkommen</td><td>___________________________</td></tr>
<tr><td>Verfügbares Vermögen</td><td>___________________________</td></tr>
<tr><td>Verbindlichkeiten</td><td>___________________________</td></tr>
</tbody></table>
<h3>3. Anlageziele</h3>
<p>[ ] Vermögenserhalt &nbsp; [ ] Stetiger Ertrag &nbsp; [ ] Vermögensaufbau &nbsp; [ ] Spekulation</p>
<h3>4. Risikobereitschaft</h3>
<p>[ ] Sicherheitsorientiert &nbsp; [ ] Konservativ &nbsp; [ ] Ausgewogen &nbsp; [ ] Wachstumsorientiert &nbsp; [ ] Spekulativ</p>
<h3>5. Kenntnisse und Erfahrungen</h3>
<table class='table table-bordered'><thead><tr><th>Produktart</th><th>Kenntnisse</th><th>Erfahrung</th></tr></thead><tbody>
<tr><td>Aktien</td><td>[ ] keine [ ] Grundkenntnis [ ] vertieft</td><td>[ ] keine [ ] &lt; 3 Jahre [ ] &gt; 3 Jahre</td></tr>
<tr><td>Anleihen</td><td>[ ] keine [ ] Grundkenntnis [ ] vertieft</td><td>[ ] keine [ ] &lt; 3 Jahre [ ] &gt; 3 Jahre</td></tr>
<tr><td>Fonds</td><td>[ ] keine [ ] Grundkenntnis [ ] vertieft</td><td>[ ] keine [ ] &lt; 3 Jahre [ ] &gt; 3 Jahre</td></tr>
<tr><td>Derivate</td><td>[ ] keine [ ] Grundkenntnis [ ] vertieft</td><td>[ ] keine [ ] &lt; 3 Jahre [ ] &gt; 3 Jahre</td></tr>
</tbody></table>");

        await Dok("Formular: Antrag auf Spesenerstattung",
            "Standardformular für die Abrechnung von Spesen und Reisekosten.",
            HauptKap("Formulare"), null, "Formular",
            "formular,spesen,reisekosten,abrechnung",
            wagner, new DateTime(2025, 4, 6),
            @"<h2>Antrag auf Spesenerstattung</h2>
<p><em>Bitte vollständig ausfüllen und mit Originalbelegen einreichen. Spätester Einreichungstermin: 30 Tage nach Reiseende.</em></p>
<h3>1. Antragsteller</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Name</td><td>___________________________</td></tr>
<tr><td>Personalnummer</td><td>___________________________</td></tr>
<tr><td>Abteilung</td><td>___________________________</td></tr>
</tbody></table>
<h3>2. Reisedaten</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Reisezweck</td><td>___________________________</td></tr>
<tr><td>Reiseziel</td><td>___________________________</td></tr>
<tr><td>Beginn / Ende</td><td>___________________________</td></tr>
</tbody></table>
<h3>3. Aufstellung der Kosten</h3>
<table class='table table-bordered'><thead><tr><th>Position</th><th>Beleg-Nr.</th><th>Datum</th><th>Betrag (EUR)</th></tr></thead><tbody>
<tr><td>Bahn / Flug</td><td>__________</td><td>__________</td><td>__________</td></tr>
<tr><td>Hotel</td><td>__________</td><td>__________</td><td>__________</td></tr>
<tr><td>Verpflegungspauschale</td><td>__________</td><td>__________</td><td>__________</td></tr>
<tr><td>Taxi / ÖPNV</td><td>__________</td><td>__________</td><td>__________</td></tr>
<tr><td>Sonstiges</td><td>__________</td><td>__________</td><td>__________</td></tr>
<tr><td><strong>Summe</strong></td><td></td><td></td><td><strong>__________</strong></td></tr>
</tbody></table>
<h3>4. Genehmigung</h3>
<table class='table table-bordered'><tbody>
<tr><td width='30%'>Antragsteller</td><td>Datum: ________ Unterschrift: ________________</td></tr>
<tr><td>Vorgesetzter</td><td>Datum: ________ Unterschrift: ________________</td></tr>
<tr><td>Verwaltung (sachliche Prüfung)</td><td>Datum: ________ Unterschrift: ________________</td></tr>
</tbody></table>");

        // ZUSATZ_DOKUMENTE_ENDE
    }
}
