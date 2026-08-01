using AutoBuyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoBuyer.Infrastructure.Data.Configurations;

public sealed class PriceHistoryConfiguration
    : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(
        EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("price_history");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(history => history.IsAvailable)
            .IsRequired();

        builder.Property(history => history.CapturedAt)
            .IsRequired();

        builder.HasOne(history => history.ProductTarget)
            .WithMany()
            .HasForeignKey(history => history.ProductTargetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(history => history.ProductTargetId);

        builder.HasIndex(history => new
        {
            history.ProductTargetId,
            history.CapturedAt
        });
    }
}