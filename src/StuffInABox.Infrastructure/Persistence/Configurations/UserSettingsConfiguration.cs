using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StuffInABox.Domain.Entities;

namespace StuffInABox.Infrastructure.Persistence.Configurations;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.HasKey(s => s.UserId);
        builder.Property(s => s.Theme).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Design).IsRequired().HasMaxLength(40);
        builder.Property(s => s.DisplayName).HasMaxLength(UserSettings.MaxDisplayNameLength);
        builder.Property(s => s.PlanTier)
            .IsRequired()
            .HasMaxLength(UserSettings.MaxPlanTierLength)
            .HasDefaultValue(UserSettings.DefaultPlanTier);
    }
}
