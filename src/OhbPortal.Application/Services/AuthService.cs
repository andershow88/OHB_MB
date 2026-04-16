using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OhbPortal.Application.Interfaces;
using OhbPortal.Domain.Enums;

namespace OhbPortal.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _db;

    public AuthService(IApplicationDbContext db) => _db = db;

    public async Task<LoginErgebnis> AnmeldenAsync(string benutzername, string passwort)
    {
        var hash = HashPasswort(passwort);
        var user = await _db.Benutzer
            .FirstOrDefaultAsync(b => b.Benutzername == benutzername
                                   && b.PasswortHash == hash
                                   && b.IstAktiv);
        if (user is null)
            return new LoginErgebnis(false, 0, string.Empty, string.Empty, Rolle.Reader);

        return new LoginErgebnis(true, user.Id, user.Benutzername, user.Anzeigename, user.Rolle);
    }

    public string HashPasswort(string passwort)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(passwort));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
