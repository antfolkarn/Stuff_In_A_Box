namespace StuffInABox.Application.Common.Interfaces;

/// <summary>
/// Analyzes a photo and returns a suggested Swedish name plus searchable tags
/// (objects, colours, materials, book titles, and every item when several are
/// visible). Honors a "never throws" contract — returns null when recognition
/// is unavailable or fails, so the add-item flow is never blocked.
/// </summary>
public interface IImageRecognitionService
{
    Task<RecognitionResult?> RecognizeAsync(byte[] imageBytes, CancellationToken ct = default);
}

/// <param name="Name">Short Swedish title for the main object, or a mixed label when several are present.</param>
/// <param name="Tags">Lowercased Swedish keywords: object(s), colour(s), material, category, book titles.</param>
public sealed record RecognitionResult(string? Name, IReadOnlyList<string> Tags);
