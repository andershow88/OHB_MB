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
    private readonly IApplicationDbContext _db;

    public DokumenteController(
        IDokumentService svc,
        IFreigabeService freigabe,
        IKenntnisnahmeService kn,
        IAnhangService anhang,
        IApplicationDbContext db)
    {
        _svc = svc;
        _freigabe = freigabe;
        _kn = kn;
        _anhang = anhang;
        _db = db;
    }

    public async Task<IActionResult> Index(string? q, int? kapitelId, DokumentStatus? status,
        string? kategorie, bool? pruefUeberfaellig)
    {
        var filter = new DokumentFilterDto(q, kapitelId, status, kategorie, pruefUeberfaellig);
        ViewBag.Filter = filter;
        await FuelleDropdowns(kapitelId);
        var liste = await _svc.GetAlleAsync(filter);
        return View(liste);
    }

    public async Task<IActionResult> Archiv()
    {
        ViewBag.SeitenTitel = "Archiv";
        ViewBag.SeitenIcon = "bi-archive";
        var liste = await _svc.GetAlleAsync(new DokumentFilterDto(IncludeArchiviert: true));
        return View("Index", liste.Where(d => d.Archiviert).ToList());
    }

    public async Task<IActionResult> Papierkorb()
    {
        ViewBag.SeitenTitel = "Papierkorb";
        ViewBag.SeitenIcon = "bi-trash";
        var liste = await _svc.GetAlleAsync(new DokumentFilterDto(NurGeloescht: true));
        return View("Index", liste);
    }

    public async Task<IActionResult> Details(int id)
    {
        var d = await _svc.GetDetailAsync(id);
        if (d is null) return NotFound();
        ViewBag.Versionen = (await _svc.GetVersionenAsync(id)).ToList();
        ViewBag.Audit = (await _svc.GetAuditAsync(id)).ToList();
        ViewBag.Freigaben = (await _freigabe.GetGruppenAsync(id)).ToList();
        ViewBag.Kenntnisnahmen = (await _kn.GetProDokumentAsync(id)).ToList();
        ViewBag.AlleBenutzer = await _db.Benutzer
            .Where(b => b.IstAktiv)
            .OrderBy(b => b.Anzeigename)
            .Select(b => new { b.Id, b.Anzeigename })
            .ToListAsync();
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
            vm.InhaltHtml, vm.FreigabeModus), AktuellerBenutzerId);
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
            OeffentlichLesbar = d.OeffentlichLesbar
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
            vm.Druckverbot, vm.OeffentlichLesbar),
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
