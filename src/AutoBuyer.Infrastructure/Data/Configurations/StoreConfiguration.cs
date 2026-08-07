using AutoBuyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoBuyer.Infrastructure.Data.Configurations;

public sealed class StoreConfiguration
    : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("stores");

        builder.HasKey(store => store.Id);

        builder.Property(store => store.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(store => store.Name)
            .IsUnique();

        builder.Property(store => store.BaseUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(store => store.BaseUrl);

        builder.Property(store => store.IsEnabled)
            .IsRequired();

        builder.Property(store => store.CreatedAt)
            .IsRequired();
    }
}
