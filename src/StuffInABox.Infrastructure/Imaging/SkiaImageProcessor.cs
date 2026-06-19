using SkiaSharp;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Imaging;

/// <summary>
/// Validates images by magic bytes and re-encodes them with SkiaSharp, which
/// drops all EXIF/metadata in the process (decode to pixels, encode fresh).
/// </summary>
public sealed class SkiaImageProcessor : IImageProcessor
{
    private const int JpegQuality = 85;

    public ProcessedImage ProcessAndStripMetadata(byte[] content)
    {
        var detected = DetectFormat(content)
            ?? throw new InvalidImageException("Filen är inte en giltig JPEG-, PNG- eller WEBP-bild.");

        using var bitmap = SKBitmap.Decode(content)
            ?? throw new InvalidImageException("Bilden kunde inte avkodas.");
        using var image = SKImage.FromBitmap(bitmap);

        // Re-encode to the detected format — this produces clean bytes with no metadata.
        var (skFormat, extension, contentType) = detected;
        using var data = image.Encode(skFormat, JpegQuality)
            ?? throw new InvalidImageException("Bilden kunde inte kodas om.");

        return new ProcessedImage(data.ToArray(), extension, contentType);
    }

    private static (SKEncodedImageFormat Format, string Ext, string ContentType)? DetectFormat(byte[] b)
    {
        // JPEG: FF D8 FF
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return (SKEncodedImageFormat.Jpeg, ".jpg", "image/jpeg");

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
            && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            return (SKEncodedImageFormat.Png, ".png", "image/png");

        // WEBP: "RIFF" .... "WEBP"
        if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
            && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50)
            return (SKEncodedImageFormat.Webp, ".webp", "image/webp");

        return null;
    }
}
