using StuffInABox.Domain.Entities;

namespace StuffInABox.Infrastructure.Admin;

/// <summary>The built-in starting catalog (Låda / Hushåll / PRO). Single source of truth for
/// both the database seed and the in-memory fallback, so they never drift. Once seeded, the
/// admin editor owns the values in the DB and these defaults are no longer consulted.</summary>
internal static class PlanDefaults
{
    // tier, priceSek, maxSpaces, maxItems, maxMembers, aiPhotosPerMonth, storageMb,
    // claudeEnrichment, priorityQueue, allThemes, sortOrder. -1 = unlimited.
    public static IReadOnlyList<Plan> All() =>
    [
        Plan.Create("free",   0,   1,  100,  1, 5,    250,   false, false, false, 0),
        Plan.Create("medium", 49,  5,  1000, 2, 100,  1024,  false, false, false, 1),
        Plan.Create("large",  99, -1,  -1,   5, 1000, 10240, false, true,  false, 2),
    ];
}
