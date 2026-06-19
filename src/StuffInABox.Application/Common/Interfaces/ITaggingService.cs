namespace StuffInABox.Application.Common.Interfaces;

public interface ITaggingService
{
    Task<IReadOnlyList<string>> GenerateTagsAsync(string itemName, CancellationToken ct = default);
}
