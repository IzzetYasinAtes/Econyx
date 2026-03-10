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

        builder.OwnsOne(b => b.Balance, b => b.ConfigureMoney("Balance"));
        builder.OwnsOne(b => b.TotalPnL, b => b.ConfigureMoney("TotalPnL"));
        builder.OwnsOne(b => b.ApiCosts, b => b.ConfigureMoney("ApiCosts"));

        builder.HasIndex(b => b.CreatedAt);
    }
}
