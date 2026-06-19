namespace StuffInABox.Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> StoreAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    string GetUrl(string storageKey);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
