using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Web.Endpoints;

/// <summary>
/// Serves uploaded photos from the external uploads directory at <c>/uploads/{key}</c>,
/// gated by a signed query token instead of being world-readable. Anonymous on purpose:
/// an <c>&lt;img&gt;</c> tag can't send an Authorization header, so the signature is the
/// capability. Replaces a plain static-file mapping.
/// </summary>
public static class PhotoEndpoints
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public static void MapPhotoEndpoints(this WebApplication app)
    {
        app.MapGet("/uploads/{key}", (
            string key,
            [FromQuery] string? sig,
            IPhotoUrlSigner signer,
            IConfiguration config,
            HttpContext ctx) =>
        {
            // Defense-in-depth against path traversal: only a bare filename is valid.
            if (key != Path.GetFileName(key))
                return Results.NotFound();

            if (!signer.Verify(key, sig))
                return Results.StatusCode(StatusCodes.Status403Forbidden);

            var root = config["Storage:LocalPath"]!;
            var path = Path.Combine(root, key);
            if (!File.Exists(path))
                return Results.NotFound();

            if (!ContentTypes.TryGetContentType(key, out var contentType))
                contentType = "application/octet-stream";

            // The signed URL is stable within its window, so the browser can cache it.
            ctx.Response.Headers.CacheControl = "private, max-age=3600";
            return Results.File(path, contentType, enableRangeProcessing: true);
        });
    }
}
