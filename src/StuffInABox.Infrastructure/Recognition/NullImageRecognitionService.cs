using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Recognition;

/// <summary>Default no-op recognizer used when no provider is configured.</summary>
public sealed class NullImageRecognitionService : IImageRecognitionService
{
    public Task<RecognitionResult?> RecognizeAsync(byte[] imageBytes, CancellationToken ct = default) =>
        Task.FromResult<RecognitionResult?>(null);
}
