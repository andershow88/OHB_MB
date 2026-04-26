using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.DTOs;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Enums;
using OhbPortal.Web.ViewModels;

namespace OhbPortal.Web.Controllers;

[Authorize]
public class DokumenteController : BaseController
{
    private readonly IDokumentService _svc;
    private readonly IFreigabeService _freigabe;
    private readonly IKenntnisnahmeService _kn;
    private readonly IAnhangService _anhang;
    private readonly IBerechtigungService _ber;
    private readonly IAuditService _audit;
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _fileStorage;

    public DokumenteController(
        IDokumentService svc,
        IFreigabeService freigabe,
        IKenntnisnahmeService kn,
        IAnhangService anhang,
        IBerechtigungService ber,
        IAuditService audit,
        IApplicationDbContext db,
        IFileStorage fileStorage)
    {
        _svc = svc;
        _freigabe = freigabe;
        _kn = kn;
        _anhang = anhang;
        _ber = ber;
        _audit = audit;
        _db = db;
        _fileStorage = fileStorage;
    }

    private BerechtigungsKontext Kontext => new(AktuellerBenutzerId, AktuelleRolle);

    public async Task<IActionResult> Index(string? q, int? kapitelId, DokumentStatus? status,
        string? kategorie, bool? pruefUeberfaellig)
    {
        var filter = new DokumentFilterDto(q, kapitelId, status, kategorie, pruefUeberfaellig,
            NurAktuellSichtbare: !IstEditor && !IstApprover);
        ViewBag.Filter = filter;
        await FuelleDropdowns(kapitelId);
        var liste = await _svc.GetAlleAsync(filter, Kontext);
        return View(liste);
    }

    public async Task<IActionResult> Archiv()
    {
        ViewBag.SeitenTitel = "Archiv";
        ViewBag.SeitenIcon = "bi-archive";
        var liste = await _svc.GetAlleAsync(new DokumentFilterDto(IncludeArchiviert: true), Kontext);
        return View("Index", liste.Where(d => d.Archiviert).ToList());
    }

    public async Task<IActionResult> Papierkorb()
    {
        ViewBag.SeitenTitel = "Papierkorb";
        ViewBag.SeitenIcon = "bi-trash";
        var liste = await _svc.GetAlleAsync(new DokumentFilterDto(NurGeloescht: true), Kontext);
        return View("Index", liste);
    }

    public async Task<IActionResult> Details(int id)
    {
        var d = await _svc.GetDetailAsync(id);
        if (d is null) return NotFound();

        // ACL-Prüfung (außer für Editor/Admin/Approver reicht Leserecht)
        if (!await _svc.DarfLesenAsync(id, Kontext))
            return NotFound();

        // Sichtbar-ab/bis durchsetzen für Nicht-Editoren und Nicht-Approver
        if (!IstEditor && !IstApprover)
        {
            var jetzt = DateTime.UtcNow;
            var nichtSichtbar = (d.SichtbarAb.HasValue && d.SichtbarAb.Value > jetzt)
                             || (d.SichtbarBis.HasValue && d.SichtbarBis.Value < jetzt);
            if (nichtSichtbar) return NotFound();
        }
        ViewBag.Versionen = (await _svc.GetVersionenAsync(id)).ToList();
        ViewBag.Audit = (await _svc.GetAuditAsync(id)).ToList();
        ViewBag.Freigaben = (await _freigabe.GetGruppenAsync(id)).ToList();
        ViewBag.Kenntnisnahmen = (await _kn.GetProDokumentAsync(id)).ToList();
        ViewBag.AlleBenutzer = await _db.Benutzer
            .Where(b => b.IstAktiv)
            .OrderBy(b => b.Anzeigename)
            .Select(b => new { b.Id, b.Anzeigename })
            .ToListAsync();
        ViewBag.AlleTeams = await _db.Teams
            .Where(t => t.IstAktiv)
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();
        ViewBag.Berechtigungen = (await _ber.GetProDokumentAsync(id)).ToList();
        return View(d);
    }

    [HttpGet]
    public async Task<IActionResult> Neu(int? kapitelId)
    {
        if (!IstEditor) return Forbid();
        await FuelleDropdowns(kapitelId);
        return View(new DokumentBearbeitenViewModel { KapitelId = kapitelId ?? 0 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Neu(DokumentBearbeitenViewModel vm)
    {
        if (!IstEditor) return Forbid();
        if (!ModelState.IsValid)
        {
            await FuelleDropdowns(vm.KapitelId);
            return View(vm);
        }
        var id = await _svc.ErstellenAsync(new DokumentErstellenDto(
            vm.Titel, vm.Kurzbeschreibung, vm.KapitelId, vm.VerantwortlicherBereichId,
            vm.Kategorie, vm.Tags, vm.SichtbarAb, vm.SichtbarBis, vm.Pruefterm,
            vm.InhaltHtml, vm.FreigabeModus,
            vm.OeffentlichLesbar, vm.Druckverbot,
            vm.VerlinkteDokumentIds), AktuellerBenutzerId);
        TempData["Erfolg"] = "Dokument angelegt.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Bearbeiten(int id)
    {
        if (!IstEditor) return Forbid();
        var d = await _svc.GetDetailAsync(id);
        if (d is null) return NotFound();
        await FuelleDropdowns(d.KapitelId);
        ViewBag.BestehendeVerlinkungen = d.Verlinkungen
            .Select(l => new { l.ZielDokumentId, l.ZielTitel }).ToList();

        return View(new DokumentBearbeitenViewModel
        {
            Id = d.Id,
            Titel = d.Titel,
            Kurzbeschreibung = d.Kurzbeschreibung,
            KapitelId = d.KapitelId,
            VerantwortlicherBereichId = d.VerantwortlicherBereichId,
            Kategorie = d.Kategorie,
            Tags = d.Tags,
            SichtbarAb = d.SichtbarAb,
            SichtbarBis = d.SichtbarBis,
            Pruefterm = d.Pruefterm,
            InhaltHtml = d.InhaltHtml,
            FreigabeModus = d.FreigabeModus,
            FreigabeReihenfolge = d.FreigabeReihenfolge,
            Druckverbot = d.Druckverbot,
            OeffentlichLesbar = d.OeffentlichLesbar,
            VerlinkteDokumentIds = d.Verlinkungen.Select(l => l.ZielDokumentId).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Bearbeiten(int id, DokumentBearbeitenViewModel vm)
    {
        if (!IstEditor) return Forbid();
        if (!ModelState.IsValid)
        {
            await FuelleDropdowns(vm.KapitelId);
            return View(vm);
        }
        await _svc.AktualisierenAsync(id, new DokumentBearbeitenDto(
            vm.Titel, vm.Kurzbeschreibung, vm.KapitelId, vm.VerantwortlicherBereichId,
            vm.Kategorie, vm.Tags, vm.SichtbarAb, vm.SichtbarBis, vm.Pruefterm,
            vm.InhaltHtml, vm.FreigabeModus, vm.FreigabeReihenfolge,
            vm.Druckverbot, vm.OeffentlichLesbar, vm.VerlinkteDokumentIds),
            AktuellerBenutzerId, vm.AenderungsHinweis);
        TempData["Erfolg"] = "Dokument gespeichert.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FreigabeStarten(int id)
    {
        if (!IstEditor) return Forbid();
        try { await _freigabe.FreigabeStartenAsync(id, AktuellerBenutzerId); TempData["Erfolg"] = "Freigabe-Workflow gestartet."; }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FreigabeZustimmen(int dokumentId, int freigabeGruppeId, string? kommentar)
    {
        try { await _freigabe.ZustimmenAsync(freigabeGruppeId, AktuellerBenutzerId, kommentar); TempData["Erfolg"] = "Zustimmung erfasst."; }
        catch (UnauthorizedAccessException) { TempData["Fehler"] = "Sie sind kein Mitglied dieser Freigabegruppe."; }
        return RedirectToAction(nameof(Details), new { id = dokumentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FreigabeAblehnen(int dokumentId, int freigabeGruppeId, string? kommentar)
    {
        try { await _freigabe.AblehnenAsync(freigabeGruppeId, AktuellerBenutzerId, kommentar); TempData["Erfolg"] = "Ablehnung erfasst."; }
        catch (UnauthorizedAccessException) { TempData["Fehler"] = "Sie sind kein Mitglied dieser Freigabegruppe."; }
        return RedirectToAction(nameof(Details), new { id = dokumentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KenntnisnahmeBestaetigen(int kenntnisnahmeId, int dokumentId)
    {
        await _kn.BestaetigenAsync(kenntnisnahmeId, AktuellerBenutzerId);
        TempData["Erfolg"] = "Kenntnisnahme bestätigt.";
        return RedirectToAction(nameof(Details), new { id = dokumentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Verschieben(int id, int zielKapitelId)
    {
        if (!IstEditor) return Forbid();
        try
        {
            await _svc.VerschiebenInKapitelAsync(id, zielKapitelId, AktuellerBenutzerId);
            return Ok();
        }
        catch (KeyNotFoundException) { return NotFound("Dokument nicht gefunden."); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet]
    public async Task<IActionResult> Drucken(int id)
    {
        var d = await _svc.GetDetailAsync(id);
        if (d is null) return NotFound();
        if (!await _svc.DarfLesenAsync(id, Kontext)) return NotFound();
        if (d.Druckverbot) return Forbid();

        if (!IstEditor && !IstApprover)
        {
            var jetzt = DateTime.UtcNow;
            var nichtSichtbar = (d.SichtbarAb.HasValue && d.SichtbarAb.Value > jetzt)
                             || (d.SichtbarBis.HasValue && d.SichtbarBis.Value < jetzt);
            if (nichtSichtbar) return NotFound();
        }

        return View(d);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SchnellAnlegen(string titel, int kapitelId, string? kurzbeschreibung, string? kategorie)
    {
        if (!IstEditor) return Forbid();
        if (string.IsNullOrWhiteSpace(titel))
            return BadRequest("Titel ist erforderlich.");
        if (kapitelId <= 0)
            return BadRequest("Kapitel ist erforderlich.");

        var id = await _svc.ErstellenAsync(new DokumentErstellenDto(
            titel.Trim(),
            string.IsNullOrWhiteSpace(kurzbeschreibung) ? null : kurzbeschreibung.Trim(),
            kapitelId, null,
            string.IsNullOrWhiteSpace(kategorie) ? null : kategorie.Trim(),
            null, null, null, null,
            null, FreigabeModus.Keine,
            true, false,
            new List<int>()), AktuellerBenutzerId);
        return Json(new { id, redirect = Url.Action(nameof(Bearbeiten), new { id }) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Autosave(int? id, DokumentBearbeitenViewModel vm)
    {
        if (!IstEditor) return Forbid();
        if (string.IsNullOrWhiteSpace(vm.Titel)) return BadRequest("Titel fehlt.");
        if (vm.KapitelId <= 0) return BadRequest("Kapitel fehlt.");

        if (id is null or 0)
        {
            var newId = await _svc.ErstellenAsync(new DokumentErstellenDto(
                vm.Titel, vm.Kurzbeschreibung, vm.KapitelId, vm.VerantwortlicherBereichId,
                vm.Kategorie, vm.Tags, vm.SichtbarAb, vm.SichtbarBis, vm.Pruefterm,
                vm.InhaltHtml, vm.FreigabeModus,
                vm.OeffentlichLesbar, vm.Druckverbot,
                vm.VerlinkteDokumentIds), AktuellerBenutzerId);
            return Json(new { id = newId, savedAt = DateTime.UtcNow, isNew = true });
        }
        else
        {
            await _svc.AutosaveAsync(id.Value, new DokumentBearbeitenDto(
                vm.Titel, vm.Kurzbeschreibung, vm.KapitelId, vm.VerantwortlicherBereichId,
                vm.Kategorie, vm.Tags, vm.SichtbarAb, vm.SichtbarBis, vm.Pruefterm,
                vm.InhaltHtml, vm.FreigabeModus, vm.FreigabeReihenfolge,
                vm.Druckverbot, vm.OeffentlichLesbar, vm.VerlinkteDokumentIds),
                AktuellerBenutzerId);
            return Json(new { id = id.Value, savedAt = DateTime.UtcNow, isNew = false });
        }
    }

    [HttpGet]
    public async Task<IActionResult> TagsVorschlaege(string? q)
    {
        var term = (q ?? string.Empty).Trim().ToLowerInvariant();
        var alle = await _db.Dokumente
            .Where(d => !d.Geloescht && d.Tags != null && d.Tags != "")
            .Select(d => d.Tags!)
            .ToListAsync();
        var unique = alle
            .SelectMany(t => t.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Select(t => t.ToLowerInvariant())
            .Where(t => t.Length > 0 && (term.Length == 0 || t.Contains(term)))
            .Distinct()
            .OrderBy(t => t)
            .Take(15)
            .ToList();
        return Json(unique);
    }

    [HttpGet]
    public async Task<IActionResult> Vorschlaege(string q, int? excludeId)
    {
        var term = q?.Trim() ?? "";
        if (term.Length < 2) return Json(Array.Empty<object>());
        var treffer = await _db.Dokumente
            .Include(d => d.Kapitel)
            .Where(d => !d.Geloescht && !d.Archiviert
                     && (excludeId == null || d.Id != excludeId)
                     && d.Titel.Contains(term))
            .OrderBy(d => d.Titel)
            .Take(10)
            .Select(d => new DokumentVorschlagDto(d.Id, d.Titel, d.Kapitel.Titel))
            .ToListAsync();
        return Json(treffer);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BildHochladen(IFormFile datei, int? dokumentId)
    {
        if (!IstEditor) return Forbid();
        if (datei is null || datei.Length == 0)
            return Json(new { error = "Keine Datei übertragen." });
        if (!datei.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Json(new { error = "Nur Bilddateien erlaubt." });
        if (datei.Length > 10 * 1024 * 1024)
            return Json(new { error = "Bild zu groß (max. 10 MB)." });

        var ordner = dokumentId.HasValue ? $"dok_{dokumentId}/inline" : "inline_temp";
        await using var stream = datei.OpenReadStream();
        var key = await _fileStorage.SpeichernAsync(stream, datei.FileName, ordner);
        return Json(new { url = $"/uploads/{key}" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AnhangHochladen(int dokumentId, IFormFile datei)
    {
        if (!IstEditor) return Forbid();
        if (datei is null || datei.Length == 0) { TempData["Fehler"] = "Bitte Datei wählen."; return RedirectToAction(nameof(Details), new { id = dokumentId }); }
        await using var stream = datei.OpenReadStream();
        await _anhang.HochladenAsync(dokumentId, stream, datei.FileName, datei.ContentType ?? "application/octet-stream", datei.Length, AktuellerBenutzerId);
        TempData["Erfolg"] = "Datei hochgeladen.";
        return RedirectToAction(nameof(Details), new { id = dokumentId });
    }

    [HttpGet]
    public async Task<IActionResult> AnhangHerunterladen(int anhangId)
    {
        try
        {
            var (inhalt, ct, name) = await _anhang.HerunterladenAsync(anhangId);
            return File(inhalt, ct, name);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (FileNotFoundException) { return NotFound(); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AnhangLoeschen(int anhangId, int dokumentId)
    {
        if (!IstEditor) return Forbid();
        await _anhang.LoeschenAsync(anhangId, AktuellerBenutzerId);
        TempData["Erfolg"] = "Anhang gelöscht.";
        return RedirectToAction(nameof(Details), new { id = dokumentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Archivieren(int id)
    {
        if (!IstApprover && !IstAdmin) return Forbid();
        await _svc.ArchivierenAsync(id, AktuellerBenutzerId);
        TempData["Erfolg"] = "Dokument archiviert.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Wiederherstellen(int id)
    {
        if (!IstApprover && !IstAdmin) return Forbid();
        await _svc.WiederherstellenAsync(id, AktuellerBenutzerId);
        TempData["Erfolg"] = "Dokument wiederhergestellt.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> InPapierkorb(int id)
    {
        if (!IstEditor) return Forbid();
        await _svc.InPapierkorbVerschiebenAsync(id, AktuellerBenutzerId);
        TempData["Erfolg"] = "Dokument in Papierkorb verschoben.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EndgueltigLoeschen(int id)
    {
        if (!IstAdmin) return Forbid();
        await _svc.EndgueltigLoeschenAsync(id, AktuellerBenutzerId);
        TempData["Erfolg"] = "Dokument endgültig gelöscht.";
        return RedirectToAction(nameof(Papierkorb));
    }

    [HttpPost]
    public async Task<IActionResult> PrueftermAendern(int id, [FromBody] PrueftermDto dto)
    {
        if (!IstEditor) return Json(new { error = "Keine Berechtigung." });

        var dok = await _db.Dokumente.FindAsync(id);
        if (dok == null) return Json(new { error = "Dokument nicht gefunden." });

        var alterWert = dok.Pruefterm?.ToString("dd.MM.yyyy") ?? "nicht gesetzt";
        var neuerWert = dto.NeuerPruefterm?.ToString("dd.MM.yyyy") ?? "entfernt";

        dok.Pruefterm = dto.NeuerPruefterm.HasValue
            ? DateTime.SpecifyKind(dto.NeuerPruefterm.Value, DateTimeKind.Utc)
            : null;
        await _db.SaveChangesAsync();

        var beschreibung = $"Pr\u00fcftermin ge\u00e4ndert: {alterWert} \u2192 {neuerWert}";
        if (!string.IsNullOrWhiteSpace(dto.Kommentar))
            beschreibung += $" \u2014 {dto.Kommentar}";

        await _audit.LogAsync(AuditTyp.PrueftermGeaendert, AktuellerBenutzerId,
            dokumentId: id, beschreibung: beschreibung);

        return Json(new { ok = true, neuerWert });
    }

    public record PrueftermDto(DateTime? NeuerPruefterm, string? Kommentar);

    private async Task FuelleDropdowns(int? aktKapitelId)
    {
        var kapitel = await _db.Kapitel
            .Include(k => k.ElternKapitel)
            .OrderBy(k => k.ElternKapitelId).ThenBy(k => k.Sortierung)
            .Select(k => new { k.Id, Pfad = (k.ElternKapitel != null ? k.ElternKapitel.Titel + " › " : "") + k.Titel })
            .ToListAsync();
        ViewBag.Kapitel = new SelectList(kapitel, "Id", "Pfad", aktKapitelId);

        var teams = await _db.Teams.Where(t => t.IstAktiv).OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name }).ToListAsync();
        ViewBag.Teams = new SelectList(teams, "Id", "Name");
    }
}
