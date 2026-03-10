namespace Econyx.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Econyx.Domain.Entities;
using Econyx.Domain.ValueObjects;

internal sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trades");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.MarketQuestion)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(t => t.Side)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.Quantity)
            .HasPrecision(18, 6);

        builder.Property(t => t.StrategyName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Platform)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.OwnsOne(t => t.EntryPrice, b => b.ConfigureMoney("Entry"));
        builder.OwnsOne(t => t.ExitPrice, b => b.ConfigureMoney("Exit"));
        builder.OwnsOne(t => t.PnL, b => b.ConfigureMoney("PnL"));
        builder.OwnsOne(t => t.Fees, b => b.ConfigureMoney("Fee"));

        builder.HasIndex(t => t.MarketId);
        builder.HasIndex(t => t.ClosedAt);
    }
}
