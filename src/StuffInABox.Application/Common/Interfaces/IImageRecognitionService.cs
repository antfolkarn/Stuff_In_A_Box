namespace StuffInABox.Application.Common.Interfaces;

/// <summary>
/// Recognizes the main object in a photo and returns a short Swedish noun to
/// pre-fill the item name. Honors a "never throws" contract — returns null when
/// recognition is unavailable or fails, so the add-item flow is never blocked.
/// </summary>
public interface IImageRecognitionService
{
    Task<string?> RecognizeAsync(byte[] imageBytes, CancellationToken ct = default);
}
