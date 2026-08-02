using AutoBuyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoBuyer.Infrastructure.Data.Configurations;

public sealed class PromotionCandidateConfiguration
    : IEntityTypeConfiguration<PromotionCandidate>
{
    public void Configure(
        EntityTypeBuilder<PromotionCandidate> builder)
    {
        builder.ToTable("promotion_candidates");

        builder.HasKey(candidate => candidate.Id);

        builder.Property(candidate => candidate.TelegramChatId)
            .IsRequired();

        builder.Property(candidate => candidate.TelegramMessageId)
            .IsRequired();

        builder.Property(candidate => candidate.ProductName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(candidate => candidate.AdvertisedPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(candidate => candidate.OriginalUrl)
            .HasMaxLength(2_000)
            .IsRequired();

        builder.Property(candidate => candidate.ResolvedUrl)
            .HasMaxLength(2_000);

        builder.Property(candidate => candidate.Coupon)
            .HasMaxLength(500);

        builder.Property(candidate => candidate.Conditions)
            .HasMaxLength(2_000);

        builder.Property(candidate => candidate.OriginalMessage)
            .HasMaxLength(10_000)
            .IsRequired();

        builder.Property(candidate => candidate.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(candidate => candidate.ReceivedAt)
            .IsRequired();

        builder.HasIndex(candidate => new
        {
            candidate.TelegramChatId,
            candidate.TelegramMessageId
        })
        .IsUnique();

        builder.HasIndex(candidate => candidate.Status);

        builder.HasIndex(candidate => candidate.ReceivedAt);

        builder.HasOne(candidate => candidate.Store)
            .WithMany()
            .HasForeignKey(candidate => candidate.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(candidate => candidate.ProductTarget)
            .WithMany()
            .HasForeignKey(candidate => candidate.ProductTargetId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}