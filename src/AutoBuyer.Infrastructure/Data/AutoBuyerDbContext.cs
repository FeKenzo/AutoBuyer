using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoBuyer.Infrastructure.Data;

public sealed class AutoBuyerDbContext
    : DbContext, IUnitOfWork
{
    public AutoBuyerDbContext(
        DbContextOptions<AutoBuyerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<ProductTarget> ProductTargets =>
        Set<ProductTarget>();

    public DbSet<PriceHistory> PriceHistory =>
        Set<PriceHistory>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AutoBuyerDbContext).Assembly);
    }
}