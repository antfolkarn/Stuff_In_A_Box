using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StuffInABox.Domain.Entities;

namespace StuffInABox.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.HasKey(p => p.Tier);
        builder.Property(p => p.Tier).HasMaxLength(Plan.MaxTierLength);
    }
}
