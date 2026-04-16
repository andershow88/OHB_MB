using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.DTOs;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _db;
    private readonly IFreigabeService _freigaben;
    private readonly IKenntnisnahmeService _kenntnisnahmen;
    private readonly IDokumentService _dokumente;

    public DashboardService(
        IApplicationDbContext db,
        IFreigabeService freigaben,
        IKenntnisnahmeService kenntnisnahmen,
        IDokumentService dokumente)
    {
        _db = db;
        _freigaben = freigaben;
        _kenntnisnahmen = kenntnisnahmen;
        _dokumente = dokumente;
    }

    public Task<DashboardDto> GetAsync(int benutzerId)
        => GetAsync(benutzerId, nurAktuellSichtbare: false, new BerechtigungsKontext(benutzerId, Rolle.Admin));

    public async Task<DashboardDto> GetAsync(int benutzerId, bool nurAktuellSichtbare, BerechtigungsKontext kontext)
    {
        var total = await _db.Dokumente.CountAsync(d => !d.Geloescht);
        var entw = await _db.Dokumente.CountAsync(d => !d.Geloescht && d.Status == DokumentStatus.Entwurf);
        var frei = await _db.Dokumente.CountAsync(d => !d.Geloescht && d.Status == DokumentStatus.Freigegeben);
        var inFrei = await _db.Dokumente.CountAsync(d => !d.Geloescht && d.Status == DokumentStatus.InFreigabe);
        var ueberfaellig = await _db.Dokumente.CountAsync(d => !d.Geloescht && d.Pruefterm.HasValue && d.Pruefterm < DateTime.UtcNow);

        var offeneFreigaben = (await _freigaben.GetMeineOffenenAsync(benutzerId)).Count();
        var offeneKenntnisnahmen = (await _kenntnisnahmen.GetMeineOffenenAsync(benutzerId)).Count();

        var letzte = (await _dokumente.GetAlleAsync(
            new DokumentFilterDto(NurAktuellSichtbare: nurAktuellSichtbare), kontext)).Take(10).ToList();

        return new DashboardDto(total, entw, frei, inFrei, offeneFreigaben, offeneKenntnisnahmen,
            ueberfaellig, letzte);
    }
}
