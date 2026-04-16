namespace OhbPortal.Infrastructure.Data;

/// <summary>
/// Inhalte aus 5 extern bereitgestellten PDF-Richtlinien/Strategien
/// (Personendaten anonymisiert durch Dummy-Namen).
/// </summary>
internal static class ImportierteRichtlinienDaten
{
    public const string KiRichtlinieHtml = """
<h2>1. Vorgaben zur Nutzung von Künstlicher Intelligenz (KI) – Minimales-Risiko-Anwendungsfälle</h2>
<p>Diese Richtlinie legt Grundsätze für den Einsatz von KI-Systemen in unserer Bank fest, fokussiert auf <strong>Minimales-Risiko-Anwendungsfälle</strong> gemäß EU AI Act. Minimales-Risiko-KI-Systeme sind solche, die weder als hochriskant noch als verboten eingestuft werden und somit <strong>keinen strengen regulatorischen Auflagen</strong> des KI-Gesetzes unterliegen.</p>
<p>Dennoch sollen auch bei diesen Systemen bewährte Verfahren und interne Kontrollen angewendet werden, um Qualität, <strong>Transparenz</strong> und <strong>Verlässlichkeit</strong> sicherzustellen. Diese Richtlinie richtet sich an alle internen Fachabteilungen, insbesondere Revision und ISM, sowie weitere Stakeholder, die an Entwicklung, Einsatz oder Überwachung von KI beteiligt sind.</p>
<p>Aktuell nutzt die Bank ein einfaches KI-Modell, das Daten aus einem Formular (z. B. einem eingescannten Blatt oder gespeichertes PDF-Dokument) ausliest und automatisiert in eine Fach-Software überträgt. Anschließend überprüft ein Mitarbeiter alle übertragenen Daten manuell und korrigiert etwaige Fehler.</p>
<p>Das Modell hat <strong>weder Interaktionen mit Mitarbeitern noch mit Kunden</strong> und <strong>trifft keine autonomen Entscheidungen</strong>, sondern dient lediglich als Unterstützung bei der Datenerfassung. Zudem erhält das KI-System Feedback über die Korrekturen, um langfristig dazuzulernen. Dieses Beispiel eines <strong>assistierenden KI-Einsatzes</strong> bildet das Grundszenario für die nachfolgenden Richtlinienpunkte.</p>

<h3>1.1. Auswahl geeigneter KI-Modelle für Minimales-Risiko-Einsätze</h3>
<p>Bei Minimales-Risiko-KI-Systemen ist darauf zu achten, dass das gewählte Modell angemessen, verständlich und zuverlässig ist.</p>
<p><strong>Anforderungen an die Modellauswahl:</strong></p>
<ul>
  <li><strong>Passgenauigkeit und Einfachheit:</strong> Wählen Sie ein KI-Modell, das zum konkreten Geschäftsprozess passt und nur die notwendige Komplexität aufweist. In vielen Fällen können einfachere datengetriebene Ansätze oder klassische statistische Methoden eine ähnlich gute Vorhersagekraft liefern wie komplexe KI-Modelle – jedoch mit weniger einhergehenden Risiken.</li>
  <li><strong>Zuverlässigkeit und Performance:</strong> Stellen Sie sicher, dass das Modell robuste Ergebnisse liefert und die erforderliche Genauigkeit erreicht. Bereits vor dem Einsatz sollten Messgrößen wie Erkennungsgenauigkeit, Fehlerrate oder Verarbeitungsgeschwindigkeit geprüft werden.</li>
  <li><strong>Transparenz und Nachvollziehbarkeit:</strong> Bevorzugen Sie KI-Systeme, deren Funktionsweise nachvollziehbar oder erklärbar ist. Modelle mit verständlicher Entscheidungslogik können von Fachexperten leichter überprüft werden. Für Minimales-Risiko-KI-Systeme ist es oft möglich, auf allzu opake „Black-Box"-Modelle zu verzichten.</li>
  <li><strong>Regulatorische Konformität und Dokumentation:</strong> Erfassen Sie bereits bei der Auswahl alle relevanten Informationen zum Modell (Modelltyp, Version, Datenquellen, Hersteller oder Entwickler). Bei Zukauf externer KI-Lösungen ist zusätzlich sicherzustellen, dass der Anbieter etwaige Compliance-Vorgaben (Datenschutz, IT-Sicherheit) einhält.</li>
</ul>

<h3>1.2. Sicherstellung der Datenqualität bei Datenerfassung und -übertragung</h3>
<p>Die <strong>Datenqualität</strong> spielt eine zentrale Rolle für verlässliche KI-Ergebnisse. Es gilt der Grundsatz: Nur mit hochwertigen Eingabedaten kann das KI-System korrekte Ausgaben liefern.</p>
<ul>
  <li><strong>Qualität der Eingangsdaten:</strong> Klare, fehlerfreie Datenerfassung; eingescannte Formulare müssen gut lesbar sein (keine Flecken, ausreichende Auflösung). Einheitliche Formate und definierte Feldstrukturen erleichtern die Erkennung.</li>
  <li><strong>Verifizierte Datenübertragung:</strong> Die vom KI-Modell ausgelesenen Daten müssen vollständig und unverfälscht in die Zielsoftware übertragen werden (definierte Formate, Prüfalgorithmen, Checksummen).</li>
  <li><strong>Konsistenz und Aktualität:</strong> Änderungen und Korrekturen sind nachvollziehbar zu protokollieren (Auditierbarkeit).</li>
  <li><strong>Umgang mit Datenfehlern:</strong> Unsichere oder unleserliche Eingaben werden gekennzeichnet und zur nachgelagerten Klärung an Sachbearbeiter weitergeleitet. Fehlerhafte Datensätze dürfen nicht ungesehen in nachgelagerte Systeme übernommen werden.</li>
</ul>

<h3>1.3. Umgang mit KI-Ergebnissen: Manuelle Überprüfung und Feedbackschleifen</h3>
<p>Auch bei Minimales-Risiko-KI-Systemen gilt: <strong>Der Mensch bleibt in der Verantwortung.</strong></p>
<ul>
  <li><strong>Manuelle Überprüfung (Vier-Augen-Prinzip):</strong> Alle geschäftsrelevanten Ausgaben müssen von einem Mitarbeiter überprüft werden, bevor sie weiterverwendet oder an Kunden kommuniziert werden.</li>
  <li><strong>Klar definierte Korrekturprozesse:</strong> Erkannte Fehler werden umgehend korrigiert; das System erfasst manuell geänderte Werte sauber.</li>
  <li><strong>Feedback an das KI-Modell:</strong> Korrekturen durch Mitarbeiter werden dem KI-Modell als Lerndaten zurückgespiegelt (z. B. Aufnahme in den Trainingsdatensatz).</li>
  <li><strong>Grenzen der KI beachten:</strong> Fachabteilungen sind über bekannte Schwächen und Unsicherheitsbereiche des Systems informiert; bei geringem Vertrauen wird das Ergebnis automatisch an einen menschlichen Bearbeiter übergeben.</li>
</ul>

<h3>1.4. Validierung von Daten und Modellen</h3>
<ul>
  <li><strong>Initiale Modell- und Datenvalidierung:</strong> Vor Live-Gang wird das Modell mit repräsentativen Testdaten in einer Testumgebung erprobt. Nur wenn die Akzeptanzkriterien erfüllt sind, geht es in den Wirkbetrieb.</li>
  <li><strong>Laufende Überwachung und Qualitätskontrolle:</strong> Nach dem Go-Live Monitoring der Performance via KPIs (Fehlerrate, Korrekturen pro 100 Vorgänge) in definierten Abständen.</li>
  <li><strong>Validierung bei Modelländerungen:</strong> Bei Modellanpassungen Regressionstest gegen die alte Version; Freigabe durch die zuständige Fachstelle.</li>
  <li><strong>Einbindung unabhängiger Stellen:</strong> Interne Revision oder ein Modellvalidierungsteam werden bei Bedarf in die Prüfung einbezogen.</li>
  <li><strong>Kontinuierliche Verbesserung:</strong> Validierung ist ein fortlaufender Prozess — Erkenntnisse fließen in Datenvorverarbeitung und aktualisierte Richtlinien ein.</li>
</ul>

<h3>1.5. Fazit</h3>
<p>Diese Richtlinie soll sicherstellen, dass KI-Systeme mit geringem Risiko im Bankenumfeld sicher, effektiv und regelkonform eingesetzt werden. Auch wenn Minimales-Risiko-KI-Systeme nach dem EU AI Act keine ausdrücklichen gesetzlichen Vorgaben erfüllen müssen, folgt unsere Bank freiwillig den Best Practices und etabliert interne Leitplanken.</p>
<p>Die Kombination aus sorgfältiger Modellauswahl, hoher Datenqualität, menschlicher Überwachung und systematischer Validierung gewährleistet, dass KI-basierte Assistenzsysteme ihren Zweck erfüllen, ohne die Integrität unserer Prozesse zu gefährden.</p>
<blockquote><strong>Hinweis:</strong> Alle beteiligten Fachbereiche sind angehalten, diese Grundsätze einzuhalten und bei Fragen die zuständigen Governance-Stellen (Unternehmensentwicklung, Revision, IT, ISM, DSB) frühzeitig einzubeziehen.</blockquote>
""";

    public const string ItDorStrategieHtml = """
<h2>1. Strategische Ausrichtung</h2>
<p>Die MERKUR PRIVATBANK leitet die IT-/DOR-Strategie aus der Geschäftsstrategie der Bank ab. Mit den in der IT-/DOR-Strategie festgelegten Rahmenbedingungen werden die <strong>digitale Resilienz der IT-Landschaft</strong> und der IKT-Auslagerungen verbessert, wodurch den sich aus einem Großangriff oder Cyberangriff ergebenden Risiken zielgerichteter begegnet werden kann.</p>
<p>Die IT-/DOR-Strategie setzt den Orientierungsrahmen für die Bereitstellung von IT für die Geschäftsprozesse der Bank. Die Gesamtverantwortung für die IT liegt bei der Geschäftsleitung.</p>
<p>Bei der Festlegung und Anpassung berücksichtigen wir externe Einflussfaktoren (Marktentwicklung, Wettbewerb, Digitalisierung) und interne Anforderungen (Ertragslage, Ressourcen). Relevante regulatorische Vorgaben sind insbesondere KWG, DORA und MaRisk.</p>
<p>Strategisch betrachten wir die IT als wichtiges Unterstützungsinstrument:</p>
<ul>
  <li>Moderne und attraktive Arbeitsplätze für unsere Mitarbeiter</li>
  <li>Unterstützung zukunftsfähiger Geschäftsprozesse</li>
  <li>Ordnungsgemäße, qualitätsgerechte und sichere Umsetzung geschäftspolitischer Aktivitäten</li>
  <li>Effizienztreiber und Basis für innovative Lösungen</li>
</ul>

<h2>2. IT-strategische Ziele und Maßnahmen</h2>
<p>Als vorrangige strategische Ziele definieren wir:</p>
<ol>
  <li><strong>Ordnungsmäßigkeit und Sicherheit</strong> der IT entsprechend den einschlägigen gesetzlichen und regulatorischen Anforderungen.</li>
  <li><strong>Wirtschaftlichkeit der IT</strong> unter Betrachtung einer angemessenen Kosten-Nutzen-Relation.</li>
  <li><strong>Dynamische IT-Architektur</strong>, die die strategischen Ziele aus Geschäfts- und Risikostrategie sachgerecht unterstützt.</li>
  <li><strong>Sicherer, stabiler und performanter Betrieb</strong> von IT-Lösungen.</li>
  <li><strong>Anwender-/Kundenzufriedenheit</strong> durch Stabilität, Performance und IT-Support.</li>
</ol>

<h2>3. Organisation und Steuerung der IT</h2>

<h3>3.1. IT-Kompetenzen und Mitarbeiterbefähigung</h3>
<ul>
  <li>Die Bank investiert in den Aufbau und Erhalt von IT-Kompetenzen.</li>
  <li>Aufbau entsprechender IT-Kompetenzen auf Entscheiderebene.</li>
  <li>IT in der Personalstrategie berücksichtigt.</li>
  <li>Befähigung der Mitarbeiter und Entscheider in IT, digitaler operationaler Resilienz und Digitalisierung.</li>
  <li>Regelmäßige Überprüfung der Personalausstattung in IT-Service, Unternehmensentwicklung und Informationssicherheitsmanagement.</li>
</ul>

<h3>3.2. IT-Organisation</h3>
<p>Die Bank legt Regelungen zur IT-Aufbau- und Ablauforganisation fest und verhindert Interessenkonflikte sowie unvereinbare Tätigkeiten. Moderne Kommunikations- und Collaborationslösungen werden eingeführt; geeignete Strukturen sichern ein hohes Maß an digitaler operationaler Resilienz.</p>

<h3>3.3. IT-Projekt- und Portfoliomanagement</h3>
<p>IT-Projekte werden im Rahmen des übergeordneten Projektportfolios erfasst, überwacht und gesteuert.</p>

<h3>3.4. IT-Investitionen und -Kosten</h3>
<p>Um den Anforderungen an die digitale operationale Resilienz gerecht zu werden, stellt die Bank angemessene Budgetmittel zur Verfügung:</p>
<ul>
  <li>Personelle Ressourcen (siehe 3.1)</li>
  <li>Finanzielle Ressourcen (IT-Investitionsplanung, IT-Kosten, Budget für Informationssicherheit und DOR)</li>
  <li>Sonstige Ressourcen (z. B. Technik, Räume)</li>
</ul>
<p>Darüber hinaus wird dem Leiter IT-Service ein jährliches Budget von <strong>200 TEUR</strong> zur Verfügung gestellt, um notwendige ad-hoc-Maßnahmen im Sinne der digitalen operationalen Resilienz umsetzen zu können.</p>

<h2>4. Strategische Entwicklung der IKT-Systemlandschaft</h2>
<p>Die Bank orientiert sich an der strategischen Ausrichtung der Atruvia AG. Im Vordergrund stehen eine flexible IT-Architektur zur Bedienung des Omnikanalmodells sowie sicherer, stabiler und performanter Betrieb. Langfristige Zusammenarbeit mit Atruvia wird angestrebt.</p>
<ul>
  <li>Zentraler Bestandteil ist das von Atruvia bereitgestellte Banksystem.</li>
  <li>Weitere IKT-Systeme werden von Atruvia und der genossenschaftlichen FinanzGruppe (z. B. DZ BANK, Ratiodata, DG Nexolution) bereitgestellt und betrieben.</li>
  <li>Hybrid-Cloud-Ansatz: Atruvia-Cloud kombiniert mit einer über den Dienstleister bn-its implementierten Private Inhouse Cloud (PIC).</li>
  <li>Microsoft 365 als gemanagte Hybrid Cloud (Private von Atruvia + Public Azure von Microsoft) für Kommunikation und Collaboration.</li>
  <li>Sukzessive Reduzierung individueller Lösungen; Integration in den IT-Dienstleisterstandard.</li>
  <li>Eigenprogrammierungen nur wenn Atruvia oder Dienstleister keine adäquate Lösung anbieten; siehe Richtlinie für Eigenprogrammierungen.</li>
</ul>

<h2>5. IKT-Betrieb</h2>

<h3>5.1. IT-Ausstattung</h3>
<p>Interoperabilität wird durch Atruvia-zertifizierte Hard- und Software sichergestellt. Veraltete IKT-Systeme werden grundsätzlich nicht eingesetzt; Ausnahmen werden im übergeordneten Risikomanagement erfasst (Lebenszyklus-Management). Nachhaltiger Einsatz von IT (Recycling, Zweitmarkt).</p>

<h3>5.2. Betrieb der IKT-Systeme</h3>
<p>Ausrichtung auf Sicherheit, Verfügbarkeit, Stabilität, Wirtschaftlichkeit, Standardisierung und Automatisierung. Proaktive Steuerung, Monitoring und definierte Service Levels. Serviceanfragen werden zentral aufgenommen und priorisiert. Schwachstellen werden identifiziert und Gegenmaßnahmen eingeleitet. Test- und Freigabeverfahren sind festgesetzt; Änderungen am Bankverfahren agree21 erfolgen zentral über Atruvia.</p>

<h2>6. Resilienz der IKT</h2>

<h3>6.1. IKT-Risikomanagementrahmen</h3>
<p>Ausgehend von der Risikostrategie legen wir als <strong>Risikotoleranzschwelle</strong> für IKT-Risiken die Vermeidung von Risiken der Klassen „Äußerst relevant (B)" und „Existenzbedrohend (A)" fest. Die <strong>Auswirkungstoleranzschwelle</strong> beträgt 100.000 EUR Schadenspotential (abgeleitet aus Klasse „C" — relevant, Eintrittswahrscheinlichkeit „Sehr wahrscheinlich").</p>
<p>Ab der Risikotoleranzschwelle sind risikomindernde Maßnahmen zwingend erforderlich. Eine unabhängige IKT-Risikokontrollfunktion managt und überwacht IKT-Risiken übergreifend. Der Informationsverbund (IKT-Assetmanagement) bildet Abhängigkeiten einschließlich IKT-Drittdienstleister ab.</p>

<h3>6.2. Informationssicherheitsmanagement</h3>
<p>Oberste Ziele: Sicherstellung der digitalen operativen Resilienz. Orientierung an <strong>DIN ISO/IEC 27001</strong> als Standard i. S. d. MaRisk AT 7.2 Tz 2; angestrebter Reifegrad <strong>90 %</strong>. Cyberbedrohungslandschaft und Schwachstellen werden laufend überwacht. Prozesse zur Detektion, Kategorisierung, Behandlung und Meldung von IKT-Vorfällen sind etabliert. Identitäts- und Rechtemanagement mit regelmäßiger Berechtigungsprüfung.</p>

<h3>6.3. IKT-Drittdienstleistermanagement</h3>
<p>Für kritische oder wichtige Funktionen werden bevorzugt IT-Dienstleister der genossenschaftlichen FinanzGruppe eingesetzt. Vertragliche Vereinbarungen umfassen die DORA-Mindestinhalte. Überwachung durch den Auslagerungsbeauftragten; Informationsregister mit regulatorischen Inhalten wird geführt. Zusätzlich wird ZAM eG für die Steuerung von Drittbezügen der Atruvia und DZ BANK genutzt.</p>

<h3>6.4. IT-Notfallmanagement/IKT-Geschäftsfortführung</h3>
<p>Schnelle Wiederherstellung des Geschäftsbetriebs und Minimierung der Auswirkungen stehen im Vordergrund. Das IT-Notfallkonzept enthält IKT-Geschäftsfortführungspläne sowie funktionsfähige Reaktions- und Wiederherstellungspläne. Szenario-basierte IT-Notfalltests werden dokumentiert und berichtet.</p>

<h2>7. Verantwortlichkeiten und Umsetzung</h2>
<p>Verantwortung für die Umsetzung trägt die Geschäftsleitung. Die Überwachung erfolgt durch den Leiter IT-Service; Ergebnisse werden in ForumISM dokumentiert und berichtet. Die IT-/DOR-Strategie wird <strong>mindestens einmal jährlich</strong> oder bei Bedarf außerplanmäßig überprüft.</p>

<h3>Mitgeltende Dokumente</h3>
<ul>
  <li>Geschäftsstrategie</li>
  <li>IKT-Geschäftsfortführungsmanagement</li>
  <li>Informationssicherheitsleitlinie</li>
  <li>Investitionen – Planung, Umsetzung und Überwachung</li>
  <li>IT-Richtlinie für Anwender</li>
  <li>Richtlinie für Auslagerungs- und Drittdienstleistermanagement</li>
  <li>Richtlinie für Eigenprogrammierungen</li>
  <li>Richtlinie zum Notfallmanagement und IKT-Geschäftsfortführungsleitlinie</li>
  <li>Risikohandbuch</li>
</ul>
""";

    public const string EigenprogrammierungenHtml = """
<h2>Einleitung</h2>
<p>Eigenprogrammierungen (= Eigenanwendungen) sind grundsätzlich zulässig. Für die Programmierung sind die nachfolgenden Vorgaben zu beachten.</p>
<p>Bei Anwendungsentwicklungen im größeren Umfang sollten Planung und Durchführung der Entwicklung im Rahmen eines Projekts unter Berücksichtigung der Vorgaben zum Vorgehensmodell aus dem Projektmanagement erfolgen und dabei unter anderem auch Auswirkungsanalysen vorgenommen werden.</p>
<p>Nach BAIT Tz. 7.8/7.9 können insbesondere folgende Vorkehrungen geeignet sein:</p>
<ul>
  <li>Prüfung der Eingabedaten</li>
  <li>Systemzugangskontrollen</li>
  <li>Benutzerauthentifizierung</li>
  <li>Transaktionsautorisierung</li>
  <li>Protokollierung der Systemaktivitäten</li>
  <li>Prüfpfade (Audit Logs)</li>
  <li>Verfolgung von sicherheitsrelevanten Ereignissen</li>
  <li>Behandlung von Ausnahmen</li>
  <li>Überprüfung des Quellcodes</li>
</ul>

<h2>1.1. Anforderungs- und Entwicklungsprozess</h2>
<p>Neue Eigenprogrammierungen werden grundsätzlich erst nach Erstellung eines fachlichen Konzeptes mit Bewertung realisiert. Für die Umsetzung und den gesamten Prozess wurde eine Datenbank („MB Eigenanwendungen") konzipiert, die durch einen fest definierten Workflow führt.</p>
<p>Die Beantragung erfolgt direkt durch den Fachbereich im Ticketsystem. Je nach Komplexität ist eine entsprechende Ausführungstiefe erforderlich, die bis zu einem detaillierten Fachkonzept reicht. Abnahmekriterien werden im Vorfeld festgehalten.</p>

<h3>Kategorisierung eines Incidents</h3>
<table>
  <thead>
    <tr><th>Kategorie</th><th>Beschreibung</th></tr>
  </thead>
  <tbody>
    <tr><td>Hoch (1)</td><td>Schaden nimmt schnell zu, Aufgaben sind zeitkritisch, große Anzahl von Benutzern betroffen.</td></tr>
    <tr><td>Mittel (2)</td><td>Schaden nimmt rapide zu, Aufgaben nur mäßig zeitkritisch, mäßige Anzahl von Benutzern betroffen.</td></tr>
    <tr><td>Niedrig (3)</td><td>Schaden nimmt langsam zu, nicht zeitkritisch, minimale Anzahl von Benutzern betroffen.</td></tr>
  </tbody>
</table>

<h3>Reaktions- und Lösungszeiten</h3>
<table>
  <thead>
    <tr><th>Prio</th><th>Beschreibung</th><th>Reaktionszeit</th><th>Lösungszeit</th></tr>
  </thead>
  <tbody>
    <tr><td>1</td><td>Hoch</td><td>sofort</td><td>1 Tag</td></tr>
    <tr><td>2</td><td>Mittel</td><td>4 Stunden</td><td>2 Tage</td></tr>
    <tr><td>3</td><td>Niedrig</td><td>1 Tag</td><td>1 Woche</td></tr>
  </tbody>
</table>

<p>Alle Tickets werden in einem Jour fixe zwischen Entwicklung und Fachbereich priorisiert und einem der drei jährlichen Releases zugeordnet. Sobald das Ticket in der Testversion realisiert ist, führt der Entwickler einen Entwicklertest durch; die Dokumentation der technischen Umsetzung erfolgt in der Wiki. <strong>Sofern der einzelne Auftrag ein Volumen von 6 MT übersteigt, ist die Plausibilisierung der Codierung durch einen zweiten Entwickler im Rahmen eines Code Review durchzuführen und im Ticketsystem zu dokumentieren.</strong></p>

<p>Der Fachbereich wird zum Abnahmetest aufgefordert. Eine Ausbringung in die Produktivumgebung wird bei fehlender oder nicht ausreichender Testdokumentation nicht durchgeführt. Nach Abnahme erfolgt die Zusammenfassung zu einem Release, welches in einem gesonderten ReleaseTest abgenommen wird.</p>

<p>Die Dokumentation besteht aus drei Teildokumenten in der Wiki:</p>
<ul>
  <li><strong>Anwenderdokumentation</strong> (Beschreibung der Nutzung der Anwendung für den Endanwender mit Bedienungshinweisen)</li>
  <li><strong>Technische Systemdokumentation</strong> (Beschreibung der Codierung für den internen Gebrauch)</li>
  <li><strong>Betriebsdokumentation</strong> (Beschreibung für IT zum Betrieb, Wartung und Neuinstallation)</li>
</ul>

<h2>1.2. Vorgaben zur Softwareentwicklung und zur Entwicklungsumgebung</h2>
<ul>
  <li><strong>1.2.1. Funktionstrennung</strong> zwischen Entwicklung und Produktion; klar definierte Entwicklungsumgebung.</li>
  <li><strong>1.2.2. Verwendung von Programmiersprachen</strong> – gemäß hausinterner Vorgaben.</li>
  <li><strong>1.2.3. Programmierrichtlinien</strong> – Benennungskonventionen, Kommentare, Modulstruktur, Code-Organisation.</li>
  <li><strong>1.2.4. Qualitätssicherung</strong> und technische Prüfroutinen.</li>
  <li><strong>1.2.5. Aufbewahrung</strong> von Quellcode und Artefakten.</li>
</ul>

<h2>1.3. Test-, Abnahme- und Freigabeverfahren</h2>
<ul>
  <li><strong>1.3.1. Abnahme</strong> von selbst entwickelter Software (Eigenanwendungen).</li>
  <li><strong>1.3.2. Ausbringung</strong> von freigegebenen Releases.</li>
  <li><strong>1.3.3. Fehlerbereinigung</strong> in freigegebenen Anwendungen (Incident-/Problemmanagement).</li>
</ul>

<h2>1.4. Management-Report Eigenprogrammierung</h2>
<p>Die Entwicklung erstellt jeweils nach Ablauf des Quartals einen Management-Report, der die Geschäftsleitung über wichtige Themen per MIS informiert. Aufgeschlüsselt nach Anwendung wird auf folgende Punkte eingegangen:</p>
<ul>
  <li>Zuständiger Programmierer</li>
  <li>Releaseeinsätze mit Inhalten</li>
  <li>Status über Weiterentwicklungen, Planungsgespräche, offene Anforderungen und weiteres Vorgehen</li>
  <li>Nennenswerte Probleme und Risiken</li>
</ul>

<h2>1.5. Umgang mit ausgelagerten Entwicklungen</h2>
<ol>
  <li>Inhaltliche Grundanforderungen aus Pflichtenheft, Geschäftsprozess oder Projekt.</li>
  <li>Agile Umsetzung in Vorgängen über das Backlog.</li>
  <li>Erstellung von Anwender- und Betriebsdokumentation.</li>
  <li>Breiteneinsatz nach Abnahme.</li>
</ol>

<h2>1.6. Sonstiges</h2>
<h3>1.6.1. Außerbetriebnahme von Eigenanwendungen</h3>
<p>Für die Außerbetriebnahme ist ein spezifischer Vorgang im Forum ISM vorgesehen.</p>
<h3>1.6.2. Logging</h3>
<p>Anwendungen implementieren Logging zur Nachvollziehbarkeit und Fehlersuche (Informations-, Fehler- und Debug-Protokolle).</p>
""";

    public const string RpaRichtlinieHtml = """
<h2>1. Anforderungen/Vorgaben für RPA Entwicklungen</h2>

<h3>Grundsätzlich</h3>
<p><strong>Definition:</strong> RPA Entwicklungen (= Robotic Process Automation) sind grundsätzlich zulässig. Für die Entwicklung sind die nachfolgenden Vorgaben zu beachten.</p>
<p><strong>Software:</strong> Die MERKUR PRIVATBANK hat sich strategisch für die RPA-Software von <strong>UiPath</strong> entschieden. Diese ist auch von Atruvia zur Automatisierung ihrer Anwendungen freigegeben.</p>
<p><strong>Dienstleister:</strong> Die notwendigen Lizenzen werden über die Firma Roboyo bezogen. Roboyo ist als Partner im Bereich RPA in der Bank etabliert und im Forum OSM dokumentiert.</p>

<h3>Anforderungs- und Entwicklungsprozess</h3>
<p><strong>Grundsätzliches:</strong> Die fachliche Verantwortung für RPA-automatisierte Geschäftsprozesse verbleibt in der Fachabteilung. Die Unternehmensentwicklung verantwortet die technische Umsetzung.</p>

<h4>Prozessauswahl</h4>
<p>Jede Fachabteilung kann Vorschläge an die Unternehmensentwicklung richten. Die finale Entscheidung trifft ein Mitarbeiter aus dem Team DIP der UENT.</p>
<p><strong>Mindestanforderungen an die Analyse der Automatisierbarkeit:</strong></p>
<ul>
  <li>Prozesszeitplan und -häufigkeit</li>
  <li>Volumen bzw. erwartetes Volumen und dessen Entwicklung</li>
  <li>Machbarkeit</li>
  <li>Komplexität</li>
  <li>Nutzen</li>
</ul>

<h4>Dokumentation</h4>
<p>Für jeden Prozess erstellt ein Mitarbeiter aus dem Team DIP zusammen mit der Fachabteilung eine Dokumentation des Automatisierungspotenzials und ein <strong>Prozessdefinitionsdokument (PDD)</strong>. Das PDD dient als Grundlage für den RPA-Prozess — sowohl für externe als auch interne Entwicklungen.</p>
<p><strong>Mindestanforderungen an das PDD:</strong></p>
<ul>
  <li>Benennung des zu verwendenden technischen Users</li>
  <li>Prozessmodell (Schaubild Prozessablauf)</li>
  <li>Detaillierte Prozessschritte (Screenshot-Anleitung inkl. Kommentare)</li>
</ul>

<h4>Kompetenzen</h4>
<p>Die Bank setzt <strong>Unattended Roboter</strong> ein. Diese benötigen eigene technische User mit definierten Kompetenzen; Anlage und Kompetenzvergabe erfolgen durch die Betriebsorganisation nach Bewertung durch die IT-Sicherheit. Aus dem Namen eines technischen Users soll die Fachabteilung ersichtlich sein.</p>

<h4>Angebot und Auftragserteilung an externe Dienstleister</h4>
<p>Der Leiter der Unternehmensentwicklung verantwortet das Budget und die Verträge mit Dienstleistern — Einzelauftrag oder Rahmenvertrag mit Gesamtstundenanzahl. Die operative Auswahl kann an die Mitarbeiter delegiert werden.</p>

<h4>Genehmigung von RPA-Automatisierungen</h4>
<p>Die grundsätzliche Strategie wird vom Leiter Unternehmensentwicklung vorgegeben; einzelne Prozesse werden eigenverantwortlich vom Team DIP ausgewählt und bedürfen keiner gesonderten Genehmigung.</p>

<h3>Entwicklung</h3>
<p><strong>Grundsätzlich:</strong> RPA-Prozesse können intern oder extern entwickelt werden. Die Entscheidung obliegt dem Team DIP. Nach Abschluss der Entwicklung führt das Team DIP einen Funktionstest durch und schaltet den Prozess frei.</p>

<h4>Code-Standards</h4>
<p>Ausschließlich Code in der aktuellen Orchestrator-Version; Variablen und Argumente sind eindeutig benannt (z. B. <code>str</code> für String, <code>in</code> für In-Argumente).</p>

<h4>Datensicherung und Code-Versionierung</h4>
<p>Entwicklung und Speicherung unter <code>\\file01\NETPROG\Roboting</code> in Unterordnern pro Prozess. Upload in UiPath Orchestrator (Server <code>fb007a7g</code>) mit Kommentar im Format <code>YYYYMMDD</code> + BugFix/Neuentwicklung + Kurzbeschreibung. Rollback auf jede vorhergehende Codeversion ist möglich. Beide Server sind in der generellen Bank-Sicherung enthalten.</p>

<h4>Logfile</h4>
<p>Primäre Logfiles werden im UiPath Orchestrator gespeichert und sind unveränderlich. Log-Level und Messages sind im Code durch den Programmierer zu setzen; das Team DIP prüft im Review auf Nachvollziehbarkeit.</p>

<h4>Entwicklung durch Bankmitarbeiter</h4>
<p>Ein Mitarbeiter der Unternehmensentwicklung meldet sich mit der technischen Identität an und entwickelt den Prozess. Eine Bild- oder Videoaufzeichnung erfolgt nicht.</p>

<h4>Entwicklung durch Dienstleister</h4>
<p>Der externe Dienstleister arbeitet in der Bank-Umgebung via TeamViewer. Kennwort und Benutzername wechseln mindestens täglich. Die Sitzung wird vollständig aufgezeichnet und <strong>90 Tage gespeichert</strong>; das Einverständnis des externen Programmierers wird eingeholt. Zugriff nur für vertrauenswürdige Dienstleister mit Vertragsverhältnis; Bewertung im Forum OSM. Das Team DIP ist weisungsbefugt.</p>

<p><strong>Arbeitsgrundlage:</strong> Das PDD fungiert als Lasten-/Pflichtenheft. Eigentumsverhältnisse werden vertraglich geregelt; Dienstleister verpflichten sich zur unbefristeten Vertraulichkeit.</p>
<p><strong>Hardware und Software:</strong> Entwicklung auf Rechnern der Bank mit UiPath Studio.</p>

<h2>3. Abgrenzung der Komponenten</h2>

<h3>Programme</h3>
<p><strong>UiPath Orchestrator:</strong> verwaltet technische Identitäten, speichert und versioniert Code, steuert Ausführung.</p>
<p><strong>UiPath Studio:</strong> Programmierung des Codes, Upload in Orchestrator.</p>
<p><strong>UiPath Robot:</strong> führt Code aus dem Orchestrator mit entsprechender technischer Identität aus.</p>

<h3>Sonstiges</h3>
<p><strong>RPA-Prozesse:</strong> im UiPath Studio programmierte Abläufe.</p>
<p><strong>Technische Identitäten:</strong> vergeben Kompetenzen an Roboter. Ein technischer User bearbeitet nur Prozesse, die auch menschliche Mitarbeiter der jeweiligen Fachabteilung bearbeiten würden.</p>

<h3>Standardkompetenzen</h3>
<p>Folgende Rechte sind zur Nutzung im RPA-Umfeld zwingend erforderlich und stellen keinen Verstoß gegen das Minimalprinzip dar:</p>
<ul>
  <li><strong>Microsoft Office Lizenz</strong> – UiPath-Framework nutzt Excel-Konfigurationsdateien.</li>
  <li><strong>SMTP-Zugang</strong> – für Error-Messages und Statusreports per Mail.</li>
  <li><strong>Web 2</strong> – Lizenzverifikation online.</li>
  <li><strong>Bildschirmschoner deaktiviert</strong> – damit RPA mit der Anwendung interagieren kann.</li>
  <li><strong>TeamViewer</strong> – für Fernwartung durch Roboyo nach den unter „Entwicklung durch Dienstleister" definierten Sicherheitsstandards.</li>
</ul>

<h2>4. Test-, Abnahme- und Freigabeverfahren</h2>

<h3>Neuentwicklung oder Prozesserweiterung</h3>
<p>Vor produktivem Einsatz prüft die Unternehmensentwicklung die Übereinstimmung mit dem PDD durch einen <strong>User Acceptance Test (UAT)</strong>, dokumentiert im vorgesehenen Excel-Dokument; optional auch per Videoaufzeichnung.</p>
<p><strong>Mindestbestandteile UAT:</strong></p>
<ul>
  <li>2 Testfälle Happy Path</li>
  <li>2 Testfälle Negative Path inkl. Errorhandling</li>
</ul>

<h3>Fehlerbehebung/BugFix</h3>
<p>Änderungen am RPA-Prozess nach Änderungen der Benutzeroberfläche, die den im PDD beschriebenen Workflow nicht verändern, müssen über den Code-Versionierungs-Kommentar hinaus nicht gesondert dokumentiert werden.</p>
""";

    public const string ExterneEntwicklungHtml = """
<h2>Anforderungen / Vorgaben für Entwicklung und Hosting von Anwendungen durch externe Dritte</h2>
<p>Entwicklung und Hosting durch externe Dritte sind grundsätzlich zulässig. Für die Programmierung sind die nachfolgenden Vorgaben zu beachten.</p>
<p>Bei Entwicklungen im größeren Umfang sollten Planung und Durchführung der Entwicklung im Rahmen eines Projekts unter Berücksichtigung der Vorgaben zum Vorgehensmodell aus dem Projektmanagement erfolgen und dabei unter anderem auch Auswirkungsanalysen vorgenommen werden.</p>
<p>Vor Entwicklung findet die Geschäftsanbahnung statt (Auswahl Vertragspartner, Vertragsprüfung, Einbeziehung Fachabteilung, Datenschutz und Informationssicherheit); sie ist nicht Bestandteil dieser Richtlinie.</p>
<p>Ziel der Richtlinie ist eine eindeutige Anforderung an die Anwendungsentwicklung mit Vorgaben zur Anforderungsermittlung und zum Entwicklungsziel.</p>

<h2>1. Vorgaben bei Beauftragung und Entwicklung</h2>

<ol>
  <li><strong>Hosting und Infrastruktur:</strong> Effektives Datenmanagement und Datenschutz; Hosting ausschließlich auf deutschen Servern und in Deutschland.</li>
  <li><strong>Sicherheitsanforderungen und Verschlüsselungen:</strong> Verschlüsselung von Daten bei Übertragung und im Ruhezustand, regelmäßige Sicherheitsaudits, robuste Zugriffssteuerung, SSL-Verschlüsselung für Web-Anwendungen.</li>
  <li><strong>Programmiervorgaben:</strong> Gängige Programmiersprache, einheitlicher Code-Stil, Benennungskonventionen, Kommentare, Dokumentation, Fehlerbehandlung, Logging, Modulstruktur, automatisierte Tests, Versionskontrolle.</li>
  <li><strong>Eigentumsrechte:</strong> werden in der Vertragsverhandlungsphase geregelt.</li>
  <li>
    <strong>Programmumgebungen:</strong>
    <ul>
      <li><em>Entwicklungsumgebung (Dev):</em> Tatsächliche Entwicklung, individueller Arbeitsstil.</li>
      <li><em>Testumgebung (Test):</em> Unit-, Integrations- und Systemtests. Produktionsnah, mit Testdaten statt realer Daten.</li>
      <li><em>Produktionsumgebung:</em> Nur getestete und freigegebene Versionen; Änderungen streng kontrolliert; regelmäßige Backups.</li>
    </ul>
  </li>
  <li>
    <strong>Protokollfunktionen und Versionierungen:</strong>
    <ul>
      <li>Logging: Informations-, Fehler- und Debug-Protokolle.</li>
      <li>Versionierung: Empfehlung <em>Semantische Versionierung</em> (Hauptversion, Nebenversion, Patch-Version).</li>
    </ul>
  </li>
  <li><strong>Berechtigungsmanagement:</strong> Ausgestaltung gemäß Richtlinie Kompetenzverwaltung.</li>
  <li><strong>Releasemanagement:</strong> Siehe Abschnitt 2.</li>
  <li><strong>Fehlerbehandlung:</strong> User meldet Fehler an Produktverantwortlichen; Kontakt zum Dienstleister zur Analyse. Bug → Patch-Release durch Dienstleister; Infrastruktur-Fehler → vollständige Behebung durch Dienstleister.</li>
  <li><strong>Datensicherheit und Datenschutz:</strong> Einhaltung DSGVO und relevanter Vorschriften; Bereitstellung von Interne-Revisionsberichten, TOMs und Zertifizierungen muss gewährleistet sein.</li>
  <li><strong>Schulung und Support:</strong> Schulungen und regelmäßige Supportleistungen.</li>
  <li><strong>Überwachung und Optimierung:</strong> Regelmäßige Überwachung nach Implementierung sowie Performance-Optimierungen.</li>
  <li><strong>ForumISM:</strong> Abbildung gemäß Richtlinie zur Bearbeitung von Geschäftsprozessen in ForumISM.</li>
  <li><strong>ForumOSM:</strong> Abbildung gemäß Richtlinie Outsourcing / Auslagerung.</li>
  <li>
    <strong>Allgemeine Punkte und weitere Regelungen:</strong> Bei Neuentwicklungen sind durch den Dienstleister folgende Dokumentationen zu erstellen:
    <ul>
      <li>Anwenderdokumentation</li>
      <li>Technische Systemdokumentation</li>
      <li>Betriebsdokumentation</li>
      <li>Schnittstellenübersicht und Datenfluss</li>
    </ul>
    Die Abnahme liegt beim Fachbereich (auch für Changes).
  </li>
  <li><strong>System- und Hardware-Anforderungen:</strong> Mehrkernprozessor, ausreichender RAM, Speicherplatz, regelmäßige Datensicherung, Kompatibilität mit Betriebssystem, Firewalls, Antivirus, Intrusion-Detection, Verschlüsselung für gespeicherte und übertragene Daten.</li>
  <li><strong>Beendigung der Nutzung:</strong> Ordnungsgemäße Entfernung/Löschung aller Daten unter Einhaltung der Datenschutzanforderungen.</li>
</ol>

<h2>2. Anforderungs- und Entwicklungsprozess bzw. Test und Abnahme</h2>

<h3>2.1. Anforderungen</h3>
<ol>
  <li>Inhaltliche Grundanforderungen aus Pflichtenheft, Geschäftsprozess oder Projekt.</li>
  <li>Agile Umsetzung in User Stories aus dem Backlog.</li>
  <li>Bei neuer Version: Anwenderdokumentation durch Unternehmensentwicklung aktualisiert.</li>
  <li>Bei neuer Version: Technische Systemdokumentation durch Dienstleister aktualisiert.</li>
  <li>Breiteneinsatz nach Abnahme.</li>
</ol>

<h3>2.2. Abnahme und Testverfahren</h3>
<p><strong>a) Entwicklertest:</strong> Der Entwickler testet einzelne Vorgänge in der Testumgebung. Testdaten werden gemocked, sodass keine Bankdaten weitergegeben werden müssen. Tests erfolgen physisch beim Dienstleister; Bestätigung über das digitale Board.</p>
<p><strong>b) Funktionstest:</strong> Eine weitere Person aus Team DIP oder Fachabteilung testet Vorgänge in der Testumgebung. Die Testumgebung ist komplett von Produktion getrennt und mit eigener Instanz.</p>

<h3>2.3. Ausbringung von freigegebenen Releases</h3>
<p>Bei Erfüllung aller Voraussetzungen wird mit dem Dienstleister ein Termin vereinbart und die Anwendung released. Das Change-Management wird im Forum ISM gepflegt. Im Fehlerfall wendet sich der Anwender an die Unternehmensentwicklung.</p>
""";
}
