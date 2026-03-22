using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Econyx.Domain.Entities;

namespace Econyx.Infrastructure.Persistence.Configurations;

internal sealed class AiRequestLogConfiguration : IEntityTypeConfiguration<AiRequestLog>
{
    public void Configure(EntityTypeBuilder<AiRequestLog> builder)
    {
        builder.ToTable("AiRequestLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Provider).HasMaxLength(50).IsRequired();
        builder.Property(l => l.ModelId).HasMaxLength(200).IsRequired();
        builder.Property(l => l.MarketQuestion).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Prompt).IsRequired();
        builder.Property(l => l.ParsedReasoning).HasMaxLength(1000);
        builder.Property(l => l.FairValue).HasPrecision(18, 6);
        builder.Property(l => l.Confidence).HasPrecision(18, 6);
        builder.Property(l => l.CostUsd).HasPrecision(18, 8);

        builder.HasIndex(l => l.CreatedAt);
        builder.HasIndex(l => l.Provider);
        builder.HasIndex(l => l.ModelId);
        builder.HasIndex(l => l.IsSuccess);
    }
}
