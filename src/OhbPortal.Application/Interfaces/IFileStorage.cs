namespace OhbPortal.Application.Interfaces;

/// <summary>
/// Abstraktion für Datei-Storage. Aktuelle Implementierung schreibt lokal in wwwroot/uploads/;
/// austauschbar gegen S3, Azure Blob Storage etc.
/// </summary>
public interface IFileStorage
{
    Task<string> SpeichernAsync(Stream inhalt, string dateiname, string subordner, CancellationToken ct = default);
    Task<Stream> LadenAsync(string speicherSchluessel, CancellationToken ct = default);
    Task LoeschenAsync(string speicherSchluessel, CancellationToken ct = default);
    bool Existiert(string speicherSchluessel);
}
