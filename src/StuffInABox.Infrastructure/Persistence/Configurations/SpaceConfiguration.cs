using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Infrastructure.Persistence.Configurations;

public class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.OwnerId)
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Icon).IsRequired().HasMaxLength(50);

        builder.OwnsOne(s => s.Code, code =>
        {
            code.Property(c => c.Value)
                .HasColumnName("Code")
                .IsRequired()
                .HasMaxLength(3);
        });

        builder.HasIndex(s => new { s.OwnerId });
    }
}
