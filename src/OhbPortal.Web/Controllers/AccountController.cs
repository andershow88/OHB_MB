using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OhbPortal.Application.Interfaces;
using OhbPortal.Web.ViewModels;

namespace OhbPortal.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _auth;

    public AccountController(IAuthService auth) => _auth = auth;

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _auth.AnmeldenAsync(model.Benutzername, model.Passwort);
        if (!result.Erfolg)
        {
            ModelState.AddModelError(string.Empty, "Benutzername oder Passwort ist falsch.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new("BenutzerId", result.BenutzerId.ToString()),
            new(ClaimTypes.Name, result.Benutzername),
            new(ClaimTypes.GivenName, result.Anzeigename),
            new(ClaimTypes.Role, result.Rolle.ToString())
        };
        var identity = new ClaimsIdentity(claims, "OhbAuth");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("OhbAuth", principal,
            new AuthenticationProperties { IsPersistent = model.MerkenAuf });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("OhbAuth");
        return RedirectToAction("Login");
    }
}
