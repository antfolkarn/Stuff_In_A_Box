namespace StuffInABox.Application.Common.Interfaces;

public interface IEnrichmentQueue
{
    void EnqueueEnrichment(Guid itemId, string itemName);
}
