using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Infrastructure.Persistence.Configurations;

public class SpaceMembershipConfiguration : IEntityTypeConfiguration<SpaceMembership>
{
    public void Configure(EntityTypeBuilder<SpaceMembership> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.UserId)
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();

        builder.Property(m => m.SpaceId).IsRequired();

        // A user can be a member of a space at most once.
        builder.HasIndex(m => new { m.SpaceId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}
