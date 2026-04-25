using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.DTOs;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Enums;
using ClosedXML.Excel;
using UglyToad.PdfPig;

namespace OhbPortal.Web.Controllers;

[Authorize]
[Route("[controller]/[action]")]
public class AiAssistentController : BaseController
{
    private readonly IApplicationDbContext _db;
    private readonly IDokumentService _dokumentService;
    private readonly IFreigabeService _freigabeService;
    private readonly IKapitelService _kapitelService;
    private readonly IFileStorage _fileStorage;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AiAssistentController> _logger;

    public AiAssistentController(
        IApplicationDbContext db,
        IDokumentService dokumentService,
        IFreigabeService freigabeService,
        IKapitelService kapitelService,
        IFileStorage fileStorage,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<AiAssistentController> logger)
    {
        _db = db;
        _dokumentService = dokumentService;
        _freigabeService = freigabeService;
        _kapitelService = kapitelService;
        _fileStorage = fileStorage;
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public record ChatNachrichtDto(string rolle, string text);
    public record ChatAnfrageDto(string frage, List<ChatNachrichtDto>? verlauf, AktionBestaetigungDto? bestaetigeAktion);
    public record AktionBestaetigungDto(string typ, JsonElement parameter);

    // ── Hauptendpoint ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Fragen([FromBody] ChatAnfrageDto anfrage)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                     ?? _config["OpenAiApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return Json(new { error = "KI-Assistent ist nicht konfiguriert (OPENAI_API_KEY fehlt)." });

        // ── Bestätigte Aktion ausführen ───────────────────────────────────
        if (anfrage.bestaetigeAktion is not null)
            return await AktionAusfuehren(anfrage.bestaetigeAktion);

        if (string.IsNullOrWhiteSpace(anfrage?.frage))
            return Json(new { error = "Bitte eine Frage eingeben." });

        try
        {
            var kontext = await BaueKontextAsync(anfrage.frage);
            var systemPrompt = BaueSystemPrompt();

            var messages = new List<object> { new { role = "system", content = systemPrompt } };

            if (anfrage.verlauf is { Count: > 0 })
                foreach (var n in anfrage.verlauf.TakeLast(10))
                    messages.Add(new { role = n.rolle == "assistant" ? "assistant" : "user", content = n.text });

            var userContent = string.IsNullOrWhiteSpace(kontext)
                ? anfrage.frage
                : $"{anfrage.frage}\n\n---\n*Aktuelle Systemdaten (automatisch geladen):*\n{kontext}";
            messages.Add(new { role = "user", content = userContent });

            var requestBody = new
            {
                model = "gpt-4.1-mini",
                max_tokens = 1500,
                temperature = 0.3,
                messages,
                tools = BaueToolDefinitionen(),
                tool_choice = "auto"
            };

            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Add("Authorization", $"Bearer {apiKey}");
            req.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var resp = await client.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI {Status}: {Body}", resp.StatusCode, body);
                return Json(new { error = $"KI-Dienst antwortete mit {(int)resp.StatusCode}." });
            }

            using var doc = JsonDocument.Parse(body);
            var choice = doc.RootElement.GetProperty("choices")[0];
            var finishReason = choice.GetProperty("finish_reason").GetString();
            var message = choice.GetProperty("message");

            // ── Tool-Call erkannt → Bestätigungsanfrage an Frontend ───────
            if (finishReason == "tool_calls" && message.TryGetProperty("tool_calls", out var toolCalls))
            {
                var tc = toolCalls[0];
                var funcName = tc.GetProperty("function").GetProperty("name").GetString()!;
                var funcArgs = tc.GetProperty("function").GetProperty("arguments").GetString()!;
                var argsElement = JsonDocument.Parse(funcArgs).RootElement;

                var beschreibung = BaueAktionsBeschreibung(funcName, argsElement);

                return Json(new
                {
                    aktion = new
                    {
                        typ = funcName,
                        parameter = argsElement,
                        beschreibung
                    }
                });
            }

            // ── Normale Textantwort ──────────────────────────────────────
            var antwort = message.GetProperty("content").GetString() ?? string.Empty;
            return Json(new { antwort });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KI-Anfrage fehlgeschlagen");
            return Json(new { error = "KI-Anfrage fehlgeschlagen: " + ex.Message });
        }
    }

    // ── Aktion ausführen ──────────────────────────────────────────────────────
    private async Task<IActionResult> AktionAusfuehren(AktionBestaetigungDto aktion)
    {
        try
        {
            var p = aktion.parameter;
            switch (aktion.typ)
            {
                case "dokument_erstellen":
                {
                    var kapitelId = GetInt(p, "kapitel_id");
                    if (kapitelId == 0)
                    {
                        var kapitelName = GetString(p, "kapitel_name");
                        if (!string.IsNullOrWhiteSpace(kapitelName))
                        {
                            var kap = await _db.Kapitel.AsNoTracking()
                                .FirstOrDefaultAsync(k => k.Titel.Contains(kapitelName));
                            kapitelId = kap?.Id ?? 0;
                        }
                    }
                    if (kapitelId == 0)
                        return Json(new { antwort = "**Fehler:** Kapitel konnte nicht zugeordnet werden. Bitte ein gültiges Kapitel angeben." });

                    var dto = new DokumentErstellenDto(
                        Titel: GetString(p, "titel") ?? "Neues Dokument",
                        Kurzbeschreibung: GetString(p, "kurzbeschreibung"),
                        KapitelId: kapitelId,
                        VerantwortlicherBereichId: null,
                        Kategorie: GetString(p, "kategorie"),
                        Tags: GetString(p, "tags"),
                        SichtbarAb: null,
                        SichtbarBis: null,
                        Pruefterm: null,
                        InhaltHtml: GetString(p, "inhalt"),
                        FreigabeModus: FreigabeModus.Keine,
                        OeffentlichLesbar: true,
                        Druckverbot: false);

                    var id = await _dokumentService.ErstellenAsync(dto, AktuellerBenutzerId);
                    return Json(new { antwort = $"Dokument **#{id}** \u201E{dto.Titel}\u201C wurde als Entwurf angelegt.\n\n[Zum Dokument \u2192](/Dokumente/Details/{id})" });
                }

                case "dokument_bearbeiten":
                {
                    var id = GetInt(p, "dokument_id");
                    if (id == 0)
                        return Json(new { antwort = "**Fehler:** Keine Dokument-ID angegeben." });

                    var detail = await _dokumentService.GetDetailAsync(id);
                    if (detail is null)
                        return Json(new { antwort = $"**Fehler:** Dokument #{id} nicht gefunden." });

                    var neuerInhalt = GetString(p, "inhalt") ?? detail.InhaltHtml;
                    var dto = new DokumentBearbeitenDto(
                        Titel: GetString(p, "titel") ?? detail.Titel,
                        Kurzbeschreibung: GetString(p, "kurzbeschreibung") ?? detail.Kurzbeschreibung,
                        KapitelId: detail.KapitelId,
                        VerantwortlicherBereichId: detail.VerantwortlicherBereichId,
                        Kategorie: GetString(p, "kategorie") ?? detail.Kategorie,
                        Tags: GetString(p, "tags") ?? detail.Tags,
                        SichtbarAb: detail.SichtbarAb,
                        SichtbarBis: detail.SichtbarBis,
                        Pruefterm: detail.Pruefterm,
                        InhaltHtml: neuerInhalt,
                        FreigabeModus: detail.FreigabeModus,
                        FreigabeReihenfolge: detail.FreigabeReihenfolge,
                        Druckverbot: detail.Druckverbot,
                        OeffentlichLesbar: detail.OeffentlichLesbar);

                    await _dokumentService.AktualisierenAsync(id, dto, AktuellerBenutzerId,
                        GetString(p, "aenderungshinweis") ?? "\u00dcber KI-Assistent bearbeitet");
                    return Json(new { antwort = $"Dokument **#{id}** \u201E{dto.Titel}\u201C wurde aktualisiert (neue Version).\n\n[Zum Dokument \u2192](/Dokumente/Details/{id})" });
                }

                case "status_aendern":
                {
                    var id = GetInt(p, "dokument_id");
                    var statusStr = GetString(p, "neuer_status") ?? "";
                    if (!Enum.TryParse<DokumentStatus>(statusStr, true, out var neuerStatus))
                        return Json(new { antwort = $"**Fehler:** Ung\u00fcltiger Status \u201E{statusStr}\u201C." });

                    await _dokumentService.StatusAendernAsync(id, neuerStatus, AktuellerBenutzerId, "\u00dcber KI-Assistent ge\u00e4ndert");
                    return Json(new { antwort = $"Status von Dokument **#{id}** auf **{neuerStatus}** ge\u00e4ndert.\n\n[Zum Dokument \u2192](/Dokumente/Details/{id})" });
                }

                case "freigabe_starten":
                {
                    var id = GetInt(p, "dokument_id");
                    await _freigabeService.FreigabeStartenAsync(id, AktuellerBenutzerId);
                    return Json(new { antwort = $"Freigabe-Workflow f\u00fcr Dokument **#{id}** wurde gestartet.\n\n[Zum Dokument \u2192](/Dokumente/Details/{id})" });
                }

                default:
                    return Json(new { antwort = $"**Fehler:** Unbekannte Aktion \u201E{aktion.typ}\u201C." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktion {Typ} fehlgeschlagen", aktion.typ);
            return Json(new { antwort = $"**Fehler beim Ausführen:** {ex.Message}" });
        }
    }

    // ── Tool-Definitionen für OpenAI ──────────────────────────────────────────
    private static object[] BaueToolDefinitionen() => new object[]
    {
        new
        {
            type = "function",
            function = new
            {
                name = "dokument_erstellen",
                description = "Neues Dokument im OHB-Portal anlegen. Erstellt es als Entwurf. Der Benutzer muss die Aktion bestätigen.",
                parameters = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["titel"] = new { type = "string", description = "Titel des Dokuments" },
                        ["kurzbeschreibung"] = new { type = "string", description = "Kurze Zusammenfassung (1-2 Sätze)" },
                        ["kapitel_id"] = new { type = "integer", description = "ID des Zielkapitels (siehe Systemdaten)" },
                        ["kapitel_name"] = new { type = "string", description = "Name des Kapitels (Fallback wenn ID unbekannt)" },
                        ["kategorie"] = new { type = "string", description = "z.B. Richtlinie, Arbeitsanweisung, Strategie, Formular" },
                        ["tags"] = new { type = "string", description = "Komma-separierte Schlagwörter" },
                        ["inhalt"] = new { type = "string", description = "HTML-Inhalt des Dokuments (optional)" }
                    },
                    required = new[] { "titel" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "dokument_bearbeiten",
                description = "Bestehendes Dokument bearbeiten (Titel, Inhalt, Tags etc. ändern). Erstellt eine neue Version.",
                parameters = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["dokument_id"] = new { type = "integer", description = "ID des zu bearbeitenden Dokuments" },
                        ["titel"] = new { type = "string", description = "Neuer Titel (leer lassen = unverändert)" },
                        ["kurzbeschreibung"] = new { type = "string", description = "Neue Kurzbeschreibung" },
                        ["kategorie"] = new { type = "string", description = "Neue Kategorie" },
                        ["tags"] = new { type = "string", description = "Neue Tags (komma-separiert)" },
                        ["inhalt"] = new { type = "string", description = "Neuer HTML-Inhalt" },
                        ["aenderungshinweis"] = new { type = "string", description = "Notiz zur Änderung" }
                    },
                    required = new[] { "dokument_id" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "status_aendern",
                description = "Status eines Dokuments ändern. Mögliche Werte: Entwurf, InFreigabe, Freigegeben, Abgelehnt, Archiviert.",
                parameters = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["dokument_id"] = new { type = "integer", description = "ID des Dokuments" },
                        ["neuer_status"] = new { type = "string", @enum = new[] { "Entwurf", "InFreigabe", "Freigegeben", "Abgelehnt", "Archiviert" }, description = "Neuer Dokumentstatus" }
                    },
                    required = new[] { "dokument_id", "neuer_status" }
                }
            }
        },
        new
        {
            type = "function",
            function = new
            {
                name = "freigabe_starten",
                description = "Freigabe-Workflow für ein Dokument starten. Setzt Status auf InFreigabe und benachrichtigt Freigabegruppen.",
                parameters = new
                {
                    type = "object",
                    properties = new Dictionary<string, object>
                    {
                        ["dokument_id"] = new { type = "integer", description = "ID des Dokuments" }
                    },
                    required = new[] { "dokument_id" }
                }
            }
        }
    };

    // ── Aktionsbeschreibung für Bestätigungs-UI ──────────────────────────────
    private string BaueAktionsBeschreibung(string typ, JsonElement args)
    {
        return typ switch
        {
            "dokument_erstellen" =>
                "Neues Dokument anlegen: **" + (GetString(args, "titel") ?? "?") + "**" +
                (GetString(args, "kapitel_name") is { } kn ? "\nKapitel: " + kn : "") +
                (GetString(args, "kategorie") is { } kat ? "\nKategorie: " + kat : ""),

            "dokument_bearbeiten" =>
                "Dokument **#" + GetInt(args, "dokument_id") + "** bearbeiten" +
                (GetString(args, "titel") is { } t ? "\nNeuer Titel: " + t : "") +
                (GetString(args, "aenderungshinweis") is { } h ? "\nHinweis: " + h : ""),

            "status_aendern" =>
                "Status von Dokument **#" + GetInt(args, "dokument_id") + "** \u00e4ndern auf **" + GetString(args, "neuer_status") + "**",

            "freigabe_starten" =>
                "Freigabe-Workflow starten f\u00fcr Dokument **#" + GetInt(args, "dokument_id") + "**",

            _ => "Aktion: " + typ
        };
    }

    // ── System-Prompt ─────────────────────────────────────────────────────────
    private static string BaueSystemPrompt() => """
        Du bist der KI-Assistent des Organisationshandbuch-Portals (OHB) der Merkur Privatbank KGaA.

        **Deine Fähigkeiten:**
        1. **Fragen beantworten** — zu Dokumenten, Richtlinien, Prozessen, Inhalten.
        2. **Dokumente finden** — nach Titel, Kapitel, Kategorie, Tags oder Inhalt.
        3. **Aktionen ausführen** — du kannst Dokumente anlegen, bearbeiten, Status ändern und Freigaben starten. Nutze dafür die bereitgestellten Funktionen/Tools.

        **Regeln für Aktionen:**
        - Wenn der Benutzer eine Aktion wünscht (z.B. "Leg ein Dokument an", "Ändere den Titel", "Starte die Freigabe"), rufe die passende Funktion auf.
        - Bei unklaren Angaben (z.B. fehlendes Kapitel) frage gezielt nach, statt zu raten.
        - Nenne bei neuen Dokumenten immer einen sinnvollen Titel und schlage ein passendes Kapitel vor.
        - Wenn Inhalt gewünscht ist, erstelle strukturierten HTML-Inhalt (<h2>, <h3>, <p>, <ul>, <ol>, <table>).

        **Kontext zum Portal:**
        Das OHB-Portal verwaltet Dokumente in hierarchischen Kapiteln. Jedes Dokument hat:
        - Titel, Kurzbeschreibung, HTML-Inhalt
        - Kapitel (Themenbaum) und optional Kategorie sowie Tags
        - Status: Entwurf > InFreigabe > Freigegeben (oder Abgelehnt) > Archiviert
        - Versionen, Anhänge, Freigabe-Workflow (4-Augen oder Gruppen)

        **Rollen:** Reader, Reviewer, Editor, Approver, Bereichsverantwortlicher, Admin.

        **Hinweise:**
        - Antworte auf Deutsch, präzise, freundlich, strukturiert.
        - Verwende Markdown.
        - Erfinde keine Daten, die nicht in den Systemdaten stehen.

        **Verlinkung (sehr wichtig):**
        Erzeuge bei jeder Erwähnung eines konkreten Dokuments oder Kapitels einen klickbaren Markdown-Link, damit der Nutzer direkt dorthin springen kann.
        - Dokumente: `[Titel #42](/Dokumente/Details/42)` — die Zahl ist die Dokument-ID aus den Systemdaten.
        - Kapitel: `[Kapitelname](/Kapitel/Index/3)` — die Zahl ist die Kapitel-ID aus den Systemdaten (siehe Kapitel-Übersicht).
        - Schreibe niemals nur "Dokument 42" oder "Kapitel 3" ohne Link, wenn ID und Name aus den Systemdaten bekannt sind.
        """;

    // ── Kontext-Aufbau (DB-Abfragen) ──────────────────────────────────────────
    private async Task<string> BaueKontextAsync(string frage)
    {
        var sb = new StringBuilder();
        var frageLower = frage.ToLowerInvariant();
        var darfEntwuerfeSehen = IstEditor || IstApprover || IstAdmin;
        var jetzt = DateTime.UtcNow;

        IQueryable<Domain.Entities.Dokument> basisQuery = _db.Dokumente
            .AsNoTracking()
            .Where(d => !d.Geloescht);

        if (!darfEntwuerfeSehen)
            basisQuery = basisQuery.Where(d =>
                d.Status == DokumentStatus.Freigegeben && !d.Archiviert &&
                (d.SichtbarAb == null || d.SichtbarAb <= jetzt) &&
                (d.SichtbarBis == null || d.SichtbarBis >= jetzt));

        // Statistik
        var anzahlGesamt = await basisQuery.CountAsync();
        var anzahlFreigegeben = await basisQuery.CountAsync(d => d.Status == DokumentStatus.Freigegeben);
        sb.AppendLine("## Portal-Statistik");
        sb.AppendLine($"- Dokumente gesamt: **{anzahlGesamt}**, davon freigegeben: {anzahlFreigegeben}");
        if (darfEntwuerfeSehen)
        {
            var anzahlEntwurf = await basisQuery.CountAsync(d => d.Status == DokumentStatus.Entwurf);
            var anzahlInFreigabe = await basisQuery.CountAsync(d => d.Status == DokumentStatus.InFreigabe);
            sb.AppendLine($"- Entwürfe: {anzahlEntwurf}, In Freigabe: {anzahlInFreigabe}");
        }
        sb.AppendLine();

        // Kapitel-Übersicht (immer, damit die KI Kapitel-IDs kennt für Aktionen)
        var kapitel = await _db.Kapitel.AsNoTracking()
            .OrderBy(k => k.ElternKapitelId).ThenBy(k => k.Sortierung).Take(50).ToListAsync();
        if (kapitel.Any())
        {
            sb.AppendLine("## Verfügbare Kapitel (ID · Titel)");
            foreach (var k in kapitel)
            {
                var praefix = k.ElternKapitelId.HasValue ? "  └ " : "- ";
                sb.AppendLine($"{praefix}#{k.Id} · {k.Titel}");
            }
            sb.AppendLine();
        }

        // Dokument-Detail bei #ID
        var idMatch = Regex.Match(frage, @"(?:#|dokument[\s-]*(?:nr|nummer|id)?\s*:?\s*)(\d{1,6})", RegexOptions.IgnoreCase);
        if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out var dokId))
        {
            var treffer = await basisQuery
                .Include(d => d.Kapitel).Include(d => d.Anhaenge)
                .FirstOrDefaultAsync(d => d.Id == dokId);
            if (treffer != null)
            {
                sb.AppendLine($"## Dokument #{treffer.Id} (Detail)");
                sb.AppendLine($"- **Titel:** {treffer.Titel}");
                sb.AppendLine($"- **Kapitel:** {treffer.Kapitel?.Titel} (ID {treffer.KapitelId})");
                sb.AppendLine($"- **Status:** {treffer.Status}");
                if (!string.IsNullOrWhiteSpace(treffer.Kategorie)) sb.AppendLine($"- **Kategorie:** {treffer.Kategorie}");
                if (!string.IsNullOrWhiteSpace(treffer.Tags)) sb.AppendLine($"- **Tags:** {treffer.Tags}");
                if (!string.IsNullOrWhiteSpace(treffer.Kurzbeschreibung)) sb.AppendLine($"- **Kurzbeschreibung:** {treffer.Kurzbeschreibung}");
                sb.AppendLine($"- **Version:** {treffer.AktuelleVersion}, geändert am {treffer.GeaendertAm:dd.MM.yyyy}");

                var inhalt = StripHtml(treffer.InhaltHtml);
                if (!string.IsNullOrWhiteSpace(inhalt))
                {
                    sb.AppendLine("\n**Inhalt:**");
                    sb.AppendLine(Kuerzen(inhalt, 4000));
                }

                if (treffer.Anhaenge.Any())
                {
                    sb.AppendLine($"\n**Anhänge ({treffer.Anhaenge.Count}):**");
                    foreach (var a in treffer.Anhaenge.Take(10))
                        sb.AppendLine($"- {a.Dateiname} ({a.ContentType}, {a.DateigroesseBytes / 1024} KB)");

                    if (frageLower.Contains("anhang") || frageLower.Contains("pdf") ||
                        frageLower.Contains("datei") || frageLower.Contains("inhalt") ||
                        frageLower.Contains("excel") || frageLower.Contains("xlsm") ||
                        frageLower.Contains("xlsx") || frageLower.Contains("tabelle"))
                    {
                        var pdfText = await ExtrahierePdfInhalt(treffer.Anhaenge);
                        if (!string.IsNullOrWhiteSpace(pdfText))
                        {
                            sb.AppendLine("\n**Extrahierter PDF-Inhalt (Auszug):**");
                            sb.AppendLine(Kuerzen(pdfText, 4000));
                        }

                        var excelText = await ExtrahiereExcelInhalt(treffer.Anhaenge);
                        if (!string.IsNullOrWhiteSpace(excelText))
                        {
                            sb.AppendLine("\n**Extrahierter Excel-Inhalt (Auszug):**");
                            sb.AppendLine(Kuerzen(excelText, 4000));
                        }
                    }
                }
                sb.AppendLine();
            }
        }

        // Stichwortsuche
        var suchbegriffe = ExtrahiereSuchbegriffe(frage);
        if (suchbegriffe.Count > 0)
        {
            var treffer = new List<Domain.Entities.Dokument>();
            foreach (var term in suchbegriffe.Take(3))
            {
                var t = term;
                var teil = await basisQuery.Include(d => d.Kapitel)
                    .Where(d => d.Titel.Contains(t) ||
                        (d.Kurzbeschreibung != null && d.Kurzbeschreibung.Contains(t)) ||
                        (d.Tags != null && d.Tags.Contains(t)) ||
                        (d.Kategorie != null && d.Kategorie.Contains(t)) ||
                        (d.InhaltHtml != null && d.InhaltHtml.Contains(t)))
                    .OrderByDescending(d => d.GeaendertAm).Take(10).ToListAsync();
                foreach (var d in teil)
                    if (!treffer.Any(x => x.Id == d.Id)) treffer.Add(d);
            }
            if (treffer.Any())
            {
                sb.AppendLine($"## Passende Dokumente ({treffer.Count})");
                foreach (var d in treffer.Take(12))
                {
                    sb.Append($"- **#{d.Id}** · {d.Titel}");
                    if (d.Kapitel != null) sb.Append($" _(Kapitel: {d.Kapitel.Titel}, ID {d.KapitelId})_");
                    sb.AppendLine($" · Status: {d.Status}");
                }
                sb.AppendLine();
            }
        }

        // Zuletzt geändert bei generischen Fragen
        if ((frageLower.Contains("neuest") || frageLower.Contains("letzt") || frageLower.Contains("aktuell") ||
             frageLower.Contains("zeig") || frageLower.Contains("liste") || frageLower.Contains("überblick") ||
             frageLower.Contains("ueberblick")) && suchbegriffe.Count == 0)
        {
            var neueste = await basisQuery.Include(d => d.Kapitel)
                .OrderByDescending(d => d.GeaendertAm).Take(15).ToListAsync();
            if (neueste.Any())
            {
                sb.AppendLine("## Zuletzt geänderte Dokumente");
                foreach (var d in neueste)
                    sb.AppendLine($"- **#{d.Id}** · {d.Titel} _(Kapitel: {d.Kapitel?.Titel})_ · {d.GeaendertAm:dd.MM.yyyy}");
                sb.AppendLine();
            }
        }

        return sb.ToString().Trim();
    }

    // ── PDF-Extraktion ────────────────────────────────────────────────────────
    private async Task<string> ExtrahierePdfInhalt(ICollection<Domain.Entities.Anhang> anhaenge)
    {
        var sb = new StringBuilder();
        foreach (var anhang in anhaenge
                     .Where(a => a.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase) ||
                                 a.Dateiname.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                     .Take(3))
        {
            try
            {
                if (!_fileStorage.Existiert(anhang.SpeicherSchluessel)) continue;
                await using var stream = await _fileStorage.LadenAsync(anhang.SpeicherSchluessel);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Position = 0;
                using var pdf = PdfDocument.Open(ms);
                sb.AppendLine($"### Aus: {anhang.Dateiname}");
                foreach (var page in pdf.GetPages().Take(15))
                {
                    foreach (var word in page.GetWords())
                        sb.Append(word.Text).Append(' ');
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PDF-Extraktion fehlgeschlagen für {Datei}", anhang.Dateiname);
            }
        }
        return sb.ToString().Trim();
    }

    // ── Excel-Extraktion (xlsx, xlsm) ──────────────────────────────────────────
    private async Task<string> ExtrahiereExcelInhalt(ICollection<Domain.Entities.Anhang> anhaenge)
    {
        var sb = new StringBuilder();
        foreach (var anhang in anhaenge
                     .Where(a => a.Dateiname.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                                 a.Dateiname.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase) ||
                                 a.ContentType.Contains("spreadsheet", StringComparison.OrdinalIgnoreCase) ||
                                 a.ContentType.Contains("excel", StringComparison.OrdinalIgnoreCase))
                     .Take(3))
        {
            try
            {
                if (!_fileStorage.Existiert(anhang.SpeicherSchluessel)) continue;
                await using var stream = await _fileStorage.LadenAsync(anhang.SpeicherSchluessel);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Position = 0;
                using var workbook = new XLWorkbook(ms);

                sb.AppendLine($"### Aus: {anhang.Dateiname}");
                foreach (var ws in workbook.Worksheets.Take(5))
                {
                    sb.AppendLine($"#### Blatt: {ws.Name}");
                    var rangeUsed = ws.RangeUsed();
                    if (rangeUsed == null) { sb.AppendLine("_(leer)_"); continue; }

                    var rowCount = 0;
                    foreach (var row in rangeUsed.RowsUsed().Take(50))
                    {
                        var cells = row.CellsUsed().Select(c => c.GetFormattedString()).ToList();
                        if (cells.Count == 0) continue;
                        sb.AppendLine(string.Join(" | ", cells));
                        rowCount++;
                    }

                    var totalRows = rangeUsed.RowCount();
                    if (totalRows > 50)
                        sb.AppendLine($"_... ({totalRows - 50} weitere Zeilen)_");
                    sb.AppendLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Excel-Extraktion fehlgeschlagen für {Datei}", anhang.Dateiname);
            }
        }
        return sb.ToString().Trim();
    }

    // ── Hilfsfunktionen ───────────────────────────────────────────────────────
    private static string? GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int GetInt(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    private static readonly HashSet<string> Stopwoerter = new(StringComparer.OrdinalIgnoreCase)
    {
        "der","die","das","und","oder","ist","sind","wie","was","wo","wer","wann","warum","welche","welcher","welches",
        "ein","eine","einen","einem","einer","eines","mir","mich","dich","dir","sie","ihn","ihr","es","zu","zum","zur",
        "für","fuer","mit","bei","von","vom","aus","auf","im","in","den","dem","des","auch","nicht","aber","doch","nur",
        "dass","daß","kann","können","koennen","bitte","zeig","zeige","zeigen","finde","finden","such","suche","suchen",
        "gibt","habe","haben","hat","hätte","würde","werden","wird","sein","war","waren","über","ueber","uns","unser",
        "dokument","dokumente","handbuch","portal","ohb","kapitel","lege","erstelle","mach","mache","bitte","neues","neue","neuen",
        "ändere","aendere","ändern","aendern","starte","starten","setze","setzen"
    };

    private static List<string> ExtrahiereSuchbegriffe(string frage) =>
        Regex.Matches(frage, @"[\p{L}][\p{L}\-]{2,}")
            .Select(m => m.Value)
            .Where(t => t.Length >= 3 && !Stopwoerter.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var ohneSkripte = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var ohneTags = Regex.Replace(ohneSkripte, @"<[^>]+>", " ");
        var dekodiert = System.Net.WebUtility.HtmlDecode(ohneTags);
        return Regex.Replace(dekodiert, @"\s+", " ").Trim();
    }

    private static string Kuerzen(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen] + "…";

    // ── Feedback persistieren ─────────────────────────────────────────────────
    public record FeedbackEingabeDto(string FrageInitial, string AntwortLetzte, bool Positiv, string? Modell);

    [HttpPost]
    public async Task<IActionResult> Feedback([FromBody] FeedbackEingabeDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.FrageInitial))
            return Json(new { ok = false, error = "Ungültige Eingabe" });

        try
        {
            var fb = new Domain.Entities.KiFeedback
            {
                BenutzerId = AktuellerBenutzerId,
                FrageInitial = dto.FrageInitial.Length > 4000 ? dto.FrageInitial[..4000] : dto.FrageInitial,
                AntwortLetzte = string.IsNullOrEmpty(dto.AntwortLetzte)
                    ? ""
                    : (dto.AntwortLetzte.Length > 8000 ? dto.AntwortLetzte[..8000] : dto.AntwortLetzte),
                Bewertung = dto.Positiv
                    ? Domain.Entities.KiFeedbackBewertung.Positiv
                    : Domain.Entities.KiFeedbackBewertung.Negativ,
                ZeitstempelUtc = DateTime.UtcNow,
                ModellName = string.IsNullOrWhiteSpace(dto.Modell)
                    ? null
                    : (dto.Modell.Length > 100 ? dto.Modell[..100] : dto.Modell)
            };
            _db.KiFeedbacks.Add(fb);
            await _db.SaveChangesAsync();
            return Json(new { ok = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KI-Feedback konnte nicht gespeichert werden");
            return Json(new { ok = false });
        }
    }
}
