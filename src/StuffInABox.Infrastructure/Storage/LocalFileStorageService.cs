using Microsoft.Extensions.Configuration;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Storage;

public class LocalFileStorageService(IConfiguration config, IPhotoUrlSigner signer) : IStorageService
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private string UploadsRoot
    {
        get
        {
            var configured = config["Storage:LocalPath"];
            // Default outside wwwroot so SPA rebuilds don't wipe uploads.
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(Directory.GetCurrentDirectory(), "uploads")
                : configured;
        }
    }

    public async Task<string> StoreAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        var storageKey = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(UploadsRoot, storageKey);
        Directory.CreateDirectory(UploadsRoot);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
        return storageKey;
    }

    // Signed so the photo can't be fetched from /uploads without a valid, expiring token.
    public string GetUrl(string storageKey) => $"/uploads/{storageKey}?sig={signer.Sign(storageKey)}";

    public async Task<byte[]?> GetAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(UploadsRoot, storageKey);
        if (!File.Exists(fullPath)) return null;
        return await File.ReadAllBytesAsync(fullPath, ct);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(UploadsRoot, storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
