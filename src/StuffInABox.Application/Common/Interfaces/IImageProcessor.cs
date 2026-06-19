namespace StuffInABox.Application.Common.Interfaces;

/// <summary>
/// Validates and normalizes user-supplied images: verifies the format via magic
/// bytes, rejects anything that is not JPEG/PNG/WEBP, and re-encodes to strip
/// EXIF/metadata (which can leak GPS location and other PII).
/// </summary>
public interface IImageProcessor
{
    /// <summary>Throws <see cref="InvalidImageException"/> if the content is not a supported image.</summary>
    ProcessedImage ProcessAndStripMetadata(byte[] content);
}

public sealed record ProcessedImage(byte[] Bytes, string Extension, string ContentType);

public sealed class InvalidImageException(string message) : Exception(message);
