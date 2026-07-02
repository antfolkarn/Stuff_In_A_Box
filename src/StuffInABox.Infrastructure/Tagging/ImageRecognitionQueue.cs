using System.Runtime.CompilerServices;
using System.Threading.Channels;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Tagging;

/// <summary>
/// In-memory queue of item ids awaiting background photo recognition. Two lanes: priority
/// (paying plans with the PriorityQueue entitlement) is always drained before the normal lane,
/// so a paying user's upload isn't stuck behind a free user's burst. Consumed by
/// <see cref="ImageRecognitionWorker"/>.
/// </summary>
public class ImageRecognitionQueue : IImageRecognitionQueue
{
    private readonly Channel<Guid> _priority = Channel.CreateUnbounded<Guid>();
    private readonly Channel<Guid> _normal = Channel.CreateUnbounded<Guid>();

    public void EnqueueRecognition(Guid itemId, bool priority = false) =>
        (priority ? _priority : _normal).Writer.TryWrite(itemId);

    /// <summary>Yields queued ids for the single worker loop, always taking from the priority
    /// lane before the normal one.</summary>
    public async IAsyncEnumerable<Guid> DequeueAllAsync([EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_priority.Reader.TryRead(out var itemId) || _normal.Reader.TryRead(out itemId))
            {
                yield return itemId;
                continue;
            }

            // Both lanes empty — wait until either has an item, then loop and re-check priority-first.
            var priorityWait = _priority.Reader.WaitToReadAsync(ct).AsTask();
            var normalWait = _normal.Reader.WaitToReadAsync(ct).AsTask();
            await Task.WhenAny(priorityWait, normalWait);
        }
    }
}
