namespace Econyx.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Econyx.Domain.Entities;
using Econyx.Domain.ValueObjects;

internal sealed class BalanceSnapshotConfiguration : IEntityTypeConfiguration<BalanceSnapshot>
{
    public void Configure(EntityTypeBuilder<BalanceSnapshot> builder)
    {
        builder.ToTable("BalanceSnapshots");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.TotalPnLPercent)
            .HasPrecision(18, 4);

        builder.Property(b => b.WinRate)
            .HasPrecision(5, 4);

        builder.Property(b => b.Mode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(b => b.Balance, ConfigureMoney("Balance"));
        builder.OwnsOne(b => b.TotalPnL, ConfigureMoney("TotalPnL"));
        builder.OwnsOne(b => b.ApiCosts, ConfigureMoney("ApiCosts"));

        builder.HasIndex(b => b.CreatedAt);
    }

    private static Action<OwnedNavigationBuilder<BalanceSnapshot, Money>> ConfigureMoney(string prefix)
        => mb =>
        {
            mb.Property(m => m.Amount)
                .HasColumnName($"{prefix}Amount")
                .HasPrecision(18, 6);
            mb.Property(m => m.Currency)
                .HasColumnName($"{prefix}Currency")
                .HasMaxLength(10);
        };
}
