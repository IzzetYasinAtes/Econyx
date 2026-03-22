using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Econyx.Domain.Entities;

namespace Econyx.Infrastructure.Persistence.Configurations;

internal sealed class TradingConfigurationConfiguration : IEntityTypeConfiguration<TradingConfiguration>
{
    public void Configure(EntityTypeBuilder<TradingConfiguration> builder)
    {
        builder.ToTable("TradingConfigurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Mode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.InitialBalance).HasPrecision(18, 2);
        builder.Property(c => c.MaxPositionSizePercent).HasPrecision(18, 2);
        builder.Property(c => c.MinEdgeThreshold).HasPrecision(18, 4);
        builder.Property(c => c.MinVolumeUsd).HasPrecision(18, 2);
        builder.Property(c => c.MaxSpreadCents).HasPrecision(18, 2);
        builder.Property(c => c.StopLossPercent).HasPrecision(18, 2);
        builder.Property(c => c.TakeProfitPercent).HasPrecision(18, 2);
        builder.Property(c => c.SurvivalModeThresholdUsd).HasPrecision(18, 2);

        builder.HasIndex(c => c.IsActive);
    }
}
