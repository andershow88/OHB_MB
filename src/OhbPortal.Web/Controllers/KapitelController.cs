using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OhbPortal.Application.Interfaces;

namespace OhbPortal.Web.Controllers;

[Authorize]
public class KapitelController : BaseController
{
    private readonly IKapitelService _svc;
    public KapitelController(IKapitelService svc) => _svc = svc;

    public async Task<IActionResult> Index()
    {
        var baum = await _svc.GetBaumAsync();
        return View(baum);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Anlegen(string titel, int? elternId, string? beschreibung, string? icon)
    {
        if (!IstEditor) return Forbid();
        if (string.IsNullOrWhiteSpace(titel))
        {
            TempData["Fehler"] = "Bitte Titel eingeben.";
            return RedirectToAction(nameof(Index));
        }
        await _svc.AnlegenAsync(titel.Trim(), elternId, beschreibung, icon, AktuellerBenutzerId);
        TempData["Erfolg"] = "Kapitel angelegt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Bearbeiten(int id, string titel, string? beschreibung, string? icon)
    {
        if (!IstEditor) return Forbid();
        if (string.IsNullOrWhiteSpace(titel))
        {
            TempData["Fehler"] = "Titel darf nicht leer sein.";
            return RedirectToAction(nameof(Index));
        }
        try
        {
            await _svc.AktualisierenAsync(id, titel.Trim(), beschreibung, icon, AktuellerBenutzerId);
            TempData["Erfolg"] = "Kapitel aktualisiert.";
        }
        catch (KeyNotFoundException) { TempData["Fehler"] = "Kapitel nicht gefunden."; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NachOben(int id)
    {
        if (!IstEditor) return Forbid();
        await _svc.NachObenVerschiebenAsync(id, AktuellerBenutzerId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NachUnten(int id)
    {
        if (!IstEditor) return Forbid();
        await _svc.NachUntenVerschiebenAsync(id, AktuellerBenutzerId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Verschieben(int id, int zielId, string position)
    {
        if (!IstEditor) return Forbid();
        try
        {
            await _svc.VerschiebenAsync(id, zielId, position, AktuellerBenutzerId);
            return Ok();
        }
        catch (KeyNotFoundException) { return NotFound("Kapitel nicht gefunden."); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Loeschen(int id)
    {
        if (!IstAdmin) return Forbid();
        try
        {
            await _svc.LoeschenAsync(id, AktuellerBenutzerId);
            TempData["Erfolg"] = "Kapitel gelöscht.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
