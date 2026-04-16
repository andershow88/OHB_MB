using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OhbPortal.Application.Interfaces;

namespace OhbPortal.Web.Controllers;

[Authorize]
public class FreigabenController : BaseController
{
    private readonly IFreigabeService _svc;
    public FreigabenController(IFreigabeService svc) => _svc = svc;

    public async Task<IActionResult> Meine()
    {
        var liste = await _svc.GetMeineOffenenAsync(AktuellerBenutzerId);
        return View(liste);
    }
}

[Authorize]
public class KenntnisnahmenController : BaseController
{
    private readonly IKenntnisnahmeService _svc;
    public KenntnisnahmenController(IKenntnisnahmeService svc) => _svc = svc;

    public async Task<IActionResult> Meine()
    {
        var liste = await _svc.GetMeineOffenenAsync(AktuellerBenutzerId);
        return View(liste);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ZuweisenBenutzer(int dokumentId, int benutzerId, DateTime? faelligkeit)
    {
        if (!IstEditor) return Forbid();
        try
        {
            await _svc.ZuweisenBenutzerAsync(dokumentId, benutzerId, faelligkeit, AktuellerBenutzerId);
            TempData["Erfolg"] = "Kenntnisnahme an Benutzer zugewiesen.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return Redirect($"/Dokumente/Details/{dokumentId}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ZuweisenTeam(int dokumentId, int teamId, DateTime? faelligkeit)
    {
        if (!IstEditor) return Forbid();
        try
        {
            await _svc.ZuweisenTeamAsync(dokumentId, teamId, faelligkeit, AktuellerBenutzerId);
            TempData["Erfolg"] = "Kenntnisnahme an Team zugewiesen.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return Redirect($"/Dokumente/Details/{dokumentId}");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Loeschen(int kenntnisnahmeId, int dokumentId)
    {
        if (!IstEditor) return Forbid();
        await _svc.LoeschenAsync(kenntnisnahmeId, AktuellerBenutzerId);
        TempData["Erfolg"] = "Kenntnisnahme entfernt.";
        return Redirect($"/Dokumente/Details/{dokumentId}");
    }
}
