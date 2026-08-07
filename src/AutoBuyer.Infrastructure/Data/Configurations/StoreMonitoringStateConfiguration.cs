using AutoBuyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoBuyer.Infrastructure.Data.Configurations;

public sealed class StoreMonitoringStateConfiguration
    : IEntityTypeConfiguration<StoreMonitoringState>
{
    public void Configure(
        EntityTypeBuilder<StoreMonitoringState> builder)
    {
        builder.ToTable("store_monitoring_states");

        builder.HasKey(state => state.Id);

        builder.Property(state => state.Host)
            .HasMaxLength(255)
            .IsRequired();

        builder.HasIndex(state => state.Host)
            .IsUnique();

        builder.Property(state => state.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(state => state.LastError)
            .HasMaxLength(2_000);

        builder.Property(state => state.ConsecutiveFailures)
            .IsRequired();

        builder.Property(state => state.UpdatedAt)
            .IsRequired();

        builder.HasIndex(state => state.NextAllowedAttemptAt);

        builder.HasIndex(state => state.Status);
    }
}