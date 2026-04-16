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
}
