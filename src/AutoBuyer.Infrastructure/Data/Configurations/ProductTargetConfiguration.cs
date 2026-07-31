using AutoBuyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoBuyer.Infrastructure.Data.Configurations;

public sealed class ProductTargetConfiguration
    : IEntityTypeConfiguration<ProductTarget>
{
    public void Configure(
        EntityTypeBuilder<ProductTarget> builder)
    {
        builder.ToTable("product_targets");

        builder.HasKey(target => target.Id);

        builder.Property(target => target.Name)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(target => target.ProductUrl)
            .HasMaxLength(2_000)
            .IsRequired();

        builder.Property(target => target.TargetPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(target => target.AutoBuyEnabled)
            .IsRequired();

        builder.Property(target => target.MonitoringEnabled)
            .IsRequired();

        builder.Property(target => target.CreatedAt)
            .IsRequired();

        builder.HasOne(target => target.Store)
            .WithMany()
            .HasForeignKey(target => target.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(target => target.StoreId);

        builder.HasIndex(target => target.MonitoringEnabled);
    }
}