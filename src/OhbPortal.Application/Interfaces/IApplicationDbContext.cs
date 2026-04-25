using Microsoft.EntityFrameworkCore;
using OhbPortal.Domain.Entities;

namespace OhbPortal.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Benutzer> Benutzer { get; }
    DbSet<Team> Teams { get; }
    DbSet<BenutzerTeam> BenutzerTeams { get; }
    DbSet<Kapitel> Kapitel { get; }
    DbSet<Dokument> Dokumente { get; }
    DbSet<DokumentVersion> DokumentVersionen { get; }
    DbSet<DokumentLink> DokumentLinks { get; }
    DbSet<Anhang> Anhaenge { get; }
    DbSet<FreigabeGruppe> FreigabeGruppen { get; }
    DbSet<FreigabeGruppeMitglied> FreigabeGruppeMitglieder { get; }
    DbSet<FreigabeZustimmung> FreigabeZustimmungen { get; }
    DbSet<Kenntnisnahme> Kenntnisnahmen { get; }
    DbSet<DokumentBerechtigung> Berechtigungen { get; }
    DbSet<AuditEintrag> AuditEintraege { get; }
    DbSet<KiFeedback> KiFeedbacks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
