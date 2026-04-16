using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Web.Controllers;

public abstract class BaseController : Controller
{
    protected int AktuellerBenutzerId =>
        int.TryParse(User.FindFirst("BenutzerId")?.Value, out var id) ? id : 0;

    protected Rolle AktuelleRolle =>
        Enum.TryParse<Rolle>(User.FindFirst(ClaimTypes.Role)?.Value, out var r) ? r : Rolle.Reader;

    protected bool IstEditor =>
        AktuelleRolle is Rolle.Editor or Rolle.Admin or Rolle.Bereichsverantwortlicher;

    protected bool IstApprover =>
        AktuelleRolle is Rolle.Approver or Rolle.Admin;

    protected bool IstAdmin =>
        AktuelleRolle == Rolle.Admin;
}
