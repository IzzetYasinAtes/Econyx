namespace Econyx.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Econyx.Domain.Entities;

internal sealed class MarketConfiguration : IEntityTypeConfiguration<Market>
{
    public void Configure(EntityTypeBuilder<Market> builder)
    {
        builder.ToTable("Markets");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ExternalId)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.Platform)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Question)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(4000);

        builder.Property(m => m.Category)
            .HasMaxLength(200);

        builder.Property(m => m.VolumeUsd)
            .HasPrecision(18, 2);

        builder.Property(m => m.Spread)
            .HasPrecision(18, 6);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.ResolvedOutcome)
            .HasMaxLength(500);

        builder.OwnsMany(m => m.Outcomes, ob =>
        {
            ob.ToJson();

            ob.Property(o => o.Name)
                .HasMaxLength(500);

            ob.OwnsOne(o => o.Price, pb =>
            {
                pb.Property(p => p.Value).HasColumnName("PriceValue");
            });

            ob.OwnsOne(o => o.Token, tb =>
            {
                tb.Property(t => t.Value).HasMaxLength(256).HasColumnName("TokenId");
            });
        });

        builder.HasIndex(m => new { m.ExternalId, m.Platform })
            .IsUnique();

        builder.HasIndex(m => m.Status);
    }
}
