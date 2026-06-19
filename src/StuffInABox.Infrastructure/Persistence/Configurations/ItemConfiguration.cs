using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StuffInABox.Domain.Entities;
using StuffInABox.Domain.ValueObjects;

namespace StuffInABox.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.OwnerId)
            .HasConversion(v => v.Value, v => new UserId(v))
            .IsRequired();

        builder.Property(i => i.BoxNumber)
            .HasConversion(v => v.Value, v => new BoxNumber(v))
            .IsRequired();

        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.Property(i => i.PhotoStorageKey).HasMaxLength(500);

        // Tags stored as JSON TEXT (works for both SQLite and PostgreSQL)
        var tagsComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<string>>(
            (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
            v => v.Aggregate(0, (h, t) => HashCode.Combine(h, t.GetHashCode())),
            v => v.ToList());

        builder.Property(i => i.Tags)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => (IReadOnlyList<string>)(System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()))
            .HasColumnType("TEXT")
            .Metadata.SetValueComparer(tagsComparer);

        builder.HasIndex(i => new { i.BoxNumber, i.OwnerId });
        builder.HasIndex(i => i.OwnerId);
    }
}
