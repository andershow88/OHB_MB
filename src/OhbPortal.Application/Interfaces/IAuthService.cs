using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Interfaces;

public interface IAuthService
{
    Task<LoginErgebnis> AnmeldenAsync(string benutzername, string passwort);
    string HashPasswort(string passwort);
}

public record LoginErgebnis(
    bool Erfolg,
    int BenutzerId,
    string Benutzername,
    string Anzeigename,
    Rolle Rolle);
