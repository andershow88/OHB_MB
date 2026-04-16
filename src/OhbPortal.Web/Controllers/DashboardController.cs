using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OhbPortal.Application.DTOs;
using OhbPortal.Application.Interfaces;

namespace OhbPortal.Web.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardService _svc;
    public DashboardController(IDashboardService svc) => _svc = svc;

    public async Task<IActionResult> Index()
    {
        var kontext = new BerechtigungsKontext(AktuellerBenutzerId, AktuelleRolle);
        var dto = await _svc.GetAsync(AktuellerBenutzerId,
            nurAktuellSichtbare: !IstEditor && !IstApprover,
            kontext);
        return View(dto);
    }
}
