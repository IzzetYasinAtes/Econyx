using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Econyx.Domain.Entities;

namespace Econyx.Infrastructure.Persistence.Configurations;

internal sealed class AiModelConfigurationConfiguration : IEntityTypeConfiguration<AiModelConfiguration>
{
    public void Configure(EntityTypeBuilder<AiModelConfiguration> builder)
    {
        builder.ToTable("AiModelConfigurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Provider)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.ModelId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.PromptPricePer1M)
            .HasPrecision(18, 8);

        builder.Property(c => c.CompletionPricePer1M)
            .HasPrecision(18, 8);

        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => new { c.Provider, c.ModelId });
    }
}
