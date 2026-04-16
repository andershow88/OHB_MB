# Organisationshandbuch (OhbPortal)

Ein modernes Webportal für Organisationsinhalte — Unternehmensrichtlinien, Verzeichnisse,
Formulare, Ablauforganisationen, Prozessmodelle und allgemeine Info-Dokumente. Ablösung der
HCL/IBM-Notes-Datenbank eines Organisationshandbuchs. Kein 1:1-Nachbau — die fachliche
Logik wurde übernommen und in eine klare, aufgeräumte, enterprise-taugliche Oberfläche überführt.

## Tech-Stack

- **ASP.NET Core 8 MVC** mit Clean Architecture (Domain / Application / Infrastructure / Web)
- **Entity Framework Core 8** — PostgreSQL (Railway) oder SQLite (lokal, automatischer Fallback)
- **Bootstrap 5** + hauseigenes `merkur.css` (inkl. Dark-Mode)
- **Quill 2** (Rich-Text-Editor, via CDN)
- **Cookie-Auth** mit SHA256-gehashten Demo-Passwörtern
- **Docker / Railway**-ready

## Schnellstart lokal

```bash
cd /Users/andersonbuettenbender/OHB_MB
dotnet restore
dotnet run --project src/OhbPortal.Web
```

Die Anwendung startet auf `http://localhost:5000` (oder `PORT`-Env-Variable). Beim ersten Start
wird automatisch eine SQLite-DB angelegt und mit Demo-Daten befüllt.

## Demo-Zugänge

Auf der Login-Seite **"MERKUR"** als Zauberwort eingeben, dann direkt aus dem Grid auswählen.

| Benutzer | Passwort     | Rolle                     |
|----------|--------------|---------------------------|
| admin    | Admin1234!   | Administrator             |
| editor   | Demo1234!    | Editor                    |
| approver | Demo1234!    | Approver                  |
| reviewer | Demo1234!    | Reviewer                  |
| bereich  | Demo1234!    | Bereichsverantwortlicher  |
| reader   | Demo1234!    | Reader                    |

## Deployment auf Railway

1. Neues Projekt + GitHub-Repo verbinden
2. Railway erkennt `Dockerfile` / `railway.toml` automatisch
3. Optional PostgreSQL-Service hinzufügen — Railway setzt `DATABASE_URL` automatisch
4. (Optional) Volume unter `/app/wwwroot/uploads` mounten, damit Anhänge persistent bleiben

## Fachlicher Scope / MVP

**Enthalten**
- Hierarchischer **Themenbaum** mit Seed-Hauptkapiteln (1–5) und 17 Unterkapiteln unter "Sachgebiete / Anweisungen"
- **Dokumente** mit Metadaten (Titel, Beschreibung, Kapitel, Verantwortlicher Bereich, Kategorie, Tags, Sichtbar-ab/-bis, Prüftermin, Druckverbot, Sichtbarkeit)
- **Rich-Text-Editor** (Überschriften, Listen, Blockquotes, Code, Links, Formatierung)
- **Versionierung**: jede Speicherung erzeugt eine neue Version mit Snapshot + Änderungshinweis
- **Freigabe-Workflow**: Modi `Keine` / `VierAugen` / `Gruppen`, Reihenfolge `Parallel` / `Sequentiell`, mehrere Gruppen mit n Mitgliedern und konfigurierbarer Anzahl benötigter Zustimmungen, Audit pro Entscheidung
- **Kenntnisnahmen** pro Benutzer oder Team mit Fälligkeit, Überfällig-Anzeige, Bestätigung
- **Audit-Log / Änderungsverlauf** als Timeline pro Dokument
- **Anhänge**: performantes Upload/Download via `IFileStorage`-Abstraktion (aktuell lokaler Provider, austauschbar gegen S3/Azure Blob)
- **Suche & Filter** über Titel/Inhalt/Tags, Kapitel, Status, Kategorie, überfällige Prüftermine
- **Archiv** und **Papierkorb** (Soft-Delete, Wiederherstellung)
- **Dashboard** mit KPIs + Handlungsbedarf + letzten Änderungen
- **Dark Mode** (persistiert via localStorage)
- **Beispielrichtlinie** "Richtlinie zum Einsatz von Fernwartung" inkl. Freigabekonfig (2 Gruppen parallel), Kenntnisnahmen, Versionen 1+2, Audit-Einträge

**Bewusst offen gelassen** (siehe TODOs)
- Freigabegruppen-UI (Anlegen/Pflegen von Gruppen und Mitgliedern im Dokument-Edit; aktuell über Seed oder Admin-Konsole)
- Admin-Bereich für Benutzer- und Team-Verwaltung
- ACL-Vererbung entlang des Themenbaums (Daten­modell bereits vorbereitet: `DokumentBerechtigung`)
- Volltext-Suche mit Highlighting (derzeit einfache `Contains`-Abfragen)
- E-Mail-Benachrichtigungen für Freigaben und Kenntnisnahmen
- PDF-Export / Druck-Freigabe

## Architekturentscheidungen

### Clean Architecture (4 Projekte)
- **Domain** — Entitäten + Enums, keine Dependencies
- **Application** — `IApplicationDbContext`, DTOs, Services, Interfaces (EF Core nur für `DbSet<>`-Signaturen)
- **Infrastructure** — `ApplicationDbContext`, `LocalFileStorage`, Seeder
- **Web** — Controllers, Razor Views, ViewModels, DI-Config

### Datenmodell (Auszug)
- `Kapitel` hierarchisch via `ElternKapitelId`
- `Dokument` zentral mit Metadaten, Soft-Delete und Archiv-Flag
- `DokumentVersion` als unveränderlicher Snapshot bei jedem Save
- `FreigabeGruppe` → n `FreigabeGruppeMitglied` + n `FreigabeZustimmung`, Auswertung server­seitig nach `BenoetigteZustimmungen`
- `Kenntnisnahme` entweder für Benutzer ODER Team
- `AuditEintrag` pro relevantem Ereignis, mit Typ-Enum

### Freigabe-Auswertung
- Jede Gruppe gilt als erfüllt, sobald ≥ `BenoetigteZustimmungen` Zustimmungen vorliegen.
- Ein einziges `Abgelehnt` setzt das Dokument sofort auf `Abgelehnt`.
- Sind alle Gruppen erfüllt, wird der Status auf `Freigegeben` gesetzt.
- Sequentielle Reihenfolge ist datenmodellseitig vorbereitet (`FreigabeGruppe.Reihenfolge`), die UI schaltet sequenziell noch nicht automatisch frei — TODO.

### Datei-Speicherung
- `IFileStorage` in Application, Implementierung `LocalFileStorage` in Infrastructure.
- Ablage unter `wwwroot/uploads/dok_{id}/{guid}_{originalname}`.
- Für Produktion empfohlen: S3/Azure Blob via Austausch der DI-Registrierung.

## Angenommene Details

- Berechtigungen: Seeded Rollen genügen im MVP. Rollen-Claim wird aus Login-Ergebnis abgeleitet.
- Autoren/Leser-Konzept: Editor, Bereichsverantwortlicher und Admin dürfen bearbeiten; Approver und Admin archivieren/löschen; Reader liest.
- Sichtbar-ab/-bis: Nur Informations-Felder im MVP (noch keine automatische Zugriffsfilterung).
- Links zwischen Dokumenten: Datenmodell vorhanden, Pflege-UI folgt.

## Nächste sinnvolle Schritte

1. UI zum Pflegen von Freigabegruppen (Mitglieder, Reihenfolge, Quorum) direkt im Dokument
2. Admin-Bereich: Benutzer, Rollen, Teams
3. Sequentielle Workflow-Ausführung (erst Gruppe 1 muss erfüllt sein, dann 2, …)
4. E-Mail-Benachrichtigungen (SendGrid / SMTP)
5. ACL-Vererbung & Dokumentberechtigungen-UI
6. Volltextsuche (PostgreSQL `tsvector` oder ElasticSearch)
7. Versandprotokolle & Mailvorlagen
8. PDF-Export einer Richtlinie
