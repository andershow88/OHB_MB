using Microsoft.Extensions.Hosting;
using OhbPortal.Application.Interfaces;

namespace OhbPortal.Infrastructure.Storage;

/// <summary>
/// Lokales Datei-Storage unter wwwroot/uploads/{subordner}/{guid}_{dateiname}.
/// Für Production austauschbar gegen S3/Azure Blob via IFileStorage-Implementierung.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IHostEnvironment env)
    {
        _rootPath = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SpeichernAsync(Stream inhalt, string dateiname, string subordner, CancellationToken ct = default)
    {
        var safeOrdner = Sanitize(subordner);
        var safeName = Sanitize(dateiname);
        var ordnerPfad = Path.Combine(_rootPath, safeOrdner);
        Directory.CreateDirectory(ordnerPfad);

        var endgueltigName = $"{Guid.NewGuid():N}_{safeName}";
        var vollerPfad = Path.Combine(ordnerPfad, endgueltigName);

        await using var datei = File.Create(vollerPfad);
        await inhalt.CopyToAsync(datei, ct);

        return Path.Combine(safeOrdner, endgueltigName).Replace('\\', '/');
    }

    public Task<Stream> LadenAsync(string speicherSchluessel, CancellationToken ct = default)
    {
        var pfad = Path.Combine(_rootPath, speicherSchluessel.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(pfad)) throw new FileNotFoundException(pfad);
        Stream s = File.OpenRead(pfad);
        return Task.FromResult(s);
    }

    public Task LoeschenAsync(string speicherSchluessel, CancellationToken ct = default)
    {
        var pfad = Path.Combine(_rootPath, speicherSchluessel.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(pfad)) File.Delete(pfad);
        return Task.CompletedTask;
    }

    public bool Existiert(string speicherSchluessel)
    {
        var pfad = Path.Combine(_rootPath, speicherSchluessel.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(pfad);
    }

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars().Concat(new[] { '/', '\\' }))
            name = name.Replace(c, '_');
        return name.Length > 180 ? name[..180] : name;
    }
}
