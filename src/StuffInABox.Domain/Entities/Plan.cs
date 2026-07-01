namespace StuffInABox.Domain.Entities;

/// <summary>A subscription tier and its limits/flags, stored in the database so the admin app
/// can edit them live. <c>-1</c> on a numeric limit means "unlimited". The <see cref="Tier"/>
/// key (e.g. "free"/"medium"/"large") is stable and is what <c>UserSettings.PlanTier</c> points at.</summary>
public class Plan
{
    public const int MaxTierLength = 40;

    public string Tier { get; private set; }
    public int PriceSek { get; private set; }
    public int MaxSpaces { get; private set; }
    public int MaxItems { get; private set; }
    public int MaxMembers { get; private set; }
    public int AiPhotosPerMonth { get; private set; }
    public long StorageMb { get; private set; }
    public bool ClaudeEnrichment { get; private set; }
    public bool PriorityQueue { get; private set; }
    public bool AllThemes { get; private set; }
    /// <summary>Display order in the pricing list (cheapest/lowest first).</summary>
    public int SortOrder { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Plan() { Tier = null!; }

    public static Plan Create(
        string tier, int priceSek, int maxSpaces, int maxItems, int maxMembers,
        int aiPhotosPerMonth, long storageMb, bool claudeEnrichment, bool priorityQueue,
        bool allThemes, int sortOrder)
    {
        var plan = new Plan { Tier = Normalize(tier) };
        plan.Apply(priceSek, maxSpaces, maxItems, maxMembers, aiPhotosPerMonth, storageMb,
            claudeEnrichment, priorityQueue, allThemes, sortOrder);
        return plan;
    }

    public void Update(
        int priceSek, int maxSpaces, int maxItems, int maxMembers, int aiPhotosPerMonth,
        long storageMb, bool claudeEnrichment, bool priorityQueue, bool allThemes, int sortOrder) =>
        Apply(priceSek, maxSpaces, maxItems, maxMembers, aiPhotosPerMonth, storageMb,
            claudeEnrichment, priorityQueue, allThemes, sortOrder);

    private void Apply(
        int priceSek, int maxSpaces, int maxItems, int maxMembers, int aiPhotosPerMonth,
        long storageMb, bool claudeEnrichment, bool priorityQueue, bool allThemes, int sortOrder)
    {
        PriceSek = priceSek;
        MaxSpaces = maxSpaces;
        MaxItems = maxItems;
        MaxMembers = maxMembers;
        AiPhotosPerMonth = aiPhotosPerMonth;
        StorageMb = storageMb;
        ClaudeEnrichment = claudeEnrichment;
        PriorityQueue = priorityQueue;
        AllThemes = allThemes;
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Normalize(string tier)
    {
        if (string.IsNullOrWhiteSpace(tier)) throw new ArgumentException("Tier krävs.", nameof(tier));
        var t = tier.Trim().ToLowerInvariant();
        if (t.Length > MaxTierLength) throw new ArgumentException("Tier är för lång.", nameof(tier));
        return t;
    }
}
