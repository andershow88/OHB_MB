using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Entities;

namespace OhbPortal.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opt) : base(opt) { }

    public DbSet<Benutzer> Benutzer => Set<Benutzer>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<BenutzerTeam> BenutzerTeams => Set<BenutzerTeam>();
    public DbSet<Kapitel> Kapitel => Set<Kapitel>();
    public DbSet<Dokument> Dokumente => Set<Dokument>();
    public DbSet<DokumentVersion> DokumentVersionen => Set<DokumentVersion>();
    public DbSet<DokumentLink> DokumentLinks => Set<DokumentLink>();
    public DbSet<Anhang> Anhaenge => Set<Anhang>();
    public DbSet<FreigabeGruppe> FreigabeGruppen => Set<FreigabeGruppe>();
    public DbSet<FreigabeGruppeMitglied> FreigabeGruppeMitglieder => Set<FreigabeGruppeMitglied>();
    public DbSet<FreigabeZustimmung> FreigabeZustimmungen => Set<FreigabeZustimmung>();
    public DbSet<Kenntnisnahme> Kenntnisnahmen => Set<Kenntnisnahme>();
    public DbSet<DokumentBerechtigung> Berechtigungen => Set<DokumentBerechtigung>();
    public DbSet<AuditEintrag> AuditEintraege => Set<AuditEintrag>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<Benutzer>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => b.Benutzername).IsUnique();
            e.Property(b => b.Benutzername).HasMaxLength(100).IsRequired();
            e.Property(b => b.Anzeigename).HasMaxLength(200);
            e.Property(b => b.EMail).HasMaxLength(200);
        });

        mb.Entity<Team>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(150).IsRequired();
        });

        mb.Entity<BenutzerTeam>(e =>
        {
            e.HasKey(bt => new { bt.BenutzerId, bt.TeamId });
            e.HasOne(bt => bt.Benutzer).WithMany(b => b.Teams).HasForeignKey(bt => bt.BenutzerId);
            e.HasOne(bt => bt.Team).WithMany(t => t.Mitglieder).HasForeignKey(bt => bt.TeamId);
        });

        mb.Entity<Kapitel>(e =>
        {
            e.HasKey(k => k.Id);
            e.Property(k => k.Titel).HasMaxLength(200).IsRequired();
            e.HasOne(k => k.ElternKapitel)
                .WithMany(k => k.Unterkapitel)
                .HasForeignKey(k => k.ElternKapitelId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(k => k.ElternKapitelId);
        });

        mb.Entity<Dokument>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Titel).HasMaxLength(300).IsRequired();
            e.HasOne(d => d.Kapitel).WithMany(k => k.Dokumente).HasForeignKey(d => d.KapitelId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.VerantwortlicherBereich).WithMany().HasForeignKey(d => d.VerantwortlicherBereichId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(d => d.ErstelltVon).WithMany().HasForeignKey(d => d.ErstelltVonId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(d => d.GeaendertVon).WithMany().HasForeignKey(d => d.GeaendertVonId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(d => d.KapitelId);
            e.HasIndex(d => d.Status);
            e.HasIndex(d => d.Geloescht);
        });

        mb.Entity<DokumentVersion>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasOne(v => v.Dokument).WithMany(d => d.Versionen).HasForeignKey(v => v.DokumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(v => v.ErstelltVon).WithMany().HasForeignKey(v => v.ErstelltVonId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(v => new { v.DokumentId, v.Versionsnummer });
        });

        mb.Entity<DokumentLink>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasOne(l => l.Dokument).WithMany(d => d.Verlinkungen).HasForeignKey(l => l.DokumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.ZielDokument).WithMany().HasForeignKey(l => l.ZielDokumentId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Anhang>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Dateiname).HasMaxLength(400).IsRequired();
            e.Property(a => a.SpeicherSchluessel).HasMaxLength(600).IsRequired();
            e.Property(a => a.ContentType).HasMaxLength(150);
            e.HasOne(a => a.Dokument).WithMany(d => d.Anhaenge).HasForeignKey(a => a.DokumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.HochgeladenVon).WithMany().HasForeignKey(a => a.HochgeladenVonId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<FreigabeGruppe>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Bezeichnung).HasMaxLength(200).IsRequired();
            e.HasOne(g => g.Dokument).WithMany(d => d.FreigabeGruppen).HasForeignKey(g => g.DokumentId).OnDelete(DeleteBehavior.Cascade);
        });
        mb.Entity<FreigabeGruppeMitglied>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasOne(m => m.FreigabeGruppe).WithMany(g => g.Mitglieder).HasForeignKey(m => m.FreigabeGruppeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Benutzer).WithMany().HasForeignKey(m => m.BenutzerId).OnDelete(DeleteBehavior.Restrict);
        });
        mb.Entity<FreigabeZustimmung>(e =>
        {
            e.HasKey(z => z.Id);
            e.HasOne(z => z.FreigabeGruppe).WithMany(g => g.Zustimmungen).HasForeignKey(z => z.FreigabeGruppeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(z => z.Benutzer).WithMany().HasForeignKey(z => z.BenutzerId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(z => new { z.FreigabeGruppeId, z.BenutzerId }).IsUnique();
        });

        mb.Entity<Kenntnisnahme>(e =>
        {
            e.HasKey(k => k.Id);
            e.HasOne(k => k.Dokument).WithMany(d => d.Kenntnisnahmen).HasForeignKey(k => k.DokumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(k => k.Benutzer).WithMany().HasForeignKey(k => k.BenutzerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(k => k.Team).WithMany().HasForeignKey(k => k.TeamId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(k => k.BestaetigtVon).WithMany().HasForeignKey(k => k.BestaetigtVonId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<DokumentBerechtigung>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasOne(b => b.Dokument).WithMany(d => d.Berechtigungen).HasForeignKey(b => b.DokumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.Benutzer).WithMany().HasForeignKey(b => b.BenutzerId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(b => b.Team).WithMany().HasForeignKey(b => b.TeamId).OnDelete(DeleteBehavior.SetNull);
        });

        mb.Entity<AuditEintrag>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasOne(a => a.Benutzer).WithMany().HasForeignKey(a => a.BenutzerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Dokument).WithMany(d => d.AuditEintraege).HasForeignKey(a => a.DokumentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Kapitel).WithMany().HasForeignKey(a => a.KapitelId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(a => a.Zeitpunkt);
        });
    }
}
