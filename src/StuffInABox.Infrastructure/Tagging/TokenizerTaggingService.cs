using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Infrastructure.Tagging;

public class TokenizerTaggingService : ITaggingService
{
    private static readonly string[] Separators = [" ", "-", "_", ",", ".", "/"];

    public Task<IReadOnlyList<string>> GenerateTagsAsync(string itemName, CancellationToken ct = default)
    {
        IReadOnlyList<string> tags = itemName
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 1)
            .Distinct()
            .ToList();
        return Task.FromResult(tags);
    }
}
