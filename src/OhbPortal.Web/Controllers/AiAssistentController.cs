using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Enums;
using UglyToad.PdfPig;

namespace OhbPortal.Web.Controllers;

[Authorize]
[Route("[controller]/[action]")]
public class AiAssistentController : BaseController
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _fileStorage;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AiAssistentController> _logger;

    public AiAssistentController(
        IApplicationDbContext db,
        IFileStorage fileStorage,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<AiAssistentController> logger)
    {
        _db = db;
        _fileStorage = fileStorage;
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public record ChatNachrichtDto(string rolle, string text);
    public record ChatAnfrageDto(string frage, List<ChatNachrichtDto>? verlauf);

    [HttpPost]
    public async Task<IActionResult> Fragen([FromBody] ChatAnfrageDto anfrage)
    {
        if (string.IsNullOrWhiteSpace(anfrage?.frage))
            return Json(new { error = "Bitte eine Frage eingeben." });

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                     ?? _config["OpenAiApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return Json(new { error = "KI-Assistent ist nicht konfiguriert (OPENAI_API_KEY fehlt)." });

        try
        {
            var kontext = await BaueKontextAsync(anfrage.frage);

            var systemPrompt = BaueSystemPrompt();

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            if (anfrage.verlauf is { Count: > 0 })
            {
                foreach (var n in anfrage.verlauf.TakeLast(10))
                {
                    var rolle = n.rolle == "assistant" ? "assistant" : "user";
                    messages.Add(new { role = rolle, content = n.text });
                }
            }

            var userContent = string.IsNullOrWhiteSpace(kontext)
                ? anfrage.frage
                : $"{anfrage.frage}\n\n---\n*Aktuelle Systemdaten (automatisch geladen):*\n{kontext}";

            messages.Add(new { role = "user", content = userContent });

            var requestBody = new
            {
                model = "gpt-4o-mini",
                max_tokens = 1500,
                temperature = 0.3,
                messages
            };

            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Add("Authorization", $"Bearer {apiKey}");
            req.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var resp = await client.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI Fehler {Status}: {Body}", resp.StatusCode, body);
                return Json(new { error = $"KI-Dienst antwortete mit {(int)resp.StatusCode}." });
            }

            using var doc = JsonDocument.Parse(body);
            var antwort = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            return Json(new { antwort });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KI-Anfrage fehlgeschlagen");
            return Json(new { error = "KI-Anfrage fehlgeschlagen: " + ex.Message });
        }
    }

    private static string BaueSystemPrompt() => @"Du bist der KI-Assistent des Organisationshandbuch-Portals (OHB) der Merkur Privatbank KGaA.

**Deine Aufgabe:**
- Fragen zu Dokumenten, Richtlinien, Prozessen und Organisationshandbuch-Inhalten beantworten.
- Benutzern helfen, passende Dokumente im Portal zu finden.
- Inhalte aus Dokumenten zusammenfassen oder erklären, wenn diese im Kontext bereitgestellt werden.

**Kontext zum Portal:**
Das OHB-Portal verwaltet Dokumente (Richtlinien, Arbeitsanweisungen, Prozessbeschreibungen) in hierarchischen Kapiteln. Jedes Dokument hat:
- Titel, Kurzbeschreibung, HTML-Inhalt
- Kapitel (Themenbaum) und optional Kategorie sowie Tags
- Status: Entwurf, InFreigabe, Freigegeben, Abgelehnt, Archiviert
- Versionen, Anhänge (PDF, Bilder), Freigabe-Workflow (4-Augen oder Gruppen)
- Kenntnisnahmen (Bestätigungen durch Mitarbeiter)
- Sichtbarkeitszeiträume und Prüftermine

**Rollen:** Reader, Reviewer, Editor, Approver, Bereichsverantwortlicher, Admin.

**Wichtige Hinweise:**
- Antworte auf Deutsch, präzise, freundlich und strukturiert.
- Verwende Markdown für Listen, Überschriften und Hervorhebungen.
- Wenn du Dokumente vorschlägst, nenne Titel und Kapitel; gib, falls vorhanden, die Dokument-ID an (Format: `#42`).
- Verweise bei konkreten Dokumenten auf den Pfad `/Dokumente/Details/{id}` als Link.
- Wenn die Systemdaten keine passenden Dokumente enthalten, sage es ehrlich und schlage eine präzisere Suchanfrage vor.
- Erfinde keine Dokumente, IDs, Inhalte oder Kapitel, die nicht in den bereitgestellten Systemdaten stehen.
- Wenn ein Nutzer nach einem spezifischen Dokument fragt und der Inhalt im Kontext steht, fasse ihn zusammen oder zitiere die relevante Stelle.";

    private async Task<string> BaueKontextAsync(string frage)
    {
        var sb = new StringBuilder();
        var frageLower = frage.ToLowerInvariant();

        // Berechtigungslogik: Nicht-Editor/Approver sehen nur freigegebene Dokumente im Sichtbarkeitszeitraum
        var darfEntwuerfeSehen = IstEditor || IstApprover || IstAdmin;
        var jetzt = DateTime.UtcNow;

        IQueryable<Domain.Entities.Dokument> basisQuery = _db.Dokumente
            .AsNoTracking()
            .Where(d => !d.Geloescht);

        if (!darfEntwuerfeSehen)
        {
            basisQuery = basisQuery.Where(d =>
                d.Status == DokumentStatus.Freigegeben &&
                !d.Archiviert &&
                (d.SichtbarAb == null || d.SichtbarAb <= jetzt) &&
                (d.SichtbarBis == null || d.SichtbarBis >= jetzt));
        }

        // --- Statistik ---
        var anzahlGesamt = await basisQuery.CountAsync();
        var anzahlFreigegeben = await basisQuery.CountAsync(d => d.Status == DokumentStatus.Freigegeben);
        var anzahlEntwurf = darfEntwuerfeSehen
            ? await basisQuery.CountAsync(d => d.Status == DokumentStatus.Entwurf)
            : 0;
        var anzahlInFreigabe = darfEntwuerfeSehen
            ? await basisQuery.CountAsync(d => d.Status == DokumentStatus.InFreigabe)
            : 0;
        var anzahlPruefUeberfaellig = await basisQuery
            .CountAsync(d => d.Pruefterm != null && d.Pruefterm < jetzt);

        sb.AppendLine("## Portal-Statistik");
        sb.AppendLine($"- Sichtbare Dokumente gesamt: **{anzahlGesamt}**");
        sb.AppendLine($"- Freigegeben: {anzahlFreigegeben}");
        if (darfEntwuerfeSehen)
        {
            sb.AppendLine($"- Entwürfe: {anzahlEntwurf}");
            sb.AppendLine($"- In Freigabe: {anzahlInFreigabe}");
        }
        sb.AppendLine($"- Prüftermin überfällig: {anzahlPruefUeberfaellig}");
        sb.AppendLine();

        // --- Dokument-ID in Frage erkannt? z.B. "#42" oder "Dokument 42" ---
        var idMatch = Regex.Match(frage, @"(?:#|dokument[\s-]*(?:nr|nummer|id)?\s*:?\s*)(\d{1,6})",
            RegexOptions.IgnoreCase);
        if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out var dokId))
        {
            var treffer = await basisQuery
                .Include(d => d.Kapitel)
                .Include(d => d.Anhaenge)
                .FirstOrDefaultAsync(d => d.Id == dokId);
            if (treffer != null)
            {
                sb.AppendLine($"## Dokument #{treffer.Id} (Detail)");
                sb.AppendLine($"- **Titel:** {treffer.Titel}");
                sb.AppendLine($"- **Kapitel:** {treffer.Kapitel?.Titel}");
                sb.AppendLine($"- **Status:** {treffer.Status}");
                if (!string.IsNullOrWhiteSpace(treffer.Kategorie))
                    sb.AppendLine($"- **Kategorie:** {treffer.Kategorie}");
                if (!string.IsNullOrWhiteSpace(treffer.Tags))
                    sb.AppendLine($"- **Tags:** {treffer.Tags}");
                if (!string.IsNullOrWhiteSpace(treffer.Kurzbeschreibung))
                    sb.AppendLine($"- **Kurzbeschreibung:** {treffer.Kurzbeschreibung}");
                sb.AppendLine($"- **Version:** {treffer.AktuelleVersion}");
                sb.AppendLine($"- **Geändert am:** {treffer.GeaendertAm:dd.MM.yyyy}");

                var inhalt = StripHtml(treffer.InhaltHtml);
                if (!string.IsNullOrWhiteSpace(inhalt))
                {
                    sb.AppendLine();
                    sb.AppendLine("**Inhalt:**");
                    sb.AppendLine(Kuerzen(inhalt, 4000));
                }

                if (treffer.Anhaenge.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine($"**Anhänge ({treffer.Anhaenge.Count}):**");
                    foreach (var a in treffer.Anhaenge.Take(10))
                        sb.AppendLine($"- {a.Dateiname} ({a.ContentType}, {a.DateigroesseBytes / 1024} KB)");

                    // Wenn nach Anhang-Inhalt gefragt wird: PDF extrahieren
                    if (frageLower.Contains("anhang") || frageLower.Contains("pdf") ||
                        frageLower.Contains("datei") || frageLower.Contains("inhalt"))
                    {
                        var pdfText = await ExtrahierePdfInhalt(treffer.Anhaenge);
                        if (!string.IsNullOrWhiteSpace(pdfText))
                        {
                            sb.AppendLine();
                            sb.AppendLine("**Extrahierter PDF-Inhalt (Auszug):**");
                            sb.AppendLine(Kuerzen(pdfText, 4000));
                        }
                    }
                }
                sb.AppendLine();
            }
        }

        // --- Volltextsuche basierend auf Stichworten in der Frage ---
        var suchbegriffe = ExtrahiereSuchbegriffe(frage);
        if (suchbegriffe.Count > 0)
        {
            var query = basisQuery.Include(d => d.Kapitel).AsQueryable();
            var treffer = new List<Domain.Entities.Dokument>();

            foreach (var term in suchbegriffe.Take(3))
            {
                var t = term;
                var teilTreffer = await query
                    .Where(d =>
                        d.Titel.Contains(t) ||
                        (d.Kurzbeschreibung != null && d.Kurzbeschreibung.Contains(t)) ||
                        (d.Tags != null && d.Tags.Contains(t)) ||
                        (d.Kategorie != null && d.Kategorie.Contains(t)) ||
                        (d.InhaltHtml != null && d.InhaltHtml.Contains(t)))
                    .OrderByDescending(d => d.GeaendertAm)
                    .Take(10)
                    .ToListAsync();
                foreach (var d in teilTreffer)
                    if (!treffer.Any(x => x.Id == d.Id)) treffer.Add(d);
            }

            if (treffer.Any())
            {
                sb.AppendLine($"## Passende Dokumente (gefunden: {treffer.Count})");
                foreach (var d in treffer.Take(12))
                {
                    sb.Append($"- **#{d.Id}** · {d.Titel}");
                    if (d.Kapitel != null) sb.Append($" _(Kapitel: {d.Kapitel.Titel})_");
                    sb.Append($" · Status: {d.Status}");
                    sb.AppendLine();
                    if (!string.IsNullOrWhiteSpace(d.Kurzbeschreibung))
                        sb.AppendLine($"  {Kuerzen(d.Kurzbeschreibung, 200)}");
                }
                sb.AppendLine();
            }
        }

        // --- Zuletzt geänderte Dokumente bei generischen Fragen ---
        if ((frageLower.Contains("neuest") || frageLower.Contains("letzt") ||
             frageLower.Contains("aktuell") || frageLower.Contains("zeig") ||
             frageLower.Contains("liste") || frageLower.Contains("überblick") ||
             frageLower.Contains("ueberblick")) && suchbegriffe.Count == 0)
        {
            var neueste = await basisQuery
                .Include(d => d.Kapitel)
                .OrderByDescending(d => d.GeaendertAm)
                .Take(15)
                .ToListAsync();

            if (neueste.Any())
            {
                sb.AppendLine("## Zuletzt geänderte Dokumente");
                foreach (var d in neueste)
                {
                    sb.Append($"- **#{d.Id}** · {d.Titel}");
                    if (d.Kapitel != null) sb.Append($" _(Kapitel: {d.Kapitel.Titel})_");
                    sb.Append($" · geändert am {d.GeaendertAm:dd.MM.yyyy}");
                    sb.AppendLine();
                }
                sb.AppendLine();
            }
        }

        // --- Kapitel-Struktur bei Fragen zu Kapitel/Themenbaum ---
        if (frageLower.Contains("kapitel") || frageLower.Contains("themenbaum") ||
            frageLower.Contains("struktur") || frageLower.Contains("übersicht") ||
            frageLower.Contains("uebersicht"))
        {
            var kapitel = await _db.Kapitel
                .AsNoTracking()
                .OrderBy(k => k.ElternKapitelId)
                .ThenBy(k => k.Sortierung)
                .Take(40)
                .ToListAsync();

            if (kapitel.Any())
            {
                sb.AppendLine("## Themenbaum (Kapitel)");
                foreach (var k in kapitel)
                {
                    var praefix = k.ElternKapitelId.HasValue ? "  └ " : "- ";
                    sb.AppendLine($"{praefix}#{k.Id} {k.Titel}");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString().Trim();
    }

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

    private static readonly HashSet<string> Stopwoerter = new(StringComparer.OrdinalIgnoreCase)
    {
        "der","die","das","und","oder","ist","sind","wie","was","wo","wer","wann","warum","welche","welcher","welches",
        "ein","eine","einen","einem","einer","eines","mir","mich","dich","dir","sie","ihn","ihr","es","zu","zum","zur",
        "für","fuer","mit","bei","von","vom","aus","auf","im","in","den","dem","des","auch","nicht","aber","doch","nur",
        "dass","daß","kann","können","koennen","bitte","zeig","zeige","zeigen","finde","finden","such","suche","suchen",
        "gibt","habe","haben","hat","hätte","würde","werden","wird","sein","war","waren","über","ueber","uns","unser",
        "this","that","and","or","the","to","of","for","with","on","in","at","by","is","are","was","were","a","an",
        "dokument","dokumente","handbuch","portal","ohb","kapitel"
    };

    private static List<string> ExtrahiereSuchbegriffe(string frage)
    {
        var tokens = Regex.Matches(frage, @"[\p{L}][\p{L}\-]{2,}")
            .Select(m => m.Value)
            .Where(t => t.Length >= 3 && !Stopwoerter.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return tokens;
    }

    private static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var ohneSkripte = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", " ",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var ohneTags = Regex.Replace(ohneSkripte, @"<[^>]+>", " ");
        var dekodiert = System.Net.WebUtility.HtmlDecode(ohneTags);
        return Regex.Replace(dekodiert, @"\s+", " ").Trim();
    }

    private static string Kuerzen(string text, int maxLen) =>
        text.Length <= maxLen ? text : text.Substring(0, maxLen) + "…";
}
