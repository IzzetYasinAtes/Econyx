namespace Econyx.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Econyx.Domain.Entities;

internal sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.MarketQuestion)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(p => p.Platform)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Side)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.Quantity)
            .HasPrecision(18, 6);

        builder.Property(p => p.StrategyName)
            .HasMaxLength(200)
            .IsRequired();

        builder.OwnsOne(p => p.EntryPrice, ConfigureMoney("Entry"));
        builder.OwnsOne(p => p.CurrentPrice, ConfigureMoney("Current"));
        builder.OwnsOne(p => p.ExitPrice, ConfigureMoney("Exit"));

        builder.HasIndex(p => p.IsOpen);
        builder.HasIndex(p => p.MarketId);
    }

    private static Action<OwnedNavigationBuilder<Position, Domain.ValueObjects.Money>> ConfigureMoney(string prefix)
        => mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName($"{prefix}PriceAmount")
                .HasPrecision(18, 6);
            mb.Property(m => m.Currency)
                .HasColumnName($"{prefix}PriceCurrency")
                .HasMaxLength(10);
        };
}
