namespace Econyx.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Econyx.Domain.Entities;
using Econyx.Domain.ValueObjects;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.PlatformOrderId)
            .HasMaxLength(256);

        builder.Property(o => o.Side)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(o => o.Quantity)
            .HasPrecision(18, 6);

        builder.Property(o => o.FilledQuantity)
            .HasPrecision(18, 6);

        builder.Property(o => o.Mode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.Platform)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.RejectionReason)
            .HasMaxLength(1000);

        builder.OwnsOne(o => o.Price, ConfigureMoney("Order"));
        builder.OwnsOne(o => o.FilledPrice, ConfigureMoney("Filled"));

        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.MarketId);
    }

    private static Action<OwnedNavigationBuilder<Order, Money>> ConfigureMoney(string prefix)
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
