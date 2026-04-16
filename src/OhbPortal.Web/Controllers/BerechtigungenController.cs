using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Web.Controllers;

[Authorize]
public class BerechtigungenController : BaseController
{
    private readonly IBerechtigungService _svc;
    public BerechtigungenController(IBerechtigungService svc) => _svc = svc;

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Hinzufuegen(int dokumentId, string zielTyp,
        int? benutzerId, int? teamId, Rolle? rolle, BerechtigungsTyp typ)
    {
        if (!IstEditor) return Forbid();
        try
        {
            int? bId = zielTyp == "benutzer" ? benutzerId : null;
            int? tId = zielTyp == "team" ? teamId : null;
            Rolle? r = zielTyp == "rolle" ? rolle : null;
            await _svc.HinzufuegenAsync(dokumentId, bId, tId, r, typ, AktuellerBenutzerId);
            TempData["Erfolg"] = "Berechtigung hinzugefügt.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return Redirect($"/Dokumente/Details/{dokumentId}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Entfernen(int id, int dokumentId)
    {
        if (!IstEditor) return Forbid();
        await _svc.EntfernenAsync(id, AktuellerBenutzerId);
        TempData["Erfolg"] = "Berechtigung entfernt.";
        return Redirect($"/Dokumente/Details/{dokumentId}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TypAendern(int id, int dokumentId, BerechtigungsTyp typ)
    {
        if (!IstEditor) return Forbid();
        await _svc.TypAendernAsync(id, typ, AktuellerBenutzerId);
        TempData["Erfolg"] = "Berechtigungstyp geändert.";
        return Redirect($"/Dokumente/Details/{dokumentId}");
    }
}
