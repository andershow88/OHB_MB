using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OhbPortal.Application.DTOs;
using OhbPortal.Application.Interfaces;

namespace OhbPortal.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin) => _admin = admin;

    // ── Dashboard ────────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var dto = await _admin.GetDashboardAsync();
        return View(dto);
    }

    // ── Benutzer ─────────────────────────────────────────────────────────────

    public async Task<IActionResult> Benutzer()
    {
        var liste = await _admin.GetBenutzerAsync();
        return View(liste);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BenutzerAnlegen(BenutzerAnlegenEingabe model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Fehler"] = string.Join(" · ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Benutzer));
        }
        try
        {
            await _admin.BenutzerAnlegenAsync(model, AktuellerBenutzerId);
            TempData["Erfolg"] = $"Benutzer '{model.Benutzername}' angelegt.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return RedirectToAction(nameof(Benutzer));
    }

    [HttpGet]
    public async Task<IActionResult> BenutzerBearbeiten(int id)
    {
        var b = await _admin.GetBenutzerDetailAsync(id);
        if (b is null) return NotFound();
        return View(b);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BenutzerBearbeiten(int id, BenutzerBearbeitenEingabe model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Fehler"] = "Bitte Eingaben prüfen.";
            return RedirectToAction(nameof(BenutzerBearbeiten), new { id });
        }
        try
        {
            await _admin.BenutzerAktualisierenAsync(id, model, AktuellerBenutzerId);
            TempData["Erfolg"] = "Benutzer aktualisiert.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return RedirectToAction(nameof(BenutzerBearbeiten), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BenutzerAktivUmschalten(int id, bool aktiv)
    {
        try
        {
            await _admin.BenutzerAktivitaetUmschaltenAsync(id, aktiv, AktuellerBenutzerId);
            TempData["Erfolg"] = aktiv ? "Benutzer aktiviert." : "Benutzer deaktiviert.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return RedirectToAction(nameof(BenutzerBearbeiten), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PasswortZuruecksetzen(int id, string neuesPasswort)
    {
        try
        {
            await _admin.PasswortZuruecksetzenAsync(id, neuesPasswort, AktuellerBenutzerId);
            TempData["Erfolg"] = "Passwort zurückgesetzt.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return RedirectToAction(nameof(BenutzerBearbeiten), new { id });
    }

    // ── Teams ────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Teams()
    {
        var liste = await _admin.GetTeamsAsync();
        return View(liste);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TeamAnlegen(TeamEingabe model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Fehler"] = "Teamname fehlt.";
            return RedirectToAction(nameof(Teams));
        }
        try
        {
            var id = await _admin.TeamAnlegenAsync(model, AktuellerBenutzerId);
            TempData["Erfolg"] = $"Team '{model.Name}' angelegt.";
            return RedirectToAction(nameof(TeamBearbeiten), new { id });
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return RedirectToAction(nameof(Teams));
    }

    [HttpGet]
    public async Task<IActionResult> TeamBearbeiten(int id)
    {
        var t = await _admin.GetTeamDetailAsync(id);
        if (t is null) return NotFound();
        ViewBag.AlleBenutzer = await _admin.GetBenutzerAsync();
        return View(t);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TeamBearbeiten(int id, TeamEingabe model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Fehler"] = "Bitte Eingaben prüfen.";
            return RedirectToAction(nameof(TeamBearbeiten), new { id });
        }
        try
        {
            await _admin.TeamAktualisierenAsync(id, model, AktuellerBenutzerId);
            TempData["Erfolg"] = "Team aktualisiert.";
        }
        catch (InvalidOperationException ex) { TempData["Fehler"] = ex.Message; }
        return RedirectToAction(nameof(TeamBearbeiten), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TeamLoeschen(int id)
    {
        try
        {
            await _admin.TeamLoeschenAsync(id, AktuellerBenutzerId);
            TempData["Erfolg"] = "Team gelöscht.";
            return RedirectToAction(nameof(Teams));
        }
        catch (InvalidOperationException ex)
        {
            TempData["Fehler"] = ex.Message;
            return RedirectToAction(nameof(TeamBearbeiten), new { id });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TeamMitgliedHinzufuegen(int teamId, int benutzerId)
    {
        await _admin.TeamMitgliedHinzufuegenAsync(teamId, benutzerId, AktuellerBenutzerId);
        TempData["Erfolg"] = "Mitglied hinzugefügt.";
        return RedirectToAction(nameof(TeamBearbeiten), new { id = teamId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TeamMitgliedEntfernen(int teamId, int benutzerId)
    {
        await _admin.TeamMitgliedEntfernenAsync(teamId, benutzerId, AktuellerBenutzerId);
        TempData["Erfolg"] = "Mitglied entfernt.";
        return RedirectToAction(nameof(TeamBearbeiten), new { id = teamId });
    }
}
