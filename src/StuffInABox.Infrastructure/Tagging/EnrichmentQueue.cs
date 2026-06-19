using System.Threading.Channels;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Tagging;

public record EnrichmentRequest(Guid ItemId, string ItemName);

public class EnrichmentQueue : IEnrichmentQueue
{
    private readonly Channel<EnrichmentRequest> _channel =
        Channel.CreateUnbounded<EnrichmentRequest>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<EnrichmentRequest> Reader => _channel.Reader;

    public void EnqueueEnrichment(Guid itemId, string itemName) =>
        _channel.Writer.TryWrite(new EnrichmentRequest(itemId, itemName));
}
