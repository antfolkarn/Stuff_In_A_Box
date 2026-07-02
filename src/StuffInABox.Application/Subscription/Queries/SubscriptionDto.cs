namespace StuffInABox.Application.Subscription.Queries;

/// <summary>Everything the Settings "Subscription" block needs. <c>-1</c> limits mean
/// "unlimited". Usage covers only the cheap counts we already have (spaces, items); AI and
/// storage meters arrive with usage tracking (Fas B).</summary>
public sealed record SubscriptionDto(
    string Tier,
    int PriceSek,
    SubscriptionUsageDto Usage,
    IReadOnlyList<PlanOptionDto> Plans);

public sealed record SubscriptionUsageDto(
    int Spaces,
    int MaxSpaces,
    int Items,
    int MaxItems,
    int AiPhotos,
    int AiPhotosLimit,
    long StorageMb,
    long StorageLimitMb);

public sealed record PlanOptionDto(
    string Tier,
    int PriceSek,
    int MaxSpaces,
    int MaxItems,
    int MaxMembers,
    int AiPhotosPerMonth,
    long StorageMb,
    bool ClaudeEnrichment,
    bool PriorityQueue,
    bool AllThemes,
    bool Current);
