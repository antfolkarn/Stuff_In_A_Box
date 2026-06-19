using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.Repositories;

namespace StuffInABox.Infrastructure.Persistence.Repositories;

public class UserIdentityRepository(AppDbContext db) : IUserIdentityRepository
{
    public async Task<UserIdentity?> FindAsync(string provider, string externalId, CancellationToken ct = default) =>
        await db.UserIdentities.FirstOrDefaultAsync(
            u => u.Provider == provider.ToLowerInvariant() && u.ExternalId == externalId, ct);

    public async Task AddAsync(UserIdentity identity, CancellationToken ct = default)
    {
        await db.UserIdentities.AddAsync(identity, ct);
        await db.SaveChangesAsync(ct);
    }
}
