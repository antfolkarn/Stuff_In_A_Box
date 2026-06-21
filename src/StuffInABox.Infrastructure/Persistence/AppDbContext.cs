using Microsoft.EntityFrameworkCore;
using StuffInABox.Domain.Entities;

namespace StuffInABox.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Box> Boxes => Set<Box>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
