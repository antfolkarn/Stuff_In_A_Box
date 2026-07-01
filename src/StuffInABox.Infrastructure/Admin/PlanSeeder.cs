using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Infrastructure.Admin;

/// <summary>Seeds the built-in tiers into an empty <c>Plans</c> table on startup. Idempotent —
/// once the table has any rows (i.e. the admin has taken ownership) it does nothing.</summary>
public static class PlanSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        try
        {
            if (await db.Plans.AnyAsync(ct)) return;
            await db.Plans.AddRangeAsync(PlanDefaults.All(), ct);
            await db.SaveChangesAsync(ct);
        }
        catch (DbException)
        {
            // Stale dev SQLite DB predating the Plans table (EnsureCreated won't add it) — skip
            // rather than crash startup; recreate the dev DB to pick it up. Postgres always has
            // the table via migrations, so this only ever fires locally.
        }
    }
}
