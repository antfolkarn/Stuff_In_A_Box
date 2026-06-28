using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Storage;

/// <summary>
/// Stores uploaded photos in Cloudflare R2 (S3-compatible) for stateless hosting.
/// Reads go through time-limited presigned GET URLs, so the bucket stays private and
/// the browser fetches the image directly from R2 (our /uploads endpoint is unused in
/// this mode). Selected via <c>Storage:Provider=r2</c>.
/// </summary>
public sealed class R2StorageService : IStorageService, IDisposable
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly TimeSpan _validity;

    public R2StorageService(IConfiguration config)
    {
        var accessKey = config["Storage:R2:AccessKey"]
            ?? throw new InvalidOperationException("Storage:R2:AccessKey must be configured.");
        var secretKey = config["Storage:R2:SecretKey"]
            ?? throw new InvalidOperationException("Storage:R2:SecretKey must be configured.");
        _bucket = config["Storage:R2:Bucket"]
            ?? throw new InvalidOperationException("Storage:R2:Bucket must be configured.");

        // R2 endpoint is https://<accountid>.r2.cloudflarestorage.com (or an explicit ServiceUrl).
        var serviceUrl = config["Storage:R2:ServiceUrl"];
        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            var accountId = config["Storage:R2:AccountId"]
                ?? throw new InvalidOperationException("Storage:R2:AccountId or Storage:R2:ServiceUrl must be configured.");
            serviceUrl = $"https://{accountId}.r2.cloudflarestorage.com";
        }

        _validity = TimeSpan.FromMinutes(config.GetValue<int?>("Storage:UrlValidityMinutes") ?? 360);

        _s3 = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true,       // R2 expects path-style addressing
            AuthenticationRegion = "auto", // R2's region token
        });
    }

    public async Task<string> StoreAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        var storageKey = $"{Guid.NewGuid():N}{ext}";
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true, // R2 doesn't support streaming SigV4 payload signing
        }, ct);
        return storageKey;
    }

    // Presigned, time-limited GET URL — signed locally, no network call.
    public string GetUrl(string storageKey) =>
        _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(_validity),
        });

    public Task DeleteAsync(string storageKey, CancellationToken ct = default) =>
        _s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = storageKey }, ct);

    public async Task<byte[]?> GetAsync(string storageKey, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _s3.GetObjectAsync(
                new GetObjectRequest { BucketName = _bucket, Key = storageKey }, ct);
            using var ms = new MemoryStream();
            await resp.ResponseStream.CopyToAsync(ms, ct);
            return ms.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public void Dispose() => _s3.Dispose();
}
