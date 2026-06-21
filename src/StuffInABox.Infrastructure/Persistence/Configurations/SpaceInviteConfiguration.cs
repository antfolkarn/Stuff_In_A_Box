using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Infrastructure.Persistence.Configurations;

public class SpaceInviteConfiguration : IEntityTypeConfiguration<SpaceInvite>
{
    public void Configure(EntityTypeBuilder<SpaceInvite> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.SpaceId).IsRequired();

        builder.Property(i => i.Token).IsRequired().HasMaxLength(64);

        builder.Property(i => i.CreatedBy)
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();

        builder.HasIndex(i => i.Token).IsUnique();
        builder.HasIndex(i => i.SpaceId);
    }
}
