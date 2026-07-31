using AutoBuyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoBuyer.Infrastructure.Data;

public class AutoBuyerDbContext : DbContext
{
    public AutoBuyerDbContext(DbContextOptions<AutoBuyerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Store> Stores => Set<Store>();

    public DbSet<ProductTarget> ProductTargets => Set<ProductTarget>();

    public DbSet<User> Users => Set<User>();
}