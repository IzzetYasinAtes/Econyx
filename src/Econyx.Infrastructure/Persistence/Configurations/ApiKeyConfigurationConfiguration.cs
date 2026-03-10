using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Econyx.Domain.Entities;

namespace Econyx.Infrastructure.Persistence.Configurations;

internal sealed class ApiKeyConfigurationConfiguration : IEntityTypeConfiguration<ApiKeyConfiguration>
{
    public void Configure(EntityTypeBuilder<ApiKeyConfiguration> builder)
    {
        builder.ToTable("ApiKeyConfigurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Provider)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.EncryptedKey)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(c => c.MaskedDisplay)
            .HasMaxLength(50);

        builder.HasIndex(c => c.Provider).IsUnique();
    }
}
