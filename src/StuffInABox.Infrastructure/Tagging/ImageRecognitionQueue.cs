using System.Threading.Channels;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Tagging;

/// <summary>
/// In-memory queue of item ids awaiting background photo recognition. Mirrors
/// <see cref="EnrichmentQueue"/>; consumed by <see cref="ImageRecognitionWorker"/>.
/// </summary>
public class ImageRecognitionQueue : IImageRecognitionQueue
{
    private readonly Channel<Guid> _channel =
        Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public void EnqueueRecognition(Guid itemId) => _channel.Writer.TryWrite(itemId);
}
