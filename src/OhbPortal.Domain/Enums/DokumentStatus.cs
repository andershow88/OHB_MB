namespace OhbPortal.Domain.Enums;

public enum DokumentStatus
{
    Entwurf = 0,
    InFreigabe = 1,
    Freigegeben = 2,
    Abgelehnt = 3,
    Archiviert = 4
}

public enum FreigabeModus
{
    Keine = 0,
    VierAugen = 1,
    Gruppen = 2
}

public enum FreigabeReihenfolge
{
    Parallel = 0,
    Sequentiell = 1
}

public enum FreigabeEntscheidung
{
    Ausstehend = 0,
    Zugestimmt = 1,
    Abgelehnt = 2
}

public enum KenntnisnahmeStatus
{
    Offen = 0,
    Bestaetigt = 1,
    Ueberfaellig = 2
}

public enum Rolle
{
    Reader = 0,
    Reviewer = 1,
    Editor = 2,
    Approver = 3,
    Bereichsverantwortlicher = 4,
    Admin = 5
}

public enum BerechtigungsTyp
{
    Lesen = 0,
    Bearbeiten = 1,
    Verwalten = 2
}

public enum AuditTyp
{
    DokumentErstellt = 0,
    DokumentBearbeitet = 1,
    DokumentGeloescht = 2,
    DokumentArchiviert = 3,
    DokumentWiederhergestellt = 4,
    VersionAngelegt = 5,
    FreigabeGestartet = 10,
    FreigabeZugestimmt = 11,
    FreigabeAbgelehnt = 12,
    FreigabeAbgeschlossen = 13,
    KenntnisnahmeZugewiesen = 20,
    KenntnisnahmeBestaetigt = 21,
    AnhangHochgeladen = 30,
    AnhangGeloescht = 31,
    BerechtigungGeaendert = 40,
    PrueftermGeaendert = 45,
    KapitelErstellt = 50,
    KapitelGeaendert = 51,
    KapitelGeloescht = 52
}
