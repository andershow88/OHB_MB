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
