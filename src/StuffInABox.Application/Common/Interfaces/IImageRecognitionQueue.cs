namespace StuffInABox.Application.Common.Interfaces;

/// <summary>
/// Queues a freshly uploaded item photo for background recognition (name + tags).
/// The upload returns immediately; a worker processes the queue with bounded concurrency.
/// </summary>
public interface IImageRecognitionQueue
{
    /// <summary>Queues an item for recognition. <paramref name="priority"/> plans are drained
    /// ahead of everyone else so a paying user's upload isn't stuck behind a free user's burst.</summary>
    void EnqueueRecognition(Guid itemId, bool priority = false);
}
