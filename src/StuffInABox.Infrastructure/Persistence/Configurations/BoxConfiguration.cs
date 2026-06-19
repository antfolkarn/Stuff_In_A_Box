using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Infrastructure.Persistence.Configurations;

public class BoxConfiguration : IEntityTypeConfiguration<Box>
{
    public void Configure(EntityTypeBuilder<Box> builder)
    {
        builder.HasKey(b => new { b.Number, b.OwnerId });

        builder.Property(b => b.Number)
            .HasConversion(v => v.Value, v => new BoxNumber(v))
            .IsRequired();

        builder.Property(b => b.OwnerId)
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();

        builder.Property(b => b.Label).IsRequired().HasMaxLength(100);
        builder.Property(b => b.SpaceId).IsRequired();

        builder.HasIndex(b => new { b.SpaceId, b.OwnerId });
    }
}
