using AutoBuyer.Application.Abstractions.Persistence;
using AutoBuyer.Infrastructure.Data;
using AutoBuyer.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoBuyer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "A connection string 'Postgres' não foi configurada.");

        services.AddDbContext<AutoBuyerDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<
            IProductTargetRepository,
            ProductTargetRepository>();

        services.AddScoped<IStoreRepository, StoreRepository>();

        services.AddScoped<IUnitOfWork>(
            provider =>
                provider.GetRequiredService<AutoBuyerDbContext>());

        return services;
    }
}