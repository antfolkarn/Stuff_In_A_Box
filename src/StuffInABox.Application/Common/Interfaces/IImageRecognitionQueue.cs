namespace StuffInABox.Application.Common.Interfaces;

/// <summary>
/// Queues a freshly uploaded item photo for background recognition (name + tags).
/// The upload returns immediately; a worker processes the queue with bounded concurrency.
/// </summary>
public interface IImageRecognitionQueue
{
    void EnqueueRecognition(Guid itemId);
}
