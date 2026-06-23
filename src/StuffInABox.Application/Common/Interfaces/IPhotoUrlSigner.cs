namespace StuffInABox.Application.Common.Interfaces;

/// <summary>
/// Signs and verifies time-limited access tokens for uploaded photos. Lets an
/// <c>&lt;img&gt;</c> tag load a photo without an Authorization header while still
/// keeping the file from being world-readable forever. Mirrors the presigned-URL
/// model of R2/S3, the planned production storage.
/// </summary>
public interface IPhotoUrlSigner
{
    /// <summary>Signature token (bucketed expiry + HMAC) granting temporary read access to a storage key.</summary>
    string Sign(string storageKey);

    /// <summary>True if the signature matches the storage key and has not expired.</summary>
    bool Verify(string storageKey, string? signature);
}
