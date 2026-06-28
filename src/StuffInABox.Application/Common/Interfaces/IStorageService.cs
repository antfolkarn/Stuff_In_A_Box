namespace StuffInABox.Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> StoreAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    string GetUrl(string storageKey);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);

    /// <summary>Reads a stored object's bytes back, or null if it no longer exists.
    /// Used by background workers (e.g. photo recognition) that need the image after upload.</summary>
    Task<byte[]?> GetAsync(string storageKey, CancellationToken ct = default);
}
