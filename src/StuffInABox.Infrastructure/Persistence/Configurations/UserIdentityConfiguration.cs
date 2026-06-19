using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StuffInABox.Domain.Entities;

namespace StuffInABox.Infrastructure.Persistence.Configurations;

public class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.HasKey(u => u.InternalUserId);

        builder.Property(u => u.Provider).IsRequired().HasMaxLength(20);
        builder.Property(u => u.ExternalId).IsRequired().HasMaxLength(500);
        builder.Property(u => u.PasswordHash).HasMaxLength(200);

        builder.HasIndex(u => new { u.Provider, u.ExternalId }).IsUnique();
    }
}
