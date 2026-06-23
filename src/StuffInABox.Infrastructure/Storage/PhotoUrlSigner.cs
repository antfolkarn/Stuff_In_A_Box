using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Storage;

/// <summary>
/// HMAC-SHA256 signer for photo URLs. The expiry is bucketed to a window boundary so
/// the same key produces a stable URL within the window — this lets the browser cache
/// the image instead of re-downloading it on every list refresh, while still expiring.
/// </summary>
public sealed class PhotoUrlSigner(IConfiguration config) : IPhotoUrlSigner
{
    private long ValiditySeconds =>
        (long)TimeSpan.FromMinutes(config.GetValue<int?>("Storage:UrlValidityMinutes") ?? 360).TotalSeconds;

    private byte[] Key
    {
        get
        {
            // Dedicated key if set, otherwise reuse the JWT secret (already required, ≥32 chars).
            var secret = config["Storage:UrlSigningKey"]
                ?? config["Jwt:Secret"]
                ?? throw new InvalidOperationException("Storage:UrlSigningKey or Jwt:Secret must be configured.");
            return Encoding.UTF8.GetBytes(secret);
        }
    }

    public string Sign(string storageKey)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // Bucket up to a window boundary; "+2" guarantees at least one full window of validity.
        var expiry = ((now / ValiditySeconds) + 2) * ValiditySeconds;
        return $"{expiry}.{Mac(storageKey, expiry)}";
    }

    public bool Verify(string storageKey, string? signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        var dot = signature.IndexOf('.');
        if (dot <= 0) return false;
        if (!long.TryParse(signature[..dot], out var expiry)) return false;
        if (DateTimeOffset.FromUnixTimeSeconds(expiry) < DateTimeOffset.UtcNow) return false;

        var expected = Encoding.UTF8.GetBytes(Mac(storageKey, expiry));
        var provided = Encoding.UTF8.GetBytes(signature[(dot + 1)..]);
        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }

    private string Mac(string storageKey, long expiry)
    {
        using var hmac = new HMACSHA256(Key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{storageKey}.{expiry}"));
        // URL-safe base64 (no padding) so it drops cleanly into a query string.
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
