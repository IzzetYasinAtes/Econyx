namespace Econyx.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Econyx.Domain.ValueObjects;

internal static class MoneyOwnershipExtensions
{
    public static void ConfigureMoney<TOwner>(
        this OwnedNavigationBuilder<TOwner, Money> builder,
        string prefix) where TOwner : class
    {
        builder.Property(m => m.Amount)
            .HasColumnName($"{prefix}Amount")
            .HasPrecision(18, 6);
        builder.Property(m => m.Currency)
            .HasColumnName($"{prefix}Currency")
            .HasMaxLength(10);
    }
}
