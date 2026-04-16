using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OhbPortal.Application.Interfaces;

namespace OhbPortal.Web.Controllers;

[Authorize]
public class FreigabeGruppenController : BaseController
{
    private readonly IFreigabeService _freigabe;

    public FreigabeGruppenController(IFreigabeService freigabe) => _freigabe = freigabe;

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Anlegen(int dokumentId, string bezeichnung, int reihenfolge, int benoetigteZustimmungen)
    {
        if (!IstEditor) return Forbid();
        if (string.IsNullOrWhiteSpace(bezeichnung))
        {
            TempData["Fehler"] = "Bitte Bezeichnung angeben.";
            return Redirect($"/Dokumente/Details/{dokumentId}");
        }
        try
        {
            await _freigabe.GruppeAnlegenAsync(dokumentId, bezeichnung, reihenfolge, benoetigteZustimmungen, AktuellerBenutzerId);
            TempData["Erfolg"] = "Freigabegruppe angelegt.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return Redirect($"/Dokumente/Details/{dokumentId}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Bearbeiten(int gruppeId, int dokumentId, string bezeichnung, int reihenfolge, int benoetigteZustimmungen)
    {
        if (!IstEditor) return Forbid();
        try
        {
            await _freigabe.GruppeBearbeitenAsync(gruppeId, bezeichnung, reihenfolge, benoetigteZustimmungen, AktuellerBenutzerId);
            TempData["Erfolg"] = "Freigabegruppe aktualisiert.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return Redirect($"/Dokumente/Details/{dokumentId}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Loeschen(int gruppeId, int dokumentId)
    {
        if (!IstEditor) return Forbid();
        try
        {
            await _freigabe.GruppeLoeschenAsync(gruppeId, AktuellerBenutzerId);
            TempData["Erfolg"] = "Freigabegruppe gelöscht.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return Redirect($"/Dokumente/Details/{dokumentId}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MitgliedHinzufuegen(int gruppeId, int benutzerId, int dokumentId)
    {
        if (!IstEditor) return Forbid();
        try
        {
            await _freigabe.MitgliedHinzufuegenAsync(gruppeId, benutzerId, AktuellerBenutzerId);
            TempData["Erfolg"] = "Mitglied hinzugefügt.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return Redirect($"/Dokumente/Details/{dokumentId}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MitgliedEntfernen(int mitgliedId, int dokumentId)
    {
        if (!IstEditor) return Forbid();
        try
        {
            await _freigabe.MitgliedEntfernenAsync(mitgliedId, AktuellerBenutzerId);
            TempData["Erfolg"] = "Mitglied entfernt.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return Redirect($"/Dokumente/Details/{dokumentId}");
    }
}
